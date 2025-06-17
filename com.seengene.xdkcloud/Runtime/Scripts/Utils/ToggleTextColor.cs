using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Seengene.XDK
{
    public class ToggleTextColor : MonoBehaviour
    {
        [SerializeField] private Color m_ColorUnactive = Color.black;
        [SerializeField] private Color m_ColorActive = Color.white;

        private Text m_Text = null;

        private void Awake()
        {
            if (m_Text == null)
                m_Text = GetComponent<Text>();
        }

        public void SetColor(bool value)
        {
            if (m_Text == null) return;

            if (value)
            {
                m_Text.color = m_ColorActive;
            }
            else
            {
                m_Text.color = m_ColorUnactive;
            }
        }
    }
}
