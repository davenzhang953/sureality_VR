using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Seengene.XDK
{
    public class DebugController : MonoBehaviour
    {

        #region Properties
        [Header("Debug Settings")]
        /// <summary>
        /// 调试面板
        /// </summary>
        [SerializeField] private GameObject debugPanel = null;

        /// <summary>
        /// 是否开始算法重定位
        /// </summary>
        [SerializeField] private Toggle m_ToggleRelocationOn = null;

        /// <summary>
        /// 输入日志开关
        /// </summary>
        [SerializeField] private Toggle m_ToggleLogOn = null;

        /// <summary>
        /// 显示特征点
        /// </summary>
        [SerializeField] private Toggle m_ToggleFeatures = null;

        /// <summary>
        /// 保存图片到本地开关
        /// </summary>
        [SerializeField] private Toggle m_ToggleSaveImages = null;

        /// <summary>
        ///  模拟首次定位成功
        /// </summary>
        [SerializeField] private Button m_BtnLocateSuccOnce = null;

        /// <summary>
        /// 停止debug
        /// </summary>
        [SerializeField] private Button m_BtnStopDebug = null;


        /// <summary>
        /// 提示信息
        /// </summary>
        [SerializeField] private Text m_InfoSceneRoot = null;
        [SerializeField] private Text m_InfoBottom = null;





        /// <summary>
        /// 预览图
        /// </summary>
        [SerializeField] private RawImage m_PreviewImage = null;

        [SerializeField] private GameObject keyPointPrefab = null;

        //[SerializeField] private PlyMeshLoader pointCloudLoader = null;
        [SerializeField] private Toggle pointCloudToggle = null;
        [SerializeField] private Button btnClearCache = null;
        [SerializeField] private Button btnOpenLogWindow = null;
        [SerializeField] private Text pointCloldProgress = null;




        private PointCloudPose pointCloudPose; 
        private int delTimes;


        List<GameObject> keyPointsList = new List<GameObject>();
        List<Vector3> featurePoints = new List<Vector3>();


        [SerializeField]
        private DebugPress pressUp = null;
        [SerializeField]
        private DebugPress pressDown = null;
        [SerializeField]
        private Button showJoystickBtn;
        [SerializeField]
        private Transform joystickTrans;
        [SerializeField]
        private float moveSpeed = 3.0f;

        private string[] bottomInfos = new string[] {" "," "," "," " };

        #endregion


        #region Unity Methods




        private void Start()
        {
            m_ToggleSaveImages.isOn = XDKCloudSession.IfSaveImages;
            m_ToggleSaveImages.onValueChanged.AddListener(OnSaveImagesLogChanged);

            m_ToggleLogOn.isOn = XDKCloudSession.IfLogOn;
            m_ToggleLogOn.onValueChanged.AddListener(OnLogOnToggleChanged);

            m_ToggleFeatures.isOn = XDKCloudSession.IfShowFeatures;
            m_ToggleFeatures.onValueChanged.AddListener(OnShowfeaturesChanged);

            m_ToggleRelocationOn.isOn = XDKCloudSession.IfRelocationOn;
            m_ToggleRelocationOn.onValueChanged.AddListener(OnToggleRelocationOnChanged);

            pointCloudToggle.onValueChanged.AddListener(OnShowPointCloudChanged);
            m_PreviewImage.gameObject.SetActive(XDKCloudSession.IfDebugOn);

            m_BtnLocateSuccOnce.onClick.AddListener(simulateRelocateSuccess);
            m_BtnStopDebug.onClick.AddListener(OnStopDebug);
            btnClearCache.onClick.AddListener(ClearCacheFiles);
            btnOpenLogWindow.onClick.AddListener(OnOpenLogWindow);

            pressUp.OnPressing = OnCameraMoveUp;
            pressDown.OnPressing = OnCameraMoveDown;
            showJoystickBtn.onClick.AddListener(OnShowJoystickBtnClick);
            joystickTrans.gameObject.SetActive(false);
            var debugJoystick = joystickTrans.GetComponentInChildren<DebugJoystick>();
            debugJoystick.onJoystickDraging = OnJoysticeMove;


            pointCloudPose = SceneRoot.Instance.GetComponentInChildren<PointCloudPose>(true);

            if (debugPanel)
            {
                debugPanel.SetActive(XDKCloudSession.IfDebugOn);
            }
            StopCoroutine("checkSession");
            StartCoroutine(checkSession());
        }


        private void OnEnable()
        {
            StopCoroutine("checkSession");
            StartCoroutine(checkSession());
        }


        private IEnumerator checkSession()
        {
            do
            {
                yield return new WaitForSeconds(0.5f);
                XDKCloudSession xdkSession = XDKCloudSession.Instance;
                if (xdkSession != null)
                {
                    //Debug.Log("checkSession XDKCloudSession.CurrentSession is ok");
                    xdkSession.OnDebugInfoEvent += OnUpdateDebugInfo;
                    xdkSession.OnPreviewImageEvent += OnPreviewImageEvent;
                    xdkSession.OnFeaturePointsEvent += OnUpdateKeyPoints;
                    if (pointCloudPose != null)
                    {
                        pointCloudPose.userCamera = xdkSession.xdkHardware.GetCurrentCamera();
                    }
                    break;
                }
                else
                {
                    Debug.Log("checkSession XDKCloudSession.CurrentSession is null");
                }
            } while (true);
        }


        public void OnOpenDebug()
        {
            XDKCloudSession.IfDebugOn = true;
            debugPanel.gameObject.SetActive(true);
        }

        private void OnStopDebug()
        {
            XDKCloudSession.IfDebugOn = false;
            debugPanel.gameObject.SetActive(false);
            if (keyPointsList != null)
            {
                keyPointsList.ForEach((go) => { DestroyImmediate(go); });
                keyPointsList.Clear();
            }
        }




        private void simulateRelocateSuccess()
        {
            XDKCloudSession xdkSession = XDKCloudSession.Instance;
            if (xdkSession != null)
            {
                xdkSession.SimulateFirstRelocatSuccess();
            }
        }

        private void OnOpenLogWindow()
        {
            RuntimeConsole logConsole = FindObjectOfType<RuntimeConsole>(true);
            if (logConsole)
            {
                Debug.Log("logConsole is ok");
                logConsole.ShowLogs();
            }
            else
            {
                Debug.Log("logConsole is null");
            }
        }

        private void ClearCacheFiles()
        {
            delTimes++;
            if (pointCloudPose)
            {
                var str = pointCloudPose.ClearAllCachedFiles();
                Debug.Log("delete cache succ: "+str);
            }
            else
            {
                Debug.LogError("pointCloudPose is null");
            }
            if (pointCloldProgress != null)
            {
                pointCloldProgress.text = "已删除 (" + delTimes + ")";
            }
        }

        private void OnDisable()
        {
            XDKCloudSession xdkSession = XDKCloudSession.Instance;
            if (xdkSession != null)
            {
                Debug.Log("XDKCloudSession.CurrentSession is okkk");
                xdkSession.OnPreviewImageEvent -= OnPreviewImageEvent;
                xdkSession.OnFeaturePointsEvent -= OnUpdateKeyPoints;
            }
            else
            {
                Debug.Log("XDKCloudSession.CurrentSession is nullll");
            }
        }

        private void Update()
        {
            if (debugPanel.activeSelf)
            {
                XDKCloudSession.IfDebugOn = true;
            }
            if (XDKCloudSession.Instance == null)
            {
                return;
            }
            if (SceneRoot.Instance == null)
            {
                return;
            }

            if (XDKCloudSession.IfDebugOn)
            {
                if (!debugPanel.activeSelf)
                {
                    debugPanel.SetActive(true);
                }

                Pose pose = XDKCloudSession.Instance.xdkHardware.GetCameraPose();
                string cameraPos = pose.position.ToString("f2");
                string cameraRot = pose.rotation.eulerAngles.ToString("f2");
                string sessionID = XDKCloudSession.Instance.sessionID;
                string RootInfo = SceneRoot.Instance.getRootInfo();
                long mapID = XDKCloudSession.Instance.currentMap;
                string url = XDKCloudInterface.currUrl;
                string strategyStatus = XDKCloudSession.Instance.strategyMgr.GetConditions();
                m_InfoSceneRoot.text = $"SceneRoot {RootInfo}\nARCamera Pos: {cameraPos}, Rot: {cameraRot}\nServer: {url}\nMapID: {mapID} SessionID: {sessionID} \n{strategyStatus}";
                //Debug.Log("ttt=" + Time.time);
            }

        }


        #endregion



        private void OnUpdateKeyPoints(List<Vector3> points)
        {
            featurePoints = points;

            showFeaturePoints();
        }


        private void OnPreviewImageEvent(Texture2D texture)
        {
            m_PreviewImage.texture = texture;
            float ratio = Screen.width > Screen.height ? 1 : 0.5f; // 如果是竖屏，那么缩小0.5倍。
            m_PreviewImage.rectTransform.sizeDelta = new Vector2(texture.width, texture.height) * ratio;
        }

        /// <summary>
        /// 在界面上显示调试信息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="msg"></param>
        public void OnUpdateDebugInfo(DebugInfoType type, string msg)
        {

            switch (type)
            {
                case DebugInfoType.ImageSend:
                    bottomInfos[0] = msg;
                    break;
                case DebugInfoType.RelocateSucc:
                    bottomInfos[1] = msg;
                    break;
                case DebugInfoType.ScenePose:
                    bottomInfos[2] = msg;
                    break;
                default:
                    bottomInfos[3] = msg;
                    break;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(bottomInfos[0]);
            sb.Append('\n');
            sb.Append(bottomInfos[1]);
            sb.Append('\n');
            sb.Append(bottomInfos[2]);
            sb.Append('\n');
            sb.Append(bottomInfos[3]);
            m_InfoBottom.text = sb.ToString();
        }

        private void OnSaveImagesLogChanged(bool value)
        {
            XDKCloudSession.IfSaveImages = value;
            m_PreviewImage.gameObject.SetActive(value);

            XDKCloudSession xdkSession = XDKCloudSession.Instance;
            if (xdkSession != null)
            {
                if (value)
                {
                    xdkSession.OnPreviewImageEvent += OnPreviewImageEvent;
                }
                else
                {
                    xdkSession.OnPreviewImageEvent -= OnPreviewImageEvent;
                }
            }
        }


        private void OnShowPointCloudChanged(bool value)
        {

            ToggleTextColor ttc = pointCloudToggle.GetComponent<ToggleTextColor>();
            if (ttc != null)
            {
                ttc.SetColor(value);
            }

            XDKCloudSession xdkSession = XDKCloudSession.Instance;
            if (xdkSession.mapInfo == null)
            {
                string msg = "No MapInfo";
                OnUpdateDebugInfo(DebugInfoType.TimeInfo, msg);
                pointCloldProgress.text = msg;
                Debug.Log(msg);
                return;
            }

            if (value) // 要显示点云
            {
                if (pointCloudPose != null)
                {
                    pointCloudPose.UsePotree = true;
                    pointCloudPose.scaleTxtUrl = xdkSession.mapInfo.data.scaleUrl;
                    pointCloudPose.plyFileUrl = xdkSession.mapInfo.data.dense_model_unity_url;
                    pointCloudPose.mapID = xdkSession.mapInfo.data.id.ToString();
                    pointCloudPose.onLoadEnd = onZipLoadDone;
                    pointCloudPose.onProgress = onZipLoadProgress;
                    pointCloudPose.ResetPose();
                    pointCloudPose.PrepareLoaders();
                    this.StartCoroutine(pointCloudPose.loadRemoteFile());
                }
                else
                {
                    Debug.LogError("No object to show PointCloud.");
                }
            }
            else
            {
                if (pointCloudPose != null) {
                    pointCloudPose.RemoveAllPointCloud();
                }
                else
                {
                    Debug.Log("No object to show PointCloud, Nothing to hide.");
                }
            }
        }



        private void onZipLoadDone(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                Debug.Log("点云文件下载完成");
            }
            else
            {
                Debug.LogError("下载远程的点云zip包失败：" + msg);
                pointCloudPose.UsePotree = false;
                pointCloudPose.onLoadEnd = onPlyMeshLoaded;
                pointCloudPose.onProgress = onPlyMeshProgress;
                pointCloudPose.ResetPose();
                pointCloudPose.PrepareLoaders();
                this.StartCoroutine(pointCloudPose.loadRemoteFile());
            }
        }


        private void onPlyMeshLoaded(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                onPlyMeshProgress(1.0f);
                Debug.Log("点云加载完毕，使用了 PlyMesh 方式");
            }
            else
            {
                Debug.Log("点云加载失败 "+msg);
            }
        }


        private void onPlyMeshProgress(float val)
        {
            if (pointCloldProgress != null)
            {
                pointCloldProgress.text = val.ToString("f2");
            }
        }


        private void onZipLoadProgress(float val)
        {
            if (pointCloldProgress != null)
            {
                pointCloldProgress.text = val.ToString("f2");
            }
        }

        /// <summary>
        /// 点云解压缩的进度
        /// </summary>
        /// <param name="val"></param>
        private void onPointCloudUnzipProgress(float val)
        {
            if (pointCloldProgress != null)
            {
                pointCloldProgress.text = val.ToString("f2");
            }
        }



        private void OnLogOnToggleChanged(bool value)
        {
            XDKCloudSession.IfLogOn = value;
            RuntimeConsole.Listening = value;
        }

        private void OnShowfeaturesChanged(bool value)
        {
            XDKCloudSession.IfShowFeatures = value;

            showFeaturePoints();
        }


        private void showFeaturePoints()
        {
            keyPointsList.ForEach((go) => { DestroyImmediate(go); });
            keyPointsList.Clear();

            if (XDKCloudSession.IfShowFeatures && SceneRoot.Instance)
            {
                var mapOffset = SceneRoot.Instance.GetMapObjRoot();
                //Debug.Log("mapoffset, localPos=" + mapOffset.localPosition + " euler=" + mapOffset.localRotation.eulerAngles + " localScale=" + mapOffset.localScale);
                for (int i = 0; i < featurePoints.Count; i++)
                {
                    GameObject go = Instantiate(keyPointPrefab, mapOffset);
                    go.transform.localPosition = new Vector3(featurePoints[i].x, -featurePoints[i].y, featurePoints[i].z);
                    keyPointsList.Add(go);
                }
            }
        }

        private void OnToggleRelocationOnChanged(bool value)
        {
            XDKCloudSession.IfRelocationOn = value;

            if (XDKCloudSession.IfRelocationOn)
            {
                Debug.Log("开启重定位功能!");
            }
            else
            {
                Debug.Log("关闭重定位功能!");
            }
        }

        void OnJoysticeMove(Vector2 dir)
        {
            if (SceneRoot.Instance == null)
            {
                Debug.Log("SceneRoot.Instance is null");
                return;
            }
            if (XDKCloudSession.Instance == null)
            {
                Debug.Log("XDKCloudSession.Instance is null");
                return;
            }
            Camera cam = XDKCloudSession.Instance.xdkHardware.GetCurrentCamera();
            var forward = cam.transform.forward;
            forward.y = 0;
            forward = forward.normalized;
            var right = cam.transform.right;
            right.y = 0;
            right = right.normalized;

            var pos = SceneRoot.Instance.transform.position
                - forward * dir.y * Time.deltaTime * moveSpeed
                - right * dir.x * Time.deltaTime * moveSpeed;
            SceneRoot.Instance.transform.position =pos;
        }

        void OnShowJoystickBtnClick()
        {
            if (joystickTrans.gameObject.activeInHierarchy)
            {
                joystickTrans.gameObject.SetActive(false);
            }
            else
            {
                joystickTrans.gameObject.SetActive(true);
            }
        }

        void OnCameraMoveUp()
        {
            if (SceneRoot.Instance)
            {
                var pos = SceneRoot.Instance.transform.position - Vector3.up * Time.deltaTime * moveSpeed;
                SceneRoot.Instance.transform.position = pos;
            }
            
        }

        void OnCameraMoveDown()
        {
            if (SceneRoot.Instance)
            {
                var pos = SceneRoot.Instance.transform.position - Vector3.down * Time.deltaTime * moveSpeed;
                SceneRoot.Instance.transform.position = pos;
            }
        }
    }
}