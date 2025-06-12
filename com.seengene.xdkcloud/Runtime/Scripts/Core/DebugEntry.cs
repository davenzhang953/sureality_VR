using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Seengene.XDK
{
    [RequireComponent(typeof(Canvas))]
    public class DebugEntry : MonoBehaviour
    {
        [SerializeField]
        private Button btnQuitApp;

        [SerializeField]
        private Button btnDebugMode;

        [SerializeField]
        private bool ShowFPS;


        public UnityEvent OnStartDebug = new UnityEvent();



        // Start is called before the first frame update
        void Start()
        {
            btnDebugMode.onClick.AddListener(() => {
                XDKCloudSession.IfDebugOn = true;
                OnStartDebug.Invoke();
                Debug.Log("XDKCloudSession.IfDebugOn=" + XDKCloudSession.IfDebugOn);
            });

            if(btnQuitApp != null)
            {
                btnQuitApp.onClick.AddListener(() => {
                    OnBtnQuitApp();
                });
            }

            if (!ShowFPS)
            {
                var fps = GetComponentInChildren<ShowFpsOnGUI>(true);
                if (fps)
                {
                    fps.gameObject.SetActive(false);
                    fps.enabled = false;
                }
            }
        }

        private void OnBtnQuitApp()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
