using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;

namespace Seengene.XDK
{

    [RequireComponent(typeof(XDKCloudInterface))]
    public class XDKCloudSession : MonoBehaviour
    {
        #region static params

        //XDK当前的版本号
        public static string XDKVersion = "0.9.0";

        /// <summary>
        /// 是否Debug模式
        /// </summary>
        public static bool IfDebugOn = false;

        /// <summary>
        /// 是否开启打印日志
        /// </summary>
        public static bool IfLogOn = false;

        /// <summary>
        ///  是否开启算法重定位
        /// </summary>
        public static bool IfRelocationOn = true;

        /// <summary>
        /// 是否显示特征点
        /// </summary>
        public static bool IfShowFeatures = false;

        /// <summary>
        ///  是否保存图片到本地
        /// </summary>
        public static bool IfSaveImages = false;

        /// <summary>
        /// 指定的定位服务器地址
        /// </summary>
        public static string XDKServer = null;

        /// <summary>
        /// 指定的定位地图数据
        /// </summary>
        public static XDKMapItem XDKMap = null;



        public static XDKCloudSession Instance = null;


        #endregion

        [HideInInspector]
        public XDKHardware xdkHardware = null;

        /// <summary>
        /// 相机视频流质量
        /// </summary>
        public ARVideoQuality videoStreamingQuality = ARVideoQuality.Normal;


        public string configServer = "http://114.116.234.175:9992/"; //  

        /// <summary>
        /// 定位服务器的地址
        /// </summary>
        public string relocateServer = "http://eonimap-reloc.seengene.com";


        /// <summary>
        /// 使用的地图的id
        /// </summary>
        public XDKMapItem mapItem = null; //76093130942944 89797119235552

        /// <summary>
        /// 定位策略
        /// </summary>
        public Strategy relocateStrategy;

        /// <summary>
        /// 给开发者分配的secretKey
        /// </summary>
        public string secretKey = "ea783b24398aa9";


        public bool StartOnMonoStart = true;


        public bool SetFrame60 = true;


        #region UnityAction & UnityEvent

        /// <summary>
        /// 开始定位
        /// </summary>
        public UnityEvent EvtStartWork = new UnityEvent();

        /// <summary>
        /// 首次重定位成功事件
        /// </summary>
        public UnityEvent EvtFirstLocateSuccess = new UnityEvent();

        /// <summary>
        /// 获取地图数据失败
        /// </summary>
        public UnityEvent<long> EvtMapInfoFail = new UnityEvent<long>();

        /// <summary>
        /// 超过指定时间还没能定位成功
        /// </summary>
        public UnityEvent<int> EvtRelocateTimeout = new UnityEvent<int>();

        /// <summary>
        /// 使用定位结果 Action
        /// </summary>
        public UnityEvent<int, Pose> EvtScenePoseUsed = new UnityEvent<int, Pose>();

        

        /// <summary>
        /// 更新预览图 Action
        /// </summary>
        public UnityAction<Texture2D> OnPreviewImageEvent;

        /// <summary>
        /// 获取session成功 Action
        /// </summary>
        public UnityAction<string> OnAuthSuccEvent;

        /// <summary>
        /// 发送定位请求 Action
        /// </summary>
        public UnityAction<RelocationRequest> OnRelocationRequest;

        /// <summary>
        /// 定位成功 Action
        /// </summary>
        public UnityAction<RelocationResponse> OnRelocationSuccess;

        /// <summary>
        /// Debug回调事件
        /// </summary>
        public UnityAction<DebugInfoType, string> OnDebugInfoEvent;

        /// <summary>
        /// 更新空间特征点 Action
        /// </summary>
        public UnityAction<List<Vector3>> OnFeaturePointsEvent;

        #endregion


        #region Properties


        /// <summary>
        /// 云定位接口
        /// </summary>
        private XDKCloudInterface networker;

        /// <summary>
        /// 重定位成功次数
        /// </summary>
        private int m_RelocSuccessCounter = 0;
        private int m_UpdatePoseCounter = 0;



        private int lastSeqNum;
        private float rootScale;
        private bool stopped;
        private bool toInvokeFirstSucc;
        private bool toReqSession;
        private float frameWaitTime;
        private float delayTime = 2.0f; // 获取session失败后，下一次请求session前需要等待的时间
        private Texture2D previewTex;
        private bool isPaused;

        internal long currentMap = 0;
        internal string sessionID;
        internal StrategyManager strategyMgr;

        [HideInInspector]
        public XDKMapInfo mapInfo;


        #endregion


        private void Awake()
        {
            Instance = this;

            if (!string.IsNullOrEmpty(XDKServer))
            {
                relocateServer = XDKServer;
            }
            if (XDKMap != null)
            {
                mapItem = XDKMap;
            }
            
            Debug.Log("XDKCloudSession awake， relocateServer="+ relocateServer);
            networker = GetComponent<XDKCloudInterface>();

            strategyMgr = new StrategyManager(relocateStrategy);

            if (SetFrame60)
            {
                Application.targetFrameRate = 60;
            }
        }


        /// <summary>
        /// 发送debug信息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="type"></param>
        public void SendDebugEvent(DebugInfoType type, string msg)
        {
            if (IfDebugOn)
            {
                OnDebugInfoEvent?.Invoke(type, msg);
            }
            if (IfLogOn)
            {
                Debug.Log(msg);
            }
        }

        private void Start()
        {
            RuntimeConsole.Listening = IfLogOn;
            Debug.Log("XDKCloudSession dataProvider=" + xdkHardware.GetName());

            previewTex = new Texture2D(4, 4);
            
            StartCoroutine(OpenCamera());

            if (StartOnMonoStart)
            {
                StartWork();
            }
        }

        private void OnApplicationPause(bool _pause)
        {
            Debug.Log("---> puase="+_pause);
            if (_pause)
            {
                isPaused = true;
            }
            else
            {
                if (isPaused && strategyMgr != null)
                {
                    strategyMgr.ClearHistorys();
                    strategyMgr.StartNewRound(true);
                }
                isPaused = false;
            }
        }


#if UNITY_EDITOR

        public XDKMapItem GetMapItem()
        {
            return mapItem;
        }
#endif
        /// <summary>
        /// 调用相机权限
        /// </summary>
        /// <returns></returns>
        private IEnumerator OpenCamera()
        {
            Debug.Log("OpenCamera check permessions");
            yield return new WaitForSeconds(1); // 延迟一点时间，再弹出提示

            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                WebCamDevice[] devices = WebCamTexture.devices;
                Debug.Log("OpenCamera 相机权限请求成功");
            }
            else
            {
                Debug.Log("OpenCamera 相机权限请求失败");
            }
        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            StopWork();
        }




        private void Update()
        {

            var camera = xdkHardware.GetCurrentCamera();
            if (camera && strategyMgr != null)
                strategyMgr.UpdateCamera(camera);

            
            if (stopped)
                return;


            if (currentMap > 0)
            {
                if (toReqSession)
                {
                    StartAuth();
                    toReqSession = false;
                }
                else
                {
                    CheckMapTime();
                }
            }
            

            if (IfRelocationOn && sessionID != null)
            {
                if (strategyMgr.NeedSendImage())
                {
                    if (frameWaitTime > 0)
                    {
                        frameWaitTime -= Time.deltaTime;
                    }
                    else
                    {
                        if (xdkHardware.IsDataReady())
                        {
                            frameWaitTime = relocateStrategy.FrameIntervalInRound;
                            xdkHardware.GetFrameData(DoFrameData);
                        }
                    }
                }
            }
            
        }






        private void StartAuth()
        {
            SendDebugEvent(DebugInfoType.TimeInfo, "Start Auth, mapId="+ currentMap);
            sessionID = null;
            networker.StopRelocate();
            networker.Authorize();
        }






        private void DoFrameData(XDKFrameData frameData)
        {
            if (xdkHardware.IsLBSReady())
            {
                XDKLBSData lbsData = xdkHardware.GetLBSData();
                networker.RelocateFrame(frameData, lbsData);
            }
            else
            {
                networker.RelocateFrame(frameData, null);
            }
        }



        internal void OnRelocateRequest(RelocationRequest request)
        {
            OnRelocationRequest?.Invoke(request);
            var strDebug = string.Format("更新图片 {0}: 定位成功 {1}, sessionID={2}", request.seq, m_RelocSuccessCounter, sessionID);
            SendDebugEvent(DebugInfoType.ImageSend, strDebug);

            if (IfSaveImages && OnPreviewImageEvent != null)
            {
                if (previewTex == null)
                {
                    previewTex = new Texture2D(4, 4);
                }
                previewTex.LoadImage(request.imageBytes);
                previewTex.Apply();
                OnPreviewImageEvent?.Invoke(previewTex);
            }
        }



        /// <summary>
        /// 处理获取session的请求返回数据
        /// </summary>
        /// <param name="response"></param>
        private void HandleAuthorizationResponse(AuthorizationResponse response)
        {
            if (stopped)
            {
                return;
            }
            if (response.status == (int)MapQueryStatus.MAP_SUCCESS)
            {
                SendDebugEvent(DebugInfoType.TimeInfo, "Authorize Success! Session: " + response.sessionID);
                sessionID = response.sessionID;
                rootScale = (float)response.scale;
                delayTime = 2.0f;
                OnAuthSuccEvent?.Invoke(sessionID);
            }
            else if(response.status == (int)MapQueryStatus.MAP_LOADING)
            {
                SendDebugEvent(DebugInfoType.TimeInfo, "Server Is Loading Map!");
                sessionID = null;
                delayTime = 3.0f;
            }
            else
            {
                SendDebugEvent(DebugInfoType.TimeInfo, "Authorize Failed, Code: " + response.status);
                sessionID = null;
                networker.StopRelocate();
                StartCoroutine(delayAndAuth()); // 重新认证，准备获取session
            }
        }


        private IEnumerator delayAndAuth()
        {
            yield return new WaitForSeconds(delayTime);
            delayTime += 2;
            if (delayTime > 30) {
                delayTime = 30;
            }
            StartAuth();
        }

        /// <summary>
        /// 处理定位接口返回数据
        /// </summary>
        /// <param name="response"></param>
        private void HandleRelocationResponse(RelocationResponse response)
        {
            if (stopped)
            {
                return;
            }
            float averageTime = 0f;
            if (XDKCloudInterface.totalCount > 0)
            {
                averageTime = XDKCloudInterface.totalTime / XDKCloudInterface.totalCount;
            }
            string strDebug = string.Format("联网次数{0}, 平均耗时{1}, 最小{2}, 最大{3}, 丢弃请求{4}, 认证次数{5}，seq={6}",
                XDKCloudInterface.totalCount, averageTime.ToString("f2"),
                XDKCloudInterface.minTime.ToString("f0"), XDKCloudInterface.maxTime.ToString("f0"),
                XDKCloudInterface.dropCount, XDKCloudInterface.authCount, response.seq);
            SendDebugEvent(DebugInfoType.TimeInfo, strDebug);

            switch (response.status)
            {
                case (int)RelocalizeQueryStatus.SUCCESS:
                    if (response.seq <= lastSeqNum)
                    {
                        return; // 收到了已经过期的反馈结果，直接抛弃掉
                    }
                    lastSeqNum = response.seq;
                    UpdateSpatialRoot(response);
                    break;
                case (int)RelocalizeQueryStatus.BA_SESSION_EXPIRED:
                    sessionID = null;
                    networker.StopRelocate();
                    SendDebugEvent(DebugInfoType.TimeInfo, "session expired");
                    StartAuth();// 重新认证，准备获取session
                    return;
                default:
                    RelocalizeQueryStatus temp = (RelocalizeQueryStatus)response.status;
                    string msg = string.Format("重定位结果 seq={0}, Status={1}, ", response.seq, temp.ToString());
                    SendDebugEvent(DebugInfoType.RelocateSucc, msg);
                    break;
            }

            if (strategyMgr.IsRoundEnded()) // 一轮定位结束了
            {
                UseSpatialResult(response.frameIndex, false);
                networker.ClearRequests();
                strategyMgr.StartNewRound();
            }
            if (strategyMgr.IsRoundTimeout()) // 一轮定位超时了
            {
                int rountIndex = strategyMgr.GetRound();
                EvtRelocateTimeout?.Invoke(rountIndex);
            }
        }


        /// <summary>
        /// 更新空间映射根坐标
        /// </summary>
        private void UpdateSpatialRoot(RelocationResponse response)
        {
            m_RelocSuccessCounter++;

            if (IfDebugOn)
            {
                if (MusicPlayer.Instance != null)
                {
                    MusicPlayer.Instance.PlayMusic();
                }
            }

            List<double> pose = response.transform_ltg;
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

            matrix = matrix.transpose;

            matrix.m01 = -matrix.m01;
            matrix.m02 = -matrix.m02;
            matrix.m10 = -matrix.m10;
            matrix.m13 = -matrix.m13;
            matrix.m20 = -matrix.m20;
            matrix.m23 = -matrix.m23;

            var m_SpatialPosition = matrix.GetPosition();
            m_SpatialPosition.z = -m_SpatialPosition.z;
            var m_SpatialRotation = matrix.GetRotation();
            m_SpatialRotation.z = -m_SpatialRotation.z;
            m_SpatialRotation.w = -m_SpatialRotation.w;
            var m_SpatialScale = new Vector3(rootScale, rootScale, rootScale);
            
            response.scenePosition = m_SpatialPosition;
            response.sceneRotation = m_SpatialRotation;
            OnRelocationSuccess?.Invoke(response);

            if (m_RelocSuccessCounter >= 3) // 丢弃前2次的定位结果。目的是跳过前期 ARFoundation 不太稳定的阶段。
            {
                Pose scenePose = new Pose();
                scenePose.position = response.scenePosition;
                scenePose.rotation = response.sceneRotation;
                Pose cameraPose = new Pose();
                cameraPose.position = response.cameraPosition;
                cameraPose.rotation = Quaternion.Euler(response.cameraEulerAngles);
                strategyMgr.AddRelocateResult(response.frameIndex, scenePose, cameraPose, out bool jumped);
                if (jumped) // 之前相机位置有跳变，需要马上应用定位结果
                {
                    UseSpatialResult(response.frameIndex, true);
                }


                if (IfDebugOn)
                {
                    OnFeaturePointsEvent?.Invoke(response.point3d_vec);
                }
            }
            

            string strDebug = string.Format("重定位成功 seq={0}, pos={1}, euler={2}, succCount={3}, point3d.Count={4}",
                    response.seq, m_SpatialPosition.ToString("f2"), m_SpatialRotation.eulerAngles.ToString("f2"), m_RelocSuccessCounter, response.point3d_vec.Count);
            SendDebugEvent(DebugInfoType.RelocateSucc, strDebug);
        }

        /// <summary>
        /// 使用过滤后的场景位姿，更新显示状态
        /// </summary>
        /// <param name="frameIndex"></param>
        private void UseSpatialResult(int frameIndex, bool jumped)
        {
            var sceneRoot = SceneRoot.Instance;
            if (sceneRoot == null)
            {
                Debug.LogError("SceneRoot.Instance is null");
                return;
            }
            bool succ = strategyMgr.GetFilterResult(out Pose scenePose, out float evaluation, out bool NoSmooth);
            //Debug.Log($"scenePose succ={succ} position={scenePose.position} euler={scenePose.rotation.eulerAngles}");
            if (currentMap == mapItem.MapIdDaytime)
            {
                sceneRoot.ResetMapOffset(Vector3.zero, Vector3.zero, Vector3.one);
            }
            else
            {
                sceneRoot.ResetMapOffset(mapItem.OffsetPos, mapItem.OffsetEuler, Vector3.one);
            }
            if (succ)
            {
                m_UpdatePoseCounter++;
                sceneRoot.m_SmoothMoveType = relocateStrategy.SmoothMoveType;
                sceneRoot.m_SmoothMoveTime = relocateStrategy.SmoothMoveTime;
                // 临时测试
                // scenePose.position.x = -scenePose.position.x;
                sceneRoot.OnSpatialMapped(NoSmooth, scenePose.position, scenePose.rotation);
                var strDebug = string.Format("场景位姿{0}, seq={1}, pos={2}, euler={3}, Smooth={4}, Jumped={5}",
                    m_UpdatePoseCounter, networker.requestCounter, scenePose.position.ToString("f2"), scenePose.rotation.eulerAngles.ToString("f2"), !NoSmooth, jumped );
                SendDebugEvent(DebugInfoType.ScenePose, strDebug);

                if (toInvokeFirstSucc) // 调用首次定位成功的事件
                {
                    EvtFirstLocateSuccess?.Invoke();
                    toInvokeFirstSucc = false;
                }

                EvtScenePoseUsed?.Invoke(frameIndex, scenePose); // 抛出事件
            }
        }



        


        /// <summary>
        /// 按照时间，是否需要切换地图了？
        /// </summary>
        /// <returns></returns>
        private bool CheckMapTime()
        {
            long tempID = mapItem.GetMapIdNow();
            if (currentMap != tempID)
            {
                currentMap = tempID;
                lastSeqNum = -1; // 重新开始计数
                sessionID = null;
                stopped = true;
                Debug.Log("---> change currentMap " + currentMap);
                RequestMapInfo();
                return true;
            }
            return false;
        }



        public void StartWork()
        {
            if (XDKMap != null)
            {
                mapItem = XDKMap;
            }
            var loom = Loom.Current; // 防止loom在app切换到后台时进行初始化
            SendDebugEvent(DebugInfoType.TimeInfo, "Loom="+loom);
            Debug.Log("Strategy: "+relocateStrategy.ToString());
            SceneRoot.Instance.IsAtOrigin = true;

            currentMap = mapItem.GetMapIdNow();
            if (currentMap > 0)
            {
                Debug.Log(mapItem.ToString());
                Debug.Log("XDKMapItem currentMap=" + currentMap);
                toInvokeFirstSucc = true;
                RequestMapInfo();
                EvtStartWork?.Invoke();
            }
            else
            {
                Debug.LogError("XDKMapItem is wrong!!");
                EvtMapInfoFail?.Invoke(currentMap);
            }
        }


        /// <summary>
        /// 请求当前地图的基础信息
        /// </summary>
        private void RequestMapInfo()
        {
            string url = configServer;
            if (string.IsNullOrEmpty(url))
            {
                url = "null";
            }
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            url += "officalmanage/api/map/get?id=" + currentMap;
            StartCoroutine(ToolHttp.httpPostForm(url, new WWWForm(), null, OnDownloadMapInfo));
        }



        /// <summary>
        /// 请求地图数据信息
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        void OnDownloadMapInfo(string mark, byte[] data)
        {
            if (string.Equals(mark, "succ"))
            {
                string content = System.Text.Encoding.UTF8.GetString(data);
                mapInfo = JsonUtility.FromJson<XDKMapInfo>(content);

                if (mapInfo.code != 200)
                {
                    OnDebugInfoEvent?.Invoke(DebugInfoType.TimeInfo, "GetMapConfig, CodeError:" + mapInfo.code);
                    EvtMapInfoFail?.Invoke(currentMap);
                }
                else
                {
                    string plyFileUrl = mapInfo.data.dense_model_unity_url;
                    Debug.Log("plyFileUrl=" + plyFileUrl);
                    PointCloudPose meshLoader = SceneRoot.Instance.GetComponentInChildren<PointCloudPose>(true);
                    meshLoader.plyFileUrl = plyFileUrl;
                    meshLoader.scaleTxtUrl = mapInfo.data.scaleUrl;
                    meshLoader.mapID = mapInfo.data.id.ToString();
                    meshLoader.RemoveAllPointCloud();
                }
            }
            else
            {
                mapInfo = null;
                EvtMapInfoFail?.Invoke(currentMap);
            }

            afterMapConfigLoaded(); // 不论获取地图配置成功或者失败，都继续工作，开始定位
        }

        /// <summary>
        /// 获取地图数据成功后的处理
        /// </summary>
        private void afterMapConfigLoaded()
        {
            stopped = false;
            toReqSession = true;
            
            m_RelocSuccessCounter = 0;
            m_UpdatePoseCounter = 0;
            delayTime = 2.0f;

            XDKCloudInterface.authCount = 0;
            XDKCloudInterface.totalTime = 0;
            XDKCloudInterface.totalCount = 0;
            XDKCloudInterface.minTime = 0f;
            XDKCloudInterface.maxTime = 0f;
            XDKCloudInterface.dropCount = 0;


            xdkHardware.gameObject.SetActive(true);
            xdkHardware.StartWork();
            strategyMgr.StartWork();

            networker.xSession = this;
            networker.requestCounter = 0;
            networker.OnAuthorized = HandleAuthorizationResponse;
            networker.OnRelocated = HandleRelocationResponse;

        }



        public void StopWork()
        {
            stopped = true;
            toInvokeFirstSucc = false;
            toReqSession = false;
            sessionID = null;

            if (networker != null)
            {
                networker.StopRelocate();
                networker.OnAuthorized = null;
                networker.OnRelocated = null;
            }

            xdkHardware.StopWork();

            SendDebugEvent(DebugInfoType.TimeInfo, "Stop Work.");
        }


        /// <summary>
        /// 手动模拟首次定位成功
        /// </summary>
        public void SimulateFirstRelocatSuccess()
        {
            if (m_RelocSuccessCounter >= 1)
            {
                SendDebugEvent(DebugInfoType.TimeInfo, "SimulateFirstRelocatSuccess too much");
            }
            else
            {
                SendDebugEvent(DebugInfoType.TimeInfo, "SimulateFirstRelocatSuccess invoked");
                if (toInvokeFirstSucc)
                {
                    toInvokeFirstSucc = false;
                    EvtFirstLocateSuccess?.Invoke();
                }
                m_RelocSuccessCounter = 1;
                if (SceneRoot.Instance != null)
                {
                    SceneRoot.Instance.ShowAllChildren(true);
                }
            }
        }

    }




}