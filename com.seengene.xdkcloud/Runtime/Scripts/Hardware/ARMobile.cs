using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Seengene.XDK
{
    [DefaultExecutionOrder(-1)]
    [DisallowMultipleComponent]
    public class ARMobile : MonoBehaviour
    {
        [SerializeField]
        private XDKCloudSession xdkSession = null;


        private void Awake()
        {
            bool useAREngine = false;
#if UNITY_ANDROID && !UNITY_EDITOR && !AR_GLASSES
            useAREngine = HuaWeiARHelper.IsUseAREngine;
#endif
            List<XDKHardware> hardwares = new List<XDKHardware>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var hardware = transform.GetChild(i).GetComponent<XDKHardware>();
                if (hardware)
                {
                    hardwares.Add(hardware);
                }
            }

            foreach (var item in hardwares)
            {
                if (item.IsHuaweiAR() == useAREngine)
                {
                    xdkSession.xdkHardware = item;
                }
                else
                {
                    Destroy(item.gameObject);
                }
            }

            if (xdkSession.xdkHardware)
            {
                xdkSession.xdkHardware.gameObject.SetActive(true);
                Debug.Log("XDKHareware is active, name=" + xdkSession.xdkHardware.GetName()+ " useAREngine="+ useAREngine);
            }
            else
            {
                Debug.LogError("XDKHareware is not setted, hardwares.Count="+ hardwares.Count);
            }

            Destroy(this);
        }
    }
}

