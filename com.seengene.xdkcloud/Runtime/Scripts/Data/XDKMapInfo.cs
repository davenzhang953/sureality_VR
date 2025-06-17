using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seengene.XDK
{
    /// <summary>
    /// 地图信息
    /// </summary>
    [Serializable]
    public class XDKMapInfo
    {
        public string msg;
        public int code;
        public MapInfoData data;
    }

    /// <summary>
    /// 地图信息数据
    /// </summary>
    [Serializable]
    public class MapInfoData
    {
        public long id;
        public string account;
        public string name;
        public string companyId;
        public string companyName;
        public string programId;
        public string programName;
        public string url;
        public string map_v2_bin_url;
        public string dense_model_unity_url;
        public string scaleUrl;
        public string engine_config_url;
        public string dense_model_url;
        public string potree_url; // 很多时候不能取到这个数据
        public string rebuild;
        public string checkStatus;
        public string postStatus;
        public string description;
        public string status;
        public string checkTime;
        public string logFile;
        public string buildStatus;
    }



}
