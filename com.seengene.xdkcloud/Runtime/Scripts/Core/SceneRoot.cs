using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Seengene.XDK
{
    public class SceneRoot : MonoBehaviour
    {
        public static SceneRoot Instance;

        private Vector3 m_StartPosition;
        private Quaternion m_StartRotation;


        [HideInInspector]
        public bool IsAtOrigin;

        [HideInInspector]
        public float m_SmoothMoveTime = 1.0f;

        [HideInInspector]
        public SmoothMoveType m_SmoothMoveType;

        [HideInInspector]
        public Vector3 m_SpatialScale = Vector3.one;
        private Vector3 m_TargetPosition;
        private Quaternion m_TargetRotation;

        private Coroutine moveSceneRoot;
        private Transform MapOffset;

        [SerializeField]
        private bool HideChildrenOnStart;
        private bool HideByApplicationPause;

        private void Awake()
        {
            if (transform.childCount != 1)
            {
                Debug.LogError("SceneRoot should has one child, and it's name should be MapOffset");
                return;
            }
            Instance = this;
            MapOffset = transform.GetChild(0);
            IsAtOrigin = true;
        }

        private void Start()
        {
            if (HideChildrenOnStart)
            {
                ShowAllChildren(false);
            }

        }

        private void OnApplicationPause(bool _pause)
        {
            Debug.Log("---> puase sceneRoot =" + _pause);
            if (_pause)
            {
                ShowAllChildren(false);
                HideByApplicationPause = true;
            }
        }



        public void ResetItself()
        {
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 重置MapOffset的参数。用在按时间切换地图的功能里。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="euler"></param>
        /// <param name="scale"></param>
        public void ResetMapOffset(Vector3 pos, Vector3 euler, Vector3 scale)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(pos, Quaternion.Euler(euler), scale);
            matrix = matrix.inverse;

            MapOffset.localPosition = matrix.GetPosition();
            MapOffset.localRotation = matrix.GetRotation();
            MapOffset.localScale = matrix.GetScale();
        }

        public Transform GetMapObjRoot()
        {
            return MapOffset;
        }


        public string getRootInfo()
        {
            return string.Format("Pos={0}, Euler={1}, Scale={2}",
                    MapOffset.position.ToString("f2"),
                    MapOffset.eulerAngles.ToString("f2"),
                    MapOffset.lossyScale.ToString("f2")
                );
        }


        public void ShowAllChildren(bool visi)
        {
            if (MapOffset)
            {
                MapOffset.gameObject.SetActive(visi);
            }
        }


        /// <summary>
        /// 定位成功后，根据定位结果调整 SceneRoot 的位姿
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="m_RelocSuccessCounter"></param>
        /// <param name="m_SpatialPosition"></param>
        /// <param name="m_SpatialRotation"></param>
        public void OnSpatialMapped(bool noSmooth, Vector3 m_SpatialPosition, Quaternion m_SpatialRotation)
        {
            if (transform.childCount != 1)
            {
                Debug.LogError("SceneRoot should has one child, and it's name should be MapOffset");
                return;
            }
            if (HideChildrenOnStart)
            {
                MapOffset.gameObject.SetActive(true);
            }
            if (HideByApplicationPause)
            {
                MapOffset.gameObject.SetActive(true);
                HideByApplicationPause = false;
            }



            if (moveSceneRoot != null)
            {
                StopCoroutine(moveSceneRoot); // 停止之前的平滑过程
                moveSceneRoot = null;
            }
            transform.localScale = m_SpatialScale;
            if (IsAtOrigin || m_SmoothMoveTime < 0.01f || noSmooth)
            {
                IsAtOrigin = false;
                transform.position = m_SpatialPosition;
                transform.rotation = m_SpatialRotation;
            }
            else
            {
                m_TargetPosition = m_SpatialPosition;
                m_TargetRotation = m_SpatialRotation;
                moveSceneRoot = StartCoroutine(CorMoveToTarget());
            }
        }

        /// <summary>
        /// 进行平滑移动
        /// </summary>
        /// <returns></returns>
        private IEnumerator CorMoveToTarget()
        {
            m_StartPosition = transform.position;
            m_StartRotation = transform.rotation;


            if (m_SmoothMoveType == SmoothMoveType.Linear)
            {
                float factor = 0;
                while (factor < 1)
                {
                    factor += Time.deltaTime / m_SmoothMoveTime;
                    factor = Mathf.Clamp01(factor);
                    transform.position = Vector3.Lerp(m_StartPosition, m_TargetPosition, factor);
                    transform.rotation = Quaternion.Lerp(m_StartRotation, m_TargetRotation, factor);
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                float passed = 0f;
                while (passed < m_SmoothMoveTime)
                {
                    passed += Time.deltaTime;
                    float factor = EaseInOutRatio(passed, m_SmoothMoveTime);
                    transform.position = Vector3.Lerp(m_StartPosition, m_TargetPosition, factor);
                    transform.rotation = Quaternion.Lerp(m_StartRotation, m_TargetRotation, factor);
                    yield return new WaitForEndOfFrame();
                }
            }

            /// final state
            transform.position = m_TargetPosition;
            transform.rotation = m_TargetRotation;
        }



        private float EaseInOutRatio(float curTime, float duration)
        {
            var progress = 2 * curTime / duration;
            if (progress < 1)
            {
                return Mathf.Pow(progress, 3) / 2;
            }
            else
            {
                progress -= 2;
                return Mathf.Pow(progress, 3) / 2 + 1;
            }
        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
