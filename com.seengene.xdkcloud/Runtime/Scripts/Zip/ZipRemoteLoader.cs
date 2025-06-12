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
    public class ZipRemoteLoader : MonoBehaviour
    {
        [HideInInspector]
        public Action<float> onProgress;
        [HideInInspector]
        public Action<string> onLoadEnd;

        [HideInInspector]
        public string zipFileUrl;

        [HideInInspector]
        public string mapID;

        [HideInInspector]
        public bool filesLoaded;
        [HideInInspector]
        public string errorMsg;

        [HideInInspector]
        public Action<float> onUnzipProgress;
        [HideInInspector]
        public Action<string> onUnzipFinish;



        private void Start()
        {
        
        }

        /// <summary>
        /// 加载远程zip资源
        /// </summary>
        /// <param name="plyUrl"></param>
        /// <param name="scaleUrl"></param>
        /// <param name="mapID"></param>
        /// <param name="onLoadEnd"></param>
        /// <param name="onProgress"></param>
        public void loadPointCloudFromUrl()
        {
            StartCoroutine(loadRemoteFile());
        }


        /// <summary>
        /// 移除缓存的文件
        /// </summary>
        /// <param name="mapID"></param>
        public int deleteCached(string mapID)
        {
            int count = 0;
            string zipFolderPath = getZipFolderPath(mapID);
            if (Directory.Exists(zipFolderPath))
            {
                Directory.Delete(zipFolderPath, true);
                count++;
            }
            string zipFilePath = getZipFilePath(mapID);
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
                count++;
            }
            return count;
        }



        /// <summary>
        /// 加载网络图片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IEnumerator loadRemoteFile()
        {
            float allProgress = 0;
            string zipFolderPath= getZipFolderPath(mapID);
            if (Directory.Exists(zipFolderPath))
            {
                Debug.Log("zipFolder exist. path=" + zipFolderPath);
                allProgress = 0.9f;
            }
            else
            {
                string zipFilePath = getZipFilePath(mapID);
                long len = getFileLength(zipFilePath);
                if (len > 0) // 有内容的文件
                {
                    Debug.Log("zipFile exist. length="+len+" path=" + zipFilePath);
                    allProgress = 0.9f;
                }
                else // 从远程下载
                {
                    Debug.Log("download zip file start, url=" + zipFileUrl);
                    onProgress?.Invoke(0);

                    int errCode = 0;
                    yield return ToolHttp.loadRemoteFile(
                        zipFileUrl,
                        zipFilePath,
                        (err)=> { errCode = err; },
                        (progress)=> { onProgress?.Invoke(progress); }
                    );

                    if (errCode == 0)
                    {
                        yield return unzipPointCloud();
                    }
                    else
                    {
                        onLoadEnd?.Invoke("fail to download, url="+zipFileUrl);
                        yield break;
                    }
                }
            }
        
            allProgress = 1.0f;
            filesLoaded = true;
            onProgress?.Invoke(allProgress);
            yield return new WaitForSeconds(0.1f);
            onLoadEnd?.Invoke(null);
        }

        /// <summary>
        /// 获取文件的大小
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private long getFileLength(string path)
        {
            if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                return info.Length;
            }
            return 0;
        }

        /// <summary>
        /// 解压缩下载到的zip文件
        /// </summary>
        /// <param name="onUnzipFinish"></param>
        /// <param name="onProgress"></param>
        public IEnumerator unzipPointCloud()
        {
            this.onUnzipFinish = onLoadEnd;
            this.onUnzipProgress = onProgress;
            string zipFilePath = getZipFilePath(mapID);
            string tempPath = getZipFolderPath(mapID) + "temp";
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            FileStream fs = File.OpenRead(zipFilePath);
            Debug.Log("begin unzipTheFile..... fileStream ok ");
            //StartCoroutine(ZipUtility.UnzipFileAsync(fs, tempPath, null, onUnzipFinish, onUnzipProgress));
            yield return ZipUtility.UnzipTarGzAsync(zipFilePath, tempPath, onUnzipFinish, onProgress);

            string zipFolderPath = ZipRemoteLoader.getZipFolderPath(mapID.ToString());
            string folderPo = findFileOrFolder(tempPath, "po"); // 这里必须是小写
            Directory.Move(folderPo, zipFolderPath);

            // 删除zip文件
            string zipFile = getZipFilePath(mapID.ToString());
            try
            {
                if (File.Exists(zipFile))
                {
                    File.Delete(zipFile);
                }
            }
            catch (Exception e) { }
        }


        private string findFileOrFolder(string root, string name)
        {
            name = name.ToLower();
            foreach (string item in Directory.GetDirectories(root))
            {
                string fileName = Path.GetFileName(item).ToLower();
                if (string.Equals(fileName, name))
                {
                    return item;
                }
                string ret = findFileOrFolder(item, name);
                if (ret != null)
                {
                    return ret;
                }
            }
            return null;
        }

        public static string getZipFolderPath(string mapID)
        {
            string folder = Path.Combine(Application.persistentDataPath, "octree");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, mapID + "_unzip");
            return filePath;
        }

        public static string getZipFolderSubPath(string mapID)
        {
            string filePath = Path.Combine(getZipFolderPath(mapID), "work");
            filePath = Path.Combine(filePath, "Po");
            int length = Application.persistentDataPath.Length;
            return filePath.Substring(length + 1);
        }

        public static string getZipFilePath(string mapID)
        {
            string folder = Path.Combine(Application.persistentDataPath, "octree");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, mapID + ".tar.gz");
            return filePath;
        }



    }
}