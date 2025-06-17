using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ButterflyManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("请在 Inspector 中拖入蝴蝶物体")]
    public Transform butterfly; // 需要拖入蝴蝶物体
    public Transform cameraTransform;
    public Transform landingHand;
    public InputActionReference GrabAction;
    public AudioSource landingAudio;
    public GameObject MicIcon; // 新增，拖入MicIcon物体

    [Header("Flight Settings")]
    public Vector3 followOffset = Vector3.zero;      // 跟随相机时的偏移
    public Vector3 landingOffset = Vector3.zero;       // 降落点的偏移（可在 Inspector 中调整）
    public float flySpeed = 2.0f;
    public float rotationSpeed = 5.0f;
    public float threshold = 0.1f;                     // 判断是否到达目标的距离

    [Header("Animation Speeds")]
    [Tooltip("降落状态下的动画播放速度（模拟蝴蝶休息）")]
    public float landingAnimSpeed = 0.5f;
    [Tooltip("起步阶段（0.5秒内）的动画播放速度，模拟加速/减速")]
    public float accelerationAnimSpeed = 1.5f;
    [Tooltip("匀速移动过程中的动画播放速度")]
    public float cruiseAnimSpeed = 1.0f;

    public Animation anim;
    private bool isGrabbing = false;
    private bool landingAudioPlayed = false;

    // 用于检测状态切换，判断加速阶段
    private float stateChangeTime = 0f;
    private bool previousIsGrabbing;

    [Header("Floating Settings")]
    public float floatAmount = 0.5f;  
    public float floatSpeed = 1f;     

    public bool isLanding = false;

    // ==== [MODIFIED by LI Hao] ====
    // private float micIconDelayTime = 1.5f; 
    // private float micIconTimer = 0f; 
    // ==============================

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (GrabAction != null)
            GrabAction.action.Enable();

        if (anim != null)
            anim.Play();

        previousIsGrabbing = GetGrabStatus();
        stateChangeTime = Time.time;

        if (MicIcon != null)
            MicIcon.SetActive(false); // 在开始时隐藏MicIcon
    }

    void Update()
    {
        bool currentIsGrabbing = GetGrabStatus();
        if (currentIsGrabbing != previousIsGrabbing)
        {
            stateChangeTime = Time.time; // 状态切换时记录时间
            previousIsGrabbing = currentIsGrabbing;
        }
        isGrabbing = currentIsGrabbing;

        // 当不处于抓取状态时，重置降落音效标志，便于下一次降落时再次播放
        if (!isGrabbing)
        {
            landingAudioPlayed = false;
            if (MicIcon != null)
                MicIcon.SetActive(false); // 松开时关闭MicIcon
            isLanding = false; // 重置isLanding状态

            // ==== [MODIFIED by LI Hao] ====
            // micIconTimer = 0f; // 重置定时器
            // ==============================
        }
        // ==== [MODIFIED by LI Hao] ====
        else
        {
            if (MicIcon != null)
                MicIcon.SetActive(true);
        }

        if (!landingAudioPlayed && landingAudio != null && isGrabbing)
        {
            landingAudio.Play();
            landingAudioPlayed = true;
        }
        // ==============================

        MoveButterfly(isGrabbing);
        RotateButterfly(isGrabbing);
        UpdateAnimationSpeed();

        // 控制MicIcon的显示状态
        // ==== [MODIFIED by LI Hao] ====
        // HandleMicIconVisibility();
        // ==============================
    }

    // 获取抓取按钮的状态（按下或松开）
    private bool GetGrabStatus()
    {
        if (GrabAction != null)
        {
            float gripValue = GrabAction.action.ReadValue<float>();
            return gripValue == 1;
        }
        return false;
    }

    // 移动蝴蝶到目标位置（根据抓取状态选择目标）
    private void MoveButterfly(bool isGrabbing)
    {
        if (butterfly == null)
            return;

        Vector3 targetPos = isGrabbing ? landingHand.TransformPoint(landingOffset) : cameraTransform.TransformPoint(followOffset);

        if (!isGrabbing)
        {
            float floatingOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            targetPos.y += floatingOffset;
        }

        Vector3 currentPos = butterfly.position;
        Vector3 towardTarget = targetPos - currentPos;
        butterfly.position += towardTarget * Time.deltaTime * flySpeed;
    }

    // 根据目标方向旋转蝴蝶
    private void RotateButterfly(bool isGrabbing)
    {
        if (butterfly == null)
            return;

        Vector3 targetPos = isGrabbing ? landingHand.TransformPoint(landingOffset) : cameraTransform.TransformPoint(followOffset);
        Vector3 towardTarget = targetPos - butterfly.position;

        if (towardTarget.magnitude > threshold)
        {
            Quaternion targetRot = Quaternion.LookRotation(towardTarget.normalized, Vector3.up);
            butterfly.rotation = Quaternion.Slerp(butterfly.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
        else if (isGrabbing && towardTarget.magnitude <= threshold)
        {
            Quaternion targetRot = Quaternion.LookRotation(cameraTransform.position - butterfly.position, Vector3.up);
            butterfly.rotation = Quaternion.Slerp(butterfly.rotation, targetRot, Time.deltaTime * rotationSpeed);

            // ==== [MODIFIED by LI Hao] ====
            // if (!landingAudioPlayed && landingAudio != null)
            // {
            //     landingAudio.Play();
            //     landingAudioPlayed = true;
            // }
            // ==============================
        }
    }

    // 更新动画播放速度
    private void UpdateAnimationSpeed()
    {
        if (anim == null || anim.clip == null)
            return;

        Vector3 targetPos = isGrabbing ? landingHand.TransformPoint(landingOffset) : cameraTransform.TransformPoint(followOffset);
        float distance = Vector3.Distance(butterfly.position, targetPos);
        float newAnimSpeed = cruiseAnimSpeed;

        if (isGrabbing && distance <= threshold)
        {
            newAnimSpeed = landingAnimSpeed;
        }
        else if (Time.time - stateChangeTime < 0.5f)
        {
            newAnimSpeed = accelerationAnimSpeed;
        }
        else
        {
            newAnimSpeed = cruiseAnimSpeed;
        }

        anim[anim.clip.name].speed = newAnimSpeed;
    }

    // 控制MicIcon的显示状态
    // ==== [MODIFIED by LI Hao] ====
    // private void HandleMicIconVisibility()
    // {
    //     if (isGrabbing && landingAudioPlayed && !isLanding)
    //     {
    //         if (micIconTimer < micIconDelayTime)
    //         {
    //             micIconTimer += Time.deltaTime;
    //         }
    //         else
    //         {
    //             if (MicIcon != null)
    //             {
    //                 MicIcon.SetActive(true); // 延迟1秒后显示MicIcon
    //             }
    //             isLanding = true; // 标记已进入降落状态
    //         }
    //     }
    // }
    // ==============================
}
