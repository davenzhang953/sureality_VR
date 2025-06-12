using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEngine.SceneManagement;

namespace Seengene.XDK
{
    [InitializeOnLoad]
    public class EnvironmentCheck
    {
        static EnvironmentCheck()
        {
            CheckGlasses();
        }


        private static void CheckGlasses()
        {
            bool foundIt = false;
            Scene scene = SceneManager.GetActiveScene();
            var objects = scene.GetRootGameObjects();
            foreach (var item in objects)
            {
                var t = item.GetComponentInChildren<ARGlasses>(true);
                if (t != null)
                {
                    foundIt = true;
                    break;
                }
            }

            if (foundIt)
            {
                Debug.Log("Editor: Add Define AR_GLASSES");
                Unity.VisualScripting.DefineUtility.AddDefine("AR_GLASSES");
            }
        }
    }
}