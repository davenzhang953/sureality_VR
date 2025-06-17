using System;
using System.Collections;
using System.Collections.Generic;
using Seengene.XDK;
using UnityEngine;


namespace Seengene.XDK
{
    public class LocationCallback : AndroidJavaProxy
    {

        /// <summary>
        /// 当接收到GPS定位信息
        /// </summary>
        public Action<GPSLocationData> OnReceiveGPSLocationEvent = null;

        public Action<int> OnGPSLocationFailedEvent = null;

        public LocationCallback() : base("com.seengene.sdk.lbs.LocationCallback") { }

        void onReceiveLocation(double latitude, double longitude, float radius, string coorType, int locType, string address, float heading)
        {
            //Debug.LogFormat("LocationCallback.onReceiveLocation,  latitude:{0}, longitude:{1}, radius:{2}, coorType:{3}," +
            //    " locType: {4}, address：{5}, heading: {6}",
            //    latitude, longitude, radius, coorType,
            //    locType, address, heading);

            OnReceiveGPSLocationEvent?.Invoke(new GPSLocationData(latitude, longitude, radius, coorType, locType, address, heading));
        }

        void onFailed(int code)
        {
            Debug.LogFormat("LocationCallback.onFailed, code:{0}", code);
            OnGPSLocationFailedEvent?.Invoke(code);
        }
    }
}