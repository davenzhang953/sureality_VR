using UnityEngine;
using UnityEngine.SceneManagement;






namespace Seengene.XDK
{
    public class FollowCamera : MonoBehaviour
    {

        [SerializeField]
        private Camera cameraNow;


        [SerializeField]
        private float forwardDis = 1.8f;

        [SerializeField]
        private float upwardDis;


        [SerializeField]
        private bool useCameraY;


        [SerializeField]
        private bool lookCamera;

        // ==== [MODIFIED by LI Hao] ====
        [SerializeField]
        private float followSpeed = 5f;

        [SerializeField]
        private float rotationSpeed = 5f;

        [SerializeField]
        private float threshold = 0.1f;
        // ==============================



        private void Awake()
        {
            SceneManager.sceneUnloaded += OnSceneUnLoaded;
        }


        // Update is called once per frame
        void Update()
        {
            if (cameraNow == null)
            {
                cameraNow = Camera.main;
            }
            Vector3 forward = cameraNow.transform.forward;
            if (useCameraY)
            {
                forward.y = 0;
                forward = forward.normalized;
            }
            Vector3 pos = cameraNow.transform.position + forward * forwardDis;
            pos.y = pos.y + upwardDis; // 相机的位置的高度上加上调节。 
            // ==== [MODIFIED by LI Hao] ====
            // transform.position = pos;
            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * followSpeed);
            // ==============================

            if (lookCamera)
            {
                // ==== [MODIFIED by LI Hao] ====
                // Vector3 pos2 = pos * 2 - cameraNow.transform.position;
                // transform.LookAt(pos2);
                Vector3 towardTarget = transform.position - cameraNow.transform.position;
                
                if (towardTarget.magnitude > threshold)
                {
                    Quaternion targetRot = Quaternion.LookRotation(towardTarget.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
                // ==============================
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnLoaded;
        }

        private void OnSceneUnLoaded(Scene scene)
        {
            Destroy(this.gameObject);
        }
    }
}

