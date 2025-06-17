using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


namespace Seengene.XDK
{
    public class XDKSimulator : XDKHardware
    {

        [SerializeField]
        private XDKCloudSession m_Session;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private bool everyFrame;
        private bool IsReady;
        private bool stopped;
        private Coroutine corWriteFile;
        private float authSuccTime = -1;
        private string sessionID;
        private int lastFrameIndex;

        private XDKSimulatorData dataSource;
        private XDKFrameData newFrameData;
        private bool PoseUsed;

        

        private Dictionary<int, Pose> dic = new Dictionary<int, Pose>();

#if UNITY_EDITOR

        private void Awake()
        {
            m_Session.StartOnMonoStart = false;
            m_Session.xdkHardware = this;
            XDKCloudSession.IfSaveImages = true;
            XDKCloudSession.IfDebugOn = true;
        }

        private void Start()
        {
            authSuccTime = -1;

            dataSource = GetComponent<XDKSimulatorData>();
            dataSource.InitDatas();

            var mapItem = new XDKMapItem();
            mapItem.MapIdDaytime = dataSource.GetMapID();
            mapItem.UseTwoMaps = false;
            XDKCloudSession.Instance.mapItem = mapItem;
            XDKCloudSession.Instance.OnAuthSuccEvent = OnAuthSucc;
            XDKCloudSession.Instance.EvtScenePoseUsed.RemoveAllListeners();
            XDKCloudSession.Instance.EvtScenePoseUsed.AddListener(OnScenePoseUsed);
            XDKCloudSession.Instance.StartWork();
        }


        private void Update()
        {
            if (stopped)
            {
                return;
            }
            if (authSuccTime >= 0)
            {
                newFrameData = null;
                if (everyFrame)
                {
                    if (lastFrameIndex < dataSource.GetFrameCount())
                    {
                        newFrameData = dataSource.GetFrame(lastFrameIndex);
                    }
                }
                else
                {
                    newFrameData = dataSource.GetFrameDataByTime(authSuccTime);
                }

                if (newFrameData != null)
                {
                    if (newFrameData.rgbTexture == null)
                    {
                        dataSource.LoadImage(newFrameData);
                        Debug.Log($"Create frameData, seq={newFrameData.index} position={newFrameData.cameraPosition} euler={newFrameData.cameraEuler}");
                    }
                    IsReady = true;
                }
                else
                {
                    if(corWriteFile == null)
                    {
                        corWriteFile = StartCoroutine(delayToOutputFile());
                    }
                }

                dataSource.UpdateCameraPose(_camera, authSuccTime);
            }

        }

#endif


        private void OnAuthSucc(string session)
        {
            authSuccTime = Time.time;
            sessionID = session;
            lastFrameIndex = 0;
        }



        private void OnScenePoseUsed(int frameIndex, Pose pose)
        {
            Debug.Log("use scenePose add to dic, frameIndex=" + frameIndex);
            dic[frameIndex] = pose;
        }


        private IEnumerator delayToOutputFile()
        {
            yield return new WaitForSeconds(5);
            var mapID = dataSource.GetMapID();
            WriteOutputFile($"map_{mapID}_session_{sessionID}.txt");
            stopped = true;
        }

        private void WriteOutputFile(string fileName)
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                var str = "map, session, oldSession\n";
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                fs.Write(bytes);

                str = $"{dataSource.GetMapID()} {sessionID} {dataSource.GetOldSession()}\n\n";
                bytes = Encoding.UTF8.GetBytes(str);
                fs.Write(bytes);

                str = "seq, cameraPositionInWorld(3), cameraEulerInWorld(3), cameraPositionInScene(3), cameraEulerInScene(3), keyFrame(1) \n";
                bytes = Encoding.UTF8.GetBytes(str);
                fs.Write(bytes);

                PoseUsed = false;
                int count = dataSource.GetFrameCount();
                for (int i = 0; i < count; i++)
                {
                    bytes = GetFrameData(i);
                    fs.Write(bytes);
                }
                fs.Flush();
                fs.Close();
            }
            Debug.Log("output to file, path=" + filePath);
        }

        private byte[] GetFrameData(int index)
        {
            XDKFrameData data = dataSource.GetFrame(index);
            StringBuilder sb = new StringBuilder();
            sb.Append(data.index);
            sb.Append(' ');
            sb.Append(data.cameraPosition.x);
            sb.Append(' ');
            sb.Append(data.cameraPosition.y);
            sb.Append(' ');
            sb.Append(data.cameraPosition.z);
            sb.Append(' ');
            sb.Append(data.cameraEuler.x);
            sb.Append(' ');
            sb.Append(data.cameraEuler.y);
            sb.Append(' ');
            sb.Append(data.cameraEuler.z);
            int keyFrame = 0;
            if (dic.ContainsKey(data.index))
            {
                var pose = dic[data.index];
                transform.position = pose.position;
                transform.rotation = pose.rotation;
                PoseUsed = true;
                keyFrame = 1;
            }
            if (PoseUsed)
            {
                var posInScene = transform.InverseTransformPoint(data.cameraPosition);
                var rotInScene = transform.InverseTransformDirection(data.cameraEuler);
                rotInScene = ObjectTool.ClampEuler(rotInScene);
                sb.Append(' ');
                sb.Append(posInScene.x);
                sb.Append(' ');
                sb.Append(posInScene.y);
                sb.Append(' ');
                sb.Append(posInScene.z);
                sb.Append(' ');
                sb.Append(rotInScene.x);
                sb.Append(' ');
                sb.Append(rotInScene.y);
                sb.Append(' ');
                sb.Append(rotInScene.z);
                sb.Append(' ');
                sb.Append(keyFrame);
            }

            sb.Append('\n');
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return bytes;
        }






        public override Camera GetCurrentCamera()
        {
            return _camera;
        }

        public override void GetFrameData(Action<XDKFrameData> callback)
        {
            if (newFrameData != null)
            {
                callback?.Invoke(newFrameData);
                IsReady = false;
                lastFrameIndex++;
            }
        }

        public override string GetName()
        {
            return "Simulator";
        }

        public override bool IsDataReady()
        {
            return IsReady;
        }

        public override void StartWork()
        {
            stopped = false;
            authSuccTime = -1;
            IsReady = false;
            lastFrameIndex = 0;
            dic.Clear();
            Debug.Log("Simulator startwork");
        }

        public override void StopWork()
        {
            Debug.Log("Simulator stopwork");
            stopped = true;
        }




    }
}
