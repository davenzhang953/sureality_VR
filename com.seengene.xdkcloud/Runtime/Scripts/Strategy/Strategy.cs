using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seengene.XDK
{

    public enum SmoothMoveType
    {
        Linear = 0,
        EaseInOut = 1
    }



    [System.Serializable]
    public class Strategy
    {
        [Header("新一轮定位的触发条件")]
        /// <summary>
        /// 最大移动距离，超过这个距离马上开始新一轮定位
        /// </summary>
        [Range(2f, 60f)]
        public float RelocateDistance = 10;

        /// <summary>
        /// 最大时间间隔，超过这个时间马上开始新一轮定位
        /// </summary>
        [Range(0f, 99f)]
        public float RelocatePeriod = 20;

        /// <summary>
        /// 相邻2帧之间，相机位置差的阈值。
        /// </summary>
        [Range(0.1f, 10.0f)]
        public float CameraMoveThreshold = 3.0f;

        [Header("暂时阻止定位的条件")]
        /// <summary>
        /// 角速度限制，设备当前角速度超过这个值不能定位
        /// </summary>
        [Range(1f, 180f)]
        public float AngularSpeedLimit = 15;


        [Header("一轮定位的参数")]
        /// <summary>
        /// 一轮定位工作要求定位成功几次
        /// </summary>
        [Range(1, 8)]
        public int RelocateCountInRound = 3;


        /// <summary>
        /// 一轮定位中，2个定位请求之间的最小时间间隔。
        /// </summary>
        [Range(0.5f, 5.0f)]
        public float FrameIntervalInRound = 1.0f;

        /// <summary>
        /// 一轮定位工作，不能超过这个时间限制
        /// </summary>
        [Range(5, 80)]
        public int RoundTimeLimit = 15;

        [Header("定位结果应用的参数")]
        /// <summary>
        /// 应用定位结果时，对多少个结果进行过滤和运算
        /// </summary>
        [Range(1, 20)]
        public int ResultCountUsing = 7;

        /// <summary>
        /// 应用定位结果时，过滤掉稳定性差的结果的比例
        /// </summary>
        [Range(0, 0.5f)]
        public float ResultDropRatio = 0.3f;

        [Header("场景根节点平滑方式")]
        /// <summary>
        /// 修改SceneRoot位姿的方式
        /// </summary>
        public SmoothMoveType SmoothMoveType = SmoothMoveType.Linear;

        /// <summary>
        /// 修改SceneRoot位姿的平滑时间
        /// </summary>
        public float SmoothMoveTime = 1.0f;

        

        public override string ToString()
        {
            var str = $"RelocateDistance={RelocateDistance}, RelocatePeriod={RelocatePeriod}, CameraMoveThreshold={CameraMoveThreshold}" +
                $"AngularSpeedLimit={AngularSpeedLimit}, RelocateCountInRound={RelocateCountInRound}, "+
                $"FrameIntervalInRound={FrameIntervalInRound}, RoundTimeLimit={RoundTimeLimit}, "+
                $"ResultCountUsing={ResultCountUsing}, ResultDropRatio={ResultDropRatio}, "+
                $"SmoothMoveType={SmoothMoveType}, SmoothMoveTime={SmoothMoveTime}";
            return str;
        }

        public void RestToDefault()
        {
            RelocateDistance = 10;
            RelocatePeriod = 15;
            CameraMoveThreshold = 1f;
            AngularSpeedLimit = 40;
            RelocateCountInRound = 3;
            FrameIntervalInRound = 1;
            RoundTimeLimit = 15;
            ResultCountUsing = 5;
            ResultDropRatio = 0.41f;
            SmoothMoveType = SmoothMoveType.Linear;
            SmoothMoveTime = 1f;
        }

    }
}

