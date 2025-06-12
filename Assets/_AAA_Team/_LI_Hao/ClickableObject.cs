using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using HighlightPlus;

[RequireComponent(typeof(HighlightEffect))]
public class ClickableObject : MonoBehaviour
{
    private HighlightEffect _highlight;

    [Header("关联的展品信息弹窗")]
    [SerializeField] private ExhibitInfoPopup _exhibitInfoPopup;

    [Header("是否为关闭按钮")]
    [Tooltip("若为 true，点击时将关闭弹窗；否则根据名称打开展品信息")]
    [SerializeField] private bool _isCloseButton = false;

    private void Awake()
    {
        _highlight = GetComponent<HighlightEffect>();
    }

    public void OnHoverEnter(HoverEnterEventArgs args)
    {
        _highlight.highlighted = true;
    }

    public void OnHoverExit(HoverExitEventArgs args)
    {
        _highlight.highlighted = false;
    }

    public void OnSelect(SelectEnterEventArgs args)
    {
        if (_exhibitInfoPopup == null)
        {
            Debug.LogWarning($"❌ ClickableObject: ExhibitInfoPopup 未绑定（对象：{gameObject.name}）");
            return;
        }

        if (_isCloseButton)
            _exhibitInfoPopup.Hide();
        else
            _exhibitInfoPopup.Show(transform.name);
    }
}
