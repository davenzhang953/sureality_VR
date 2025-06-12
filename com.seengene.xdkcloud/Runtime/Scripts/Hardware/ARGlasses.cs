using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seengene.XDK
{
    [DefaultExecutionOrder(-1)]
    [DisallowMultipleComponent]
    public class ARGlasses : MonoBehaviour
    {
        [SerializeField]
        private XDKCloudSession xdkSession = null;

        // Start is called before the first frame update
        private void Awake()
        {
            xdkSession.xdkHardware = ObjectTool.FindAnyObjectByType<XDKHardware>();

            if (xdkSession.xdkHardware)
            {
                xdkSession.xdkHardware.gameObject.SetActive(true);
                Debug.Log("XDKHareware is active, name=" + xdkSession.xdkHardware.GetName());
            }
            else
            {
                Debug.LogError("XDKHareware is not setted.");
            }
            Destroy(this);
        }

    }
}
