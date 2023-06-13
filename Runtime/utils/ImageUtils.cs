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

using UnityEngine.Experimental.Rendering;

using Google.Protobuf;

using Com.Bcom.Solar.Gprc;
using System.Threading.Tasks;

namespace Com.Bcom.Solar
{
    public class ImageUtils
    {
        static public Frame ApplyCompression(Frame f)
        {
            switch (f.Image.ImageCompression)
            {
                case ImageCompression.None:
                    {
                        // Do nothing
                        break;
                    }
                case ImageCompression.Png:
                case ImageCompression.Jpg:
                    {
                        f.Image.Data = ByteString.CopyFrom(
                            ApplyCompression(
                                f.Image.Layout,
                                f.Image.Width,
                                f.Image.Height,
                                f.Image.Data.ToByteArray(),
                                f.Image.ImageCompression));
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Unkown Image compression kind");
                    }
            }
            return f;
        }

        async static public Task<Frame> ApplyCompressionAsync(Frame f)
        {
            return await Task.Run(() => ApplyCompression(f));
        }

        static private byte[] ApplyCompression(ImageLayout imLayout, uint imWidth, uint imHeight, byte[] imData, ImageCompression imageCompression)
        {
            GraphicsFormat format;
            switch (imLayout)
            {
                case ImageLayout.Rgb24: format = GraphicsFormat.B8G8R8A8_UNorm; break;
                case ImageLayout.Grey8: format = GraphicsFormat.R8_UNorm; break;
                case ImageLayout.Grey16: format = GraphicsFormat.R16_UNorm; break;
                default: throw new ArgumentException("Unkown image layout");
            }

            switch (imageCompression)
            {
                case ImageCompression.Png: return UnityEngine.ImageConversion.EncodeArrayToPNG(FlipOpt(imLayout, imWidth, imHeight, imData), format, imWidth, imHeight);
                case ImageCompression.Jpg: return UnityEngine.ImageConversion.EncodeArrayToJPG(FlipOpt(imLayout, imWidth, imHeight, imData), format, imWidth, imHeight);
                case ImageCompression.None: throw new ArgumentException("None should not be used here"); // return imData;
                default: throw new ArgumentException("Unknown image compression");

            }
        }

        // Images compressed with UnityEngine.ImageConversion are flipped, use Flip() to unflip them
        static private byte[] Flip(ImageLayout layout, uint width, uint height, byte[] data)
        {
            byte[] result = new byte[data.Length];
            short nbBytesPerPixels;
            switch (layout)
            {
                case ImageLayout.Rgb24: nbBytesPerPixels = 4; break; // because is converted in B8G8R8A8_UNorm
                case ImageLayout.Grey8: nbBytesPerPixels = 1; break;
                case ImageLayout.Grey16: nbBytesPerPixels = 2; break;
                default: throw new ArgumentException("Unkown image layout");
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int destIndex = (int)((x * nbBytesPerPixels) + y * (width * nbBytesPerPixels));
                    int srcIndex = (int)((x * nbBytesPerPixels) + (height - 1 - y) * (width * nbBytesPerPixels));

                    for (int c = 0; c < nbBytesPerPixels; c++)
                    {
                        result[destIndex + c] = data[srcIndex + c];
                    }
                }
            }

            return result;
        }

        private static byte[] pixel1Bytes = new byte[1];
        private static byte[] pixel2Bytes = new byte[2];
        private static byte[] pixel4Bytes = new byte[4];
        static private byte[] FlipOpt(ImageLayout layout, uint width, uint height, byte[] data)
        {
            byte[] pixelArray;
            switch (layout)
            {
                case ImageLayout.Rgb24: pixelArray = pixel4Bytes; break; // because is converted in B8G8R8A8_UNorm
                case ImageLayout.Grey8: pixelArray = pixel1Bytes; break;
                case ImageLayout.Grey16: pixelArray = pixel2Bytes; break;
                default: throw new ArgumentException("Unkown image layout");
            }
            uint nbBytesPerPixels = (uint)pixelArray.Length;
            uint nbBytesPerLines = width * nbBytesPerPixels;
            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int destIndex = (int)((x * nbBytesPerPixels) + y * nbBytesPerLines);
                    int srcIndex = (int)((x * nbBytesPerPixels) + (height - 1 - y) * nbBytesPerLines);
                    for (int c = 0; c < nbBytesPerPixels; c++)
                    {
                        pixelArray[c] = data[destIndex + c];
                        data[destIndex + c] = data[srcIndex + c];
                        data[srcIndex + c] = pixelArray[c];
                    }
                }
            }
            return data;
        }

    }
}
