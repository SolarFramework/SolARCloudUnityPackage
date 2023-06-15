using Com.Bcom.Solar.Gprc;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace ArTwin
{
    public static class Extensions
    {
        public static Task<NativeArray<byte>> ConvertTask(this XRCpuImage cpuImage, XRCpuImage.ConversionParams conversionParams)
        {
            var task = new TaskCompletionSource<NativeArray<byte>>();
            cpuImage.ConvertAsync(conversionParams, (status, _, natArray) =>
            {
                if (status == XRCpuImage.AsyncConversionStatus.Ready)
                    task.SetResult(natArray);
                else
                    task.SetException(new Exception(status.ToString()));
            });
            return task.Task;
        }

        public async static Task<NativeArray<byte>> ConvertTask(this XRCpuImage cpuImage, XRCpuImage.ConversionParams conversionParams, Encoding encoding, int jpgQuality)
        {
            var natArray = await cpuImage.ConvertTask(conversionParams);

            if (encoding == Encoding.None) return natArray;

            var resolution = conversionParams.outputDimensions;
            int w = resolution.x, h = resolution.y;

            GraphicsFormat format = conversionParams.outputFormat.ToGraphicsFormat();

            using (natArray)
            {
                switch (encoding)
                {
                    case Encoding.Jpg: return ImageConversion.EncodeNativeArrayToJPG(natArray, format, (uint)w, (uint)h, quality: jpgQuality);
                    case Encoding.Png: return ImageConversion.EncodeNativeArrayToPNG(natArray, format, (uint)w, (uint)h);
                    default: throw new NotImplementedException(encoding.ToString());
                }
            }
        }

        static GraphicsFormat ToGraphicsFormat(this TextureFormat textureFormat)
        {
            switch (textureFormat)
            {
                case TextureFormat.Alpha8: return GraphicsFormat.R8_UNorm;
                case TextureFormat.R8: return GraphicsFormat.R8_UNorm;
                case TextureFormat.R16: return GraphicsFormat.R16_UNorm;
                case TextureFormat.RGB24: return GraphicsFormat.R8G8B8_UNorm;
                default: throw new NotImplementedException(textureFormat.ToString());
            }
        }
    }
}
