using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Seengene.XDK
{
    public interface XDKDataProvider
    {
        public string GetName();

        public void StartWork();

        public void StopWork();

        public bool IsDataReady();

        public Camera GetCurrentCamera();

        public Pose GetCameraPose();

        public void GetFrameData(Action<XDKFrameData> callback);
    }


    
}
