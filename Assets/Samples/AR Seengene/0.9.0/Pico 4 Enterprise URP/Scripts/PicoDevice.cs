using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Seengene.XDK;
using Unity.XR.CoreUtils;
using Unity.XR.PICO.TOBSupport;
using Unity.XR.PXR;
using UnityEngine;

/// <summary>
/// 要点：
///     1、XR Origin节点中的Camera Y Offset需要设置成0。否则影响到定位结果。
///     2、URP的AssetSetting中，必须将Quality中的HDR取消勾选。依据Pico文档：https://developer-cn.pico-interactive.com/document/unity/seethrough/
/// </summary>
public class PicoDevice : XDKHardware
{
    private static int dataIndex;

    [SerializeField]
    private XROrigin xrOrigin;

    [SerializeField]
    private GameObject leftHand;
    
    [SerializeField]
    private GameObject rightHand;

    
    private bool cpuImageReady;

    private byte[] CameraDataRGB;
    private int CameraDataWidth;
    private int CameraDataHeight;
    private Vector3 cameraPos;
    private Quaternion cameraRot;
    private int trackingStatus;
    private RGBCameraParamsNew cameraParams;
    private float lastCheckTime = -1f;
    private bool IsOpenDone = false;

    private bool IsWorking = false;
    private bool AccessingFrame = false;

    private int originWidth = 2048;
    private int originHeight = 1536;
    private byte[] originImgByte;
    private PXRCaptureRenderMode captureRenderMode = PXRCaptureRenderMode.PXRCapture_RenderMode_LEFT;
    private bool isRunning = false;
    private static bool reopen = false;
    private byte[] CameraDataRGBA;
    /// <summary>
    /// 爻图需要上报的Camera坐标
    /// </summary>
    public Transform mainCameraTransform;
    private void Awake()
    {
        PXR_Manager.EnableVideoSeeThrough = true;
        // PXR_Boundary.UseGlobalPose(true);
        PXR_Enterprise.Configurefor4U();
        Debug.Log($"{tag} Awake: OpenCameraAsyncfor4U");
        PXR_Enterprise.OpenCameraAsyncfor4U(ret =>
        {
            Debug.Log($"{tag} OpenCameraAsyncFor4U ret=  {ret}");
            StartCoroutine(DelayStartGetImageData());
        });
    }
    
    /// <summary>
    /// 延迟开启获取RGB Camera数据,立刻获取经常会出现意外crash
    /// </summary>
    /// <returns></returns>
    private IEnumerator DelayStartGetImageData()
    {
        yield return new WaitForSeconds(3.0f);
        StartGetImageData();
    }
    

    // 相机开启和关闭接口需要成对调用。
    private void OnDisable()
    {
        bool succ = PXR_Enterprise.CloseCamerafor4U();
        Debug.Log($"{tag} Close Pico rgb camera, result = " + succ);
        IsOpenDone = false;
    }

    private void Start()
    {
        // 1、打开全局 seethrgouth
        // PXR_Boundary.EnableSeeThroughManual(true);

        // 2、设置全局坐标系(此时，Unity Camera.main.transform 数据来源：head globalPose，与                 AcquireVSTCameraFrameAntiDistortion返回值 frame.globalPose数据源相同)
        PXR_Plugin.Boundary.UPxr_SetSeeThroughState(true);

        // 3、初始化接口
        PXR_Enterprise.InitEnterpriseService();
        
        originImgByte = new byte[originWidth*originHeight*4];
    }

    unsafe void CopyBytes(byte[] src, byte[] dst)
    {
        fixed (byte* pSrc = src, pDst = dst)
        {
            Buffer.MemoryCopy(pSrc, pDst, dst.Length, Math.Min(src.Length, dst.Length));
        }
    }

    private void StartGetImageData()
    {
        Debug.Log($"{tag} StartGetImageData");
        IntPtr data = Marshal.UnsafeAddrOfPinnedArrayElement(originImgByte,0);
        PXR_Enterprise.SetCameraFrameBufferfor4U(originWidth,originHeight,ref data, (Frame frame) =>
        {
            CameraDataWidth = (int)frame.width;
            CameraDataHeight = (int)frame.height;
            if (CameraDataRGB == null)
            {
                CameraDataRGB = new byte[originWidth*originHeight*3];
            }
            
            if (CameraDataRGBA == null)
            {
                CameraDataRGBA = new byte[originWidth*originHeight*4];
            }
            
            CopyBytes(originImgByte, CameraDataRGBA);
            
            cpuImageReady = true;
            
            Debug.Log($"{tag} Get available image from Pico RGB Camera, size = "+frame.datasize);
        });
        Debug.Log($"{tag} Pre to StartGetImageDatafor4U Mode=  {captureRenderMode}");
        bool ret = PXR_Enterprise.StartGetImageDatafor4U(captureRenderMode, originWidth, originHeight);
        isRunning = true;
        Debug.Log($"{tag} StartGetImageDatafor4U ret=  {ret}");
    }

    private void Update()
    {
        if (reopen)
        {
            reopen = false;
            Debug.Log($"{tag} Check reopen flag, reopen Pico rgb camera");
            // StartCoroutine(DelayStartGetImageData());
            PXR_Enterprise.StartGetImageDatafor4U(captureRenderMode, originWidth, originHeight);
        }
        
        if (Time.time < lastCheckTime + 1)
        {
            return;
        }
        lastCheckTime = Time.time;
        CheckHandsRayController();
    }
    

    /// <summary>
    /// 检查，是否要隐藏掉左右手柄
    /// </summary>
    private void CheckHandsRayController()
    {
        var showLeft = PXR_Input.IsControllerConnected(PXR_Input.Controller.LeftController);
        var showRight = PXR_Input.IsControllerConnected(PXR_Input.Controller.RightController);
        //Debug.Log("showLeft="+ showLeft+ " showRight="+ showRight+" time="+Time.time);
        if (leftHand)
        {
            leftHand.SetActive(showLeft);
        }
        if (rightHand)
        {
            rightHand.SetActive(showRight);
        }
    }


    public override Camera GetCurrentCamera()
    {
        return xrOrigin.Camera;
    }


    public override bool IsDataReady()
    { 
        return cpuImageReady;
    }

    public override void GetFrameData(Action<XDKFrameData> callback)
    {
        if (cpuImageReady)
        {
            cpuImageReady = false;
            ExtractRGBFromRGBA(CameraDataRGBA, CameraDataRGB);
            cameraParams = PXR_Enterprise.GetCameraParametersNewfor4U(originWidth, originHeight);
            XDKFrameData frameData = new XDKFrameData();
            frameData.textureDataRGB24 = CameraDataRGB;
            frameData.textureWidth = CameraDataWidth;
            frameData.textureHeight = CameraDataHeight;
            frameData.cameraIntrinsicsImgW = CameraDataWidth;
            frameData.cameraIntrinsicsImgH = CameraDataHeight;
            frameData.focalLengthX = (float)cameraParams.fx;
            frameData.focalLengthY = (float)cameraParams.fy;
            frameData.principalPointX = (float)cameraParams.cx;
            frameData.principalPointY = (float)cameraParams.cy;
            
            var mainCameraPos = mainCameraTransform.position;
            var mainCameraRot = mainCameraTransform.rotation;
            mainCameraPos.z = -mainCameraPos.z;
            mainCameraRot.z = -mainCameraRot.z;
            mainCameraRot.w = -mainCameraRot.w;
            cameraPos = mainCameraPos;
            cameraRot = mainCameraRot;

            // 这里是OpenGL坐标系，
            Vector3 positionIn = cameraPos; 
            positionIn.z = -positionIn.z;
            Quaternion rotationIn = cameraRot;
            rotationIn.z = -rotationIn.z;
            rotationIn.w = -rotationIn.w;

            // 这里需要Unity坐标系
            frameData.cameraPosition = positionIn; 
            frameData.cameraEuler = rotationIn.eulerAngles; 

            frameData.IsPortraitImage = false;
            frameData.index = dataIndex;


            dataIndex++;

            Debug.Log("PicoDevice, frameData="+frameData.ToString());

            callback?.Invoke(frameData);
        }
        else
        {
            Debug.LogError("CpuImage is null");
        }
    }


    public override UnityEngine.Pose GetCameraPose()
    {
        UnityEngine.Pose pose = new UnityEngine.Pose();
        pose.position = cameraPos;
        pose.rotation = cameraRot;
        return pose;
    }

    
    public override string GetName()
    {
        return transform.name;
    }


    public override void StartWork()
    {
        IsWorking = true;
        cpuImageReady = false;
    }

    public override void StopWork()
    {
        IsWorking = false;
        cpuImageReady = false;
    }

    private unsafe void ExtractRGBFromRGBA(byte[] rgba, byte[] rgb)
    {
        if (rgba.Length % 4 != 0)
        {
            Debug.LogError($"{tag} Extract rgb from rgba failed, rgba length is not a multiple of 4");
            return;
        }
        int pixelCount = rgba.Length / 4;
        fixed (byte* pRGBA = rgba, pRGB = rgb)
        {
            byte* src = pRGBA;
            byte* dest = pRGB;

            for (int i = 0; i < pixelCount; i++)
            {
                *dest++ = *src++;
                *dest++ = *src++;
                *dest++ = *src++;
                src++;
            }
        }
    }
    
    void OnApplicationPause(bool pause)
    {
        Debug.Log("OpenVST , pause=" + pause);
        if (pause)
        {
            Debug.Log($"{tag} App paused, close the pico rgb camera");
            PXR_Enterprise.CloseCamerafor4U();
            // PXR_Boundary.EnableSeeThroughManual(false);
        }
        else
        {
            Debug.Log($"{tag} App recover, pre to reopen pico rgb camera");
            PXR_Enterprise.OpenCameraAsyncfor4U(ret =>
            {
                Debug.Log($"{tag} Reopen rgb camera, result is {ret}");
                reopen = ret;
            });
            // PXR_Boundary.EnableSeeThroughManual(true);
        }
    }

    private void OnApplicationQuit()
    {
        PXR_Enterprise.CloseCamerafor4U();
    }
}
