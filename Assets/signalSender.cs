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
        [Tooltip("�ֲ�׷���¼���������ڶ����Խ����������Ƽ��ĸ��¹ؽ����ݡ�")]
        XRHandTrackingEvents m_LeftHandTrackingEvents;

        [SerializeField]
        [Tooltip("�ֲ�׷���¼���������ڶ����Խ����������Ƽ��ĸ��¹ؽ����ݡ�")]
        XRHandTrackingEvents m_RightHandTrackingEvents;

        [SerializeField]
        [Tooltip("�����⵽���ֲ���״�����ơ�")]
        ScriptableObject m_HandShapeOrPose;

        [SerializeField]
        [Tooltip("��ʼ�źš�")]
        string m_Signal;

        [SerializeField]
        [Tooltip("�����źš�")]
        string endSignal;

        [SerializeField]
        [Tooltip("�ֲ����뱣����������״�ͷ�������ʱ�䣬��ִ�����ơ�")]
        float m_MinimumHoldTime = 0.2f;

        [SerializeField]
        [Tooltip("ִ�����Ƽ���ʱ������")]
        float m_GestureDetectionInterval = 0.1f;

        //[SerializeField]
        [Tooltip("����ά��ʱ�䡣")]
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
                m_WasDetected = false; // ���ü��״̬
            }

            m_WasDetected = detected;

            if (!m_PerformedTriggered && detected)
            {
                var holdTimer = Time.timeSinceLevelLoad - m_HoldStartTime;
                if (holdTimer > m_HoldDuration)
                {
                    // ���ͽ����ź�
                    SignalManager.Instance.SendEndSignal(endSignal);
                    Debug.Log($"�����ź��ѷ���: {endSignal} by {gameObject.name}");

                    // �ӳ����뷢�Ϳ�ʼ�ź�
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        SignalManager.Instance.SendSignal(m_Signal);
                        Debug.Log($"�ź��ѷ���: {m_Signal} by {gameObject.name}");
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
