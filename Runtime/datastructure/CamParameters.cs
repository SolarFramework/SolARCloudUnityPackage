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

using Com.Bcom.Solar.Gprc;

namespace Com.Bcom.Solar
{
    public class CamParameters
    {
        public readonly string name;
        public readonly uint id;
        public readonly CameraType type;
        public readonly CamResolution resolution;
        public readonly CamIntrinsics intrisincs;
        public readonly CamDistortion distortion;

        public CamParameters(
            string name,
            uint id,
            CameraType type,
            CamResolution resolution,
            CamIntrinsics intrisincs,
            CamDistortion distortion)
        {
            this.name = name;
            this.id = id;
            this.type = type;
            this.resolution = resolution;
            this.intrisincs = intrisincs;
            this.distortion = distortion;
        }
    }
}
