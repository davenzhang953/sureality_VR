using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[CreateAssetMenu(fileName = "ExhibitInfoRepository", menuName = "AAA/ExhibitInfoRepository")]
public class ExhibitInfoRepository : ScriptableObject
{
    [SerializeField] private TextAsset _jsonSource;

    private Dictionary<string, ExhibitInfo> _infoByName;

    public IReadOnlyDictionary<string, ExhibitInfo> InfoByName => _infoByName;

    private void OnEnable()
    {
        if (_jsonSource == null)
        {
            Debug.LogError("❌ ExhibitInfoRepository: JSON 未绑定");
            return;
        }

        try
        {
            _infoByName = JsonConvert
                .DeserializeObject<Dictionary<string, ExhibitInfo>>(_jsonSource.text)
                ?? new Dictionary<string, ExhibitInfo>();
            Debug.Log($"✅ ExhibitInfoRepository: 加载 {_infoByName.Count} 条展品信息");
        }
        catch (JsonException ex)
        {
            Debug.LogError($"❌ ExhibitInfoRepository: JSON 解析失败: {ex.Message}");
            _infoByName = new Dictionary<string, ExhibitInfo>();
        }
    }

    public ExhibitInfo GetExhibitInfoByName(string name)
    {
        if (!_infoByName.TryGetValue(name, out var info))
        {
            Debug.LogWarning($"❌ ExhibitInfoRepository: 未找到展品 [{name}] 的信息");
        }

        return info;
    }
}

[System.Serializable]
public class ExhibitInfo
{
    public int ExhibitionAreaId;
    public string IntroEnglish;
    public string IntroSimplifiedChinese;
    public string IntroTraditionalChinese;

    public string GetLocalizedIntro(AudioRecorder.Language language) => language switch
    {
        AudioRecorder.Language.Cantonese         => IntroTraditionalChinese,
        AudioRecorder.Language.Chinese           => IntroSimplifiedChinese,
        _                                        => IntroEnglish
    };
}
