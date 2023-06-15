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

using ArTwin;
using Com.Bcom.Solar.Gprc;
using Google.Protobuf;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Com.Bcom.Solar
{
    public class SolARCloudArFoundation : MonoBehaviour
    {

        public event Action<bool> OnSensorStarted;
        public event Action OnSensorStopped;
        public event Action<LogLevel, string> OnLog;
        // bool: new debug state (enabled/disabled)
        public event Action<bool> OnToggleDebug;

        [Tooltip("Number of frame fetched from sensors per seconds")]
        public int framerate = 30;
        [HideInInspector]
        public XRCpuImage.Transformation transformation = XRCpuImage.Transformation.None;
        public Vector2Int cameraResolution = new Vector2Int(640, 480);
        [Range(0, 100)]
        public int jpgQuality = 100;
        public ImageLayout imageLayout = ImageLayout.Grey8;

        public bool ShowDebug
        {
            get { return showDebug; }
            private set
            {
                if (showDebug != value)
                {
                    try
                    {
                        OnToggleDebug?.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        Log(LogLevel.ERROR, $"OnToggleDebug callback error: {e.Message}");
                    }
                }
                showDebug = value;
            }
        }

        private SolARCloud solARCloud;

        private bool showIp = false;
        private bool showConsole = false;
        private bool showControls = false;
        private bool showAdvanced = false;
        private bool showDebug = false;
        private bool started = false;
        private bool connected = false;

        private LogConsole console;
        private Vector2 scrollConsole;

        private ARSessionOrigin arSessionOrigin;
        private ARCameraManager arCameraManager;

        private Pose receivedPose;
        private bool poseReceived = false;

        private OneEuroFilter<Vector3> positionFilter;
        private OneEuroFilter<Quaternion> rotationFilter;

        [SerializeField]
        private bool smoothReloc = true;

        private struct FilterConfig
        {
            public float frequency;
            public float minCutoff;
            public float beta;
            public float dCutoff;
        };

        private FilterConfig positionFilterConfig = new FilterConfig()
        {
            frequency = 60f,
            minCutoff = 0.5f,
            beta = 0.0001f,
            dCutoff = 0.1f
        };
        private FilterConfig rotationFilterConfig = new FilterConfig()
        {
            frequency = 60f,
            minCutoff = 0.5f,
            beta = 0.0001f,
            dCutoff = 0.1f
        };

        void Start()
        {
            solARCloud = GetComponent<SolARCloud>();
            if (solARCloud == null)
            {
                Log(LogLevel.ERROR, "required SolARCloud component not found");
                return;
            }

            // ArFoundation
            arCameraManager = FindObjectOfType<ARCameraManager>();
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();

            solARCloud.OnReceivedPose += ApplyPose;

            console = new LogConsole();

            // TODO(jmhenaff): there should be a separation between this class and the GUI, and the GUI
            // should subscribe to OnLog as follows (here it's still mixed up)
            solARCloud.OnLog += OnLogImpl;
            OnLog += OnLogImpl;

            positionFilter = new OneEuroFilter<Vector3>(positionFilterConfig.frequency, positionFilterConfig.minCutoff, positionFilterConfig.beta, positionFilterConfig.dCutoff);
            rotationFilter = new OneEuroFilter<Quaternion>(rotationFilterConfig.frequency, rotationFilterConfig.minCutoff, rotationFilterConfig.beta, rotationFilterConfig.dCutoff);
        }

        void Update()
        {
            if (poseReceived)
            {
                arSessionOrigin.transform.SetPositionAndRotation(
                    smoothReloc ? positionFilter.Filter(receivedPose.position) : receivedPose.position,
                    smoothReloc ? rotationFilter.Filter(receivedPose.rotation) : receivedPose.rotation);
                poseReceived = false;
            }
            else if (smoothReloc)
            {
                arSessionOrigin.transform.SetPositionAndRotation(
                    positionFilter.Filter(receivedPose.position),
                    rotationFilter.Filter(receivedPose.rotation));
            }
        }

        protected async void OnGUI()
        {
            var scale = Screen.dpi / 100;
            GUIUtility.ScaleAroundPivot(scale * Vector2.one, Vector2.zero);

            using (new GUILayout.HorizontalScope(GUILayout.Width(Screen.width / scale)))
            {
                {
                    var w = 28f;
                    var rect = new Rect(0, 0, Screen.height / scale, w);
                    using (new GUILayout.AreaScope(rect))
                    {
                        using (Disposable.CreateWithState(GUI.matrix, m => GUI.matrix = m))
                        {
                            GUIUtility.RotateAroundPivot(-90, Screen.height / 2 * Vector2.one);
                            using (new GUILayout.HorizontalScope())
                            {
                                showAdvanced = GUILayout.Toggle(showAdvanced, "Advanced", GUI.skin.button);
                                showConsole = GUILayout.Toggle(showConsole, "Console", GUI.skin.button);
                                showControls = GUILayout.Toggle(showControls, "Controls", GUI.skin.button);
                                using (ImGuiTools.Enable(!started))
                                {
                                    showIp = GUILayout.Toggle(showIp, "IP", GUI.skin.button);
                                }

                            }
                        }
                    }
                    GUILayout.Space(w);
                }

                if (showIp)
                {
                    using (new GUILayout.VerticalScope("Connect", GUI.skin.window))
                    {
                        using (ImGuiTools.Enable(!connected))
                        {
                            solARCloud.frontendIp = ArtGUILayout.TextField("Server", solARCloud.frontendIp);
                        }
                        if (connected)
                        {
                            if (GUILayout.Button("Disconnect")) { await solARCloud.Disconnect();}
                        }
                        else
                        {
                            if (GUILayout.Button("Connect")) { await solARCloud.Connect();}
                        }
                        connected = solARCloud.Isregistered();
                    }
                }

                if (showControls)
                {
                    using (new GUILayout.VerticalScope())
                    {

                        using (new GUILayout.VerticalScope("Controls", GUI.skin.window))
                        {
                            using (ImGuiTools.Enable(connected))
                            {
                                if (!started)
                                {
                                    if (GUILayout.Button("Start")) StartAsyncGlobal();
                                }
                                else
                                {
                                    if (GUILayout.Button("Stop")) StopAsync();
                                }
                                using (ImGuiTools.Enable(!started))
                                {
                                    if (solARCloud.pipelineMode == PipelineMode.RelocalizationAndMapping)
                                    {
                                        if (GUILayout.Button("Reloc&Map")) solARCloud.TogglePipelineMode();
                                    }
                                    else
                                    {
                                        if (GUILayout.Button("Reloc")) solARCloud.TogglePipelineMode();
                                    }
                                }
                            }
                        }
                    }
                }

                if (showConsole)
                {
                    using (new GUILayout.VerticalScope("Console", GUI.skin.window))
                    {
                        if (GUILayout.Button("Clear")) console.logs.Clear();
                        using (var scope = new GUILayout.ScrollViewScope(scrollConsole))
                        {
                            scrollConsole = scope.scrollPosition;
                            foreach (var line in console.logs.Reverse())
                            {
                                GUILayout.Label(line);
                            }
                        }
                    }
                }

                if (showAdvanced)
                {
                    using (new GUILayout.VerticalScope("Advanced", GUI.skin.window))
                    {
                        using (ImGuiTools.Enable(!started && solARCloud.pipelineMode == PipelineMode.RelocalizationAndMapping))
                        {
                            if (GUILayout.Button("Reset map")) ResetAsync();
                        }
                        ShowDebug = GUILayout.Toggle(ShowDebug, "Show debug");
                    }
                }
            }
        }


        private void ApplyPose(RelocAndMappingResult result)
        {
            if (result.Result.PoseStatus == RelocalizationPoseStatus.NoPose) return;
            receivedPose = result.Result.GetUnityPose();
            poseReceived = true;
        }

        private async void StartAsyncGlobal()
        {
            started = true;
            bool res = await solARCloud.StartRelocAndMapping(GetCamParameters());
            Log(LogLevel.DEBUG, "Done");
            if (!res) { Log(LogLevel.ERROR, "Services not started"); OnSensorStarted?.Invoke(false); return; }

            // Start receiving frames
            arCameraManager.frameReceived += OnFrameReceived;
            OnSensorStarted?.Invoke(true);
        }

        private async void StopAsync()
        {
            started = false;
            arCameraManager.frameReceived -= OnFrameReceived;
            await solARCloud.StopRelocAndMapping();
            OnSensorStopped?.Invoke();
        }

        private async void ResetAsync() => await solARCloud.SolARReset();

        private CamParameters GetCamParameters()
        {
            if (!arCameraManager.TryGetIntrinsics(out var cameraIntrinsics))
                return null;

            // Resolution
            var resolution = cameraIntrinsics.resolution;
            int w = resolution.x, h = resolution.y;
            var scale = new Vector2((float)cameraResolution.x / w, (float)cameraResolution.y / h);
            scale.x = scale.y = Mathf.Min(scale.x, scale.y, 1); // ScaleToFit
            w = Mathf.RoundToInt(w * scale.x);
            h = Mathf.RoundToInt(h * scale.y);
            resolution = new Vector2Int(w, h);

            // Default Calibration
            var focal = Vector2.Scale(cameraIntrinsics.focalLength, scale);
            var pp = Vector2.Scale(cameraIntrinsics.principalPoint, scale);
            var result = new CamParameters(
                name: SystemInfo.deviceUniqueIdentifier,
                id: 0,
                type: Gprc.CameraType.Gray,
                resolution: new CamResolution(width: (uint)w, height: (uint)h),
                intrisincs: new CamIntrinsics(fx: focal.x, fy: focal.y, cx: pp.x, cy: pp.y),
                // distortion: new CamDistortion(k1: -0.0094f, k2: 0.0030f, p1: -0.0061f, p2: -0.0089f, k3: 0f));
                distortion: new CamDistortion(k1: 0f, k2: 0f, p1: 0f, p2: 0f, k3: 0f));


            // Custom Calibration
            var resource = Resources.Load<CalibrationSO>($"Calibrations/{SystemInfo.deviceUniqueIdentifier}");
            if (resource == null) return result;
            var res = resource.resolutions.SingleOrDefault(v => v.resolution == resolution);
            if (res == null) return result;
            focal = res.focals;
            pp = res.pPoint;
            result = new CamParameters(
                name: SystemInfo.deviceUniqueIdentifier,
                id: 0,
                type: Gprc.CameraType.Gray,
                resolution: new CamResolution(width: (uint)w, height: (uint)h),
                intrisincs: new CamIntrinsics(fx: focal.x, fy: focal.y, cx: pp.x, cy: pp.y),
                distortion: new CamDistortion(k1: res.distortions.x, k2: res.distortions.y, p1: 0f, p2: 0f, k3: res.distortions.z));

            return result;
        }

        async void OnFrameReceived(ARCameraFrameEventArgs args = default)
        {
            // Skip frame if no free slots
            if (solARCloud.advancedGrpcSettings.threadSlots <= 0) return;
            Interlocked.Decrement(ref solARCloud.advancedGrpcSettings.threadSlots);
            try
            {
                var frame = await GrabFrame();
                // TODO(jmhenaff): cleaner solution where Frame is correctly build i.o. having an uncompressed buffer while it's compression
                // field says otherwise
                await ImageUtils.ApplyCompressionAsync(frame);
                await solARCloud.OnFrameReceived(frame);
            }
            finally
            {
                Interlocked.Increment(ref solARCloud.advancedGrpcSettings.threadSlots);
            }
        }

        private async Task<Frame> GrabFrame()
        {
            var tuple = PrepareSync();

            var frame = tuple.Item1;
            NativeArray<byte> natArray;
            using (var cpuImage = tuple.Item2)
            {
                // TODO(jmhenaff): cleaner solution where Frame is correctly build i.o. having an uncompressed buffer while it's compression
                // field says otherwise
                // Don't apply compression here, applies later by ImageUtils that also performs a flip
                var encoding = ImageCompression.None.ToEncoding(); // solARCloud.advancedGrpcSettings.imageCompression.ToEncoding();
                natArray = await ConvertCpu(cpuImage, encoding);
            }

            ByteString grpcData;
            using (natArray)
            {
                grpcData = NativeToGrpc(natArray);
            }

            int w = cameraResolution.x, h = cameraResolution.y;
            var image = new Image
            {
                Width = (uint)w,
                Height = (uint)h,
                Layout = imageLayout,
                ImageCompression = solARCloud.advancedGrpcSettings.imageCompression,
                Data = grpcData,
            };
            frame.Image = image;

            return frame;
        }

        Tuple<Frame, XRCpuImage> PrepareSync()
        {
            var pose = arSessionOrigin.trackablesParent.worldToLocalMatrix
                       * arSessionOrigin.camera.transform.localToWorldMatrix;

            // Save current frame data
            var request = new Frame
            {
                SensorId = 0,
                // Timestamp = (ulong)timestampMs,
                // Use UTC UNIX timestamps i.o. ns timestamps retuned by ARFoundation
                // TODO: check if there's a way to get UTC UNIX timestamps from ARFoundation ?
                Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds(),
                Pose = pose.toGrpc(),
                Image = null,
            };

            // Retrive image
            var cpuImage = GrabCpuImage();
            return Tuple.Create(request, cpuImage);
        }

        Task<NativeArray<byte>> ConvertCpu(XRCpuImage cpuImage, Encoding encoding)
        {
            var textureFormat = imageLayout.ToTextureFormat();
            var conversionParams = new XRCpuImage.ConversionParams(cpuImage, textureFormat, transformation) { outputDimensions = cameraResolution };
            return cpuImage.ConvertTask(conversionParams, encoding, jpgQuality);
        }

        private XRCpuImage GrabCpuImage()
        {
            if (!arCameraManager.TryAcquireLatestCpuImage(out var cpuImage) || !cpuImage.valid) throw new Exception("TryAcquireLatestCpuImage");
            return cpuImage;
        }

        private static ByteString NativeToGrpc(NativeArray<byte> natArray)
            => UnsafeByteOperations.UnsafeWrap(natArray.ToArray());

        private void Log(LogLevel level, string message)
        {
            // solARCloud?.SolARSendMessage($"{level}: {message}");
            OnLog?.Invoke(level, message);
            //switch (level)
            //{
            //    case LogLevel.ERROR: Debug.LogError(message); break;
            //    case LogLevel.WARNING: Debug.LogWarning(message); break;
            //    case LogLevel.INFO:
            //    case LogLevel.DEBUG : Debug.Log(message); break;
            //    default:
            //        {
            //            Debug.LogError("Unkown LogLevel");
            //            throw new ArgumentException("Unkown LogLevel");
            //        }
            //}
        }

        private void OnLogImpl(LogLevel level, string message)
        {
            console?.Log(level, message);
        }

    }
}

