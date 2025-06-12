using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Seengene.XDK;

namespace Seengene.XDK
{
    public class LBSModule
    {
        private static LBSModule _instance;

        /// <summary>
        /// 
        /// </summary>
        private bool needStartRelocation = true;

        /// <summary>
        /// lbs定位结果数据
        /// </summary>
        private GPSLocationData currLocation;

        /// <summary>
        /// 
        /// </summary>
        public int dataIndex;


        public static LBSModule Instance {
            get {
                if(_instance == null)
                {
                    _instance = new LBSModule();
                }
                return _instance;
            }
        }

        public void UnityStopLocation()
        {
            needStartRelocation = true;
            StopUpdatingLocation();
        }


        public void UnityStartLocation()
        {
            if (needStartRelocation)
            {
                StartUpdatingLocation();
                needStartRelocation = false;
            }
        }

        public GPSLocationData UnityRequestLocation()
        {
            if (needStartRelocation)
            {
                StartUpdatingLocation();
                needStartRelocation = false;
            }

#if UNITY_EDITOR || UNITY_IOS
            string str = RequestCurrentLocation();
            if (!string.IsNullOrEmpty(str))
            {
                currLocation = JsonUtility.FromJson<GPSLocationData>(str);
                dataIndex++;
            }
#else
            JavaRequestCurrentLocation();
            if (currLocation == null)
            {
                currLocation = new GPSLocationData();
                currLocation.success = false;
            }
#endif

            return currLocation;
        }


        /// <summary>
        /// popup permissions view, for android
        /// </summary>
        public void RequestLocationPermissions()
        {
#if !UNITY_EDITOR && !UNITY_IOS
            RequestLocationPermissionsNative();
#endif
        }



        /// <summary>
        /// callback for android
        /// </summary>
        /// <param name="gpsLocData"></param>
        private void OnGPSLocationSuccess(GPSLocationData gpsLocData)
        {
            currLocation = gpsLocData;
            dataIndex++;
        }

        /// <summary>
        /// callback for android
        /// </summary>
        /// <param name="errorCode"></param>
        private void OnGPSLocationFailed(int errorCode)
        {
            if(currLocation == null)
            {
                currLocation = new GPSLocationData();
            }
            currLocation.success = false;
        }




#if !UNITY_EDITOR && UNITY_IOS
        [DllImport("__Internal")]
        private static extern void StartUpdatingLocation();

        [DllImport("__Internal")]
        private static extern void StopUpdatingLocation();

        [DllImport("__Internal")]
        private static extern string RequestCurrentLocation();

#elif !UNITY_EDITOR && UNITY_ANDROID


        private AndroidJavaObject GetCurrentActivity() {
            try {
                AndroidJavaClass jcUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject joCurActivity = jcUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                return joCurActivity;
            } catch (System.Exception e) {
                Debug.LogError("GetCurrentActivity failed: "+ e.Message);
            }
            return null;
        }
 
        private AndroidJavaObject GetGpsHelperJavaObject() {
            try {
                AndroidJavaObject joCurActivity = GetCurrentActivity();
                if (joCurActivity != null) {
                    AndroidJavaClass jcLocationHelper = new AndroidJavaClass("com.seengene.sdk.lbs.LocationHelper");
                    AndroidJavaObject joGPSHelper = jcLocationHelper.CallStatic<AndroidJavaObject>("getInstance", joCurActivity);
                    return joGPSHelper;
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat("初始化GPS定位模块出错: {0}", e.Message);
            }
            return null;
        }

        
        private void StartUpdatingLocation() {
            try {
                AndroidJavaObject joGPSHelper = GetGpsHelperJavaObject();
                if (joGPSHelper != null) {
                    LocationCallback callBack = new LocationCallback();
                    callBack.OnReceiveGPSLocationEvent += OnGPSLocationSuccess;
                    callBack.OnGPSLocationFailedEvent += OnGPSLocationFailed;
                    joGPSHelper.Call("setCallback", callBack);

                    joGPSHelper.Call("start");
                    Debug.Log("StartUpdatingLocation is done.");
                }
                else
                {
                    Debug.LogError("初始化GPS定位模块出错，不能获取 LocationHelper 实例对象");
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat("初始化GPS定位模块出错: {0}", e.Message);
            }
        }


        private void RequestLocationPermissionsNative() {
            try {
                AndroidJavaObject joGPSHelper = GetGpsHelperJavaObject();
                if (joGPSHelper != null) {
                    joGPSHelper.Call("doReresh()");
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat("重新请求定位权限失败: {0}", e.Message);
            }
        }

        private void JavaRequestCurrentLocation() {
            try {
                AndroidJavaObject joGPSHelper = GetGpsHelperJavaObject();
                if (joGPSHelper != null) {
                    joGPSHelper.Call("doCallback");
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat("获取GPS定位出错: {0}", e.Message);
                OnGPSLocationFailed(404);
            }
        }

        private void StopUpdatingLocation() {
            try {
                AndroidJavaObject joGPSHelper = GetGpsHelperJavaObject();
                if (joGPSHelper != null) {
                    joGPSHelper.Call("stop");
                }
            } catch (System.Exception e) {
                Debug.LogErrorFormat("停止GPS定位服务出错: {0}", e.Message);
            }
        }


#else // unity_editor



        /// <summary>
        /// Request Current Location
        /// </summary>
        /// <returns></returns>
        private string RequestCurrentLocation()
        {
            Debug.Log("RequestCurrent Location Native");
            GPSLocationData locInfo = new GPSLocationData();
            locInfo.address = "北京市海淀区西三旗桥234号";
            locInfo.latitude = 39.999686d;
            locInfo.longitude = 116.275115d;
            locInfo.heading = 33f;
            locInfo.radius = 0.00001d;
            locInfo.success = true;
            string jsData = JsonUtility.ToJson(locInfo);
            return jsData;
        }


        private void StartUpdatingLocation()
        {
            Debug.Log("StartUpdatingLocation UnityEditor");
        }
        
        private void StopUpdatingLocation()
        {
            Debug.Log("StopUpdatingLocation UnityEditor");
        }

#endif


    }


}