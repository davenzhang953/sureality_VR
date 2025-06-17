using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MenuContent
{
    public string signal; // Ӧ���ź�
    public string endSignal; // �����ź�
    public string text;
    public AudioClip audioClip;
    public Sprite imageSprite;
    public bool canRepeat;
    public int order;
    [HideInInspector]
    public bool hideMenuTriggered; // ȷ���������� Inspector ����ʾ
}

[System.Serializable]
public struct MenuContentGroup
{
    public string language;
    public MenuContent[] contents;
}
