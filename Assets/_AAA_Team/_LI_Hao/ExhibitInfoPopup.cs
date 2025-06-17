using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HighlightPlus;

[RequireComponent(typeof(Canvas))]
public class ExhibitInfoPopup : MonoBehaviour
{
    [Header("展区样式列表 (按展区序号排列)")]
    [SerializeField] private AreaStyle[] _areaStyles;

    [Header("组件引用")]
    [SerializeField] private AudioRecorder _audioRecorder;
    [SerializeField] private ExhibitInfoRepository _exhibitInfoRepository;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private MeshRenderer _closeButtonRenderer;
    [SerializeField] private HighlightEffect _closeButtonGlow;
    [SerializeField] private MeshRenderer _arrowButtonRenderer;
    [SerializeField] private MeshRenderer _borderRenderer;

    public void Show(string exhibitName)
    {
        var info = _exhibitInfoRepository.GetExhibitInfoByName(exhibitName);
        if (info == null) return;

        ApplyStyle(GetAreaStyle(info.ExhibitionAreaId));
        _contentText.text = info.GetLocalizedIntro(_audioRecorder.selectedLanguage);
        _scrollRect.verticalNormalizedPosition = 1f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private AreaStyle GetAreaStyle(int areaId)
    {
        int index = areaId - 1;
        if (index < 0 || index >= _areaStyles.Length)
        {
            Debug.LogWarning($"展区 {areaId} 越界，使用默认样式");
            return _areaStyles.Length > 0 ? _areaStyles[0] : null;
        }

        return _areaStyles[index];
    }

    private void ApplyStyle(AreaStyle style)
    {
        if (style == null) return;

        _closeButtonRenderer.material = style.Material;
        _closeButtonGlow.SetGlowColor(style.Color);
        _arrowButtonRenderer.material = style.Material;
        _borderRenderer.material = style.Material;
    }

    [System.Serializable]
    public class AreaStyle
    {
        [Tooltip("外发光颜色")] public Color Color;
        [Tooltip("按钮/边框材质")] public Material Material;
    }
}
