using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Seengene.XDK
{
    [System.Serializable]
    public class SessionObject
    {
        public long mapID;
        public string sessionID;

        public SessionObject(long _mapID, string _sessionID)
        {
            mapID = _mapID;
            sessionID = _sessionID;
        }
    }

    public class XDKSimulatorData : MonoBehaviour
    {
        [SerializeField]
        private SessionObject[] MapAndSessions = new SessionObject[]
        {
            new SessionObject( 57088600544736, "7570053781755540"),
            new SessionObject( 85505295866336, "4700260884661119"),
            new SessionObject( 81664020700640, "3056147410820619"),
            
        };

        [SerializeField]
        private string DataResourceFolder = "/Users/cuilichen/Documents/work_unity/XDK-Cloud-Package2/Simulator_test_data/";

        [SerializeField]
        private int dataIndex = 0;

        private List<XDKFrameData> allFrames = new();

        private Texture2D texImage1;


        private long mapID;
        private string currentSession;



        public bool InitDatas()
        {
            texImage1 = new Texture2D(4, 4);

            if (dataIndex < MapAndSessions.Length)
            {
                var MapConfig = MapAndSessions[dataIndex];
                currentSession = MapConfig.sessionID;
                mapID = MapConfig.mapID;
                List<string> allFiles = ListAllFiles(DataResourceFolder + currentSession);
                
                for (int i = 0; i < allFiles.Count; i++)
                {
                    var txtFile = allFiles[i];
                    var jpgFile = allFiles[i].Replace(".txt", ".jpg");
                    if (System.IO.File.Exists(jpgFile))
                    {
                        XDKFrameData frameData = ReadFrame(txtFile, i);
                        frameData.extraInfo = jpgFile;
                        frameData.index = GetSeqNum(txtFile);
                        allFrames.Add(frameData);
                    }
                }
                Debug.Log("allFrames.Count=" + allFrames.Count);
                return allFrames.Count > 0;
            }
            return false;
        }


        private List<string> ListAllFiles(string folder)
        {
            List<string> allFiles = new List<string>();
            if (System.IO.Directory.Exists(folder))
            {
                Regex regex = new Regex(@"/\d+_\d\.txt$");
                var arr = System.IO.Directory.GetFiles(folder).Where(a=>regex.IsMatch(a));
                allFiles.AddRange(arr);
                allFiles.Sort(CompareFile);

                
                //for (int i = 0; i < allFiles.Count; i++)
                //{
                //    Debug.Log($"{i} {allFiles[i]}");
                //}
            }
            return allFiles;
        }

        private int GetSeqNum(string a)
        {
            var shortName1 = System.IO.Path.GetFileName(a);
            int index1 = shortName1.IndexOf('_');
            if(index1 < 0)
            {
                throw new System.Exception("File name format error");
            }
            int num1 = int.Parse(shortName1.Substring(0, index1));
            return num1;
        }

        private int CompareFile(string a, string b)
        {
            int num1 = GetSeqNum(a);
            int num2 = GetSeqNum(b);
            if (num1 < num2)
            {
                return -1;
            }
            else if (num1 > num2)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public long GetMapID()
        {
            return mapID;
        }

        public string GetOldSession()
        {
            return currentSession;
        }

        public int GetFrameCount()
        {
            return allFrames.Count;
        }

        public XDKFrameData GetFrame(int index)
        {
            return allFrames[index];
        }

        public XDKFrameData GetFrameDataByTime(float startTime)
        {
            float time = Time.time - startTime;
            if (time < allFrames.Count)
            {
                int index = (int)time;
                return allFrames[index];
            }
            return null;
        }


        public void UpdateCameraPose(Camera camera, float startTime)
        {
            float time = Time.time - startTime;
            if (time < allFrames.Count)
            {
                int index = (int)time;
                XDKFrameData data = allFrames[index];
                camera.transform.position = data.cameraPosition;
                camera.transform.eulerAngles = data.cameraEuler;
            }
        }


        public void LoadImage(XDKFrameData frameData)
        {
            byte[] data = System.IO.File.ReadAllBytes(frameData.extraInfo);
            texImage1.LoadImage(data);
            texImage1.Apply();

            frameData.rgbTexture = texImage1;
            frameData.textureWidth = texImage1.width;
            frameData.textureHeight = texImage1.height;


            frameData.flipVertical = true;
            frameData.principalPointY = frameData.textureHeight - frameData.principalPointY;
        }



        private XDKFrameData ReadFrame(string txtFile, int seq)
        {
            XDKFrameData frameData = new XDKFrameData();
            double[] txtData = readTxtData(txtFile);
            frameData.cameraIntrinsicsImgW = 1280;
            frameData.cameraIntrinsicsImgH = 720;
            frameData.focalLengthX = (float)txtData[16];
            frameData.focalLengthY = (float)txtData[20];
            frameData.principalPointX = (float)txtData[22];
            frameData.principalPointY = (float)txtData[23];

            Matrix4x4 matrix = GetCameraMatrix(txtData);
            Vector3 cameraPosition = matrix.GetPosition();
            cameraPosition.z = -cameraPosition.z;
            Quaternion cameraRotation = matrix.GetRotation();
            cameraRotation.z = -cameraRotation.z;
            cameraRotation.w = -cameraRotation.w;

            frameData.cameraPosition = cameraPosition;
            frameData.cameraEuler = cameraRotation.eulerAngles;

            // image is null
           

            return frameData;
        }



        private double[] readTxtData(string path)
        {
            //Debug.Log(path);
            string content = System.IO.File.ReadAllText(path);
            return stringToDoubleArr(content);
        }

        private double[] stringToDoubleArr(string content)
        {
            string[] arr = content.Split(" ");
            List<double> list = new List<double>();
            for (int i = 0; i < arr.Length; i++)
            {
                //if (string.IsNullOrEmpty(arr[i]))
                //    continue;
                if(double.TryParse(arr[i], out double val))
                {
                    list.Add(val);
                }
                else
                {
                    Debug.Log("i=" + i + " val=" + arr[i]);
                }
            }
            return list.ToArray();
        }


        private Matrix4x4 GetCameraMatrix(double[] pose)
        {
            Matrix4x4 matrix = new Matrix4x4();
            matrix.m00 = (float)pose[0];
            matrix.m10 = (float)pose[1];
            matrix.m20 = (float)pose[2];
            matrix.m30 = (float)pose[3];

            matrix.m01 = (float)pose[4];
            matrix.m11 = (float)pose[5];
            matrix.m21 = (float)pose[6];
            matrix.m31 = (float)pose[7];

            matrix.m02 = (float)pose[8];
            matrix.m12 = (float)pose[9];
            matrix.m22 = (float)pose[10];
            matrix.m32 = (float)pose[11];

            matrix.m03 = (float)pose[12];
            matrix.m13 = (float)pose[13];
            matrix.m23 = (float)pose[14];
            matrix.m33 = (float)pose[15];
            return matrix;
        }





        private void testPoseMatrix()
        {
            var str = "0.9647004 0.04260756 0.2598802 0 -0.02858818 0.9979367 -0.05749042 0 -0.2617935 0.04803153 0.963928 0 " +
               "-0.1692172 -0.1770078 0.04273829 1 0 0 0 -1 -1";
            double[] arr = stringToDoubleArr(str);
            Matrix4x4 matrix = GetCameraMatrix(arr);
            //matrix = matrix.transpose;
            Vector3 cameraPosition = matrix.GetPosition();
            cameraPosition.z = -cameraPosition.z;
            Quaternion cameraRotation = matrix.GetRotation();
            cameraRotation.z = -cameraRotation.z;
            cameraRotation.w = -cameraRotation.w;

            Debug.Log("pos=" + cameraPosition.ToString("f5"));
            Debug.Log("rot=" + cameraRotation.ToString("f5"));
            Debug.Log("euler1=" + cameraRotation.eulerAngles.ToString("f5"));

            Quaternion quat = new Quaternion();
            quat.x = 0.02662603f;
            quat.y = 0.1316323f;
            quat.z = 0.0179646f;
            quat.w = 0.9907781f;
            Debug.Log("euler2=" + quat.eulerAngles.ToString("f5"));
        }

    }
}