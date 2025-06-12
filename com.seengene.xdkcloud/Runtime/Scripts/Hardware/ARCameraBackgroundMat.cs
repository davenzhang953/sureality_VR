using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Seengene.XDK
{
    [RequireComponent(typeof(Camera))]
    public class ARCameraBackgroundMat : MonoBehaviour
    {
        [HideInInspector]
        public Material currMaterial;

        [SerializeField]
        private Material _ARCoreBackground;

        [SerializeField]
        private Material _ARKitBackground;


        private ARCameraBackground arCameraBG;


        private void Awake()
        {
            arCameraBG = GetComponent<ARCameraBackground>();
        }



        void Start()
        {

#if UNITY_IOS || UNITY_EDITOR
            arCameraBG.useCustomMaterial = true;
            arCameraBG.customMaterial = _ARKitBackground;
            currMaterial = _ARKitBackground;
#else
            arCameraBG.useCustomMaterial = true;
            arCameraBG.customMaterial = _ARCoreBackground;
            currMaterial = _ARCoreBackground;
#endif
        }

        /// <summary>
        /// 为了兼容华为的渲染
        /// </summary>
        /// <param name="huaWeiNv21"></param>
        public void onHuaWeiNv21(Material huaWeiNv21)
        {
            currMaterial = huaWeiNv21;
        }

    }
}
