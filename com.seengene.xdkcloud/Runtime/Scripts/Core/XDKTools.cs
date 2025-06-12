using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Text;
using System.IO;
using System.Net;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.Calib3dModule;

namespace Seengene.XDK
{

    public class XDKTools {

        #region File tools
        /// <summary>
        /// 格式化路径成Asset的标准格式
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string FormatAssetPath(string filePath) {
            var newFilePath1 = filePath.Replace("\\", "/");
            var newFilePath2 = newFilePath1.Replace("//", "/").Trim();
            newFilePath2 = newFilePath2.Replace("///", "/").Trim();
            newFilePath2 = newFilePath2.Replace("\\\\", "/").Trim();
            return newFilePath2;
        }

        public static string GetRelativePathFromFullPath(string fullPath) {
            string mp = fullPath;
            mp = mp.Substring(mp.IndexOf("Assets"));
            mp = mp.Replace('\\', '/');
            return mp;
        }

        /// <summary>
        /// 获取下载文件的大小
        /// </summary>
        /// <returns>The length.</returns>
        /// <param name="url">URL.</param>
        public static long GetFileLengthByUrl(string url) {
            HttpWebRequest requet = HttpWebRequest.Create(url) as HttpWebRequest;
            requet.Method = "HEAD";
            HttpWebResponse response = requet.GetResponse() as HttpWebResponse;
            return response.ContentLength;
        }

        /// <summary>
        /// 计算文件大小函数(保留两位小数),Size为字节大小
        /// </summary>
        /// <param name="size">初始文件大小</param>
        /// <returns></returns>
        public static string GetFormatFileSize(long size) {
            var num = 1024.00; //byte

            if (size < num)
                return size + "B";
            if (size < Math.Pow(num, 2))
                return (size / num).ToString("f2") + "K"; //kb
            if (size < Math.Pow(num, 3))
                return (size / Math.Pow(num, 2)).ToString("f2") + "M"; //M
            if (size < Math.Pow(num, 4))
                return (size / Math.Pow(num, 3)).ToString("f2") + "G"; //G

            return (size / Math.Pow(num, 4)).ToString("f2") + "T"; //T
        }

        #endregion of File tools

        #region Texture Format Compress

        public static byte[] IntPtr8UC3ToJPGBytes(IntPtr imagePtr, int imageWidth, int imageHeight) {
            try {
                Debug.LogFormat("IntPtr8UC3ToJPGBytes: imageHeight={0}, imageWidht={1}", imageHeight, imageWidth);
                Mat grayMat = Mat.zeros(imageHeight, imageWidth, CvType.CV_8UC3);
                MatUtils.copyToMat(imagePtr, grayMat);
                MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 50);
                MatOfByte jpgMat = new MatOfByte();
                if (Imgcodecs.imencode(".jpeg", grayMat, jpgMat, compressionParams)) {
                    return jpgMat.toArray();
                } else {
                    return null;
                }
            } catch (Exception e) {
                Debug.LogErrorFormat("图片转换失败：{0}", e.Message);
                return null;
            }
        }


        /// <summary>
        /// 从Mat33中读取float数据
        /// </summary>
        /// <param name="mat33"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private static float GetFloatFromMat33(Mat mat33, int row, int col)
        {
            var val = mat33.get(row, col);
            return (float)val[0];
        }

        /// <summary>
        /// 使用Mat结构存储相机内参
        /// </summary>
        /// <param name="frameData"></param>
        /// <param name="imageW"></param>
        /// <returns></returns>
        private static Mat CreateMatIntrinsics(XDKFrameData frameData, float imageW)
        {
            float radio = 1f;

            /// 如果获取到的相机图片和内参中对相机图片的尺寸描述对不上，则需要单独缩放。
            if (frameData.cameraIntrinsicsImgW != imageW)
            {
                radio = imageW / frameData.cameraIntrinsicsImgW;
            }

            Mat intrinsicsOrigin = Mat.zeros(3, 3, CvType.CV_32FC1);
            intrinsicsOrigin.put(0, 0, frameData.focalLengthX * radio);
            intrinsicsOrigin.put(0, 1, 0);
            intrinsicsOrigin.put(0, 2, frameData.principalPointX * radio);
            intrinsicsOrigin.put(1, 0, 0);
            intrinsicsOrigin.put(1, 1, frameData.focalLengthY * radio);
            intrinsicsOrigin.put(1, 2, frameData.principalPointY * radio);
            intrinsicsOrigin.put(2, 0, 0);
            intrinsicsOrigin.put(2, 1, 0);
            intrinsicsOrigin.put(2, 2, 1);

            //Debug.Log("Check createMat focalLengthX=" + frameData.focalLengthX * radio);
            return intrinsicsOrigin;
        }



        /// <summary>
        /// 使用Mat结构存储图片、相机内参
        /// </summary>
        /// <param name="frameData"></param>
        /// <param name="imageOrigin"></param>
        /// <param name="intrinsicsOrigin"></param>
        /// <returns></returns>
        public static bool FrameDataToMat(XDKFrameData frameData, out Mat imageOrigin, out Mat intrinsicsOrigin)
        {
            if (frameData.rgbTexture != null)
            {
                int texW = frameData.rgbTexture.width;
                int texH = frameData.rgbTexture.height;
                var rgbaMat = Mat.zeros(texH, texW, CvType.CV_8UC4);
                Utils.texture2DToMat(frameData.rgbTexture, rgbaMat, false);

                // 转成灰度图
                imageOrigin = Mat.zeros(texH, texW, CvType.CV_8UC1);
                Imgproc.cvtColor(rgbaMat, imageOrigin, Imgproc.COLOR_RGBA2GRAY);

                intrinsicsOrigin = CreateMatIntrinsics(frameData, texW);
                return true;
            }
            if (frameData.textureDataRGB24 != null)
            {
                int texW = frameData.textureWidth;
                int texH = frameData.textureHeight;

                // 本身是RGB24
                var rgbMat = Mat.zeros(texH, texW, CvType.CV_8UC3);
                rgbMat.put(0, 0, frameData.textureDataRGB24);

                // 转成灰度图
                imageOrigin = Mat.zeros(texH, texW, CvType.CV_8UC1);
                Imgproc.cvtColor(rgbMat, imageOrigin, Imgproc.COLOR_RGB2GRAY);

                intrinsicsOrigin = CreateMatIntrinsics(frameData, texW);
                return true;
            }

            if (frameData.textureDataR8 != null)
            {
                int texW = frameData.textureWidth;
                int texH = frameData.textureHeight;

                // 本身就是灰度图
                imageOrigin = Mat.zeros(texH, texW, CvType.CV_8UC1);
                imageOrigin.put(0, 0, frameData.textureDataR8);

                intrinsicsOrigin = CreateMatIntrinsics(frameData, texW);
                return true;
            }

            imageOrigin = null;
            intrinsicsOrigin = null;
            return false;
        }

        /// <summary>
        /// 处理图片、相机内参的缩放
        /// </summary>
        /// <param name="imageOrigin"></param>
        /// <param name="intrinsicsOrigin"></param>
        /// <param name="imageScaled"></param>
        /// <param name="intrinsicsScaled"></param>
        public static void CheckForScale(Mat imageOrigin, Mat intrinsicsOrigin, out Mat imageScaled, out Mat intrinsicsScaled)
        {
            int texW = imageOrigin.cols();
            int texH = imageOrigin.rows();
            int maxVal = Math.Max(texW, texH);
            if (maxVal == 1280)
            {
                //Debug.Log("Check No scale");
                imageScaled = imageOrigin;
                intrinsicsScaled = intrinsicsOrigin;
            }
            else if (maxVal > 1280) // 需要缩小
            {
                float scale = 1280f / maxVal;
                float focalLengthX = GetFloatFromMat33(intrinsicsOrigin, 0, 0);
                float focalLengthY = GetFloatFromMat33(intrinsicsOrigin, 1, 1);
                float principalPointX = GetFloatFromMat33(intrinsicsOrigin, 0, 2);
                float principalPointY = GetFloatFromMat33(intrinsicsOrigin, 1, 2);

                intrinsicsScaled = Mat.zeros(3, 3, CvType.CV_32FC1);
                intrinsicsScaled.put(0, 0, focalLengthX * scale);
                intrinsicsScaled.put(0, 1, 0);
                intrinsicsScaled.put(0, 2, principalPointX * scale);
                intrinsicsScaled.put(1, 0, 0);
                intrinsicsScaled.put(1, 1, focalLengthY * scale);
                intrinsicsScaled.put(1, 2, principalPointY * scale);
                intrinsicsScaled.put(2, 0, 0);
                intrinsicsScaled.put(2, 1, 0);
                intrinsicsScaled.put(2, 2, 1);

                int texW2 = (int)(texW * scale);
                int texH2 = (int)(texH * scale);

                imageScaled = Mat.zeros(texH2, texW2, CvType.CV_8UC1);
                Imgproc.resize(imageOrigin, imageScaled, new Size(texW2, texH2));
            }
            else // 原图不变，在周围补上黑边
            {
                int texW2, texH2;
                if (texW > texH)
                {
                    texW2 = 1280;
                    texH2 = 720;
                }
                else
                {
                    texW2 = 720;
                    texH2 = 1280;
                }
                int offsetX = (texW2 - texW) / 2;
                int offsetY = (texH2 - texH) / 2;
                imageScaled = Mat.zeros(texH2, texW2, CvType.CV_8UC1);
                var rowEnd = texH2 - offsetY;
                var colEnd = texW2 - offsetX;
                //Debug.Log("texW=" + texW + " texH=" + texH + " texW2=" + texW2 + " texH2=" + texH2);
                //Debug.Log("rowStart="+offsetY+ " rowEnd="+ rowEnd+" colStart="+ offsetX+ " colEnd="+ colEnd);
                var innerMat = imageScaled.submat(offsetY, rowEnd, offsetX, colEnd);
                imageOrigin.copyTo(innerMat);// 拷贝像素

                float focalLengthX = GetFloatFromMat33(intrinsicsOrigin, 0, 0);
                float focalLengthY = GetFloatFromMat33(intrinsicsOrigin, 1, 1);
                float principalPointX = GetFloatFromMat33(intrinsicsOrigin, 0, 2);
                float principalPointY = GetFloatFromMat33(intrinsicsOrigin, 1, 2);
                intrinsicsScaled = Mat.zeros(3, 3, CvType.CV_32FC1);
                intrinsicsScaled.put(0, 0, focalLengthX);
                intrinsicsScaled.put(0, 1, 0);
                intrinsicsScaled.put(0, 2, principalPointX + offsetX);
                intrinsicsScaled.put(1, 0, 0);
                intrinsicsScaled.put(1, 1, focalLengthY);
                intrinsicsScaled.put(1, 2, principalPointY + offsetY);
                intrinsicsScaled.put(2, 0, 0);
                intrinsicsScaled.put(2, 1, 0);
                intrinsicsScaled.put(2, 2, 1);
            }
        }

        /// <summary>
        /// 处理畸变
        /// </summary>
        /// <param name="imageOrigin"></param>
        /// <param name="intrinsicsOrigin"></param>
        /// <param name="distortData"></param>
        /// <param name="distortionK1"></param>
        /// <param name="imageUndistortion"></param>
        /// <param name="intrinsicsUndistortion"></param>
        public static void CheckForDistort(Mat imageOrigin, Mat intrinsicsOrigin, DistortionData distortData, out Mat imageUndistortion, out Mat intrinsicsUndistortion, float balance = 0f)
        {
            if (distortData != null)
            {
                int texW = imageOrigin.cols();
                int texH = imageOrigin.rows();

                Mat distortion_coeff = Mat.zeros(1, 4, CvType.CV_32FC1);
                float[] distortionArr = new float[] { distortData.K1, distortData.K2, distortData.K3, distortData.K4 };
                distortion_coeff.put(0, 0, distortionArr);

                var origin_size = new Size(texW, texH);
                var undistor_size = new Size(texW * 1.5f, texH * 1.5f);
                imageUndistortion = Mat.zeros((int)undistor_size.height, (int)undistor_size.height, imageOrigin.type());

                //DebugMatIntrinsics("Check Distort 1111\n", intrinsicsOrigin);

                balance = Mathf.Clamp01(balance);
                Mat E1 = Mat.eye(3, 3, CvType.CV_32FC1);
                Mat intrinsicNew = Mat.zeros(3, 3, CvType.CV_32FC1);
                Calib3d.fisheye_estimateNewCameraMatrixForUndistortRectify(intrinsicsOrigin, distortion_coeff, origin_size, E1, intrinsicNew, balance, undistor_size);

                //DebugMatIntrinsics("Check Distort 2222\n", intrinsicNew);

                Mat E2 = Mat.eye(3, 3, CvType.CV_32FC1);
                Mat map1 = new Mat();
                Mat map2 = new Mat();
                Calib3d.fisheye_initUndistortRectifyMap(intrinsicsOrigin, distortion_coeff, E2, intrinsicNew, undistor_size, CvType.CV_16SC2, map1, map2);
                Imgproc.remap(imageOrigin, imageUndistortion, map1, map2, Imgproc.INTER_LINEAR, Core.BORDER_CONSTANT);

                intrinsicsUndistortion = intrinsicNew;
            }
            else
            {
                //Debug.Log("Check No distortion");
                imageUndistortion = imageOrigin;
                intrinsicsUndistortion = intrinsicsOrigin;
            }
        }


        private static void DebugMatIntrinsics(string prefix, Mat intrinsics)
        {
            int cols = intrinsics.cols();
            int rows = intrinsics.rows();
            float[] arr = new float[rows * cols];
            intrinsics.get(0, 0, arr);
            StringBuilder sb = new StringBuilder();
            sb.Append(prefix);
            sb.Append(" -> ");
            for (int i = 0; i < arr.Length; i++)
            {
                sb.Append(" ");
                sb.Append(arr[i]);
                sb.Append(",");
                if (i % cols == cols - 1)
                {
                    sb.Append("\n");
                }
            }
            Debug.Log(sb.ToString());

        }




        /// <summary>
        /// 处理旋转，包括图片和相机内参
        /// </summary>
        /// <param name="imageOrigin"></param>
        /// <param name="intrinsicsOrigin"></param>
        /// <param name="isVertical"></param>
        /// <param name="imageRotated"></param>
        /// <param name="intrinsicsRotated"></param>
        public static void CheckForRotate(Mat imageOrigin, Mat intrinsicsOrigin, bool isVertical, out Mat imageRotated, out Mat intrinsicsRotated)
        {
            if (isVertical)
            {
                // 图片是横向的
                int texW = imageOrigin.cols(); 
                int texH = imageOrigin.rows(); 
                int tempA = (texH - texW) / 2; // 这是一个负值

                // 先旋转，转成竖着的图片
                Mat imageTemp = Mat.zeros(texH, texW, imageOrigin.type());
                Core.rotate(imageOrigin, imageTemp, Core.ROTATE_90_CLOCKWISE);
                Mat roi1 = imageTemp.adjustROI(tempA, tempA, 0, 0); // 获取中间的正方形

                // 准备好一个横向的长方形容器
                imageRotated = Mat.zeros(texH, texW, imageOrigin.type());
                var roi2 = new OpenCVForUnity.CoreModule.Rect(-tempA, 0, texH, texH);// 获取中间的正方形

                roi1.copyTo(imageRotated.submat(roi2));// 拷贝像素

                int texW2 = imageRotated.cols();
                int texH2 = imageRotated.rows();

                //Debug.Log("cuilichen texW2=" + texW2 + " texH2=" + texH2 + " tempA=" + tempA + " texW=" + texW + " texH=" + texH);

                // 处理相机内参
                float focalLengthX = GetFloatFromMat33(intrinsicsOrigin, 0, 0);
                float focalLengthY = GetFloatFromMat33(intrinsicsOrigin, 1, 1);
                float principalPointX = GetFloatFromMat33(intrinsicsOrigin, 0, 2);
                float principalPointY = GetFloatFromMat33(intrinsicsOrigin, 1, 2);
                float principalPointX2 = (texW + texH) / 2 - principalPointY;           // 主点位置变换
                float principalPointY2 = principalPointX - Mathf.Abs(texW - texH) / 2;  // 主点位置变换

                // 输出相机内参到Mat中
                intrinsicsRotated = Mat.zeros(3, 3, CvType.CV_32FC1);
                intrinsicsRotated.put(0, 0, focalLengthX);
                intrinsicsRotated.put(0, 1, 0);
                intrinsicsRotated.put(0, 2, principalPointX2);           
                intrinsicsRotated.put(1, 0, 0);
                intrinsicsRotated.put(1, 1, focalLengthY);      
                intrinsicsRotated.put(1, 2, principalPointY2);  
                intrinsicsRotated.put(2, 0, 0);
                intrinsicsRotated.put(2, 1, 0);
                intrinsicsRotated.put(2, 2, 1);
            }
            else
            {
                //Debug.Log("Check No rotate");
                imageRotated = imageOrigin;
                intrinsicsRotated = intrinsicsOrigin;
            }
        }

        /// <summary>
        /// 处理翻转，包括图片和相机内参
        /// </summary>
        /// <param name="imageOrigin"></param>
        /// <param name="intrinsicsOrigin"></param>
        /// <param name="flipHori"></param>
        /// <param name="flipVert"></param>
        /// <param name="imageFlipped"></param>
        /// <param name="intrinsicsFlipped"></param>
        public static void CheckForFlip(Mat imageOrigin, Mat intrinsicsOrigin, bool flipHori, bool flipVert, out Mat imageFlipped, out Mat intrinsicsFlipped)
        {
            int texW = imageOrigin.cols();
            int texH = imageOrigin.rows();

            if (flipHori && flipVert)
            {
                imageFlipped = Mat.zeros(texH, texW, imageOrigin.type());
                Core.flip(imageOrigin, imageFlipped, -1);
            }
            else if (flipHori)
            {
                imageFlipped = Mat.zeros(texH, texW, imageOrigin.type());
                Core.flip(imageOrigin, imageFlipped, 1);
            }
            else if (flipVert)
            {
                imageFlipped = Mat.zeros(texH, texW, imageOrigin.type());
                Core.flip(imageOrigin, imageFlipped, 0);
            }
            else
            {
                imageFlipped = imageOrigin;
                //Debug.Log("Check No flip");
            }
            intrinsicsFlipped = GetFlipIntrinsicsMat(imageOrigin, intrinsicsOrigin, flipHori, flipVert);
        }


        /// <summary>
        /// 图片翻转时，对相机内参做处理
        /// </summary>
        /// <param name="imageOrigin"></param>
        /// <param name="intrinsicsOrigin"></param>
        /// <param name="flipHori"></param>
        /// <param name="flipVert"></param>
        /// <returns></returns>
        public static Mat GetFlipIntrinsicsMat(Mat imageOrigin, Mat intrinsicsOrigin, bool flipHori, bool flipVert)
        {
            int texW = imageOrigin.cols();
            int texH = imageOrigin.rows();

            float focalLengthX = GetFloatFromMat33(intrinsicsOrigin, 0, 0);
            float focalLengthY = GetFloatFromMat33(intrinsicsOrigin, 1, 1);
            float principalPointX = GetFloatFromMat33(intrinsicsOrigin, 0, 2);
            float principalPointY = GetFloatFromMat33(intrinsicsOrigin, 1, 2);

            var intrinsicsFlipped = Mat.zeros(3, 3, CvType.CV_32FC1);
            if (flipHori && flipVert)
            {
                intrinsicsFlipped.put(0, 0, focalLengthX);
                intrinsicsFlipped.put(0, 1, 0);
                intrinsicsFlipped.put(0, 2, texW - principalPointX);
                intrinsicsFlipped.put(1, 0, 0);
                intrinsicsFlipped.put(1, 1, focalLengthY);
                intrinsicsFlipped.put(1, 2, texH - principalPointY);
                intrinsicsFlipped.put(2, 0, 0);
                intrinsicsFlipped.put(2, 1, 0);
                intrinsicsFlipped.put(2, 2, 1);
            }
            else if (flipHori)
            {
                intrinsicsFlipped.put(0, 0, focalLengthX);
                intrinsicsFlipped.put(0, 1, 0);
                intrinsicsFlipped.put(0, 2, texW - principalPointX);
                intrinsicsFlipped.put(1, 0, 0);
                intrinsicsFlipped.put(1, 1, focalLengthY);
                intrinsicsFlipped.put(1, 2, principalPointY);
                intrinsicsFlipped.put(2, 0, 0);
                intrinsicsFlipped.put(2, 1, 0);
                intrinsicsFlipped.put(2, 2, 1);
            }
            else if (flipVert)
            {
                intrinsicsFlipped.put(0, 0, focalLengthX);
                intrinsicsFlipped.put(0, 1, 0);
                intrinsicsFlipped.put(0, 2, principalPointX);
                intrinsicsFlipped.put(1, 0, 0);
                intrinsicsFlipped.put(1, 1, focalLengthY);
                intrinsicsFlipped.put(1, 2, texH - principalPointY);
                intrinsicsFlipped.put(2, 0, 0);
                intrinsicsFlipped.put(2, 1, 0);
                intrinsicsFlipped.put(2, 2, 1);
            }
            else
            {
                intrinsicsFlipped.put(0, 0, focalLengthX);
                intrinsicsFlipped.put(0, 1, 0);
                intrinsicsFlipped.put(0, 2, principalPointX);
                intrinsicsFlipped.put(1, 0, 0);
                intrinsicsFlipped.put(1, 1, focalLengthY);
                intrinsicsFlipped.put(1, 2, principalPointY);
                intrinsicsFlipped.put(2, 0, 0);
                intrinsicsFlipped.put(2, 1, 0);
                intrinsicsFlipped.put(2, 2, 1);
            }
            return intrinsicsFlipped;
        }

        /// <summary>
        /// 从Mat33中读取参数，形成相机内参的float数组
        /// </summary>
        /// <param name="intrinsicsMat"></param>
        /// <returns></returns>
        public static float[,] ConvertIntrinsicsMatToFloatArray(Mat intrinsicsMat)
        {
            float focalLengthX = GetFloatFromMat33(intrinsicsMat, 0, 0);
            float focalLengthY = GetFloatFromMat33(intrinsicsMat, 1, 1);
            float principalPointX = GetFloatFromMat33(intrinsicsMat, 0, 2);
            float principalPointY = GetFloatFromMat33(intrinsicsMat, 1, 2);

            float[,] intrinsics = new float[3, 3] {
                    { focalLengthX, 0, principalPointX },
                    { 0, focalLengthY, principalPointY },
                    { 0, 0, 1 } };
            return intrinsics;
        }

        /// <summary>
        /// 从Mat33中读取参数，形成相机内参的float数组
        /// </summary>
        /// <param name="intrinsicsMat"></param>
        /// <returns></returns>
        public static string ConvertIntrinsicsMatToString(Mat intrinsicsMat)
        {
            StringBuilder sb = new StringBuilder();
            float[,] intrinsics = ConvertIntrinsicsMatToFloatArray(intrinsicsMat);
            for (int i = 0; i < 3; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    sb.Append(intrinsics[i, k]);
                    if(k < 2)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        sb.Append("; ");
                    }
                }
            }
            return sb.ToString();
        }


        public static byte[] GrayImageMatToJpgBytes(Mat grayMat)
        {
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 90);
            MatOfByte jpgMat = new MatOfByte();
            if (Imgcodecs.imencode(".jpeg", grayMat, jpgMat, compressionParams))
            {
                return jpgMat.toArray();
            }
            else
            {
                return null;
            }
        }



        public static byte[] YUV420_2_RGB24(Texture2D texture2D) {
            Mat yuvMat = new Mat(texture2D.height, texture2D.width, CvType.CV_8UC4);
            Utils.texture2DToMat(texture2D, yuvMat);
            Mat grayMat = new Mat(texture2D.height, texture2D.width, CvType.CV_8UC1);
            Imgproc.cvtColor(yuvMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            //Imgproc.cvtColor(Imgproc.color_yuv4202rgb);

            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 90);
            MatOfByte jpgMat = new MatOfByte();
            if (Imgcodecs.imencode(".jpeg", grayMat, jpgMat, compressionParams)) {
                return jpgMat.toArray();
            } else {
                return null;
            }
        }



        //byte[]转换为Intptr
        public static IntPtr BytesToIntptr(byte[] bytes) {
            //int size = bytes.Length;
            //IntPtr buffer = Marshal.AllocHGlobal(size);
            //try {
            //    Marshal.Copy(bytes, 0, buffer, size);
            //    return buffer;
            //} finally {
            //    //Debug.Log("BytesToIntptr.finaly");
            //    //Marshal.FreeHGlobal(buffer);
            //}
            return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        }

        public static IntPtr ArrayToIntptr(Array array) {
            return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
        }

        public static byte[] IntPtrToBytes(IntPtr imagePtr, int imageSize) {
            byte[] imageBytes = new byte[imageSize];
            Marshal.Copy(imagePtr, imageBytes, 0, imageSize);
            return imageBytes;
        }

        public static IntPtr FloatArrayToIntPtr(float[] floatArray) {
            int size = sizeof(float) * floatArray.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(floatArray, 0, ptr, floatArray.Length);
            return ptr;
        }

        public static float[] IntPtrToFloatArray(IntPtr ptr, int count) {
            float[] image2DPoints = new float[count];
            Marshal.Copy(ptr, image2DPoints, 0, count);
            //Marshal.PtrToStructure<float[]>(ptr, image2DPoints);
            return image2DPoints;
        }

        public static Texture2D LoadLocalImage(string path) {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);
            byte[] imgBytes = new byte[fileStream.Length];
            fileStream.Read(imgBytes, 0, (int)fileStream.Length);
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;
            Texture2D texture = new Texture2D(1280, 720);
            texture.LoadImage(imgBytes);
            return texture;
        }

        public static byte[] GetFileStream(string path) {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);
            byte[] imgBytes = new byte[fileStream.Length];
            fileStream.Read(imgBytes, 0, (int)fileStream.Length);
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;
            return imgBytes;
        }
        #endregion of Texture Format Compress


        #region Debug String Format

        public static string ListVectro2ToString(List<Vector2> listV2) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < listV2.Count; i++) {
                sb.Append(listV2[i].ToString("f2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string ListVector3ToString(List<Vector3> listV3) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < listV3.Count; i++) {
                sb.Append(listV3[i].ToString("f2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string ListDoubleToString(List<double> listDouble) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < listDouble.Count; i++) {
                sb.Append(listDouble[i].ToString("f2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string ListFloatToString(List<float> listFloat) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < listFloat.Count; i++) {
                sb.Append(listFloat[i].ToString("f2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string ArrayFloatToString(float[] arrayFloat) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arrayFloat.Length; i++) {
                sb.Append(arrayFloat[i].ToString("f2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string OneDimensionArrayToString(string[] arr) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length - 1; i++) {
                sb.Append(arr[i]);
                sb.Append(" ");
            }
            sb.Append(arr[arr.Length - 1]);
            return sb.ToString();
        }

        public static string TwoDimensionArrayToString(float[,] arr) {
            StringBuilder sb = new StringBuilder();
            int coutRow = arr.GetLength(0);
            int countColumn = arr.GetLength(1);

            for (int i = 0; i < coutRow; i++) {
                for (int j = 0; j < countColumn; j++) {
                    sb.Append(arr[i, j]);
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }



        //自定义输出字符数组方法
        public static string GetBytesString(byte[] bytes) {
            return GetBytesString(bytes, 0, bytes.Length, " ");
        }

        public static string GetBytesString(byte[] bytes, int index, int count, string sep) {
            if (count > bytes.Length) count = bytes.Length;

            var sb = new StringBuilder();
            for (int i = index; i < count - 1; i++) {
                sb.Append(bytes[i].ToString("X2") + sep);
            }
            sb.Append(bytes[index + count - 1].ToString("X2"));
            return sb.ToString();
        }
    }
    #endregion of Debug String Format
}