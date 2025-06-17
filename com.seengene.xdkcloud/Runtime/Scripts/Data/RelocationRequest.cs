using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


namespace Seengene.XDK
{
    public class RelocationRequest 
    {
        public string sessionID;
        public Int32 seq;
        public Matrix4x4 cameraPose;
        public float[,] cameraIntrinsics;
        public byte[] imageBytes;
        public Vector3 cameraPosition;
        public Vector3 cameraEulerAngles;
        public int frameIndex; // 本地使用，不涉及服务器

        public bool isGpsOK;
        public double longitude;
        public double latitude;
        public double gpsPrecision;
        public float gpsDirection;

        // Request
        private const byte RequestType_SessionId = 0;    // session id
        private const byte RequestType_Seq = 1;          // 一个session内的顺序码
        private const byte RequestType_Pose = 2;         // 当前追踪的位姿
        private const byte RequestType_Intrinsics = 3;   // 当前图像的内参
        private const byte RequestType_Image = 4;        // 图像数据
        private const byte RequestType_MapID = 5;        // 地图id
        private const byte RequestType_CameraModel = 6;        // 相机型号
        private const byte RequestType_CameraDitortion = 7;        // 相机畸变参数（眼镜使用）
        private const byte RequestType_GPSValue = 8;        // GPS坐标
        private const byte RequestType_GPSDirection = 9;        // GPS朝向


        /// <summary>
        /// 将定位请求序列化成字节数组
        /// </summary>
        /// <param name="uploadImageItem"></param>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            using (MemoryStream mMemStream = new MemoryStream())
            {
                using (BinaryWriter mBinaryWriter = new BinaryWriter(mMemStream, System.Text.Encoding.ASCII))
                {
                    // write sessionID
                    mBinaryWriter.Write(RequestType_SessionId);
                    byte[] bytesSessionID = System.Text.Encoding.ASCII.GetBytes(sessionID);
                    mBinaryWriter.Write(bytesSessionID.Length);
                    mBinaryWriter.Write(bytesSessionID);
                    // write seq
                    mBinaryWriter.Write(RequestType_Seq);
                    mBinaryWriter.Write(4);
                    mBinaryWriter.Write(seq);
                    // write pose
                    mBinaryWriter.Write(RequestType_Pose);
                    mBinaryWriter.Write(4 * 16);
                    for (int x = 0; x < 4; x++)
                    {
                        for (int y = 0; y < 4; y++)
                        {
                            mBinaryWriter.Write(cameraPose[x, y]);
                        }
                    }
                    // write intrinsics
                    mBinaryWriter.Write(RequestType_Intrinsics);
                    mBinaryWriter.Write(4 * 9);
                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            mBinaryWriter.Write(cameraIntrinsics[x, y]);
                        }
                    }
                    // write image
                    mBinaryWriter.Write(RequestType_Image);
                    mBinaryWriter.Write(imageBytes.Length);
                    mBinaryWriter.Write(imageBytes);

                    if (isGpsOK)
                    {
                        mBinaryWriter.Write(RequestType_GPSValue);
                        mBinaryWriter.Write(8 * 3);
                        mBinaryWriter.Write(longitude);
                        mBinaryWriter.Write(latitude);
                        mBinaryWriter.Write(gpsPrecision);

                        mBinaryWriter.Write(RequestType_GPSDirection);
                        mBinaryWriter.Write(4);
                        mBinaryWriter.Write(gpsDirection);
                    }

                    byte[] postBytes = mMemStream.ToArray();
                    return postBytes;
                }
            }
        }



        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("seq=" + seq);
            sb.Append(" sessionID=" + sessionID);
            sb.Append(" cameraPose=" + cameraPose.ToString());
            if (cameraIntrinsics != null)
            {
                sb.Append(" cameraIntrinsics");
                sb.Append(" Intrinsics[0,0]=" + cameraIntrinsics[0, 0]);
                sb.Append(" Intrinsics[0,1]=" + cameraIntrinsics[0, 1]);
                sb.Append(" Intrinsics[0,2]=" + cameraIntrinsics[0, 2]);
                sb.Append(" Intrinsics[1,0]=" + cameraIntrinsics[1, 0]);
                sb.Append(" Intrinsics[1,1]=" + cameraIntrinsics[1, 1]);
                sb.Append(" Intrinsics[1,2]=" + cameraIntrinsics[1, 2]);
                sb.Append(" Intrinsics[2,0]=" + cameraIntrinsics[2, 0]);
                sb.Append(" Intrinsics[2,1]=" + cameraIntrinsics[2, 1]);
                sb.Append(" Intrinsics[2,2]=" + cameraIntrinsics[2, 2]);
            }
            else
            {
                sb.Append(" cameraIntrinsics=null");
            }


            if (imageBytes != null)
            {
                sb.Append(" imageBytes.Length="+ imageBytes.Length);
            }
            else
            {
                sb.Append(" imageBytes=null");
            }
            sb.Append(" isGpsOK=" + isGpsOK);
            sb.Append(" longitude=" + longitude);
            sb.Append(" latitude=" + latitude);
            sb.Append(" gpsPrecision=" + gpsPrecision);
            sb.Append(" gpsDirection=" + gpsDirection);
            return sb.ToString();
        }


    }
}
