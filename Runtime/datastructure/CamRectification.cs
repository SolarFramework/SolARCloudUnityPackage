
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

using Com.Bcom.Solar.Gprc;

namespace Com.Bcom.Solar
{
    [Serializable]
    public class CamRectification
    {
        public readonly RotationMatrix rotation;
        public readonly ProjectionMatrix projection;
        public readonly StereoType stereoType;
        public readonly float baseline;

        public CamRectification(RotationMatrix rotation, ProjectionMatrix projection, StereoType stereoType, float baseline)
        {
            this.rotation = rotation;
            this.projection = projection;
            this.stereoType = stereoType;
            this.baseline = baseline;
        }
    }
}
