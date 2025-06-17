using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public GameObject menu;
    public CanvasGroup menuCanvasGroup; // ���һ�� CanvasGroup ����Կ���͸����
    public Text menuText;
    public Image menuImage;  // ������ Image ���
    public AudioSource audioSource;
    public PlayableDirector playableDirector; // �����ֶΣ����ڲ���Playable

    public MenuContentGroup[] menuContentGroups;

    private MenuContent[] currentMenuContents;
    private HashSet<string> triggeredSignals = new HashSet<string>();
    private int currentOrder = 1;
    private string currentEndSignal = null;

    private void Start()
    {
        SelectLanguage("1");
    }

    public void SelectLanguage(string language)
    {
        foreach (var group in menuContentGroups)
        {
            if (group.language == language)
            {
                currentMenuContents = group.contents;
                break;
            }
        }

        if (currentMenuContents == null)
        {
            Debug.LogError("û���ҵ���Ӧ���ԵĲ˵�����: " + language);
        }
    }

    private void OnEnable()
    {
        if (SignalManager.Instance != null)
        {
            // ʹ�� DOTween �ӳ�һִ֡��
            DOTween.To(() => 0, x => { }, 1, Time.deltaTime).OnComplete(() =>
            {
                Debug.Log("�� ShowMenu �� HideMenuSignalHandler");
                SignalManager.Instance.OnSignalReceived.AddListener(ShowMenu);
                SignalManager.Instance.OnEndSignalReceived.AddListener(HideMenuSignalHandler);
            });

        }
        else
        {
            Debug.LogError("SignalManager.Instance Ϊ null");
        }
    }

    private void OnDisable()
    {
        if (SignalManager.Instance != null)
        {
            Debug.Log("����� ShowMenu �� HideMenuSignalHandler");
            SignalManager.Instance.OnSignalReceived.RemoveListener(ShowMenu);
            SignalManager.Instance.OnEndSignalReceived.RemoveListener(HideMenuSignalHandler);
        }
    }

    private void ShowMenu(string signal)
    {

        if (currentMenuContents == null)
        {
            Debug.LogError("currentMenuContents Ϊ null");
            return;
        }

        for (int i = 0; i < currentMenuContents.Length; i++)
        {
            var content = currentMenuContents[i];

            if (content.signal == signal)
            {
                if (!content.canRepeat && (triggeredSignals.Contains(signal) || content.order > currentOrder))
                {
                    Debug.LogWarning($"�ź� {signal} �����ظ�������˳�� {content.order} ���ڵ�ǰ˳�� {currentOrder}");
                    return;
                }

                if (menuText == null)
                {
                    Debug.LogError("menuText Ϊ null");
                    return;
                }

                if (audioSource == null)
                {
                    Debug.LogError("audioSource Ϊ null");
                    return;
                }

                if (menuImage == null)
                {
                    Debug.LogError("menuImage Ϊ null");
                    return;
                }



                DOVirtual.DelayedCall(2.5f, () =>
                {

                    menuText.text = content.text.Replace("\\n", "\n"); // �滻 \n Ϊ���з�
                    audioSource.clip = content.audioClip;

                    // ���� image �� sprite���������
                    if (content.imageSprite != null)
                    {
                        menuImage.sprite = content.imageSprite;
                        menuImage.color = Color.white; // ȷ��ͼƬ��ʾΪ��͸��
                    }
                    else
                    {
                        menuImage.sprite = null; // ����Ϊ null
                        menuImage.color = new Color(1, 1, 1, 0); // ��ͼƬ��Ϊ͸��
                    }

                    menu.SetActive(true);
                    // Ȼ���� 1 ��������ʾ menu
                    menuCanvasGroup.DOFade(1, 0.5f);
                    audioSource.Play();
                }).SetId(gameObject); // ���� ID Ϊ gameObject �Ա�֮�����ȡ��

                // ���õ�ǰ�� endSignal
                currentEndSignal = content.endSignal;
                Debug.Log($"��ǰcontent�Ľ����ź�Ϊ {currentEndSignal}");
                if (content.canRepeat)
                {
                    // ���õ�ǰ�� endSignal �Ա� HideMenuSignalHandler ���
                    currentEndSignal = content.endSignal;
                    Debug.Log($"���ظ�content�Ľ����ź�Ϊ {currentEndSignal}");
                }
                else
                {
                    triggeredSignals.Add(signal);
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        currentOrder = content.order + 1;
                        Debug.Log($"��һ��˳����� {currentOrder}");
                        // ���͵�ǰ˳��ֵ��Ϊ�ź�
                        SignalManager.Instance.SendEndSignal($"order_{currentOrder}");
                    }).SetId(gameObject); // ���� ID Ϊ gameObject �Ա�֮�����ȡ��
                }

                break;
            }
        }
    }

    private void HideMenuSignalHandler(string signal)
    {
        Debug.Log($"HideMenuSignalHandler �յ��ź�: {signal}");

        for (int i = 0; i < currentMenuContents.Length; i++)
        {
            var content = currentMenuContents[i];
            if (content.endSignal == signal)
            {
                if (content.canRepeat)
                {
                    Debug.Log($"ƥ����ظ� content �� endSignal: {signal}");
                    HideMenuContentOnly(signal);
                }
                else if (!content.canRepeat && (triggeredSignals.Contains(signal) || content.order > currentOrder - 2))
                {
                    if (currentEndSignal == signal)
                    {
                        Debug.Log($"ƥ�� endSignal: {signal}");
                        HideMenu(signal);
                    }
                }
            }
        }
    }


    public void HideMenu(string signal)
    {
        //Debug.Log($"���� HideMenu ����, �ź�: {signal}");
        if (menu != null)
        {
            //Debug.Log("���� PlayableDirector");
            // ����Playable
            playableDirector.Play();

            //�ȴ�Playable������ϣ���ִ�е�������
            playableDirector.stopped += OnPlayableDirectorStopped;

            // �����ǰ�� endSignal
            currentEndSignal = null;
        }
    }

    private void OnPlayableDirectorStopped(PlayableDirector director)
    {
        Debug.Log("PlayableDirector ֹͣ���ţ���ʼ�����˵�");
        //Playable������ϣ���ʼִ�е�������
        menuCanvasGroup.DOFade(0, 0.05f).OnComplete(() =>
        {
            //menu.SetActive(false);
            Debug.Log("�˵�������");
        });

        // ����¼��󶨣������ظ�����
        playableDirector.stopped -= OnPlayableDirectorStopped;
    }


    private void HideMenuContentOnly(string signal)
    {
        //Debug.Log($"���� HideMenuContentOnly ����, �ź�: {signal}");
        if (menu != null)
        {
            Debug.Log("���ز˵��������� PlayableDirector��");
            // ִֻ�е��������������� PlayableDirector
            menuCanvasGroup.DOFade(0, 0.05f).OnComplete(() =>
            {
                menu.SetActive(false);
                Debug.Log("�˵�������");
            });

            // �����ǰ�� endSignal
            currentEndSignal = null;
        }
    }
}

