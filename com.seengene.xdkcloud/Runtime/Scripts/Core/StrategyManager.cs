using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Seengene.XDK
{
    
    public class StrategyManager 
    {
        private Strategy strategy;
        private AngularSpeed angularTool;
        private TraceUtil traceTool;
        private ResultFilter poseFilter;

        private float roundStartTime;
        private float readyTime;
        private bool checking;
        private bool conditionReady;
        private int SuccessInRound;
        private int RoundCounter;
        private int timeoutCounter = 0;
        private Vector3 lastCameraPos;
        private bool CameraPosSetted;
        private bool CameraJumped;
        private float TimeFindJump;


        public StrategyManager(Strategy _strategy)
        {
            strategy = _strategy;
            angularTool = new AngularSpeed();
            traceTool = new TraceUtil();

            StartWork();
        }

        public void StartWork()
        {
            RoundCounter = 0;
            timeoutCounter = 0;
            CameraPosSetted = false;
            CameraJumped = false;
            conditionReady = true; // 确保一开始就可以发送定位请求
            checking = false;
        }

        /// <summary>
        /// 开始新的一轮
        /// </summary>
        /// <param name="relocateRightNow"></param>
        public void StartNewRound(bool relocateRightNow = false)
        {
            checking = true;
            timeoutCounter = 0;
            conditionReady = relocateRightNow;
            traceTool.Clear();      // 距离计数重置
            roundStartTime = Time.time; // 时间计数重置
            readyTime = -1;
            SuccessInRound = 0;
            if (strategy.RoundTimeLimit < 5)
            {
                strategy.RoundTimeLimit = 5;
            }
            Debug.Log("---> Start a new round, RoundCounter=" + RoundCounter+ " relocateRightNow="+ relocateRightNow);
        }



        /// <summary>
        /// 获取瞬时角速度
        /// </summary>
        /// <param name="timeGap"></param>
        /// <returns></returns>
        public float GetAngularSpeed(float timeGap = 0.2f)
        {
            return angularTool.GetAngleSpeed(timeGap);
        }

        /// <summary>
        /// 是否需要发送定位请求
        /// </summary>
        /// <returns></returns>
        public bool NeedSendImage()
        {
            if (conditionReady)
            {
                if (GetAngularSpeed() < strategy.AngularSpeedLimit)
                {
                    return true;
                }
            }
            return false;
        }

        

        public int GetRound()
        {
            return RoundCounter;
        }


        /// <summary>
        /// 当前的各项条件的状态
        /// </summary>
        /// <returns></returns>
        public string GetConditions()
        {
            var omega = GetAngularSpeed().ToString("f2");
            var passed = (Time.time - roundStartTime).ToString("f1");
            var moved = traceTool.GetLengthNow().ToString("f1");
            return $"Strategy time: {passed} / {strategy.RelocatePeriod}, move: {moved} / {strategy.RelocateDistance}, omega={omega}";
        }


        /// <summary>
        /// 更新相机数据，判定开启定位的条件是否满足
        /// </summary>
        /// <param name="camera"></param>
        public void UpdateCamera(Camera camera)
        {
            Vector3 cameraPos = camera.transform.position;
            Quaternion cameraRot = camera.transform.rotation;
            traceTool.Add(cameraPos);
            angularTool.Add(cameraRot);

            if (CameraPosSetted)
            {
                var distance = Vector3.Distance(lastCameraPos, cameraPos);
                if (distance > strategy.CameraMoveThreshold) // 相机位置突变，超过指定的阈值
                {
                    CameraJumped = true;
                    TimeFindJump = Time.time;
                    SetCondationReady();
                    Debug.Log("distance 22 ="+distance+ " lastCameraPos="+ lastCameraPos+ " cameraPos=" + cameraPos+" time="+Time.time);
                }
            }
            
            lastCameraPos = cameraPos;
            CameraPosSetted = true;

            if (checking)
            {
                if (Time.time > roundStartTime + strategy.RelocatePeriod) // 时间超过
                {
                    SetCondationReady();
                }
                else if (traceTool.GetLengthNow() > strategy.RelocateDistance) // 距离超过
                {
                    SetCondationReady();
                }
            }
        }


        private void SetCondationReady()
        {
            conditionReady = true;
            RoundCounter++;
            readyTime = Time.time;
            checking = false;// Restart之后才会重新work
            timeoutCounter = 0; // 重置超时次数

            Debug.Log("Round_Ready, RoundCounter=" + RoundCounter);
        }


        /// <summary>
        /// 将定位结果添加到过滤器中
        /// </summary>
        /// <param name="scenePose"></param>
        /// <param name="cameraPose"></param>
        public void AddRelocateResult(int frameIndex, Pose scenePose, Pose cameraPose, out bool Jumped)
        {
            if (poseFilter == null)
            {
                var mapOffset = SceneRoot.Instance.GetMapObjRoot();
                var emptyNode = mapOffset.Find("NodeForTool");
                if (emptyNode == null)
                {
                    var obj = new GameObject("NodeForTool");
                    emptyNode = obj.transform;
                    emptyNode.SetParent(mapOffset);
                }
                poseFilter = new ResultFilter(emptyNode);
            }
            if (CameraJumped)
            {
                Jumped = true;
                Debug.Log("Camera jump!!!!! timeUsed="+(Time.time - TimeFindJump)+ " frameIndex="+ frameIndex);
                poseFilter.ClearHistory();
                CameraJumped = false;
            }
            else
            {
                Jumped = false;
            }

            poseFilter.Add(scenePose, cameraPose, RoundCounter);
            SuccessInRound++;
        }


        /// <summary>
        /// 移除原有的定位结果
        /// </summary>
        public void ClearHistorys()
        {
            if (poseFilter != null)
            {
                poseFilter.ClearHistory();
            }
            SuccessInRound = 0;

            Debug.Log("---> ClearHistorys");
        }


        /// <summary>
        /// 判定本轮是否结束
        /// </summary>
        /// <returns></returns>
        public bool IsRoundEnded()
        {
            if (conditionReady)
            {
                if (SuccessInRound >= strategy.RelocateCountInRound)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判定当前是否超时
        /// </summary>
        /// <returns></returns>
        public bool IsRoundTimeout()
        {
            if (conditionReady)
            {
                float cc = (Time.time - readyTime) / strategy.RoundTimeLimit;
                if ((int)cc > timeoutCounter)
                {
                    timeoutCounter = (int)cc;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 获取过滤后的定位结果
        /// </summary>
        /// <returns></returns>
        public bool GetFilterResult(out Pose scenePose, out float evaluation, out bool NoSmooth)
        {
            int resultCount = strategy.ResultCountUsing;
            float dropNum = resultCount * strategy.ResultDropRatio;
            if (poseFilter == null)
            {
                scenePose = new Pose();
                evaluation = 0;
                NoSmooth = false;
                return false;
            }
            else
            {
                poseFilter.GetAveragePose(resultCount, (int)dropNum, out scenePose, out evaluation, out NoSmooth);
                return true;
            }
            
        }
    }
}
