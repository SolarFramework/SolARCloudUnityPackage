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

namespace Com.Bcom.Solar
{
    [Serializable]
    public class CamDistortion
    {
        public readonly float k1;
        public readonly float k2;
        public readonly float p1;
        public readonly float p2;
        public readonly float k3;

        public CamDistortion(float k1, float k2, float p1, float p2, float k3)
        {
            this.k1 = k1;
            this.k2 = k2;
            this.p1 = p1;
            this.p2 = p2;
            this.k3 = k3;
        }
    }
}
