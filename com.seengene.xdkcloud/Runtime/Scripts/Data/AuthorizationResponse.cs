using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Seengene.XDK
{
    public class AuthorizationResponse
    {
        public string sessionID;
        public Int32 status;
        public double scale;

        // Define
        private const byte ResponseType_SessionId = 0;   // session id
        private const byte ResponseType_Status = 1;      // 请求地图结果状态码
        private const byte ResponseType_Scale = 2;       // 地图缩放值，暂时没有使用

        public AuthorizationResponse()
        {

        }


        public void ReadFromBytes(byte[] buffer)
        {
            using (MemoryStream mMemStream = new MemoryStream(buffer))
            {
                using (BinaryReader mBinaryReader = new BinaryReader(mMemStream, System.Text.Encoding.ASCII))
                {
                    while (mBinaryReader.BaseStream.Position < buffer.Length)
                    {
                        byte type = mBinaryReader.ReadByte();
                        int len = mBinaryReader.ReadInt32();
                        switch (type)
                        {
                            case ResponseType_SessionId:
                                byte[] bytes = mBinaryReader.ReadBytes(len);
                                sessionID = System.Text.Encoding.UTF8.GetString(bytes);
                                break;
                            case ResponseType_Status:
                                status = mBinaryReader.ReadInt32();
                                break;
                            case ResponseType_Scale:
                                scale = mBinaryReader.ReadDouble();
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
            return string.Format("AuthorizationResponse status:{0}, scale:{1}, sessionID:{2}",
                status, scale, sessionID);
        }
    }


}

