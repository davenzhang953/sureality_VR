using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace Seengene.XDK
{
    public class AngularSpeed
    {
        private class Step
        {
            public float time;
            public Quaternion quat;
            public float angle; // angle between
        }
        private List<Step> history;

        public AngularSpeed()
        {
            history = new List<Step>();
        }


        /// <summary>
        /// 将当前位姿的角度数据添加到队列中
        /// </summary>
        /// <param name="quat"></param>
        public void Add(Quaternion quat)
        {
            var step = new Step();
            step.time = Time.time;
            step.quat = quat;
            if (history.Count > 0)
            {
                var prev = history[history.Count - 1];
                step.angle = Quaternion.Angle(prev.quat, step.quat);
            }
            else
            {
                step.angle = 0;
            }
            history.Add(step);

            if (history.Count > 200)
            {
                history.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取角速度
        /// </summary>
        /// <param name="timeGap">统计角速度的时间片段长度</param>
        /// <returns></returns>
        public float GetAngleSpeed(float timeGap = 0.2f)
        {
            Step next = null;
            float pass = 0f;
            float total = 0;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (next != null)
                {
                    pass += next.time - history[i].time;
                    total += next.angle;
                }
                next = history[i];

                if(pass >= timeGap)
                {
                    break;
                }
            }
            if (pass > 0)
            {
                return total / pass;
            }
            return 0;
        }
    }
}
