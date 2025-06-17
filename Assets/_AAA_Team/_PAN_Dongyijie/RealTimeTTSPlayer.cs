using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class RealTimeTTSPlayer : MonoBehaviour
{
    public event Action OnTtsFinished;                
    public event Action<string> OnTtsSubtitleUpdated; // 可选，如果后端不返回subtitles，可忽略

    [SerializeField] private int serverSampleRate = 16000;
    private int deviceSampleRate;
    private AudioSource audioSource;

    private Queue<float> audioQueue = new Queue<float>();
    private object queueLock = new object();

    private List<byte> collectedData = new List<byte>();

    private bool isReceivingData = false;
    private bool isPlayingAudio = false;
    private bool ttsHasFinishedOnAudioThread = false;

    private ClientWebSocket webSocket;
    private StringBuilder subtitleBuffer = new StringBuilder();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        deviceSampleRate= AudioSettings.outputSampleRate;
    }

    void Update()
    {
        if(ttsHasFinishedOnAudioThread)
        {
            ttsHasFinishedOnAudioThread=false;
            OnTtsFinished?.Invoke();
        }
    }

    public void StartTTS(
        string text,
        string appId,
        string secretId,
        string secretKey,
        int voiceType=101001,
        float speed=0f,
        int serverSampleRate=16000
    )
    {
        if(webSocket!=null && webSocket.State==WebSocketState.Open)
        {
            StopTTS();
        }

        this.serverSampleRate= serverSampleRate;

        string wssUrl= BuildRealTimeTTSUrl(text, appId, secretId, secretKey, voiceType, speed, serverSampleRate,"pcm");

        subtitleBuffer.Clear();
        lock(queueLock){ audioQueue.Clear(); }
        collectedData.Clear();

        isReceivingData=true;
        isPlayingAudio=true;
        ttsHasFinishedOnAudioThread=false;

        Task.Run(async ()=> await ConnectAndReceive(wssUrl));
    }

    public void StopTTS()
    {
        isReceivingData=false;
        isPlayingAudio=false;
        if(webSocket!=null && webSocket.State==WebSocketState.Open)
        {
            webSocket.Abort();
        }
        webSocket=null;
    }

    private async Task ConnectAndReceive(string wssUrl)
    {
        webSocket= new ClientWebSocket();
        try
        {
            await webSocket.ConnectAsync(new Uri(wssUrl),CancellationToken.None);
            byte[] buffer= new byte[4096];

            while(webSocket.State==WebSocketState.Open && isReceivingData)
            {
                var result= await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if(result.MessageType==WebSocketMessageType.Close)
                {
                    break;
                }
                else if(result.MessageType==WebSocketMessageType.Binary)
                {
                    int count= result.Count;
                    lock(queueLock)
                    {
                        for(int i=0;i<count;i++)
                        {
                            collectedData.Add(buffer[i]);
                        }
                        for(int i=0;i<count;i+=2)
                        {
                            if(i+1<count)
                            {
                                short s= BitConverter.ToInt16(buffer,i);
                                float sampleFloat= s/32768f;
                                audioQueue.Enqueue(sampleFloat);
                            }
                        }
                    }
                }
                else if(result.MessageType==WebSocketMessageType.Text)
                {
                    // 如果后端返回subtitles
                    string msg= Encoding.UTF8.GetString(buffer,0,result.Count);
                    var resp= JsonUtility.FromJson<TTSJsonMsg>(msg);
                    if(resp!=null && resp.result!=null && resp.result.subtitles!=null && resp.result.subtitles.Length>0)
                    {
                        lock(queueLock)
                        {
                            foreach(var sub in resp.result.subtitles)
                            {
                                subtitleBuffer.Append(sub.Text);
                            }
                        }
                        OnTtsSubtitleUpdated?.Invoke(subtitleBuffer.ToString());
                    }
                    if(resp!=null && resp.final==1)
                    {
                        break;
                    }
                }
            }
        }
        catch{}
        finally
        {
            isReceivingData=false;
            if(webSocket!=null && webSocket.State==WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,"done",CancellationToken.None);
            }
            webSocket.Dispose();
            webSocket=null;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if(!isPlayingAudio)
        {
            for(int i=0;i<data.Length;i++) data[i]=0;
            return;
        }

        float ratio= (float)deviceSampleRate/(float)serverSampleRate;
        float currentSample=0f;
        float readCounter=0f;

        lock(queueLock)
        {
            for(int i=0;i<data.Length;i+=channels)
            {
                if(readCounter<=0f)
                {
                    currentSample= (audioQueue.Count>0)? audioQueue.Dequeue(): 0f;
                    readCounter+= ratio;
                }
                data[i]= currentSample;
                if(channels==2) data[i+1]= currentSample;
                readCounter-=1f;
            }
            if(audioQueue.Count==0 && !isReceivingData)
            {
                isPlayingAudio=false;
                for(int j=0;j<data.Length;j++) data[j]=0;
                ttsHasFinishedOnAudioThread=true;
            }
        }
    }

    private string BuildRealTimeTTSUrl(
        string text,
        string appId,
        string secretId,
        string secretKey,
        int voiceType,
        float speed,
        int sampleRate,
        string codec
    )
    {
        var paramDict= new SortedDictionary<string,string>()
        {
            { "Action", "TextToStreamAudioWS" },
            { "AppId", appId },
            { "SecretId", secretId },
            { "Timestamp", GetTimeStamp().ToString() },
            { "Expired", (GetTimeStamp()+3600).ToString() },
            { "SessionId", Guid.NewGuid().ToString() },
            { "Text", text },
            { "VoiceType", voiceType.ToString() },
            { "SampleRate", sampleRate.ToString() },
            { "Codec", codec },
            { "EnableSubtitle","True" },

            // 尝试减少后端分段
            { "SegmentRate","2" }
        };

        if(Math.Abs(speed)>0.01f)
        {
            paramDict["Speed"]= speed.ToString();
        }

        string domain="tts.cloud.tencent.com";
        string path="/stream_ws";
        string paramStr= string.Join("&", paramDict.Select(k=>$"{k.Key}={k.Value}"));
        string signPlain=$"GET{domain}{path}?{paramStr}";
        string signature= HmacSha1Base64UrlEncode(signPlain,secretKey);

        // 再urlencode
        paramDict["Text"]= UnityWebRequest.EscapeURL(text);

        var finalList= new List<string>();
        foreach(var kvp in paramDict)
        {
            finalList.Add($"{kvp.Key}={kvp.Value}");
        }
        finalList.Add($"Signature={signature}");

        string finalParamStr= string.Join("&",finalList);

        return $"wss://{domain}{path}?{finalParamStr}";
    }

    private long GetTimeStamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private string HmacSha1Base64UrlEncode(string raw, string secretKey)
    {
        using(var hmac= new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
        {
            byte[] hash= hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            string base64= Convert.ToBase64String(hash);
            return UnityWebRequest.EscapeURL(base64);
        }
    }

    [Serializable]
    public class TTSJsonMsg
    {
        public int code;
        public string message;
        public string session_id;
        public string request_id;
        public string message_id;
        public int final;
        public TTSResult result;
    }

    [Serializable]
    public class TTSResult
    {
        public TTSSubtitle[] subtitles;
    }

    [Serializable]
    public class TTSSubtitle
    {
        public string Text;
        public int BeginTime;
        public int EndTime;
        public int BeginIndex;
        public int EndIndex;
        public string Phoneme;
    }
}
