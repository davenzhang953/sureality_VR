using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seengene.XDK;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;
using System;
using UnityEngine.UI;

namespace Seengene.XDK
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARSessionOrigin))]
    public class ARFoundationDevice : XDKHardware
    {
        private static int dataIndex;

        [SerializeField]
        private ARCameraManager cameraManager;


        private Camera m_Camera;
        private bool isArFoundationNeedConfig;
        private bool waitForTracking;
        private float waitForStartTime;
        private Vector3 cameraPosition;
        private Vector3 cameraEulerAngles;
        private XRCameraIntrinsics cameraIntrinsics;
        private XRCpuImage cpuImage;
        private bool cpuImageReady;

        [SerializeField]
        private Transform verticalObj;

        //public Text textfield;

        private void Awake()
        {
            m_Camera = cameraManager.GetComponent<Camera>();

            isArFoundationNeedConfig = true;
            waitForTracking = true;

            if (verticalObj == null)
            {
                if (cameraManager.transform.childCount > 0)
                {
                    verticalObj = transform.GetChild(0);
                }
                else
                {
                    verticalObj = new GameObject().transform;
                }
            }
            verticalObj.SetParent(cameraManager.transform);
            verticalObj.localPosition = Vector3.zero;
            verticalObj.localEulerAngles = new Vector3(0, 0, 90);
            verticalObj.localScale = Vector3.one;

        }

        private void Update()
        {
            if (isArFoundationNeedConfig)
            {
                configARFoundation();
            }

            //if (textfield)
            //    textfield.text = m_Camera.transform.position.ToString("f3") + " " + m_Camera.transform.rotation.eulerAngles.ToString("f2") + " time=" + Time.time.ToString("f1");

            if (waitForTracking)
            {
                if (ARSession.state == ARSessionState.SessionTracking)
                {
                    waitForTracking = false;
                }
                else if (Time.time > waitForStartTime + 15f) // 超过15秒
                {
                    waitForTracking = false;
                }
                else
                {
                    return; // ARFoundation 还没有开始正常的跟踪，所以先不进行定位。
                }
            }
        }

        private void OnDestroy()
        {
            Debug.Log("ARFoundationDevice is Destroyed");
        }


        /// <summary>
        /// 给AR相机选择一组合适的配置
        /// </summary>
        private void configARFoundation()
        {
            using (var configs = cameraManager.GetConfigurations(Allocator.Temp))
            {
                if (configs.IsCreated && configs.Length > 0)
                {
                    // 查找这些配置中，最高的帧数
                    //Debug.Log($"camera config ----> total={configs.Length}");
                    int maxFrameRate = (int)configs[0].framerate;
                    for (int i = 0; i < configs.Length; i++)
                    {
                        var config = configs[i];
                        if (maxFrameRate < config.framerate)
                        {
                            maxFrameRate = (int)config.framerate;
                        }
                        //Debug.Log($"camera config {i} {config}");
                    }

                    // 将高帧数的那些配置放到list中。
                    List<XRCameraConfiguration> list = new List<XRCameraConfiguration>();
                    for (int i = 0; i < configs.Length; i++)
                    {
                        var config = configs[i];
                        if ((int)config.framerate == maxFrameRate)
                        {
                            list.Add(config);
                        }
                    }
                    list.Sort(compareConfig);

                    Debug.Log($"camera config ----> maxFrameRate={maxFrameRate} total={configs.Length} list.count={list.Count}");
                    for (int i = 0; i < list.Count; i++)
                    {
                        Debug.Log($"camera config {i} {list[i]}");
                    }

                    var configToUse = list[0];
                    if (list.Count >= 3)
                    {
                        configToUse = list[list.Count - 2];
                    }
                    if (XDKCloudSession.Instance)
                    {
                        if (XDKCloudSession.Instance.videoStreamingQuality == ARVideoQuality.High)
                        {
                            configToUse = list[list.Count - 1];
                        }
                    }
                    cameraManager.currentConfiguration = configToUse;
                    Debug.Log("camera config ----> use config=" + configToUse+", quality="+ XDKCloudSession.Instance.videoStreamingQuality);
                    isArFoundationNeedConfig = false;
                }
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private int compareConfig(XRCameraConfiguration a, XRCameraConfiguration b)
        {
            long val1 = a.width * 10000 + a.height;
            long val2 = b.width * 10000 + b.height;
            if (a.width > a.height)
            {
                val1 = a.height * 10000 + a.width;
                val2 = b.height * 10000 + b.width;
            }
            
            if (val1 < val2)
            {
                return -1;
            }
            else if (val1 > val2)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }


        public override Camera GetCurrentCamera()
        {
            return m_Camera;
        }

        public override Pose GetCameraPose()
        {
            Pose pose = new Pose();
            pose.position = m_Camera.transform.position;
            pose.rotation = m_Camera.transform.rotation;
            return pose;
        }


        public override bool IsDataReady()
        {
            if (isArFoundationNeedConfig)
            {
                return false;
            }
            if (waitForTracking)
            {
                return false;
            }
            if (gameObject.activeSelf)
            {
                if (cameraManager.TryGetIntrinsics(out cameraIntrinsics))
                {
                    cameraPosition = m_Camera.transform.position;
                    cameraEulerAngles = m_Camera.transform.eulerAngles;
                    if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                    {
                        cpuImage = image;
                        cpuImageReady = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public override void GetFrameData(Action<XDKFrameData> callback)
        {
            if (cpuImageReady)
            {
                XDKFrameData frameData = new XDKFrameData();
                StartCoroutine(GetImageAsync(frameData, callback));
            }
            else
            {
                Debug.LogError("CpuImage is null");
            }
        }


        private IEnumerator GetImageAsync(XDKFrameData frameData, Action<XDKFrameData> callback)
        {
            frameData.cameraIntrinsicsImgW = cameraIntrinsics.resolution.x;
            frameData.cameraIntrinsicsImgH = cameraIntrinsics.resolution.y;
            frameData.focalLengthX = cameraIntrinsics.focalLength.x;
            frameData.focalLengthY = cameraIntrinsics.focalLength.y;
            frameData.principalPointX = cameraIntrinsics.principalPoint.x;
            frameData.principalPointY = cameraIntrinsics.principalPoint.y;

            frameData.cameraPosition = cameraPosition;
            frameData.cameraEuler = cameraEulerAngles;

            int ww = cpuImage.width;
            int hh = cpuImage.height;
            int maxVal = Math.Max(ww, hh);
            float scale = 1280f / maxVal;
            if (Mathf.Abs(scale - 1) > 0.01f) // 需要进行缩放
            {
                hh = (int)(hh * scale);
                ww = (int)(ww * scale);
            }


            XRCpuImage.ConversionParams para = new XRCpuImage.ConversionParams();
            para.inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height);
            para.outputDimensions = new Vector2Int(ww, hh);
            para.outputFormat = TextureFormat.R8;
            para.transformation = XRCpuImage.Transformation.None;

            var request = cpuImage.ConvertAsync(para);
            while (!request.status.IsDone())
            {
                yield return null;
            }

            if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
            {
                Debug.LogErrorFormat("XRCpuImage Request failed, status={0}", request.status);
                request.Dispose();
                cpuImageReady = false;
                yield break;
            }

            int isVertical = 0;
            if (Screen.height > Screen.width)
            {
                isVertical = 1; // 允许竖屏，并且当前就是竖屏模式
            }
            else if (transform.localEulerAngles.z > 245 && transform.localEulerAngles.z < 295)
            {
                isVertical = 2; // 项目设置不允许竖屏，但是用户将手机竖起来，所以目前摄像机转角接近270度
                frameData.cameraPosition = verticalObj.transform.position;
                frameData.cameraEuler = verticalObj.transform.eulerAngles;
            }
            frameData.IsPortraitImage = isVertical > 0;
            // Image data is ready.
            var rawData = request.GetData<byte>();
            frameData.textureDataR8 = rawData.ToArray();
            frameData.textureWidth = ww;
            frameData.textureHeight = hh;
            request.Dispose();


            dataIndex++;
            frameData.index = dataIndex;
            frameData.rgbTexture = null;
            cpuImage.Dispose();
            cpuImageReady = false;



            Debug.Log("CreateFrameData " + frameData.ToString());

            callback?.Invoke(frameData);
        }




        public override string GetName()
        {
            return transform.name;
        }


        public override void StartWork()
        {

        }

        public override void StopWork()
        {
            
        }

    }

}

