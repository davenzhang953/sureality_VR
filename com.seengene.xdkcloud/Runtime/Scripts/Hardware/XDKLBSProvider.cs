using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Seengene.XDK
{
    public interface XDKLBSProvider
    {
        public string GetLBSName();

        public void LBSStartWork();
        public void LBSStopWork();

        public bool IsLBSReady();

        public XDKLBSData GetLBSData();

    }
}
