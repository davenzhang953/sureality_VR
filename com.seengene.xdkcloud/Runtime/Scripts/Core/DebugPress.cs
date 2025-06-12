using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Seengene.XDK
{
    public class DebugPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action OnPressing;

        private bool pressing;

        public void OnPointerDown(PointerEventData eventData)
        {
            pressing = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pressing = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressing = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (pressing)
            {
                OnPressing?.Invoke();
            }
        }
    }
}

