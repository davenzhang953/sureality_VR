using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Seengene.XDK
{
    public class QuaternionItem
    {
        public int weight;
        public Quaternion quat;
    }


    public static class QuaternionTool
    {


        public static Quaternion Average(List<Quaternion> quatArray)
        {
            List<QuaternionItem> list = new List<QuaternionItem>();
            for (int i = 0; i < quatArray.Count; i++)
            {
                QuaternionItem item = new QuaternionItem();
                item.weight = 1;
                item.quat = quatArray[i];
                list.Add(item);
            }

            while (list.Count > 1)
            {
                List<QuaternionItem> next = new List<QuaternionItem>();
                for (int i = 0; i < list.Count; i += 2)
                {
                    if (i + 1 >= list.Count)
                    {
                        // 只剩一个对象，直接放到next中
                        next.Add(list[i]);
                    }
                    else // 每次取出2个对象，在这2个对象间做插值。
                    {
                        var a = list[i];
                        var b = list[i + 1];
                        var ratio = a.weight * 1.0f / (a.weight + b.weight);
                        var avgQuat = Quaternion.LerpUnclamped(a.quat, b.quat, 1 - ratio);

                        // 将插值后的结果放到next中
                        QuaternionItem item = new QuaternionItem();
                        item.weight = a.weight + b.weight;
                        item.quat = avgQuat;
                        next.Add(item);
                    }
                }
                list = next;
            }

            var result = list[0];
            return result.quat;
        }




        /// <summary>
        /// 每个Quaternion对象包含一个权重，然后进行平均值计算。
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Quaternion AverageWithWeight(List<QuaternionItem> list)
        {
            while (list.Count > 1)
            {
                List<QuaternionItem> next = new List<QuaternionItem>();
                for (int i = 0; i < list.Count; i += 2)
                {
                    if (i + 1 >= list.Count)
                    {
                        // 只剩一个对象，直接放到next中
                        next.Add(list[i]); 
                    }
                    else // 每次取出2个对象，在这2个对象间做插值。
                    {
                        var a = list[i];
                        var b = list[i + 1];
                        if (a.weight == 0)
                            a.weight = 1;
                        if (b.weight == 0)
                            b.weight = 1;
                        var ratio = a.weight * 1.0f / (a.weight + b.weight);
                        var avgQuat = Quaternion.LerpUnclamped(a.quat, b.quat, 1 - ratio);

                        // 将插值后的结果放到next中
                        QuaternionItem item = new QuaternionItem();
                        item.weight = a.weight + b.weight;
                        item.quat = avgQuat;
                        next.Add(item);
                    }
                }
                list = next;
            }

            var result = list[0];
            return result.quat;
        }
    }
}
