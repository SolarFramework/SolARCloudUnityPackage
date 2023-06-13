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
    public struct RotationMatrix
    {
        public float m00;
        public float m01;
        public float m02;
        public float m10;
        public float m11;
        public float m12;
        public float m20;
        public float m21;
        public float m22;
    }

    [Serializable]
    public struct ProjectionMatrix
    {
        public float m00;
        public float m01;
        public float m02;
        public float m03;
        public float m10;
        public float m11;
        public float m12;
        public float m13;
        public float m20;
        public float m21;
        public float m22;
        public float m23;
    }
}

namespace Com.Bcom.Solar.Gprc
{
    public static class Utils
    {
        public static Matrix4x4 toMatrix4x4(float[] mat)
        {
            return new Matrix4x4
            {
                M11 = mat[0],  M12 = mat[1],  M13 = mat[2],  M14 = mat[3],
                M21 = mat[4],  M22 = mat[5],  M23 = mat[6],  M24 = mat[7],
                M31 = mat[8],  M32 = mat[9],  M33 = mat[10], M34 = mat[11],
                M41 = mat[12], M42 = mat[13], M43 = mat[14], M44 = mat[15]
            };
        }

        public static Matrix4x4 toMatrix4x4(double[] mat)
        {
            return new Matrix4x4
            {
                M11 = (float)mat[0],  M12 = (float)mat[1],  M13 = (float)mat[2],  M14 = (float)mat[3],
                M21 = (float)mat[4],  M22 = (float)mat[5],  M23 = (float)mat[6],  M24 = (float)mat[7],
                M31 = (float)mat[8],  M32 = (float)mat[9],  M33 = (float)mat[10], M34 = (float)mat[11],
                M41 = (float)mat[12], M42 = (float)mat[13], M43 = (float)mat[14], M44 = (float)mat[15]
            };
        }
    }
}
