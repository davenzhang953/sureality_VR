using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Seengene.XDK
{
    [RequireComponent(typeof(MeshFilter))]
    public class PlyMeshLoader : MonoBehaviour
    {
        [HideInInspector]
        public Action<float> onProgress;
        [HideInInspector]
        public Action<string> onLoadEnd;

        [HideInInspector]
        public string plyFileUrl;


        [HideInInspector]
        public string mapID;

        [HideInInspector]
        public float plyProgress;
        [HideInInspector]
        public bool plyLoaded;
        [HideInInspector]
        public string errorMsg;


        private Mesh myMesh;
        private PlyDataLoader plyDataLoader;




        /// <summary>
        /// 加载远程ply资源并显示在本对象上
        /// </summary>
        /// <param name="plyUrl"></param>
        /// <param name="scaleUrl"></param>
        /// <param name="mapID"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProgress"></param>
        public void loadPointCloudFromUrl(string plyUrl, string mapID, Action<string> onLoadEnd, Action<float> onProgress = null) {
            plyLoaded = false;
            errorMsg = null;
            if (plyUrl != null) {
                plyFileUrl = plyUrl;
            }
            if (mapID != null) {
                this.mapID = mapID;
            }
            this.onLoadEnd = onLoadEnd;
            this.onProgress = onProgress;
            if (string.IsNullOrEmpty(plyFileUrl)) {
                errorMsg = "plyFileUrl is blank";
                Debug.Log(errorMsg);
                onLoadEnd?.Invoke(errorMsg);
                return;
            }

            myMesh = new Mesh();
            myMesh.name = "Ply" + mapID;
            StartCoroutine(loadRemoteFile());
        }


        /// <summary>
        /// 移除缓存的文件
        /// </summary>
        /// <param name="mapID"></param>
        public int deleteCached(string mapID) {
            string folder = Path.Combine(Application.persistentDataPath, "ply");
            if (Directory.Exists(folder)) {
                string filePath = Path.Combine(folder, mapID + ".ply");
                if (File.Exists(filePath)) {
                    File.Delete(filePath);
                    Debug.Log("Delete file succ: " + filePath);
                    return 1;
                }
            }
            return 0;
        }


        /// <summary>
        /// 移除当前的显示的mesh数据
        /// </summary>
        public void clearMesh() {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf) {
                mf.mesh = null;
            }

        }


        /// <summary>
        /// 加载网络图片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IEnumerator loadRemoteFile() {
            bool plyFileIsOK = false;
            float progressDone = 0f;
            plyProgress = 0;
            string plyFilePath = getPlyFilePath(mapID);
            Debug.Log("localPath=" + plyFilePath);
            long len = getFileLength(plyFilePath);
            if (len > 0) {
                Debug.Log("plyFile exist. length=" + len + " path=" + plyFilePath);
                plyFileIsOK = true;
            }
            else // 从远程下载
            {
                Debug.Log("download ply file start, url=" + plyFileUrl);
                onProgress?.Invoke(0);

                int errCode = 0;
                yield return ToolHttp.loadRemoteFile(
                    plyFileUrl,
                    plyFilePath,
                    (err) => { errCode = err; },
                    (progress) => { onProgress?.Invoke(progress); }
                );

                if (errCode == 0)
                {
                    plyFileIsOK = true;
                }
                else
                {
                    onLoadEnd?.Invoke("fail to download, url=" + plyFileUrl);
                    yield break;
                }
            }

            if (plyFileIsOK) {
                if (plyDataLoader == null)
                {
                    plyDataLoader = new PlyDataLoader();
                }
                plyDataLoader.LoadPly(plyFilePath);

                while (plyDataLoader.isLoading) {
                    plyProgress = progressDone + (1 - progressDone) * plyDataLoader.progress;
                    onProgress?.Invoke(plyProgress);
                    yield return new WaitForSeconds(0.1f);
                }
                if (plyDataLoader.dataHeader == null) {
                    errorMsg = "Error: file format error";
                    onLoadEnd?.Invoke(errorMsg);
                } else {
                    plyProgress = 1.0f;
                    plyLoaded = true;
                    yield return null;

                    uploadMeshData();
                    onLoadEnd?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 获取文件的大小
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private long getFileLength(string path) {
            if (File.Exists(path)) {
                FileInfo info = new FileInfo(path);
                return info.Length;
            }
            return 0;
        }

        /// <summary>
        /// 更新对象的空间参数
        /// </summary>
        /// <param name="txtFilePath"></param>
        private void uploadMeshData() {
            Debug.Log("uploadMeshData starting");
            if (myMesh == null) {
                myMesh = new Mesh();
                myMesh.name = "Ply" + mapID;
            }


            MeshFilter mf = GetComponent<MeshFilter>();
            plyDataLoader.addMeshData(myMesh, mf);
        }


        public static string getPlyFilePath(string mapID) {
            string folder = Path.Combine(Application.persistentDataPath, "ply");
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, mapID + ".ply");
            return filePath;
        }


    }

}
