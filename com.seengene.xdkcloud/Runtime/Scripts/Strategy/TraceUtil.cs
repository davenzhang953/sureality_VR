using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seengene.XDK
{
    public class TraceUtil
    {
        private class Step
        {
            public float time;
            public Vector3 pos;
            public float distance; // distance between
        }
        private List<Step> history;
        private float lengthNow;

        public TraceUtil()
        {
            history = new List<Step>();
            lengthNow = 0;
        }


        /// <summary>
        /// 将当前位姿的数据添加到队列中
        /// </summary>
        /// <param name="pos"></param>
        public void Add(Vector3 pos)
        {
            var step = new Step();
            step.time = Time.time;
            step.pos = pos;
            if (history.Count > 0)
            {
                var prev = history[history.Count - 1];
                step.distance = Vector3.Distance(prev.pos, step.pos);
            }
            else
            {
                step.distance = 0;
            }
            history.Add(step);

            lengthNow += step.distance;
        }


        public void Clear()
        {
            lengthNow = 0f;
            if (history != null)
                history.Clear();
        }


        public float GetLengthNow()
        {
            return lengthNow;
        }


        /// <summary>
        /// 仅保留过去一段时间内的路径数据
        /// </summary>
        /// <param name="passTime"></param>
        public void RetainSteps(float passTime)
        {
            int index = -1;
            float endTime = Time.time;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].time < endTime - passTime)
                {
                    break; // 指定时间之前的，不需要统计
                }
                index = i;
            }
            if (index >= 0)
            {
                history.RemoveRange(0, index + 1);
            }
        }

        /// <summary>
        /// 路径的长度
        /// </summary>
        /// <param name="passTime"></param>
        /// <returns></returns>
        public float GetTraceLength(float passTime)
        {
            float endTime = Time.time;
            float total = 0;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].time < endTime - passTime)
                {
                    break; // 指定时间之前的，不需要统计
                }
                total += history[i].distance;
            }
            return total;
        }
    }
}

