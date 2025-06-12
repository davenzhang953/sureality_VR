using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Seengene.XDK
{

    #region enum
    public enum MapQueryStatus {
        MAP_SUCCESS = 0,
        MAP_FAIL = 1,
        MAP_LOADING = 2,
        NET_ERROR = 3,
        THREAD_ERROR = 4,
    }


    public enum ARVideoQuality
    {
        Normal = 0,
        High = 1,
    }

    public enum RelocalizeQueryStatus {
        // BA success, the result is filtered, implies FRAME_SUCCESS
        // In this status, m_inliers_3d_vector contains the set of all 3D points in the BA window
        SUCCESS = 0,
        // relocalization for current frame is successful, but BA failed
        FRAME_SUCCESS_BA_FAIL = 1,
        // the requested map is not ready yet on this server
        // user should request map first
        MAP_FAIL = 2,
        // relocalization for current frame is unsuccessful
        RELOCALIZE_FAIL = 3,
        // BA session is not found for this query, client should request map again
        BA_SESSION_EXPIRED = 4,
        // Http request failed
        NET_ERROR = 5,
        // runtime exception
        CODE_EXCEPTION = 6,
    }


    public enum DebugInfoType {
        ImageSend = 1,
        RelocateSucc = 2,
        TimeInfo = 3,
        ScenePose = 4,
    }

    #endregion of enum


    #region Custom Data Structure
    

    /// <summary>
    /// 请求定位，获得定位结果
    /// </summary>
    public class RelocalizeKeys {
        

        
    }


    public class RelocUploadImageItem {
        public string sessionID;
        public Int32 seq;
        public Matrix4x4 cameraPose;
        public float[,] cameraIntrinsics;
        public byte[] imageBytes;

        public bool isGpsOK;
        public double longitude;
        public double latitude;
        public double gpsPrecision;
        public float gpsDirection;
    }



    #endregion of Custom Data Structure

}