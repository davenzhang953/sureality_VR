using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityWebSocket;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class AudioRecorder : MonoBehaviour
{
    public enum Language { Chinese, English, Cantonese }

    [Header("选择 ASR 语言")]           public Language       selectedLanguage = Language.Chinese;
    [Header("ASR 字幕 (只显示最新两行)")] public TextMeshProUGUI asrSubtitleText;
    [Header("TTS 字幕 (整段 GPT)")]      public TextMeshProUGUI ttsSubtitleText;
    [SerializeField]                    private RealTimeTTSPlayer realTimeTts;

    // ==== [MODIFIED by LI Hao] ====
    public ButterflyManager ButterflyManager;
    // ==============================

    // ───────── 内部字段 ─────────
    const int sampleRate = 16000;
    AudioClip audioClip;  AudioSource audioSource;  WebSocket ws;
    bool isRecording;     string device;
    float[] sampleBuffer; int lastSamplePos;
    string _recognitionText = "", _lastResult = "";
    string currentAsrLanguage = "CHN"; int currentVoiceType = 101023;
    string appId, secretId, secretKey; bool keyIsReady;
    bool subtitleLocked = false;

    // ───────── Start & Update ─────────
    void Start()
    {
        device = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (device == null) { Debug.LogError("No mic"); enabled = false; return; }

        audioSource = GetComponent<AudioSource>();
        InitTMP(asrSubtitleText); InitTMP(ttsSubtitleText);

        if (realTimeTts) realTimeTts.OnTtsFinished += () => StartCoroutine(ClearTtsSubtitleWithDelay(2));
        StartCoroutine(FetchTencentKey());
    }

    void Update()
    {
        // ==== [MODIFIED by LI Hao] ====
        // if (Input.GetKeyDown(KeyCode.R) && !isRecording)
        if (ButterflyManager.MicIcon.activeSelf && !isRecording)
        // ==============================
        {
            if (!keyIsReady) { Debug.LogWarning("500 服务器异常"); return; }
            (currentAsrLanguage, currentVoiceType) = selectedLanguage switch
            {
                Language.English   => ("ENG", 1051),
                Language.Cantonese => ("CAN", 101019),
                _                  => ("CHN", 101023)
            };
            StartRecording();
        }
        // ==== [MODIFIED by LI Hao] ====
        // if (Input.GetKeyUp(KeyCode.R) && isRecording) StopRecording();
        if (!ButterflyManager.MicIcon.activeSelf && isRecording) StopRecording();
        // ==============================
    }

    // ───────── Key 获取 ─────────
    IEnumerator FetchTencentKey()
    {
        // ==== [MODIFIED by LI Hao] ====
        // using var www = UnityWebRequest.Get("http://10.30.9.1:2123/api/getTencentKey");
        using var www = UnityWebRequest.Get("http://47.76.39.242:2126/api/getTencentKey");
        // ==============================
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            var cfg = JsonUtility.FromJson<TencentKeyData>(www.downloadHandler.text);
            appId = cfg.appId; secretId = cfg.SecretId; secretKey = cfg.SecretKey; keyIsReady = true;
        }
    }

    // ───────── 录音 & ASR ─────────
    void StartRecording()
    {
        _recognitionText = _lastResult = ""; lastSamplePos = 0; sampleBuffer = null;
        subtitleLocked = false;
        audioClip = Microphone.Start(device, true, 30, sampleRate); isRecording = true;
        UpdateAsrSubtitle("录音中...");

        string model = currentAsrLanguage switch { "ENG" => "16k_en", "CAN" => "16k_yue", _ => "16k_zh_dialect" };
        ws = new WebSocket(GenerateAsrUrl(model));

        ws.OnOpen += (_, __) => StartCoroutine(SendAudioDataLoop());
        ws.OnMessage += (_, e) =>
        {
            if (!e.IsText || subtitleLocked) return;

            var r = JsonUtility.FromJson<TencentAsrResponse>(e.Data);
            if (r == null || r.code != 0 || r.result == null) return;

            int slice = r.result.slice_type;
            string txt = r.result.voice_text_str ?? "";
            _recognitionText = _lastResult + txt;
            UpdateAsrSubtitle(_recognitionText);

            if (slice == 2)                     // 一句话结束
            {
                _lastResult = _recognitionText;

                string prompt = currentAsrLanguage == "ENG"
                                ? "Please respond only in English: " + _recognitionText
                                : _recognitionText;

                SetThinkingSubtitle();          // 锁并显示“思考中…”
                StartCoroutine(SendToGptTest(prompt.Trim()));
            }
        };
        ws.ConnectAsync();
    }

    void StopRecording()
    {
        Microphone.End(device); isRecording = false;
        if (ws?.ReadyState == WebSocketState.Open) ws.SendAsync("{\"type\":\"end\"}");
        UpdateAsrSubtitle("录音结束，等待识别...");
    }

    IEnumerator SendAudioDataLoop()
    {
        const int interval = 40;
        while (isRecording && ws?.ReadyState == WebSocketState.Open)
        {
            int pos = Microphone.GetPosition(device), diff = pos - lastSamplePos;
            if (diff > 0)
            {
                sampleBuffer ??= new float[diff];
                if (sampleBuffer.Length != diff) sampleBuffer = new float[diff];
                audioClip.GetData(sampleBuffer, lastSamplePos);

                byte[] pcm = new byte[diff * 2];
                for (int i = 0; i < diff; i++)
                {
                    short s = (short)(sampleBuffer[i] * 32767);
                    pcm[2 * i] = (byte)(s & 0xFF); pcm[2 * i + 1] = (byte)(s >> 8);
                }
                ws.SendAsync(pcm); lastSamplePos += diff;
            }
            yield return new WaitForSeconds(interval / 1000f);
        }
    }

    // ───────── GPT → TTS ─────────
    IEnumerator SendToGptTest(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        var req = new GptRequestData { message = text };
        // ==== [MODIFIED by LI Hao] ====
        // using var www = new UnityWebRequest("http://10.30.9.1:2123/api/gpttest", "POST")
        using var www = new UnityWebRequest("http://47.76.39.242:2126/api/gpttest", "POST")
        // ==============================
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(req))),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var rsp = JsonUtility.FromJson<ChatbotResponse>(www.downloadHandler.text);
            if (rsp?.success == true)
            {
                if (asrSubtitleText) asrSubtitleText.text = "";          // 清掉思考字幕
                if (ttsSubtitleText) ttsSubtitleText.text = rsp.answer ?? "";
                realTimeTts?.StartTTS(rsp.answer, appId, secretId, secretKey, currentVoiceType, 0, 16000);
            }
            else Debug.LogWarning("GPT 失败: " + rsp?.message);
        }
        else Debug.LogError($"HTTP {www.responseCode}: {www.error}");
    }

    // ───────── 字幕 UI ─────────
    void UpdateAsrSubtitle(string s)
    {
        if (!asrSubtitleText || subtitleLocked) return;
        asrSubtitleText.text = s; asrSubtitleText.ForceMeshUpdate();
        while (asrSubtitleText.textInfo.lineCount > 2)
        {
            int end = asrSubtitleText.textInfo.lineInfo[0].lastCharacterIndex;
            asrSubtitleText.text = asrSubtitleText.text[(end + 1)..].TrimStart();
            asrSubtitleText.ForceMeshUpdate();
        }
    }

    void SetThinkingSubtitle()
    {
        subtitleLocked = true;
        asrSubtitleText.text = selectedLanguage switch
        {
            Language.English   => "Thinking, please wait…",
            Language.Cantonese => "諗緊嘢，唔該等等…",
            _                  => "正在思考中，请稍后…"
        };
    }

    IEnumerator ClearTtsSubtitleWithDelay(float sec)
    { yield return new WaitForSeconds(sec); if (ttsSubtitleText) ttsSubtitleText.text = ""; }

    // ───────── TMP 初始化 ─────────
    static void InitTMP(TextMeshProUGUI t)
    { if (!t) return; t.text = ""; t.enableWordWrapping = true; t.overflowMode = TextOverflowModes.Overflow; t.maxVisibleLines = 99; }

    // ───────── ASR URL / 签名 ─────────
    string GenerateAsrUrl(string model)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), exp = ts + 3600,
             nonce = UnityEngine.Random.Range(1, 999999999);
        string vid = Guid.NewGuid().ToString();

        var p = new SortedDictionary<string, string>{
            ["secretid"]=secretId, ["engine_model_type"]=model, ["timestamp"]=ts.ToString(),
            ["expired"]=exp.ToString(), ["nonce"]=nonce.ToString(), ["voice_id"]=vid, ["voice_format"]="1"
        };
        string sig = Sign(p);
        var sb = new StringBuilder();
        foreach (var kv in p) sb.Append($"{kv.Key}={Uri.EscapeDataString(kv.Value)}&");
        sb.Append("signature=" + Uri.EscapeDataString(sig));
        return $"wss://asr.cloud.tencent.com/asr/v2/{appId}?{sb}";
    }
    string Sign(SortedDictionary<string, string> p)
    {
        var sb = new StringBuilder($"asr.cloud.tencent.com/asr/v2/{appId}?");
        int i = 0; foreach (var kv in p) { sb.Append($"{kv.Key}={kv.Value}"); if (i++ < p.Count - 1) sb.Append('&'); }
        using var h = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    // ───────── 数据结构 ─────────
    [Serializable] public class TencentKeyData { public string appId, SecretId, SecretKey; }
    [Serializable] public class TencentAsrResponse { public int code; public string message; public TencentAsrResult result; }
    [Serializable] public class TencentAsrResult  { public int slice_type; public string voice_text_str; }
    [Serializable] public class GptRequestData   { public string message; }
    [Serializable] public class ChatbotResponse  { public bool success; public string answer, message; }
}
