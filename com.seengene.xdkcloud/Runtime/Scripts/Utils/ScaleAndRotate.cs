using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seengene.XDK
{
    public class ScaleAndRotate : MonoBehaviour
    {
        /*app，手指触摸（Touc）屏幕，脚本挂载在相机，旋转相机查看物体，并限制角度和拉近距离，脚本挂载在相机
         */

        public GameObject target;//目标移动物体

        private Touch oldTouch1;  //上次触摸点1(手指1)  
        private Touch oldTouch2;  //上次触摸点2(手指2)  
        private float eulerAngles_x;
        private float eulerAngles_y;


        //水平滚动相关  
        public float xSpeed = 5.0f;//主相机水平方向旋转速度  


        //垂直滚动相关  
        public int yMaxLimit = 90;//最大y（单位是角度）  
        public int yMinLimit = -90;//最小y（单位是角度）  
        public float ySpeed = 10.0f;//主相机纵向旋转速度 

        void Start()
        {
            Vector3 eulerAngles = this.transform.eulerAngles;//当前物体的欧拉角  
            this.eulerAngles_x = eulerAngles.y;
            this.eulerAngles_y = eulerAngles.x;
        }


        void Update()
        {
            //没有触摸  
            if (Input.touchCount <= 0)
            {
                return;
            }
            //单点触摸， 水平上下旋转  
            if (1 == Input.touchCount)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 deltaPos = touch.deltaPosition;

                //无死角旋转
                //transform.RotateAround(target.transform.position, Vector3.up, deltaPos.x);
                //transform.RotateAround(target.transform.position, -1 * transform.right, deltaPos.y);

                float sum = Vector3.Distance(this.transform.position, target.transform.position);

                if (this.target != null)
                {
                    this.eulerAngles_x += ((deltaPos.x * this.xSpeed) * sum) * 0.005f;
                    this.eulerAngles_y -= (deltaPos.y * this.ySpeed) * 0.005f;
                    this.eulerAngles_y = ClampAngle(this.eulerAngles_y, (float)this.yMinLimit, (float)this.yMaxLimit);
                    Quaternion quaternion = Quaternion.Euler(this.eulerAngles_y, this.eulerAngles_x, (float)0);
                    Vector3 vector = ((Vector3)(quaternion * new Vector3((float)0, (float)0, -sum))) + this.target.transform.position;
                    //更改主相机的旋转角度和位置  
                    this.transform.rotation = quaternion;
                    this.transform.position = vector;
                }

            }

            //多点触摸, 放大缩小  
            Touch newTouch1 = Input.GetTouch(0);
            Touch newTouch2 = Input.GetTouch(1);

            //第2点刚开始接触屏幕, 只记录，不做处理  
            if (newTouch2.phase == TouchPhase.Began)
            {
                oldTouch2 = newTouch2;
                oldTouch1 = newTouch1;
                return;
            }

            //计算老的两点距离和新的两点间距离，变大要放大模型，变小要缩放模型  
            float oldDistance = Vector2.Distance(oldTouch1.position, oldTouch2.position);
            float newDistance = Vector2.Distance(newTouch1.position, newTouch2.position);

            //两个距离之差，为正表示放大手势， 为负表示缩小手势  
            float offset = newDistance - oldDistance;
            // DebugUtil.log("物体之间的距离：" + Vector3.Distance(this.transform.position, target.transform.position));
            if (offset > 0 && Vector3.Distance(this.transform.position, target.transform.position) > 4)
            {

                transform.Translate(Vector3.forward * 0.1f);
            }
            if (offset < 0 && Vector3.Distance(this.transform.position, target.transform.position) < 10)
            {
                transform.Translate(Vector3.forward * -0.1f);
            }

            //记住最新的触摸点，下次使用  
            oldTouch1 = newTouch1;
            oldTouch2 = newTouch2;
        }

        //把角度限制到给定范围内  

        public float ClampAngle(float angle, float min, float max)
        {
            while (angle < -360)
            {
                angle += 360;
            }

            while (angle > 360)
            {
                angle -= 360;
            }
            return Mathf.Clamp(angle, min, max);

        }
    }
}