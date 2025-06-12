using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Seengene.XDK
{

    public class PositionItem
    {
        public int weight;
        public Vector3 pos;
    }

    public class PositionTool
    {
        public static Vector3 Average(List<Vector3> arr)
        {
            if (arr.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < arr.Count; i++)
                {
                    sum += arr[i];
                }
                return sum / arr.Count;
            }
            return Vector3.zero;
        }




        /// <summary>
        /// 每个Position对象包含一个权重，然后进行平均值计算。
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Vector3 AverageWithWeight(List<PositionItem> arr)
        {
            if (arr.Count > 0)
            {
                int num = 0;
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i].weight == 0)
                    {
                        arr[i].weight = 1;
                    }
                    sum += arr[i].pos * arr[i].weight;
                    num += arr[i].weight;
                }
                return sum / num;
            }
            return Vector3.zero;
        }
    }
}
