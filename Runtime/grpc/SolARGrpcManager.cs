
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
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

using Grpc.Net.Client;
using Grpc.Net.Client.Web;


namespace Com.Bcom.Solar.Gprc
{
    public class GrpcManager
    {
        public event Action<RelocAndMappingResult> OnResultReceived;
        public event Action<RelocAndMappingResult> On3dTransformReceived;

        private Empty EMPTY = new Empty();
        private ResultStatus SUCCESS = new ResultStatus(true, "");
        private RelocAndMappingResult SKIPPED = new RelocAndMappingResult(new ResultStatus(true, "Skipped"), null);

        private volatile int threadSlots;
        private volatile int networkSlots;
        private string clientUuid = "";
        
        static DateTime DEADLINE => DateTime.UtcNow.AddSeconds(2);

        private readonly SolARFrontEndProxy.SolARFrontEndProxyClient client;

        private SolARFrontEndProxy.SolARFrontEndProxyClient[] clients;

        public GrpcManager(string serviceAddress,
                           int port,
                           int threadSlots,
                           int networkSlots)
        {
            this.threadSlots = threadSlots;
            this.networkSlots = networkSlots;

            clients = new SolARFrontEndProxy.SolARFrontEndProxyClient[networkSlots];

            for (int i = 0; i < networkSlots; i++)
            {
                var newChannel = GrpcChannel.ForAddress(
                    $"{serviceAddress}:{port + i}",
                    new GrpcChannelOptions
                    {
                        HttpHandler = new GrpcWebHandler(new HttpClientHandler())
                        {
                            HttpVersion = new Version("1.1")
                        }
                    });

                clients[i] = new SolARFrontEndProxy.SolARFrontEndProxyClient(newChannel);
            }
            client = clients[0];
        }

        async public Task<ResultStatus> RegisterClient()
        {
            try
            {
                var result = await client.RegisterClientAsync(EMPTY, deadline: DEADLINE);
                clientUuid = result.ClientUuid;
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> UnRegisterClient()
        {
            try
            {
                await client.UnregisterClientAsync(new ClientUUID { ClientUuid = clientUuid }, deadline: DEADLINE);
                clientUuid = "";
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> Init(PipelineMode pipelineMode)
        {
            try
            {
                await client.InitAsync(new PipelineModeValue() { ClientUuid=clientUuid, PipelineMode = pipelineMode },
                                       deadline: DEADLINE);
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> Start()
        {
            try
            {
                await client.StartAsync(new ClientUUID { ClientUuid = clientUuid }, deadline: DEADLINE);
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> Stop()
        {
            try
            {
                await client.StopAsync(new ClientUUID { ClientUuid = clientUuid }, deadline: DEADLINE);
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> Reset()
        {
            try
            {
                await client.ResetAsync(EMPTY, deadline: DEADLINE);
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> SetCameraParameters(CamParameters cp)
        {
            try
            {
                 await client.SetCameraParametersAsync(
                    new CameraParameters
                        {
                            ClientUuid = clientUuid,
                            Name = cp.name,
                            Id = cp.id,
                            CameraType = cp.type,
                            Width = cp.resolution.width,
                            Height = cp.resolution.height,
                            Intrinsics = new Matrix3x3
                            {
                                M11 = cp.intrisincs.fx, M12 = 0,                M13 = cp.intrisincs.cx,
                                M21 = 0,                M22 = cp.intrisincs.fy, M23 = cp.intrisincs.cy,
                                M31 = 0,                M32 = 0,                M33 = 1
                            },
                            Distortion = new CameraDistortion
                            {
                                K1 = cp.distortion.k1,
                                K2 = cp.distortion.k2,
                                P1 = cp.distortion.p1,
                                P2 = cp.distortion.p2,
                                K3 = cp.distortion.k3
                            }
                        },
                    deadline: DEADLINE);
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
            return SUCCESS;
        }

        async public Task<ResultStatus> SetCameraParametersStereo(CamParameters cp1, CamParameters cp2)
        {
            try
            {
                 await client.SetCameraParametersStereoAsync(
                    new CameraParametersStereo
                        {
                            ClientUuid = clientUuid,
                            Name1 = cp1.name,
                            Id1 = cp1.id,
                            CameraType1 = cp1.type,
                            Width1 = cp1.resolution.width,
                            Height1 = cp1.resolution.height,
                            Intrinsics1 = new Matrix3x3
                            {
                                M11 = cp1.intrisincs.fx, M12 = 0,                 M13 = cp1.intrisincs.cx,
                                M21 = 0,                 M22 = cp1.intrisincs.fy, M23 = cp1.intrisincs.cy,
                                M31 = 0,                 M32 = 0,                 M33 = 1
                            },
                            Distortion1 = new CameraDistortion
                            {
                                K1 = cp1.distortion.k1,
                                K2 = cp1.distortion.k2,
                                P1 = cp1.distortion.p1,
                                P2 = cp1.distortion.p2,
                                K3 = cp1.distortion.k3
                            },
                            Name2 = cp2.name,
                            Id2 = cp2.id,
                            CameraType2 = cp2.type,
                            Width2 = cp2.resolution.width,
                            Height2 = cp2.resolution.height,
                            Intrinsics2 = new Matrix3x3
                            {
                                M11 = cp2.intrisincs.fx, M12 = 0,                 M13 = cp2.intrisincs.cx,
                                M21 = 0,                 M22 = cp2.intrisincs.fy, M23 = cp2.intrisincs.cy,
                                M31 = 0,                 M32 = 0,                 M33 = 1
                            },
                            Distortion2 = new CameraDistortion
                            {
                                K1 = cp2.distortion.k1,
                                K2 = cp2.distortion.k2,
                                P1 = cp2.distortion.p1,
                                P2 = cp2.distortion.p2,
                                K3 = cp2.distortion.k3
                            }
                        },
                    deadline: DEADLINE);
                return SUCCESS;
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
        }

        async public Task<ResultStatus> SetRectificationParameters(CamRectification cr1, CamRectification cr2)
        {
            try
            {
                 await client.setRectificationParametersAsync(
                    new RectificationParameters
                    {
                        ClientUuid = clientUuid,
                        Cam1Rotation = new Matrix3x3
                        {
                            M11 = cr1.rotation.m00, M12 = cr1.rotation.m01, M13 = cr1.rotation.m02,
                            M21 = cr1.rotation.m10, M22 = cr1.rotation.m11, M23 = cr1.rotation.m12,
                            M31 = cr1.rotation.m20, M32 = cr1.rotation.m21, M33 = cr1.rotation.m22
                        },
                        Cam1Projection = new Matrix3x4
                        {
                            M11 = cr1.projection.m00, M12 = cr1.projection.m01, M13 = cr1.projection.m02, M14 = cr1.projection.m03,
                            M21 = cr1.projection.m10, M22 = cr1.projection.m11, M23 = cr1.projection.m12, M24 = cr1.projection.m13,
                            M31 = cr1.projection.m20, M32 = cr1.projection.m21, M33 = cr1.projection.m22, M34 = cr1.projection.m23
                        },
                        Cam1StereoType = cr1.stereoType,
                        Cam1Baseline = cr1.baseline,

                        Cam2Rotation = new Matrix3x3
                        {
                            M11 = cr2.rotation.m00, M12 = cr2.rotation.m01, M13 = cr2.rotation.m02,
                            M21 = cr2.rotation.m10, M22 = cr2.rotation.m11, M23 = cr2.rotation.m12,
                            M31 = cr2.rotation.m20, M32 = cr2.rotation.m21, M33 = cr2.rotation.m22
                        },
                        Cam2Projection = new Matrix3x4
                        {
                            M11 = cr2.projection.m00, M12 = cr2.projection.m01, M13 = cr2.projection.m02, M14 = cr2.projection.m03,
                            M21 = cr2.projection.m10, M22 = cr2.projection.m11, M23 = cr2.projection.m12, M24 = cr2.projection.m13,
                            M31 = cr2.projection.m20, M32 = cr2.projection.m21, M33 = cr2.projection.m22, M34 = cr2.projection.m23,
                        },
                        Cam2StereoType = cr2.stereoType,
                        Cam2Baseline = cr2.baseline
                    },
                    deadline: DEADLINE);
                return SUCCESS;
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
        }

        public bool HasNetworkSlotAvailable()
        {
            return networkSlots > 0;
        }

        async public Task<RelocAndMappingResult> RelocalizeAndMap(Frames frames)
        {
            if (networkSlots <= 0) return SKIPPED;
            try
            {
                Interlocked.Decrement(ref networkSlots);

                frames.ClientUuid = clientUuid;
                var result = await clients[networkSlots].RelocalizeAndMapAsync(frames, deadline: DEADLINE);
                var relocAndMappingResult = new RelocAndMappingResult(SUCCESS, result);
                OnResultReceived?.Invoke(relocAndMappingResult);
                return relocAndMappingResult;
            }
            catch (Exception e)
            {
                return new RelocAndMappingResult(new ResultStatus(false, e.Message), null);
            }
            finally
            {
                Interlocked.Increment(ref networkSlots);
            }
        }

        internal bool IsRegistered()
        {
            return clientUuid != "";
        }

        async public Task<RelocAndMappingResult> Get3dTransform()
        {
            try
            {
                var result = await client.Get3DTransformAsync(new ClientUUID { ClientUuid = clientUuid },
                                                              deadline: DEADLINE);
                return new RelocAndMappingResult(SUCCESS, result);
            }
            catch (Exception e)
            {
                return new RelocAndMappingResult(new ResultStatus(false, e.Message), null);
            }
        }

        async public Task<ResultStatus> SendMessage(string message)
        {
            try
            {
                await client.SendMessageAsync(new Message { Message_ = message }, deadline: DEADLINE);
                return SUCCESS;
            }
            catch (Exception e)
            {
                return new ResultStatus(false, e.Message);
            }
        }
    }
}


