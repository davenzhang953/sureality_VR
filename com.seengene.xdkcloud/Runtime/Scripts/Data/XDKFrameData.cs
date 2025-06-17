using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seengene.XDK
{
    /// <summary>
    /// 相机畸变参数（一般给鱼眼相机使用）
    /// </summary>
    public class DistortionData
    {
        public float K1;
        public float K2;
        public float K3;
        public float K4;
    }


    public class XDKFrameData
    {
        public int index;               // 帧数据的序列号
        public Texture2D rgbTexture;    // 相机图片
        public byte[] textureDataRGB24; // RGB（3通道）的相机图片数据
        public byte[] textureDataR8;    // 灰度（单通道）的相机图片数据
        public bool flipHorizontal; // 图片是否需要水平翻转
        public bool flipVertical;   // 图片是否需要竖直翻转
        public int textureWidth;    // 相机图片的宽度
        public int textureHeight;   // 相机图片的高度

        public int cameraIntrinsicsImgW; // 相机内参对应的图片宽度
        public int cameraIntrinsicsImgH; // 相机内参对应的图片高度
        public float focalLengthX;       // 相机内参，焦距x
        public float focalLengthY;       // 相机内参，焦距y
        public float principalPointX;    // 相机内参，主点位置x
        public float principalPointY;    // 相机内参，主点位置y

        public bool hasDistortion;         // 图片是否有畸变
        public DistortionData distortData; // 图片的畸变参数

        public bool IsPortraitImage; // 图片是否是竖着的

        public Vector3 cameraPosition; // 成像时相机在AR空间中的位置
        public Vector3 cameraEuler;    // 成像时相机在AR空间中的角度

        public string extraInfo;   // 额外参数，这个参数不会被上发到定位服务器


        /// <summary>
        /// 检查图片朝向
        /// </summary>
        /// <returns></returns>
        public bool CheckImageDirection()
        {
            if (rgbTexture != null)
            {
                textureWidth = rgbTexture.width;
                textureHeight = rgbTexture.height;
            }

            bool needExchange = false;
            if (textureWidth < textureHeight)
            {
                if (cameraIntrinsicsImgW > cameraIntrinsicsImgH)
                    needExchange = true;
            }
            else
            {
                if (cameraIntrinsicsImgW < cameraIntrinsicsImgH)
                    needExchange = true;
            }


            if (needExchange)
            {
                int a = cameraIntrinsicsImgW;
                cameraIntrinsicsImgW = cameraIntrinsicsImgH;
                cameraIntrinsicsImgH = a;

                float b = focalLengthX;
                focalLengthX = focalLengthY;
                focalLengthY = b;

                float c = principalPointX;
                principalPointX = principalPointY;
                principalPointY = c;
            }
            return needExchange;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("index="+index);
            if (rgbTexture != null)
            {
                sb.Append(" rgbTexture");
                sb.Append(" texW=" + rgbTexture.width);
                sb.Append(" texH=" + rgbTexture.height);
            }
            else if(textureDataRGB24 != null)
            {
                sb.Append(" textureDataRGB24");
                sb.Append(" data.Length=" + textureDataRGB24.Length);
                sb.Append(" texW=" + textureWidth);
                sb.Append(" texH=" + textureHeight);
            }
            else if(textureDataR8 != null)
            {
                sb.Append(" textureDataR8");
                sb.Append(" data.Length=" + textureDataR8.Length);
                sb.Append(" texW=" + textureWidth);
                sb.Append(" texH=" + textureHeight);
            }
            else
            {
                sb.Append(" NoneCameraImage");
            }
            sb.Append(" rgbTexFlipHorizontal=" + flipHorizontal);
            sb.Append(" rgbTexFlipVertical=" + flipVertical);
            sb.Append(" cameraIntrinsicsImgW=" + cameraIntrinsicsImgW);
            sb.Append(" cameraIntrinsicsImgH=" + cameraIntrinsicsImgH);
            sb.Append(" focalLengthX=" + focalLengthX);
            sb.Append(" focalLengthY=" + focalLengthY);
            sb.Append(" principalPointX=" + principalPointX);
            sb.Append(" principalPointY=" + principalPointY);
            if (distortData != null)
            {
                sb.Append(" distortionK1=" + distortData.K1);
                sb.Append(" distortionK2=" + distortData.K2);
                sb.Append(" distortionK3=" + distortData.K3);
                sb.Append(" distortionK4=" + distortData.K4);
            }
            sb.Append(" IsPortraitImage=" + IsPortraitImage);
            sb.Append(" cameraPosition=" + cameraPosition.ToString("f2"));
            sb.Append(" cameraEuler=" + cameraEuler.ToString("f2"));
            sb.Append(" extraInfo=" + extraInfo);
            return sb.ToString();
        }



    }
}
