using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seengene.XDK
{

    [System.Serializable]
    public class GPSLocationData
    {
        /// <summary>
        /// 纬度
        /// </summary>
        public double latitude;

        /// <summary>
        /// 经度
        /// </summary>
        public double longitude;

        /// <summary>
        /// 精度
        /// </summary>
        public double radius;

        /// <summary>
        /// 坐标类型
        /// </summary>
        public string coorType;

        /// <summary>
        /// 定位类型
        /// </summary>
        public int locType;

        /// <summary>
        /// 地点名称
        /// </summary>
        public string address;

        /// <summary>
        /// 朝向
        /// </summary>
        public float heading;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool success;



        public GPSLocationData()
        {

        }

        public GPSLocationData(double latitude, double longitude, float radius, string coorType, int locType, string address, float heading)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.radius = radius;
            this.coorType = coorType;
            this.locType = locType;
            this.address = address;
            this.heading = heading;
            success = true;
        }



        public override string ToString()
        {
            return string.Format("latitude:{0}, longitude:{1}, radius:{2}, coorType:{3}, locType: {4}, address：{5}, heading: {6}, success: {7}",
                latitude, longitude, radius, coorType, locType, address, heading, success);
        }
    }
}
