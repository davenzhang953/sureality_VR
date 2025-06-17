using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Step_one : MonoBehaviour
{
    [SerializeField]
    private string initialSignal = "start_signal";  // Ҫ���͵ĳ�ʼ�ź�

    [SerializeField]
    private float initialDelayInSeconds = 3.0f;  // ��ʼ�ӳ�ʱ��

    [SerializeField]
    private string delayedSignal = "next_signal";  // �ӳٺ�Ҫ���͵��ź�

    [SerializeField]
    private string endSignal = "next_signal";  // �ӳٺ�Ҫ���͵��ź�

    [SerializeField]
    private float delayedSignalDelayInSeconds = 5.0f;  // �ӳ��źŵ��ӳ�ʱ��

    [SerializeField]
    private MenuController menuController;  // MenuController ʵ��


    //[SerializeField]
    private GameObject startUI;

    // �ֵ䣬���ڴ洢��ť�����Ե�ӳ���ϵ
    private Dictionary<Button, string> buttonLanguageMapping = new Dictionary<Button, string>();

    private CanvasGroup canvasGroup;

    private void Start()
    {
        DOVirtual.DelayedCall(1f, () =>
        {
            //�ְ����
            GamePlay();
        });



        // ��ȡ���а�ť�����岢���������¼�
        //GetAndAssignButtonEvents();

        //canvasGroup= startUI.GetComponent<CanvasGroup>();
        canvasGroup.DOFade(1, 0.8f);
    }

    private void GetAndAssignButtonEvents()
    {
        // ���������壬�ҵ����а�ť���󶨵���¼�
        Button[] buttons = startUI.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            // Ϊÿ����ť����һ�����ԣ�������Ը���ʵ��������е�����
            string language = button.name;  // ���谴ť�����ƾ��Ƕ�Ӧ������
            buttonLanguageMapping.Add(button, language);

            // ��ӵ���¼�
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
        // ʹ�� DOTween �ӳٷ��ͳ�ʼ�ź�
        DOVirtual.DelayedCall(initialDelayInSeconds, () =>
        {
            SignalManager.Instance.SendSignal(initialSignal);
            Debug.Log("Signal sent: " + initialSignal);

            // �ӳٷ�����һ���ź�
            DOVirtual.DelayedCall(delayedSignalDelayInSeconds, () =>
            {
                // �ӳٷ�����һ���ź�
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

