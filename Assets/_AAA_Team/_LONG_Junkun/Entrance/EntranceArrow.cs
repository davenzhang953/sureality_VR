using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using HighlightPlus;
using System.Collections;

public class EntranceArrow : MonoBehaviour
{
    private HighlightEffect highlightEffect;
    public EntranceManager entranceManager;
    public AudioRecorder audioRecorder;
    public GameObject entrancePanel;
    public GameObject entranceGate;

    [Header("Entrance Gate Scaling Settings")]
    public float scaleSpeed = 1.0f; // 缩放速度，越大越快
    public float fadeSpeed = 1.0f;  // 渐变消失速度，越大越快

    private void Start()
    {
        highlightEffect = GetComponent<HighlightEffect>();
    }

    // ==== [MODIFIED by LI Hao] ====
    // private void Update()
    // {
    //     当按下 A 键时，模拟点击该物体
    //     if (Input.GetKeyDown(KeyCode.A))
    //     {
    //         SimulateClick();
    //     }
    // }
    // ==============================

    public void OnHoverEntered(HoverEnterEventArgs hoverEnterEventArgs)
    {
        highlightEffect.highlighted = true;
    }

    public void OnHoverExited(HoverExitEventArgs hoverExitEventArgs)
    {
        highlightEffect.highlighted = false;
    }

    public void OnSelectEntered(SelectEnterEventArgs selectEnterEventArgs)
    {
        // 根据箭头名字切换语言
        switch (transform.name)
        {
            case "EN":
                audioRecorder.selectedLanguage = AudioRecorder.Language.English;
                break;
            case "CN_FAN":
                audioRecorder.selectedLanguage = AudioRecorder.Language.Cantonese;
                break;
            case "CN_JIAN":
                audioRecorder.selectedLanguage = AudioRecorder.Language.Chinese;
                break;
        }

        // 启动入口门的放大过程
        if (entranceGate != null)
        {
            entranceGate.SetActive(true);
            StartCoroutine(ScaleEntranceGate());
        }

        // 禁用 EntranceManager 的跟随
        entranceManager.followFlag = false;
    }

    // 协程：缓慢放大 entranceGate 只在 Z 轴
    private IEnumerator ScaleEntranceGate()
    {
        Vector3 initialScale = entranceGate.transform.localScale;
        Vector3 targetScale = new Vector3(initialScale.x, initialScale.y, 0.25f); // 只放大 Z 轴

        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            float currentZScale = Mathf.Lerp(initialScale.z, targetScale.z, elapsedTime);
            entranceGate.transform.localScale = new Vector3(initialScale.x, initialScale.y, currentZScale);
            elapsedTime += Time.deltaTime * scaleSpeed; // 使用 scaleSpeed 控制放大速度
            yield return null;
        }

        // 确保最终达到目标大小
        entranceGate.transform.localScale = targetScale;

        // 放大完成后等待1秒
        yield return new WaitForSeconds(0.2f);

        // 进入渐变消失过程
        StartCoroutine(FadeOutEntrancePanel());
    }

    // 协程：使 entrancePanel 渐变消失
    private IEnumerator FadeOutEntrancePanel()
    {
        Renderer[] panelRenderers = entrancePanel.GetComponentsInChildren<Renderer>();
        float alpha = 1f; // 初始透明度为1
        float elapsedTime = 0f;

        while (alpha > 0f)
        {
            // 计算透明度和 RGB 值的渐变
            float lerpFactor = Mathf.Lerp(1f, 0f, elapsedTime * fadeSpeed); // 渐变因子
            alpha = lerpFactor;

            foreach (Renderer rend in panelRenderers)
            {
                Color color = rend.material.color;
                color.a = alpha; // 修改透明度

                // 等比减小 RGB 值
                color.r *= lerpFactor;
                color.g *= lerpFactor;
                color.b *= lerpFactor;

                rend.material.color = color;
            }

            elapsedTime += Time.deltaTime; // 时间增加
            yield return null;
        }

        // 完全透明且颜色变为黑色后，关闭 entrancePanel
        foreach (Renderer rend in panelRenderers)
        {
            Color color = rend.material.color;
            color.a = 0f; // 确保完全透明
            color.r = 0f; // 完全变黑
            color.g = 0f;
            color.b = 0f;
            rend.material.color = color;
        }

        // 完全透明且变为黑色后关闭 entrancePanel
        entrancePanel.SetActive(false);
    }

    // 模拟点击物体的方法
    private void SimulateClick()
    {
        // 创建一个模拟的 SelectEnterEventArgs
        SelectEnterEventArgs selectEnterEventArgs = new SelectEnterEventArgs();

        // 直接调用 OnSelectEntered 以模拟点击
        OnSelectEntered(selectEnterEventArgs);
    }
}
