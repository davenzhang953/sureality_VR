using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MenuContent
{
    public string signal; // 应用信号
    public string endSignal; // 结束信号
    public string text;
    public AudioClip audioClip;
    public Sprite imageSprite;
    public bool canRepeat;
    public int order;
    [HideInInspector]
    public bool hideMenuTriggered; // 确保它不会在 Inspector 中显示
}

[System.Serializable]
public struct MenuContentGroup
{
    public string language;
    public MenuContent[] contents;
}
