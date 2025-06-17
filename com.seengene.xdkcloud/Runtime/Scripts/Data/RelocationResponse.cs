using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Seengene.XDK
{


    public class RelocationResponse
    {
        public Int32 status;
        public Int32 seq;
        public List<Vector2> point2d_vec = new List<Vector2>();
        public List<Vector3> point3d_vec = new List<Vector3>();
        public List<double> transform_ltg = new List<double>();
        public string extra_msg;
        public Matrix4x4 cameraPos;
        public Vector3 cameraPosition;
        public Vector3 cameraEulerAngles;
        public float[,] cameraIntrinsics;
        public string sessionID;

        public int frameIndex; // 本地使用，不涉及服务器
        public Vector3 scenePosition; // 本地使用，不涉及服务器
        public Quaternion sceneRotation; // 本地使用，不涉及服务器


        // Response
        public const byte ResponseType_Status = 0;       // 定位结果状态码
        public const byte ResponseType_Seq = 1;          // 针对session内这个seq请求的定位结果
        public const byte ResponseType_Point2dVec = 2;   // 2D点向量
        public const byte ResponseType_Point3dVec = 3;   // 3D点向量
        public const byte ResponseType_TransformLtg = 4; // local to global transform
        public const byte ResponseType_ExtraMsg = 5;     // 其他信息，说明用字符串

        public RelocationResponse()
        {

        }

        public void ReadFromBytes(byte[] buffer)
        {
            point2d_vec = new List<Vector2>();
            point3d_vec = new List<Vector3>();
            transform_ltg = new List<double>();
            extra_msg = string.Empty;


            using (MemoryStream mMemStream = new MemoryStream(buffer))
            {
                using (BinaryReader mBinaryReader = new BinaryReader(mMemStream, System.Text.Encoding.ASCII))
                {
                    while (mBinaryReader.BaseStream.Position < buffer.Length)
                    {
                        byte type = mBinaryReader.ReadByte();
                        int length = mBinaryReader.ReadInt32();
                        switch (type)
                        {
                            case ResponseType_Status:
                                status = mBinaryReader.ReadInt32();
                                break;
                            case ResponseType_Seq:
                                seq = mBinaryReader.ReadInt32();
                                break;
                            case ResponseType_Point2dVec:
                                int countV2 = length / 8;
                                for (int i = 0; i < countV2; i++)
                                {
                                    float x = mBinaryReader.ReadSingle();
                                    float y = mBinaryReader.ReadSingle();
                                    Vector2 v2 = new Vector2(x, y);
                                    point2d_vec.Add(v2);
                                }
                                break;
                            case ResponseType_Point3dVec:
                                int countV3 = length / 12;
                                for (int i = 0; i < countV3; i++)
                                {
                                    float x = mBinaryReader.ReadSingle();
                                    float y = mBinaryReader.ReadSingle();
                                    float z = mBinaryReader.ReadSingle();
                                    Vector3 v3 = new Vector3(x, y, z);
                                    point3d_vec.Add(v3);
                                }
                                break;
                            case ResponseType_TransformLtg:
                                for (int i = 0; i < 16; i++)
                                {
                                    double item = mBinaryReader.ReadDouble();
                                    transform_ltg.Add(item);
                                }
                                break;
                            case ResponseType_ExtraMsg:
                                byte[] bytes = mBinaryReader.ReadBytes(length);
                                extra_msg = System.Text.Encoding.UTF8.GetString(bytes);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return string.Format("RelocationResponse status:{0}, seq:{1}, point2d_vec:{2}, point3d_vec:{3}, transform_ltg:{4}, extra_msg:{5}",
                status, seq, XDKTools.ListVectro2ToString(point2d_vec), XDKTools.ListVector3ToString(point3d_vec), XDKTools.ListDoubleToString(transform_ltg), extra_msg);
        }
    }

}
