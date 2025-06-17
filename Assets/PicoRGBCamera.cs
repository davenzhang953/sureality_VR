using System;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.XR.PICO.TOBSupport;
using Unity.XR.PXR;
using UnityEngine;

public class PicoRGBCamera : MonoBehaviour
{
    private int width = 1920;
    private int height = 1080;
    private PXR_OverLay overlay = null;
    private byte[] imgByte;
    private Texture2D texture;
    private PXRCaptureRenderMode Mode = PXRCaptureRenderMode.PXRCapture_RenderMode_LEFT;
    public Material videoMaterial;
    private bool isRunning = false;

    private void Awake()
    {
        // ModeDropdown.onValueChanged.AddListener(SetTrackingMode);
        PXR_Manager.EnableVideoSeeThrough = true;
        // PXR_Boundary.UseGlobalPose(true);
        Debug.Log($"{tag}  Awake ");
        overlay = GetComponent<PXR_OverLay>();
        if (overlay == null)
        {
            Debug.LogError("PXRLog Overlay is null!");
            overlay = gameObject.AddComponent<PXR_OverLay>();
        }
        
        imgByte = new byte[width*height*4];
        texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        videoMaterial.SetTexture("_MainTex", texture);
        PXR_Enterprise.Configurefor4U();
        transform.tag = "Ewan Test";

        PXR_Enterprise.OpenCameraAsyncfor4U(ret =>
        {
            Debug.Log($"{tag}  OpenCameraAsync ret=  {ret}");
            StartPreview();
            // StartGetImageData();
            StartCoroutine(DelayStartGetImageData());
        });
    }

    private IEnumerator DelayStartGetImageData()
    {
        yield return new WaitForSeconds(3.0f);
        StartGetImageData();
    }
    
    public void StartPreview()
    {
        Debug.Log($"{tag} StartPreview ");
        overlay.isExternalAndroidSurface = true;   
        Debug.Log($"{tag} externalAndroidSurfaceObject "+overlay.externalAndroidSurfaceObject);
        PXR_Enterprise.StartPreviewfor4U(overlay.externalAndroidSurfaceObject,Mode);
    }
    
    public void StartGetImageData()
    {
        Debug.Log($"{tag} StartGetImageData ");
        overlay.isExternalAndroidSurface = false;
      
        IntPtr data=Marshal.UnsafeAddrOfPinnedArrayElement(imgByte,0);
        PXR_Enterprise.SetCameraFrameBufferfor4U(width,height,ref data, (Frame frame) =>
        {
            // Debug.Log($"{tag} sensorState position:[{frame.sensorState.globalPose.position.x},{frame.sensorState.globalPose.position.y},{frame.sensorState.globalPose.position.z}]," +
            //           $" orientation:[{frame.sensorState.globalPose.orientation.x},{frame.sensorState.globalPose.orientation.y},{frame.sensorState.globalPose.orientation.z},{frame.sensorState.globalPose.orientation.w}] ");
            // FrameTarget.position=frame.pose.position;
            // FrameTarget.rotation = frame.pose.rotation;
            // FrameTarget.position = new Vector3(frame.pose.position.x, frame.pose.position.y, -frame.pose.position.z);
            // FrameTarget.rotation = new Quaternion(frame.pose.rotation.x, frame.pose.rotation.y, -frame.pose.rotation.z, -frame.pose.rotation.w); 
            texture.LoadRawTextureData(imgByte);
            texture.Apply();
            Debug.Log($"{tag} imageAvailable ");
            Debug.Log("onImageAvailable cameraFramePredictedDisplayTime = "+frame.timestamp +"   Time.deltaTime:"+Time.deltaTime);
            Debug.Log("onImageAvailable size = "+frame.datasize);
            
            
            count++;
           
        });
        Debug.Log($"{tag}  OpenCameraAsync Mode=  {Mode}");
        bool ret=PXR_Enterprise.StartGetImageDatafor4U(Mode, width, height);
        isRunning=true;
        Debug.Log($"{tag}  OpenCameraAsync ret=  {ret}");
    }
    private int count = 0;
    private float deltaTime = 0f;
    public float showTime = 1f;
    private void Update()
    {
        deltaTime += Time.deltaTime;
        if (deltaTime >= showTime) {
            if (count>0)
            {
                float fps = count / deltaTime;
                float milliSecond = deltaTime * 1000 / count;
                string strFpsInfo = string.Format("当前每帧渲染间隔：{0:0.0} ms ({1:0.} 帧每秒)", milliSecond, fps);
                Debug.Log(strFpsInfo);
            }
            count = 0;
            deltaTime = 0f;
        }
        if (reopen)
        {
            Debug.Log($"{tag} Update re OpenCameraAsync Mode=  {Mode}");
            reopen = false;
            PXR_Enterprise.StartGetImageDatafor4U(Mode, width, height);
            StartCoroutine(DelayStartGetImageData());
        }
    }

    private void OnDisable()
    {
        Debug.Log($"{tag}  OnDisable close camera");
        PXR_Enterprise.CloseCamerafor4U();
    }

    private static bool reopen = false;
    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"{tag}  OnApplicationPause  {pauseStatus}");
        if (isRunning)
        {
            Debug.Log($"{tag}  isRunning  {pauseStatus}");
            if (pauseStatus)
            {
                Debug.Log($"{tag}  CloseCamera for pause state {pauseStatus}");
                PXR_Enterprise.CloseCamerafor4U();
            }
            else
            {
                PXR_Enterprise.OpenCameraAsyncfor4U(ret =>
                {
                    Debug.Log($"{tag} pause and reopen CameraAsync ret=  {ret}");
                    reopen = ret;
                });
            }
        }
    }
}
