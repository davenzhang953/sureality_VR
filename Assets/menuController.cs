using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public GameObject menu;
    public CanvasGroup menuCanvasGroup; // 添加一个 CanvasGroup 组件以控制透明度
    public Text menuText;
    public Image menuImage;  // 新增的 Image 组件
    public AudioSource audioSource;
    public PlayableDirector playableDirector; // 新增字段，用于播放Playable

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
            Debug.LogError("没有找到对应语言的菜单内容: " + language);
        }
    }

    private void OnEnable()
    {
        if (SignalManager.Instance != null)
        {
            // 使用 DOTween 延迟一帧执行
            DOTween.To(() => 0, x => { }, 1, Time.deltaTime).OnComplete(() =>
            {
                Debug.Log("绑定 ShowMenu 和 HideMenuSignalHandler");
                SignalManager.Instance.OnSignalReceived.AddListener(ShowMenu);
                SignalManager.Instance.OnEndSignalReceived.AddListener(HideMenuSignalHandler);
            });

        }
        else
        {
            Debug.LogError("SignalManager.Instance 为 null");
        }
    }

    private void OnDisable()
    {
        if (SignalManager.Instance != null)
        {
            Debug.Log("解除绑定 ShowMenu 和 HideMenuSignalHandler");
            SignalManager.Instance.OnSignalReceived.RemoveListener(ShowMenu);
            SignalManager.Instance.OnEndSignalReceived.RemoveListener(HideMenuSignalHandler);
        }
    }

    private void ShowMenu(string signal)
    {

        if (currentMenuContents == null)
        {
            Debug.LogError("currentMenuContents 为 null");
            return;
        }

        for (int i = 0; i < currentMenuContents.Length; i++)
        {
            var content = currentMenuContents[i];

            if (content.signal == signal)
            {
                if (!content.canRepeat && (triggeredSignals.Contains(signal) || content.order > currentOrder))
                {
                    Debug.LogWarning($"信号 {signal} 不能重复触发或顺序 {content.order} 大于当前顺序 {currentOrder}");
                    return;
                }

                if (menuText == null)
                {
                    Debug.LogError("menuText 为 null");
                    return;
                }

                if (audioSource == null)
                {
                    Debug.LogError("audioSource 为 null");
                    return;
                }

                if (menuImage == null)
                {
                    Debug.LogError("menuImage 为 null");
                    return;
                }



                DOVirtual.DelayedCall(2.5f, () =>
                {

                    menuText.text = content.text.Replace("\\n", "\n"); // 替换 \n 为换行符
                    audioSource.clip = content.audioClip;

                    // 设置 image 的 sprite，如果存在
                    if (content.imageSprite != null)
                    {
                        menuImage.sprite = content.imageSprite;
                        menuImage.color = Color.white; // 确保图片显示为不透明
                    }
                    else
                    {
                        menuImage.sprite = null; // 设置为 null
                        menuImage.color = new Color(1, 1, 1, 0); // 将图片设为透明
                    }

                    menu.SetActive(true);
                    // 然后在 1 秒内逐渐显示 menu
                    menuCanvasGroup.DOFade(1, 0.5f);
                    audioSource.Play();
                }).SetId(gameObject); // 设置 ID 为 gameObject 以便之后可以取消

                // 设置当前的 endSignal
                currentEndSignal = content.endSignal;
                Debug.Log($"当前content的结束信号为 {currentEndSignal}");
                if (content.canRepeat)
                {
                    // 设置当前的 endSignal 以便 HideMenuSignalHandler 检测
                    currentEndSignal = content.endSignal;
                    Debug.Log($"可重复content的结束信号为 {currentEndSignal}");
                }
                else
                {
                    triggeredSignals.Add(signal);
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        currentOrder = content.order + 1;
                        Debug.Log($"下一个顺序序号 {currentOrder}");
                        // 发送当前顺序值作为信号
                        SignalManager.Instance.SendEndSignal($"order_{currentOrder}");
                    }).SetId(gameObject); // 设置 ID 为 gameObject 以便之后可以取消
                }

                break;
            }
        }
    }

    private void HideMenuSignalHandler(string signal)
    {
        Debug.Log($"HideMenuSignalHandler 收到信号: {signal}");

        for (int i = 0; i < currentMenuContents.Length; i++)
        {
            var content = currentMenuContents[i];
            if (content.endSignal == signal)
            {
                if (content.canRepeat)
                {
                    Debug.Log($"匹配可重复 content 的 endSignal: {signal}");
                    HideMenuContentOnly(signal);
                }
                else if (!content.canRepeat && (triggeredSignals.Contains(signal) || content.order > currentOrder - 2))
                {
                    if (currentEndSignal == signal)
                    {
                        Debug.Log($"匹配 endSignal: {signal}");
                        HideMenu(signal);
                    }
                }
            }
        }
    }


    public void HideMenu(string signal)
    {
        //Debug.Log($"调用 HideMenu 方法, 信号: {signal}");
        if (menu != null)
        {
            //Debug.Log("播放 PlayableDirector");
            // 播放Playable
            playableDirector.Play();

            //等待Playable播放完毕，再执行淡出动画
            playableDirector.stopped += OnPlayableDirectorStopped;

            // 清除当前的 endSignal
            currentEndSignal = null;
        }
    }

    private void OnPlayableDirectorStopped(PlayableDirector director)
    {
        Debug.Log("PlayableDirector 停止播放，开始淡出菜单");
        //Playable播放完毕，开始执行淡出动画
        menuCanvasGroup.DOFade(0, 0.05f).OnComplete(() =>
        {
            //menu.SetActive(false);
            Debug.Log("菜单已隐藏");
        });

        // 解除事件绑定，避免重复触发
        playableDirector.stopped -= OnPlayableDirectorStopped;
    }


    private void HideMenuContentOnly(string signal)
    {
        //Debug.Log($"调用 HideMenuContentOnly 方法, 信号: {signal}");
        if (menu != null)
        {
            Debug.Log("隐藏菜单（不播放 PlayableDirector）");
            // 只执行淡出动画，不播放 PlayableDirector
            menuCanvasGroup.DOFade(0, 0.05f).OnComplete(() =>
            {
                menu.SetActive(false);
                Debug.Log("菜单已隐藏");
            });

            // 清除当前的 endSignal
            currentEndSignal = null;
        }
    }
}

