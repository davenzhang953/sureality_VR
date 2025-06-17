using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seengene.XDK
{
    public class ObjectTool 
    {
        public static T FindAnyObjectByType<T>(bool includeInavtive = false) where T : UnityEngine.Object
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var objects = scene.GetRootGameObjects();
            foreach (var item in objects)
            {
                var t = item.GetComponentInChildren<T>(includeInavtive);
                if (t != null)
                {
                    return t;
                }
            }
            return default(T);
        }

        public static void SetLeft(RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        public static void SetHeight(RectTransform rt, float hh)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, hh);
        }

        public static void SetPosY(RectTransform rt, float yy)
        {
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yy);
        }

        public static void SetPosX(RectTransform rt, float xx)
        {
            rt.anchoredPosition = new Vector2(xx, rt.anchoredPosition.y);
        }


        public static void SetPosXY(GameObject obj, float xx, float yy)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            SetPosXY(rt, xx, yy);
        }


        public static void SetPosXY(RectTransform rt, float xx, float yy)
        {
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(xx, yy);
            }
            else
            {
                Debug.Log("obj has no RectTransform");
            }
        }

        public static Vector3 ClampEuler(Vector3 euler)
        {
            while(euler.x < 0)
            {
                euler.x += 360;
            }
            while (euler.y < 0)
            {
                euler.y += 360;
            }
            while (euler.z < 0)
            {
                euler.z += 360;
            }

            while (euler.x >= 360)
            {
                euler.x -= 360;
            }
            while (euler.y >= 360)
            {
                euler.y -= 360;
            }
            while (euler.z >= 360)
            {
                euler.z -= 360;
            }
            return euler;
        }


        public static Pose HuaweiPoseToUnity(Pose p)
        {
            Vector3 euler = p.rotation.eulerAngles;
            euler.y -= 180;
            euler = ClampEuler(euler);

            Vector3 pos = p.position;
            pos.x = -pos.x;
            pos.z = -pos.z;

            Pose ret = new Pose();
            ret.position = pos;
            ret.rotation = Quaternion.Euler(euler);
            return ret;
        }




        /// <summary>
        /// 打印对象的层级信息
        /// </summary>
        /// <param name="go"></param>
        /// <param name="prefix"></param>
        public static void PrintGameObject(GameObject go, string prefix = "")
        {
            if (go == null)
            {
                Debug.LogError(prefix);
                return;
            }
            PrintGameObject(go.transform, 0, prefix);
        }



        /// <summary>
        /// 打印对象的层级信息
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="gen"></param>
        /// <param name="sb"></param>
        private static void PrintGameObject(Transform trans, int gen = 0, string prefix = null)
        {
            if (trans != null)
            {
                StringBuilder sb = new StringBuilder();
                if(prefix != null)
                {
                    sb.Append(prefix);
                    sb.Append(' ');
                }
                sb.Append(makeGenStr(gen) + gen + " ");
                sb.Append(trans.name);
                sb.Append(" localPos=");
                sb.Append(trans.localPosition.ToString());
                sb.Append(" scale=");
                sb.Append(trans.localScale.ToString());
                Debug.Log(sb.ToString());
                //遍历当前物体及其所有子物体
                for (int i = 0; i < trans.childCount; i++)
                {
                    Transform child = trans.GetChild(i);
                    PrintGameObject(child, gen + 1, prefix);
                }
            }
        }




        private static string makeGenStr(int gen)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < gen; i++)
            {
                sb.Append('\t');
            }
            return sb.ToString();
        }



    }
}

