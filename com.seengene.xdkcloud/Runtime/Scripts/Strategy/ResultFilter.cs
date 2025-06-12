using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seengene.XDK
{
    public class ResultFilter
    {
        private class Step
        {
            public int round;
            public Pose scenePose;
            public Pose cameraPose;

            public int weight; // 权重
            public Vector3 pos1; // 临时数据（计算评估值用）
            public Vector3 pos2; // 临时数据（计算评估值用）
            public float evaluation;// 评估值
        }


        private List<Step> history;
        private Transform parentNode;
        private Transform childNode;

        private Vector3 point1;
        private Vector3 point2;

        private Vector3 cameraPosition;    // 临时数据
        private Quaternion cameraRotation; // 临时数据
        private Vector3 average1; // 临时数据（pos1的平均值）
        private Vector3 average2; // 临时数据（pos2的平均值）


        //private bool cleared;



        public ResultFilter(Transform _emptyNode)
        {
            history = new List<Step>();
            parentNode = _emptyNode;


            if (parentNode.childCount == 0)
            {
                var obj = new GameObject("node");
                childNode = obj.transform;
                childNode.SetParent(parentNode);
            }
            else
            {
                childNode = parentNode.GetChild(0);
            }

            
            float forward = 1.0f; // 相机前1米的位置
            float t = forward / Mathf.Tan(Mathf.PI / 3); // 设定相机的fov是60度
            point1 = new Vector3(-t, 0, forward); // 相机成像后屏幕左侧中点，在相机坐标系中的位置
            point2 = new Vector3(t, 0, forward);  // 相机成像后屏幕右侧中点，在相机坐标系中的位置
        }


        /// <summary>
        /// 将位姿添加到队列中
        /// </summary>
        /// <param name="scenePose"></param>
        /// <param name="cameraPose"></param>
        public void Add(Pose scenePose, Pose cameraPose, int round)
        {
            Debug.Log("---> AddPoseToResultList, pos="+scenePose.position.ToString("f3")+" euler="+scenePose.rotation.eulerAngles.ToString("f2")+" round="+round);
            var step = new Step();
            step.round = round;
            step.scenePose = scenePose;
            step.cameraPose = cameraPose;
            history.Add(step);

            if (history.Count > 30)
            {
                history.RemoveAt(0);
            }
            
        }


        public void ClearHistory()
        {
            //cleared = true;
            if (history != null)
                history.Clear();
        }

        public int GetResultCount()
        {
            return history.Count;
        }


        /// <summary>
        /// 获取过滤后的位姿
        /// </summary>
        /// <param name="resultCount"></param>
        /// <param name="dropCount"></param>
        /// <returns></returns>
        public void GetAveragePose(int resultCount, int dropCount, out Pose scenePose, out float evaluation, out bool NoSmooth)
        {
            //if (cleared)
            //{
            //    Debug.Log("33333333");
            //    cleared = false;
            //}
            GeneratePose(resultCount, dropCount, out scenePose, out evaluation, out NoSmooth);
        }



        /// <summary>
        /// 根据多个定位结果，获得一个位姿。
        /// </summary>
        /// <param name="resultCount"></param>
        /// <param name="dropCount"></param>
        /// <returns></returns>
        private void GeneratePose(int resultCount, int dropCount, out Pose scenePose, out float evaluation, out bool NoSmooth)
        {
            int count = 0;
            resultCount = resultCount + 1;
            List<float> ttttt = new List<float>();
            do
            {
                count++;
                resultCount--;

                List<Step> temp = new List<Step>();
                for (int i = history.Count - 1; i >= 0; i--)
                {
                    temp.Add(history[i]); // 越晚的 item 的 index 越小
                    if (temp.Count >= resultCount)
                    {
                        break;
                    }
                }

                SetItemWeight(temp); // 给多个item设定权重

                GetCameraChildrens(temp); // 计算相机坐标系中的点在多次定位结果中的空间位置

                RecalculateAverages(temp); // 重新计算平均位置。

                CreateEvaluations(temp); // 重新计算各个Step的评估值

                FilterByEvaluation(temp, dropCount); // 抛弃部分评估值不好的对象。（和对象权重无关）

                scenePose = GetEveragePose(temp); // 从剩余的多个对象中计算得出平均值。

                evaluation = GetEvalutionForAveragePose(scenePose); // 给这个位姿计算评估值

                ttttt.Add(evaluation);
            } while (evaluation > 200 && count < 4);

            NoSmooth = ttttt.Count > 1;

            if (ttttt.Count > 1)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < ttttt.Count; i++)
                {
                    sb.Append(ttttt[i]);
                    sb.Append(" ");
                }
                Debug.Log("evaluations -> "+sb.ToString());
            }
        }

        /// <summary>
        /// 给多个item设定权重
        /// </summary>
        /// <param name="temp"></param>
        private void SetItemWeight(List<Step> temp)
        {
            for (int i = 0; i < temp.Count; i++)
            {
                if (i < temp.Count / 3.0f) // 最新的 1/3 定位结果的权重是3
                {
                    temp[i].weight = 3;
                }
                else if (i < temp.Count * 2 / 3.0f) // 中间的 1/3 定位结果的权重是2
                {
                    temp[i].weight = 2;
                }
                else // 最老的 1/3 定位结果的权重是1
                {
                    temp[i].weight = 1;
                }
            }
        }

        /// <summary>
        /// 计算相机屏幕左右2个点的位置
        /// </summary>
        /// <param name="temp"></param>
        private void GetCameraChildrens(List<Step> temp)
        {
            // 最后一次定位时，相机的位姿
            cameraPosition = temp[0].cameraPose.position;
            cameraRotation = temp[0].cameraPose.rotation;

            // 各节点的缩放值归1
            parentNode.localScale = Vector3.one;
            childNode.localScale = Vector3.one;

            // 将最后一次的相机位姿，应用在多次定位结果里。
            for (int i = 0; i < temp.Count; i++)
            {
                var item = temp[i];

                // parentNode体现了定位结果。
                parentNode.localPosition = item.scenePose.position;
                parentNode.localRotation = item.scenePose.rotation;

                // childNode体现了相机位姿
                childNode.localPosition = cameraPosition;
                childNode.localRotation = cameraRotation;

                // 相机屏幕上左右2个点在AR空间中的位置。
                item.pos1 = childNode.TransformPoint(point1);
                item.pos2 = childNode.TransformPoint(point2);
            }
        }

        /// <summary>
        /// 计算一组pos1、一组pos2的平均值
        /// </summary>
        /// <param name="temp"></param>
        private void RecalculateAverages(List<Step> temp)
        {
            average1 = Vector3.zero;
            average2 = Vector3.zero;

            for (int i = 0; i < temp.Count; i++)
            {
                var step = temp[i];
                average1 += step.pos1;
                average2 += step.pos2;
            }

            average1 /= temp.Count;
            average2 /= temp.Count;
        }

        /// <summary>
        /// 计算各个Step的评估值
        /// </summary>
        /// <param name="temp"></param>
        private void CreateEvaluations(List<Step> temp)
        {
            for (int i = 0; i < temp.Count; i++)
            {
                var step = temp[i];
                float dis1 = Vector3.Distance(step.pos1, average1) * 100; // 变成厘米单位
                float dis2 = Vector3.Distance(step.pos2, average2) * 100; // 变成厘米单位
                step.evaluation = dis1 * dis1 + dis2 * dis2;
            }
        }

        /// <summary>
        /// 获取过滤之后的Step，放弃一些位置变化大的对象。
        /// </summary>
        /// <param name="resultCount"></param>
        /// <returns></returns>
        private void FilterByEvaluation(List<Step> temp, int dropCount)
        {
            // 排序
            temp.Sort((a, b) => {
                if (a.evaluation < b.evaluation)
                    return -1;
                if (a.evaluation > b.evaluation)
                    return 1;
                return 0;
            });

            if (temp.Count <= 2) // 只有2个，不抛弃
            {
                dropCount = 0;
            }
            else if (temp.Count <= 4) // 4个以下，最多抛弃1个
            {
                if (dropCount > 1)
                    dropCount = 1;
            }

            // 抛弃一部分
            if (temp.Count > dropCount && dropCount > 0)
            {
                temp.RemoveRange(0, dropCount);
            }
        }

        /// <summary>
        /// 获取位姿的平均值（体现了权重）
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private Pose GetEveragePose(List<Step> temp)
        {
            List<QuaternionItem> quats = new List<QuaternionItem>();
            List<PositionItem> poses = new List<PositionItem>();

            for (int i = 0; i < temp.Count; i++)
            {
                Step item = temp[i];

                var quatItem = new QuaternionItem();
                quatItem.weight = item.weight;
                quatItem.quat = item.scenePose.rotation;
                quats.Add(quatItem);

                var posItem = new PositionItem();
                posItem.weight = item.weight;
                posItem.pos = item.scenePose.position;
                poses.Add(posItem);
            }

            Pose ret = new Pose();
            ret.position = PositionTool.AverageWithWeight(poses);
            ret.rotation = QuaternionTool.AverageWithWeight(quats);
            return ret;
        }


        /// <summary>
        /// 计算相机屏幕左右2个点的位置
        /// </summary>
        /// <param name="temp"></param>
        private float GetEvalutionForAveragePose(Pose averagePose)
        {
            // 各节点的缩放值归1
            parentNode.localScale = Vector3.one;
            childNode.localScale = Vector3.one;

            // parentNode体现了定位结果。
            parentNode.localPosition = averagePose.position;
            parentNode.localRotation = averagePose.rotation;

            // childNode体现了相机位姿
            childNode.localPosition = cameraPosition;
            childNode.localRotation = cameraRotation;

            // 相机屏幕上左右2个点在AR空间中的位置。
            Vector3 pos1 = childNode.TransformPoint(point1);
            Vector3 pos2 = childNode.TransformPoint(point2);
            float dis1 = Vector3.Distance(pos1, average1) * 100; // 变成厘米单位
            float dis2 = Vector3.Distance(pos2, average2) * 100; // 变成厘米单位
            float evaluation = dis1 * dis1 + dis2 * dis2;
            return evaluation;
        }
    }
}

