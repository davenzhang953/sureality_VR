using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Seengene.XDK
{
    public class ShowFpsOnGUI : MonoBehaviour
    {

        public float fpsMeasuringDelta = 2.0f;

        private float timePassed;
        private int m_FrameCount = 0;
        private float m_FPS = 0.0f;

        [SerializeField]
        private Text textfield;

        private void Start()
        {
            timePassed = 0.0f;
        }

        private void Update()
        {
            m_FrameCount = m_FrameCount + 1;
            timePassed = timePassed + Time.deltaTime;

            if (timePassed > fpsMeasuringDelta)
            {
                m_FPS = m_FrameCount / timePassed;

                timePassed = 0.0f;
                m_FrameCount = 0;

                if (textfield)
                    textfield.text = "FPS: " + m_FPS.ToString("f2") + "/" + Application.targetFrameRate;
            }
        }

        private void OnGUI()
        {
            if (textfield)
            {
                return;
            }
            GUIStyle bb = new GUIStyle();
            bb.normal.background = null;
            bb.normal.textColor = new Color(1.0f, 0.5f, 0.0f);
            bb.fontSize = 30;

            //居中显示FPS
            GUI.Label(new Rect(Screen.width - 460, 2, 200, 200), "FPS: " + m_FPS.ToString("f2") + "/" + Application.targetFrameRate, bb);
        }
    }
}