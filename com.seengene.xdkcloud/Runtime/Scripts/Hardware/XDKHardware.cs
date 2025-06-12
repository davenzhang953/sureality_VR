using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Seengene.XDK
{
    public class XDKHardware : MonoBehaviour, XDKDataProvider, XDKLBSProvider
    {
       

        #region XJ interface

        /// <summary>
        /// frameData interface
        /// </summary>
        /// <returns></returns>

        public virtual Camera GetCurrentCamera()
        {
            throw new System.NotImplementedException();
        }

        public virtual Pose GetCameraPose()
        {
            throw new System.NotImplementedException();
        }
        
        public virtual void GetFrameData(Action<XDKFrameData> callback)
        {
            throw new System.NotImplementedException();
        }

        public virtual string GetName()
        {
            throw new System.NotImplementedException();
        }

        public virtual bool IsDataReady()
        {
            throw new System.NotImplementedException();
        }

        public virtual void StartWork()
        {
            throw new System.NotImplementedException();
        }

        public virtual void StopWork()
        {
            throw new System.NotImplementedException();
        }



        public virtual bool IsHuaweiAR()
        {
            return false;
        }
        /// <summary>
        /// lbs interface
        /// </summary>
        /// <returns></returns>

        public virtual bool IsLBSReady()
        {
            if (LBSModule.Instance != null)
            {
                return true;
            }
            return false;
        }

        public virtual void LBSStartWork()
        {
            if (LBSModule.Instance != null)
            {
                LBSModule.Instance.UnityStartLocation();
            }
        }

        public virtual void LBSStopWork()
        {
            if (LBSModule.Instance != null)
            {
                LBSModule.Instance.UnityStopLocation();
            }
        }

        public virtual XDKLBSData GetLBSData()
        {
            if (LBSModule.Instance != null)
            {
                GPSLocationData gpsData = LBSModule.Instance.UnityRequestLocation();
                if (gpsData.success)
                {
                    XDKLBSData lbsData = new XDKLBSData();
                    lbsData.longitude = gpsData.longitude;
                    lbsData.latitude = gpsData.latitude;
                    lbsData.precision = gpsData.radius;
                    lbsData.direction = gpsData.heading;
                    lbsData.index = LBSModule.Instance.dataIndex;
                    return lbsData;
                }
            }
            return null;
        }

        public virtual string GetLBSName()
        {
            if (LBSModule.Instance != null)
            {
                return "SeengeneLBS";
            }
            return "NoLBS";
        }


        #endregion
    }
}

