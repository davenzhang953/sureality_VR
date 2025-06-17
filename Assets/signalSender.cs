using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands;
using DG.Tweening;


namespace UnityEngine.XR.Hands.Samples.GestureSample
{
    public class GestureSignalSender : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("手部追踪事件组件，用于订阅以接收用于手势检测的更新关节数据。")]
        XRHandTrackingEvents m_LeftHandTrackingEvents;

        [SerializeField]
        [Tooltip("手部追踪事件组件，用于订阅以接收用于手势检测的更新关节数据。")]
        XRHandTrackingEvents m_RightHandTrackingEvents;

        [SerializeField]
        [Tooltip("必须检测到的手部形状或姿势。")]
        ScriptableObject m_HandShapeOrPose;

        [SerializeField]
        [Tooltip("开始信号。")]
        string m_Signal;

        [SerializeField]
        [Tooltip("结束信号。")]
        string endSignal;

        [SerializeField]
        [Tooltip("手部必须保持在所需形状和方向的最短时间，以执行手势。")]
        float m_MinimumHoldTime = 0.2f;

        [SerializeField]
        [Tooltip("执行手势检测的时间间隔。")]
        float m_GestureDetectionInterval = 0.1f;

        //[SerializeField]
        [Tooltip("手势维持时间。")]
        float m_HoldDuration = 0.5f;

        XRHandShape m_HandShape;
        XRHandPose m_HandPose;
        bool m_WasDetected;
        bool m_PerformedTriggered;
        float m_TimeOfLastConditionCheck;
        float m_HoldStartTime;

        void OnEnable()
        {
            if (m_LeftHandTrackingEvents != null)
            {
                m_LeftHandTrackingEvents.jointsUpdated.AddListener(OnLeftHandJointsUpdated);
            }

            if (m_RightHandTrackingEvents != null)
            {
                m_RightHandTrackingEvents.jointsUpdated.AddListener(OnRightHandJointsUpdated);
            }

            m_HandShape = m_HandShapeOrPose as XRHandShape;
            m_HandPose = m_HandShapeOrPose as XRHandPose;
        }

        void OnDisable()
        {
            if (m_LeftHandTrackingEvents != null)
            {
                m_LeftHandTrackingEvents.jointsUpdated.RemoveListener(OnLeftHandJointsUpdated);
            }

            if (m_RightHandTrackingEvents != null)
            {
                m_RightHandTrackingEvents.jointsUpdated.RemoveListener(OnRightHandJointsUpdated);
            }
        }

        void OnLeftHandJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            ProcessJointsUpdated(eventArgs);
        }

        void OnRightHandJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            ProcessJointsUpdated(eventArgs);
        }

        void ProcessJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            if (!isActiveAndEnabled || Time.timeSinceLevelLoad < m_TimeOfLastConditionCheck + m_GestureDetectionInterval)
                return;

            var detected =
                eventArgs.hand.isTracked &&
                (m_HandShape != null && m_HandShape.CheckConditions(eventArgs)) ||
                (m_HandPose != null && m_HandPose.CheckConditions(eventArgs));

            if (!m_WasDetected && detected)
            {
                m_HoldStartTime = Time.timeSinceLevelLoad;
            }
            else if (m_WasDetected && !detected)
            {
                m_PerformedTriggered = false;
                m_WasDetected = false; // 重置检测状态
            }

            m_WasDetected = detected;

            if (!m_PerformedTriggered && detected)
            {
                var holdTimer = Time.timeSinceLevelLoad - m_HoldStartTime;
                if (holdTimer > m_HoldDuration)
                {
                    // 发送结束信号
                    SignalManager.Instance.SendEndSignal(endSignal);
                    Debug.Log($"结束信号已发送: {endSignal} by {gameObject.name}");

                    // 延迟三秒发送开始信号
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        SignalManager.Instance.SendSignal(m_Signal);
                        Debug.Log($"信号已发送: {m_Signal} by {gameObject.name}");
                    });

                    m_PerformedTriggered = true;
                }
            }

            m_TimeOfLastConditionCheck = Time.timeSinceLevelLoad;
        }

        public void SetGesture(ScriptableObject newHandShapeOrPose, string newSignal)
        {
            m_HandShapeOrPose = newHandShapeOrPose;
            m_Signal = newSignal;

            m_HandShape = newHandShapeOrPose as XRHandShape;
            m_HandPose = newHandShapeOrPose as XRHandPose;
        }
    }
}
