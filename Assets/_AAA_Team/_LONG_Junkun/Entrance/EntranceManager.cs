using UnityEngine;
using UnityEngine.SceneManagement;

    public class EntranceManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Camera cameraNow;
        [SerializeField] private GameObject entranceSphere;
        [SerializeField] private GameObject entrancePanel;
        [SerializeField] private GameObject entranceGate;

        [Header("Panel Position Settings")]
        [SerializeField] private float panelForwardDis = 2.0f;
        [SerializeField] private float panelUpwardDis = 0.0f;

        [Header("Gate Position Settings")]
        [SerializeField] private float gateForwardDis = 3.0f;
        [SerializeField] private float gateUpwardDis = 0.0f;

        [Header("Common Follow Settings")]
        [SerializeField] private bool useCameraY = true;
        [SerializeField] private bool lookCameraPanel = true;
        [SerializeField] private bool lookCameraGate = true;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 5f;

        public bool followFlag = true;

        private void Awake()
        {
            SceneManager.sceneUnloaded += OnSceneUnLoaded;
        }

        private void Start()
        {
            if (cameraNow == null)
                cameraNow = Camera.main;


            if (entranceSphere != null)
            {
                entranceSphere.SetActive(true);
                entranceSphere.transform.position = cameraNow.transform.position;
            }

            if (entrancePanel != null) entrancePanel.SetActive(true);
            if (entranceGate != null) entranceGate.SetActive(false);

            UpdatePanelTransform();
            UpdateGateTransform();
        }

        private void Update()
        {
            if (followFlag == true)
            {
                UpdatePanelTransform();
                UpdateGateTransform();
            }
        }

        private void UpdatePanelTransform()
        {
            if (entrancePanel == null || cameraNow == null) return;

            Vector3 camPos = cameraNow.transform.position;
            Vector3 forward = cameraNow.transform.forward;

            if (useCameraY)
            {
                forward.y = 0f;
                forward.Normalize();
            }

            // 位置
            Vector3 targetPos = camPos + forward * panelForwardDis;
            targetPos.y += panelUpwardDis;
            entrancePanel.transform.position = Vector3.Lerp(
                entrancePanel.transform.position,
                targetPos,
                followSpeed * Time.deltaTime
            );

            // 旋转：锁 Y 轴竖直，只围绕 Y 轴旋转面向相机
            if (lookCameraPanel)
            {
                Vector3 dir = targetPos - camPos;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    entrancePanel.transform.rotation = Quaternion.Slerp(
                        entrancePanel.transform.rotation,
                        targetRot,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        private void UpdateGateTransform()
        {
            if (entranceGate == null || cameraNow == null) return;

            Vector3 camPos = cameraNow.transform.position;
            Vector3 forward = cameraNow.transform.forward;

            if (useCameraY)
            {
                forward.y = 0f;
                forward.Normalize();
            }

            // 位置
            Vector3 targetPos = camPos + forward * gateForwardDis;
            targetPos.y += gateUpwardDis;
            entranceGate.transform.position = Vector3.Lerp(
                entranceGate.transform.position,
                targetPos,
                followSpeed * Time.deltaTime
            );

            // 只在 Z 轴旋转：X、Y 锁定，Z 随相机 Yaw 变化
            if (lookCameraGate)
            {
                // 计算相机在水平面上的朝向角（Yaw）
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                // 构造仅修改 Z 轴的旋转：Euler(X=-90, Y=0, Z=yaw)
                Quaternion targetRot = Quaternion.Euler(-90f, 0f, yaw);
                entranceGate.transform.rotation = Quaternion.Slerp(
                    entranceGate.transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
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

