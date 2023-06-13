/**
 * @copyright Copyright (c) 2023 B-com http://www.b-com.com/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading.Tasks;

using UnityEngine;

using Com.Bcom.Solar.Gprc;
using System.Diagnostics;

namespace Com.Bcom.Solar
{
    public class SolARCloud : MonoBehaviour
    {
        #region Events
        //public Action StartFetchingFrames;
        //public Action StopFetchingFrames;

        // bool: error?, string: message
        public event Action<string> OnConnect;
        public event Action<bool, string> OnStart;
        public event Action OnDisconnect;
        public event Action OnStop;
        public event Action<bool> OnReset;
        // PipelineMode: old mode, PipelineMode: new mode
        public event Action<PipelineMode, PipelineMode> OnPipelineModeChanged;
        // MappingStatus: old status, MappingStatus: new status
        public event Action<MappingStatus, MappingStatus> OnMappingStatusChanged;
        // PoseStatus: old status, PoseStatus: new status
        public event Action<RelocalizationPoseStatus, RelocalizationPoseStatus> OnPoseStatusChanged;
        public event Action<RelocAndMappingResult> OnReceivedPose;
        public event Action<LogLevel,string> OnLog;
        #endregion // Events


        #region UnityEditor
        [Tooltip("URL of the SolAR cloud services frontend (ex: \"http://<IP>:<port>\")")]
        public string frontendIp = "<not-set>";

        [Tooltip("Select to preform both mapping and relocalization or only relocalization")]
        public PipelineMode pipelineMode = PipelineMode.RelocalizationOnly;
        [Tooltip("Frequency at which frames are sent to the services in Relocalization And Mapping mode (<= 0: maximum rate)")]
        public ushort relocAndMappinSendingRateInFps = 0;
        [Tooltip("Frequency at which frames are sent to the services in Relocalization Only mode (<= 0: maximum rate)")]
        public ushort relocOnlySendingRateInFps = 4;

        [Serializable]
        public struct GrpcSettings
        {
            [Tooltip("Max amount off parallel reception of frames from device")]
            [HideInInspector]
            public volatile int threadSlots;
            [Tooltip("Max amount off parallel requests of reloc and/or mapping to SolAR services")]
            public volatile int networkSlots;
            [Tooltip("Select compression method of frame image buffer to save bandwidth")]
            public ImageCompression imageCompression;
        }

        [SerializeField]
        public GrpcSettings advancedGrpcSettings = new GrpcSettings()
        {
            threadSlots = 5,
            networkSlots = 5,
            imageCompression = ImageCompression.Png
        };
        #endregion //UnityEditor


        #region PrivateMembers
        private bool started = false;
        private GrpcManager grpc = null;
        private MappingStatus mappingStatus = MappingStatus.Bootstrap;
        private RelocalizationPoseStatus poseStatus = RelocalizationPoseStatus.NoPose;
        private Stopwatch stopWatch = new Stopwatch();
        int timeBetweenFramesMappingAndReloc;
        int timeBetweenFramesRelocOnly;
        int timeBetweenFrames;
        #endregion

        #region UnityMonoBehaviorLifecycle
        void Start()
        {
            timeBetweenFramesMappingAndReloc = relocAndMappinSendingRateInFps > 0 ? 1000 / relocAndMappinSendingRateInFps : 0;
            timeBetweenFramesRelocOnly = relocOnlySendingRateInFps > 0 ? 1000 / relocOnlySendingRateInFps : 0;
            timeBetweenFrames = (pipelineMode == PipelineMode.RelocalizationAndMapping) ? timeBetweenFramesMappingAndReloc : timeBetweenFramesRelocOnly;
            LoadUserPrefs();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) SaveUserPrefs();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveUserPrefs();
        }
        #endregion // UnityMonoBehaviorLifecycle

        #region PublicMethods
        async public Task OnFrameReceived(Frame frame)
        {
            if (stopWatch.ElapsedMilliseconds < timeBetweenFrames) return;
            stopWatch.Reset();
            stopWatch.Start();

            if (frame == null) { Log(LogLevel.ERROR, "OnFrameReceived(): frame is null"); return; }

            var frames = new Frames();
            frames.Frames_.Add(frame);
            await grpc.RelocalizeAndMap(frames);
        }

        async public Task OnFrameReceived(Frame frame0, Frame frame1)
        {
            if (frame0 == null || frame1 == null) return;

            var frames = new Frames();
            frames.Frames_.Add(frame0);
            frames.Frames_.Add(frame1);
            await grpc.RelocalizeAndMap(frames);
        }

        private void OnReceivedPoseInternal(RelocAndMappingResult r)
        {
            OnReceivedPose?.Invoke(r);

            if (r.Result.MappingStatus != mappingStatus)
            {
                var oldMappingStatus = mappingStatus;
                mappingStatus = r.Result.MappingStatus;
                OnMappingStatusChanged?.Invoke(oldMappingStatus, mappingStatus);
            }

            if (r.Result.PoseStatus != poseStatus)
            {
                var oldPoseStatus = poseStatus;
                poseStatus = r.Result.PoseStatus;
                OnPoseStatusChanged?.Invoke(oldPoseStatus, poseStatus);
            }
        }

        async public Task<bool> Connect()
        {
            if (!await Disconnect()) return false;

            grpc = BuildGrpcManager();
            if (grpc == null) return false;
            var result = await grpc.RegisterClient();
            if (!result.Success)
            {
                Log(LogLevel.ERROR, "Error while registering client: " + result.ErrMessage);
                return false;
            }
            grpc.OnResultReceived += OnReceivedPoseInternal;

            try
            {
                OnConnect?.Invoke(frontendIp);
            }
            catch(Exception e)
            {
                Log(LogLevel.ERROR, $"SolARCloud.Connect(): {e.Message}");
            }
            // Don't consider observer failures as a failure to connect to the services
            return true;
        }

        async public Task<bool> Disconnect()
        {
            try
            {
                if (grpc != null)
                {
                    var res = await grpc.UnRegisterClient();
                    if (!res.Success)
                    {
                        Log(LogLevel.ERROR, "Error while registering client: " + res.ErrMessage);
                        return false;
                    }
                }

                try
                {
                    OnDisconnect?.Invoke();
                }
                catch (Exception e)
                {
                    Log(LogLevel.ERROR, $"SolARCloud.Disconnect(): {e.Message}");
                }
                // Don't consider observer failures as a failure to disconnect from the services
                return true;
            }
            finally
            {
                grpc = null;
            }
        }

        public bool TogglePipelineMode()
        {
            if (started)
            {
                Log(LogLevel.ERROR, "Pipeline mode cannot be changed while running");
                return false;
            }

            PipelineMode oldMode = pipelineMode;
            switch (pipelineMode)
            {
                case PipelineMode.RelocalizationAndMapping:
                    {
                        pipelineMode = PipelineMode.RelocalizationOnly;
                        timeBetweenFrames = timeBetweenFramesRelocOnly;
                        break;
                    }
                case PipelineMode.RelocalizationOnly:
                    {
                        pipelineMode = PipelineMode.RelocalizationAndMapping;
                        timeBetweenFrames = timeBetweenFramesMappingAndReloc;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Unkown pipeline mode was selected");
                    }
            }

            OnPipelineModeChanged?.Invoke(oldMode, pipelineMode);

            return true;
        }

        //async private Task<bool> SolARRegisterClient()
        //{
        //    Debug.Log($"SolARRegisterClient()");
        //    var result = await grpc.RegisterClient();
        //    Debug.Log($"SolARRegisterClient() Done ({result.Success}, {result.ErrMessage})");
        //    if (!result.Success) Debug.LogError($"SolARRegisterClient(): {result.ErrMessage}");
        //    return result.Success;
        //}

        //async private Task<bool> SolARUnRegisterClient()
        //{
        //    Debug.Log($"SolARUnRegisterClient()");
        //    var result = await grpc.UnRegisterClient();
        //    Debug.Log($"SolARUnRegisterClient() Done ({result.Success}, {result.ErrMessage})");
        //    if (!result.Success) Debug.LogError($"SolARUnRegisterClient(): {result.ErrMessage}");
        //    return result.Success;
        //}

        async private Task<bool> SolARInit()
        {
            Log(LogLevel.INFO, $"SolARInit({pipelineMode})");
            var result = await grpc.Init(pipelineMode);
            if (!result.Success) Log(LogLevel.ERROR, $"SolARInit({pipelineMode}): {result.ErrMessage}");
            return result.Success;
        }

        async private Task<bool> SolARStart()
        {
            Log(LogLevel.INFO, $"SolARStart()");
            stopWatch.Reset();
            stopWatch.Start();
            var result = await grpc.Start();
            if (!result.Success) Log(LogLevel.ERROR, $"SolARStart(): {result.ErrMessage}");
            OnStart?.Invoke(result.Success, result.ErrMessage);
            return result.Success;
        }

        async public Task<bool> SolARStop()
        {
            Log(LogLevel.INFO, $"SolARStop()");
            stopWatch.Stop();
            var result = await grpc.Stop();
            if (!result.Success) Log(LogLevel.ERROR, $"SolARStop(): {result.ErrMessage}");
            OnStop?.Invoke();
            return result.Success;
        }

        async public Task<bool> SolARReset()
        {
            Log(LogLevel.INFO, $"SolARReset()");
            var result = await grpc.Reset();
            if (!result.Success) Log(LogLevel.ERROR, $"SolARReset(): {result.ErrMessage}");
            OnReset?.Invoke(result.Success);
            return result.Success;
        }

        async public Task<bool> SolARSendMessage(string message)
        {
            Log(LogLevel.INFO, $"SolARSendMessage()");
            var result = await grpc.SendMessage(message);
            if (!result.Success) Log(LogLevel.ERROR, $"SolARSendMessage(): {result.ErrMessage}");
            return result.Success;
        }


        async public Task<RelocAndMappingResult> SolARGet3dTransform()
        {
            Log(LogLevel.INFO, $"SolARGet3dTransform()");
            var result = await grpc.Get3dTransform();
            if (!result.Status.Success) Log(LogLevel.ERROR, $"SolARGet3dTransform(): {result.Status.ErrMessage}");
            return result;
        }

        async private Task<bool> SolARSetCamParameters(CamParameters cp)
        {
            Log(LogLevel.INFO, $"SolARSetCamParameters()");
            var result = await grpc.SetCameraParameters(cp);
            if (!result.Success) Log(LogLevel.ERROR, $"SolARSetCamParameters(): {result.ErrMessage}");
            return result.Success;           
        }

        async private Task<bool> SolARSetCamParametersStereo(CamParameters cp1, CamParameters cp2)
        {
            Log(LogLevel.INFO, $"SolARSetCamParametersStereo()");
            var result = await grpc.SetCameraParametersStereo(cp1, cp2);
            if (!result.Success) Log(LogLevel.ERROR, $"SolARSetCamParametersStereo(): {result.ErrMessage}");
            return result.Success;
        }

        async private Task<bool> SolARSetRectificationParameters(CamRectification cr1, CamRectification cr2)
        {
            Log(LogLevel.INFO, $"SolARSetRectificationParameters()");
            var result = await grpc.SetRectificationParameters(cr1, cr2);
            if (!result.Success) Log(LogLevel.ERROR, $"SolARSetRectificationParameters(): {result.ErrMessage}");
            return result.Success;
        }

        async public Task<bool> StartRelocAndMapping(CamParameters cp)
        {
            if (!await SolARInit()) return false;
            if (!await SolARSetCamParameters(cp)) return false;
            if (!await SolARStart()) return false;
            return true;
        }

        async public Task<bool> StartRelocAndMapping(CamParameters cp1,
            CamParameters cp2,CamRectification cr1, CamRectification cr2)
        {
            if (!await SolARInit()) return false;
            if (!await SolARSetCamParametersStereo(cp1, cp2)) return false;
            if (!await SolARSetRectificationParameters(cr1, cr2)) return false;
            if (!await SolARStart()) return false;
            return true;
        }

        async public Task<bool> StopRelocAndMapping()
        {
            bool res = true;
            res = res && await SolARStop();
            // StopFetchingFrames?.Invoke();
            return res;
        }

        public bool Isregistered()
        {
            return grpc != null && grpc.IsRegistered();
        }

        public bool HasNetworkSlotAvailable()
        {
            return grpc.HasNetworkSlotAvailable();
        }

        #endregion // PublicMethods

        #region PrivateMethods

        private void Log(LogLevel level, string message) => OnLog?.Invoke(level, message);

        private GrpcManager BuildGrpcManager()
        {
            Uri uri;
            try
            {
                // Check service URL format
                // Warning: only checks IP v4 address (only 2 colons possible, protocol and port)
                // TODO(jmhenaff): handle IP v6
                uri = new Uri(frontendIp);

                // Warning: only allows IP address, not textual domain names. This is to avoid a bug
                // with gRPC calls with addresses like "xxx.xxx.xxx." which seems to take a lot of resource
                // even after the deadline has expired and the client has been deleted in the manager.
                // TODO(jmhenaff): find a way to allow any well formed URLs without the aforementioned issue.
                System.Net.IPAddress.Parse(uri.Host);
            }
            catch(UriFormatException)
            {
                Log(LogLevel.ERROR, $"Service URL not a valid: {frontendIp}");
                return null;
            }

            int _frontendBasePort = uri.Port;
            string _frontEndIp = uri.GetComponents(UriComponents.SchemeAndServer & ~UriComponents.Port, UriFormat.UriEscaped);

            // New instance to force creation of new reusable channels and clients with potentially a different address
            return new GrpcManager(_frontEndIp, _frontendBasePort, advancedGrpcSettings.threadSlots, advancedGrpcSettings.networkSlots);
        }

        private void LoadUserPrefs()
        {
            frontendIp = PlayerPrefs.GetString("SolARCloudServicesAddress", frontendIp);
        }

        public void SaveUserPrefs()
        {
            PlayerPrefs.SetString("SolARCloudServicesAddress", frontendIp);
            PlayerPrefs.Save();
        }

        #endregion
    }
}


