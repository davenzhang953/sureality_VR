using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Step_one : MonoBehaviour
{
    [SerializeField]
    private string initialSignal = "start_signal";  // 要发送的初始信号

    [SerializeField]
    private float initialDelayInSeconds = 3.0f;  // 初始延迟时间

    [SerializeField]
    private string delayedSignal = "next_signal";  // 延迟后要发送的信号

    [SerializeField]
    private string endSignal = "next_signal";  // 延迟后要发送的信号

    [SerializeField]
    private float delayedSignalDelayInSeconds = 5.0f;  // 延迟信号的延迟时间

    [SerializeField]
    private MenuController menuController;  // MenuController 实例


    //[SerializeField]
    private GameObject startUI;

    // 字典，用于存储按钮和语言的映射关系
    private Dictionary<Button, string> buttonLanguageMapping = new Dictionary<Button, string>();

    private CanvasGroup canvasGroup;

    private void Start()
    {
        DOVirtual.DelayedCall(1f, () =>
        {
            //分包打包
            GamePlay();
        });



        // 获取所有按钮子物体并设置其点击事件
        //GetAndAssignButtonEvents();

        //canvasGroup= startUI.GetComponent<CanvasGroup>();
        canvasGroup.DOFade(1, 0.8f);
    }

    private void GetAndAssignButtonEvents()
    {
        // 遍历子物体，找到所有按钮并绑定点击事件
        Button[] buttons = startUI.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            // 为每个按钮分配一个语言（这里可以根据实际情况进行调整）
            string language = button.name;  // 假设按钮的名称就是对应的语言
            buttonLanguageMapping.Add(button, language);

            // 添加点击事件
            button.onClick.AddListener(() => OnButtonClicked(button));
        }
    }

    private void OnButtonClicked(Button button)
    {
        if (buttonLanguageMapping.TryGetValue(button, out string language))
        {
            menuController.SelectLanguage(language);


            canvasGroup.DOFade(0, 0.8f).OnComplete(() =>
            {
                GamePlay();
                Debug.Log("Language selected: " + language);
            });

        }
        else
        {
            Debug.LogWarning("Button clicked but no language found for button: " + button.name);
        }
    }


    public void GamePlay()
    {
        // 使用 DOTween 延迟发送初始信号
        DOVirtual.DelayedCall(initialDelayInSeconds, () =>
        {
            SignalManager.Instance.SendSignal(initialSignal);
            Debug.Log("Signal sent: " + initialSignal);

            // 延迟发送下一个信号
            DOVirtual.DelayedCall(delayedSignalDelayInSeconds, () =>
            {
                // 延迟发送下一个信号
                DOVirtual.DelayedCall(3f, () =>
                {
                    SignalManager.Instance.SendSignal(endSignal);
                    Debug.Log("Delayed signal sent: " + endSignal);
                });

                SignalManager.Instance.SendSignal(delayedSignal);
                Debug.Log("Delayed signal sent: " + delayedSignal);
            });
        });
    }
}

