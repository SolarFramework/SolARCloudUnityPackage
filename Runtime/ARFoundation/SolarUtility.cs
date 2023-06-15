using ArTwin;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Com.Bcom.Solar.Gprc
{
    /// <summary>
    /// Convertion tools for managing SolAR entities in Unity
    /// </summary>
    public static class SolarUtility
    {
        public static Encoding ToEncoding(this ImageCompression imageCompression)
        {
            switch (imageCompression)
            {
                case ImageCompression.None: return Encoding.None;
                case ImageCompression.Png: return Encoding.Png;
                case ImageCompression.Jpg: return Encoding.Jpg;
                default: throw new NotImplementedException(imageCompression.ToString());
            }
        }

        /// Returns the CameraType equivalent to the provided ImageLayout
        public static CameraType ToCameraType(this ImageLayout imageLayout)
        {
            switch (imageLayout)
            {
                case ImageLayout.Grey8: return CameraType.Gray;
                case ImageLayout.Grey16: return CameraType.Gray;
                case ImageLayout.Rgb24: return CameraType.Rgb;
                default: throw new NotImplementedException(imageLayout.ToString());
            }
        }

        /// Returns the TextureFormat equivalent to the provided ImageLayout
        public static TextureFormat ToTextureFormat(this ImageLayout imageLayout)
        {
            switch (imageLayout)
            {
                case ImageLayout.Grey8: return TextureFormat.R8;
                case ImageLayout.Grey16: return TextureFormat.R16;
                case ImageLayout.Rgb24: return TextureFormat.RGB24;
                default: throw new NotImplementedException(imageLayout.ToString());
            }
        }

        /// Returns the GraphicsFormat equivalent to the provided ImageLayout
        public static GraphicsFormat ToGraphicsFormat(this ImageLayout imageLayout)
        {
            switch (imageLayout)
            {
                case ImageLayout.Grey8: return GraphicsFormat.R8_UNorm;
                case ImageLayout.Grey16: return GraphicsFormat.D16_UNorm;
                case ImageLayout.Rgb24: return GraphicsFormat.R8G8B8_UNorm;
                default: throw new NotImplementedException(imageLayout.ToString());
            }
        }

        /// Extracts the Pose of a RelocalizationResult
        public static Pose GetUnityPose(this RelocalizationResult result)
        {
            if (result.PoseStatus == RelocalizationPoseStatus.NoPose) return default;
            var pose = result?.Pose;
            if (pose == null) return default;
            var matrix = pose.ToUnity();
            return GetUnityPose(matrix);
        }

        /// Extracts the Pose of a RelocalizationResult
        public static Pose GetUnityPose(UnityEngine.Matrix4x4 matrix)
        {
            var pos = matrix.GetColumn(3);
            var forward = matrix.GetColumn(2);
            var upwards = matrix.GetColumn(1);
            var rot = Quaternion.LookRotation(forward, upwards);
            return new Pose(pos, rot);
        }

        /// Extracts the rotation as Quaternion from a 3x3 matrix
        public static Quaternion ToQuaternion(this Matrix3x3 m)
        {
            var matrix = m.ToUnity();
            //var pos = matrix.GetColumn(3);
            var forward = matrix.GetColumn(2);
            var up = matrix.GetColumn(1);
            return Quaternion.LookRotation(forward, up);
        }

        /// Converts a Solar 3x3 matrix to a Unity 4x4 matrix
        public static UnityEngine.Matrix4x4 ToUnity(this Matrix3x3 m)
        {
            return new UnityEngine.Matrix4x4
            (
                new Vector4(+m.M11, -m.M12, +m.M13, 0),
                new Vector4(-m.M21, +m.M22, -m.M23, 0),
                new Vector4(+m.M31, -m.M32, +m.M33, 0),
                new Vector4(0, 0, 0, 1)
            ).transpose; // Transpose because constructor takes columns, not rows
        }

        /// Converts a Solar 3x4 matrix into a Unity 4x4 matrix
        public static UnityEngine.Matrix4x4 ToUnity(this Matrix3x4 m)
        {
            return new UnityEngine.Matrix4x4
            (
                new Vector4(+m.M11, -m.M12, +m.M13, +m.M14),
                new Vector4(-m.M21, +m.M22, -m.M23, -m.M24),
                new Vector4(+m.M31, -m.M32, +m.M33, +m.M34),
                new Vector4(0, 0, 0, 1)
            ).transpose; // Transpose because constructor takes columns, not rows
        }

        /// Converts a Solar 4x4 matrix into a Unity 4x4 matrix by inversing Y axes
        public static UnityEngine.Matrix4x4 ToUnity(this Matrix4x4 m)
        {
            return new UnityEngine.Matrix4x4
            (
                new Vector4(+m.M11, -m.M12, +m.M13, +m.M14),
                new Vector4(-m.M21, +m.M22, -m.M23, -m.M24),
                new Vector4(+m.M31, -m.M32, +m.M33, +m.M34),
                new Vector4(+m.M41, -m.M42, +m.M43, +m.M44)
            ).transpose; // Transpose because constructor takes columns, not rows
        }

        public static Matrix4x4 toGrpc(this UnityEngine.Matrix4x4 m)
        {
            return new Matrix4x4()
            {
                M11 = +m.m00, M12 = -m.m01, M13 = +m.m02, M14 = +m.m03,
                M21 = -m.m10, M22 = +m.m11, M23 = -m.m12, M24 = -m.m13,
                M31 = +m.m20, M32 = -m.m21, M33 = +m.m22, M34 = +m.m23,
                M41 = +m.m30, M42 = -m.m31, M43 = +m.m32, M44 = +m.m33,
            };
        }
    }
}
