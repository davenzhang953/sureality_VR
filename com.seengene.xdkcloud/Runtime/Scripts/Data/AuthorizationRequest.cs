using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Seengene.XDK
{
    public class AuthorizationRequest 
    {
        public string mapId ;
        public string sdkVersion;
        public string deviceInfo;
        public string bundleID;
        public string secretKey;


        // define
        private const byte RequestType_MapId = 0;        // 地图ID
        private const byte RequestType_DeviceInfo = 1;        // DeviceInfo
        private const byte RequestType_SdkVersion = 2;        // SdkVersion
        private const byte RequestType_PackageName = 3;        // package name
        private const byte RequestType_SecretKey = 4;        // developer's secret key


        public byte[] ToByteArray()
        {
            using (MemoryStream mMemStream = new MemoryStream())
            {
                using (BinaryWriter mBinaryWriter = new BinaryWriter(mMemStream, System.Text.Encoding.ASCII))
                {
                    mBinaryWriter.Write(RequestType_MapId);// mapid
                    byte[] bytes = System.Text.Encoding.ASCII.GetBytes(mapId);
                    mBinaryWriter.Write(bytes.Length);
                    mBinaryWriter.Write(bytes);

                    mBinaryWriter.Write(RequestType_DeviceInfo); // deviceInfo
                    bytes = System.Text.Encoding.ASCII.GetBytes(deviceInfo);
                    mBinaryWriter.Write(bytes.Length);
                    mBinaryWriter.Write(bytes);

                    mBinaryWriter.Write(RequestType_SdkVersion); // sdk version
                    bytes = System.Text.Encoding.ASCII.GetBytes(sdkVersion);
                    mBinaryWriter.Write(bytes.Length);
                    mBinaryWriter.Write(bytes);

                    mBinaryWriter.Write(RequestType_PackageName); // bundle id
                    bytes = System.Text.Encoding.ASCII.GetBytes(bundleID);
                    mBinaryWriter.Write(bytes.Length);
                    mBinaryWriter.Write(bytes);

                    mBinaryWriter.Write(RequestType_SecretKey); // developer's secret key
                    bytes = System.Text.Encoding.ASCII.GetBytes(secretKey);
                    mBinaryWriter.Write(bytes.Length);
                    mBinaryWriter.Write(bytes);

                    return mMemStream.ToArray();
                }
            }
        }
    }
}
