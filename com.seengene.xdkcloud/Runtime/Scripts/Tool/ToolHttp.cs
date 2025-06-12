using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


namespace Seengene.XDK
{
    public class ToolHttp
    {

        /// <summary>
        /// load file from remote
        /// </summary>
        /// <param name="url"></param>
        /// <param name="localFilePath"></param>
        /// <param name="onComplete"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public static IEnumerator loadRemoteFile(string url, string localFilePath, Action<int> onComplete, Action<float> onProgress = null)
        {
            long range = 0;
            string tempFile = localFilePath + ".tmp";
            if (File.Exists(tempFile))
            {
                FileInfo finfo = new FileInfo(tempFile);
                range = finfo.Length;
                Debug.Log("Range: bytes=" + range + "-  url=" + url + " ");
            }
            using (UnityWebRequest webRequest = new UnityWebRequest(url))
            {
                webRequest.SetRequestHeader("Range", "bytes=" + range + "-");
                DownloadHandlerFile fileHandler = new DownloadHandlerFile(tempFile, true);
                webRequest.downloadHandler = fileHandler;
                webRequest.SendWebRequest();
                while (webRequest.result == UnityWebRequest.Result.InProgress)
                {
                    onProgress?.Invoke(webRequest.downloadProgress);
                    yield return null;
                }
                onProgress?.Invoke(1.0f);
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    //Debug.Log("loadRemoteFile succ, url=" + url);
                    if (File.Exists(localFilePath))
                    {
                        File.Delete(localFilePath);
                    }
                    File.Move(tempFile, localFilePath); // rename file
                    onComplete.Invoke(0);
                }
                else
                {
                    Debug.Log("loadRemoteFile failed, webRequest.result=" + webRequest.result + " url=" + url + " webRequest.downloadProgress=" + webRequest.downloadProgress);
                    onComplete.Invoke(-1);
                }
            }
        }


        /// <summary>
        /// http Post
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="url"></param>
        /// <param name="form"></param>
        public static IEnumerator httpPostBytes(string url, byte[] data, string[] myHeaders, Action<string, byte[]> callBack, Action<float> onProgress = null, string contentType = "application/json")
        {
            if (string.IsNullOrEmpty(url))
            {
                callBack?.Invoke("fail", Encoding.UTF8.GetBytes("701"));
                yield break;
            }
            using (UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, UnityWebRequest.kHttpVerbPOST))
            {
                UploadHandler uploader = new UploadHandlerRaw(data as byte[]);
                webRequest.uploadHandler = uploader;
                yield return httpPostData(webRequest, url, myHeaders, callBack, onProgress, "binary (application/octet-stream)");
            }
        }



        /// <summary>
        /// http Post
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="url"></param>
        /// <param name="form"></param>
        public static IEnumerator httpPostForm(string url, WWWForm form, string[] myHeaders, Action<string, byte[]> callBack, Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                callBack?.Invoke("fail", Encoding.UTF8.GetBytes("701"));
                yield break;
            }
            using (UnityWebRequest webRequest = UnityWebRequest.Post(url, form))
            {
                yield return httpPostData(webRequest, url, myHeaders, callBack, onProgress, "application/x-www-form-urlencoded");
            }
        }



        /// <summary>
        /// http Post
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="url"></param>
        /// <param name="form"></param>
        public static IEnumerator httpPostString(string url, string form, string[] myHeaders, Action<string, byte[]> callBack, Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                callBack?.Invoke("fail", Encoding.UTF8.GetBytes("701"));
                yield break;
            }
            if (string.IsNullOrEmpty(form))
            {
                using (UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, UnityWebRequest.kHttpVerbPOST))
                {
                    yield return httpPostData(webRequest, url, myHeaders, callBack, onProgress, "text/html; charset=utf-8");
                }
            }
            else
            {
                using (UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, UnityWebRequest.kHttpVerbPOST))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(form);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    yield return httpPostData(webRequest, url, myHeaders, callBack, onProgress, "text/html; charset=utf-8");
                }
            }
        }


        /// <summary>
        /// http Get
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="url"></param>
        public static IEnumerator httpGetBytes(string url, string[] myHeaders, Action<string, byte[]> callBack, Action<float> onProgress = null)
        {
            Debug.Log("httpGetBytes, url=" + url);
            if (string.IsNullOrEmpty(url))
            {
                callBack?.Invoke("fail", Encoding.UTF8.GetBytes("701"));
                yield break;
            }
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                if (myHeaders != null)
                {
                    for (int i = 0; i < myHeaders.Length; i += 2)
                    {
                        webRequest.SetRequestHeader(myHeaders[i], myHeaders[i + 1]);
                    }
                }
                webRequest.SendWebRequest();
                while (webRequest.result == UnityWebRequest.Result.InProgress)
                {
                    onProgress?.Invoke(webRequest.downloadProgress);
                    yield return null;
                }
                onProgress?.Invoke(1.0f);
                dealResult(webRequest, callBack, url);
            }
        }


        /// <summary>
        /// http Get
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="url"></param>
        public static IEnumerator httpGetText(string url, string[] myHeaders, Action<string, string> callBack, Action<float> onProgress = null)
        {
            yield return httpGetBytes(url, myHeaders, (mark, bytes)=> {
                string content = Encoding.UTF8.GetString(bytes);
                callBack?.Invoke(mark, content);
            }, onProgress);
        }




        /// <summary>
        /// http Post
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="url"></param>
        /// <param name="form"></param>
        private static IEnumerator httpPostData(UnityWebRequest webRequest, string url, string[] myHeaders, Action<string, byte[]> callBack, Action<float> onProgress = null, string contentType = "application/json")
        {
            if (myHeaders != null)
            {
                for (int i = 0; i < myHeaders.Length; i += 2)
                {
                    webRequest.SetRequestHeader(myHeaders[i], myHeaders[i + 1]);
                }
            }
            DownloadHandler downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.certificateHandler = new MyHttpsCert();
            if (webRequest.uploadHandler != null)
            {
                webRequest.uploadHandler.contentType = contentType;
            }
            webRequest.timeout = 5; // 5秒超时。
            webRequest.SendWebRequest();
            while (webRequest.result == UnityWebRequest.Result.InProgress)
            {
                onProgress?.Invoke(webRequest.downloadProgress);
                yield return null;
            }
            onProgress?.Invoke(1.0f);
            dealResult(webRequest, callBack, url);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="callBack"></param>
        /// <param name="url"></param>
        /// <param name="echo"></param>
        private static void dealResult(UnityWebRequest webRequest, Action<string, byte[]> callBack, string url)
        {
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("ToolHttp NetworkError: " + webRequest.error + " url=" + url);
                callBack?.Invoke("fail", Encoding.UTF8.GetBytes("707"));
            }
            else if (webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("ToolHttp HttpError: " + webRequest.error + " url=" + url);
                string code = webRequest.responseCode.ToString();
                callBack?.Invoke("fail", Encoding.UTF8.GetBytes(code));
            }
            else if (webRequest.result == UnityWebRequest.Result.Success)
            {
                byte[] data = webRequest.downloadHandler.data;
                callBack?.Invoke("succ", data);
            }
            webRequest.Dispose();
        }


    }
}