using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Seengene.XDK
{
    public class DebugJoystick : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public UnityAction<Vector2> onJoystickDraging;
        public UnityAction onJoystickBeginDrag;
        public UnityAction onJoystickEndDrag;

        RectTransform rectTrans;
        RectTransform parentRectTrans;
        bool isDraging;
        Vector2 dir;
        void Awake()
        {
            rectTrans = transform.GetComponent<RectTransform>();
            parentRectTrans = transform.parent.GetComponent<RectTransform>();
        }

        void Update()
        {
            if (isDraging && dir != Vector2.zero)
            {
                onJoystickDraging?.Invoke(dir);
            }
        }
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraging)
            {
                onJoystickBeginDrag?.Invoke();
                isDraging = true;
            }

        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (parentRectTrans)
            {
                if (isDraging)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTrans, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    float limitValue = parentRectTrans.sizeDelta.x / 2.0f - rectTrans.sizeDelta.x / 2.0f;
                    if (Vector2.Distance(localPoint, Vector2.zero) > limitValue)
                    {
                        localPoint = localPoint.normalized * limitValue;
                    }
                    rectTrans.anchoredPosition = localPoint;
                    dir = localPoint.normalized;
                }

                if (eventData.pointerDrag != this.gameObject)
                {
                    isDraging = false;
                    dir = Vector2.zero;
                }
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            isDraging = false;
            rectTrans.anchoredPosition = Vector2.zero;
            onJoystickEndDrag?.Invoke();
            dir = Vector2.zero;
        }

    }
}

