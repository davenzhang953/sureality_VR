using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BAPointCloudRenderer.CloudController;
using UnityEngine;
using UnityEngine.Networking;


namespace Seengene.XDK
{
    /// <summary>
    /// Design for download 'scale.txt'
    /// Use position and scale from 'scale.txt' to the gameObject
    /// </summary>

    public class PointCloudPose : MonoBehaviour
    {
        [HideInInspector]
        public string mapID;

        [HideInInspector]
        public string scaleTxtUrl;

        [HideInInspector]
        public string plyFileUrl;

        [HideInInspector]
        public bool UsePotree;

        [HideInInspector]
        public Action<float> onProgress;

        [HideInInspector]
        public Action<string> onLoadEnd;


        [HideInInspector]
        public Camera userCamera;

        private PlyMeshLoader m_PlyMeshObject;
        private DynamicPointCloudSet m_OctreeObject;
        private long fileSize;


        /// <summary>
        /// 移除2种点云对象
        /// </summary>
        public void RemoveAllPointCloud(bool resetPose = true)
        {
            removeOctree();
            clearPlyMesh();

            if (resetPose)
            {
                ResetPose();
            }
        }


        /// <summary>
        /// 准备好2种点云对象的容器
        /// </summary>
        public void PrepareLoaders()
        {
            m_OctreeObject = transform.GetComponentInChildren<DynamicPointCloudSet>(true);
            m_OctreeObject.gameObject.SetActive(false);
            m_PlyMeshObject = transform.GetComponentInChildren<PlyMeshLoader>(true);
            m_PlyMeshObject.gameObject.SetActive(false);
        }

        /// <summary>
        /// 重置Transform
        /// </summary>
        public void ResetPose()
        {
            Debug.Log("ResetPose for PointCloud");
            transform.localEulerAngles = Vector3.zero;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
        }


        /// <summary>
        /// 加载scale.txt
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IEnumerator loadRemoteFile()
        {
            if (string.IsNullOrEmpty(scaleTxtUrl))
            {
                Debug.LogError("scaleTxtUrl is null, can not download scale.txt, mapID=" + mapID);
                yield return loadPointCloudFile();
            }
            else
            {
                yield return loadScaleFile();
            }

        }


        /// <summary>
        /// 加载scale.txt
        /// </summary>
        /// <returns></returns>
        private IEnumerator loadScaleFile()
        {
            string txtFilePath = getScaleFilePath(mapID);
            long len = getFileLength(txtFilePath);
            if (len > 0)
            {
                Debug.Log("scaleFile exist. length=" + len + " path=" + txtFilePath);
                UseScaleTxt(txtFilePath);
                yield return loadPointCloudFile();
            }
            else // 从远程下载
            {
                Debug.Log("download txt file start, url=" + scaleTxtUrl);
                onProgress?.Invoke(0);

                int errCode = 0;
                yield return ToolHttp.loadRemoteFile(
                    scaleTxtUrl,
                    txtFilePath,
                    (err) => { errCode = err; },
                    (progress) => { onProgress?.Invoke(progress); }
                );

                if (errCode == 0)
                {
                    UseScaleTxt(txtFilePath);
                    yield return loadPointCloudFile();
                }
                else
                {
                    onLoadEnd?.Invoke("fail to download, url=" + scaleTxtUrl);
                    yield break;
                }
            }
        }



        /// <summary>
        /// 加载点云
        /// </summary>
        /// <returns></returns>
        private IEnumerator loadPointCloudFile()
        {
            RemoveAllPointCloud(false);

            if (UsePotree)
            {
                m_OctreeObject.gameObject.SetActive(true);
                m_OctreeObject.userCamera = userCamera;

                ZipRemoteLoader zipRemote = m_OctreeObject.GetComponent<ZipRemoteLoader>();
                zipRemote.mapID = mapID;
                zipRemote.zipFileUrl = plyFileUrl.Replace("/map/", "/Potree/").Replace("/fused_new.ply", "/po.tar.gz") + "?t=" + (DateTime.Now.Ticks / 10000);
                zipRemote.onProgress = onProgress;
                zipRemote.onLoadEnd = onLoadEnd;
                yield return zipRemote.loadRemoteFile();

                showOctree();
            }
            else
            {
                m_PlyMeshObject.gameObject.SetActive(true);
                m_PlyMeshObject.onProgress = onProgress;
                m_PlyMeshObject.onLoadEnd = onLoadEnd;
                m_PlyMeshObject.mapID = mapID;
                m_PlyMeshObject.plyFileUrl = plyFileUrl;
                yield return m_PlyMeshObject.loadRemoteFile();
            }
        }


        /// <summary>
        /// 显示八叉树点云
        /// </summary>
        private void showOctree()
        {
            m_OctreeObject.showBoundingBox = true;

            ZipRemoteLoader zipLoader = m_OctreeObject.GetComponent<ZipRemoteLoader>();
            string cloudPath = ZipRemoteLoader.getZipFolderPath(zipLoader.mapID);

            PointCloudLoader pcLoader = m_OctreeObject.GetComponent<PointCloudLoader>();
            pcLoader.loadingPath = null;
            if (Directory.Exists(cloudPath))
            {
                pcLoader.LoadPointCloud(cloudPath);
            }
            else
            {
                Debug.LogError("Folder no exists, path="+cloudPath);
            }
        }

        /// <summary>
        /// 清空ply点云
        /// </summary>
        private void clearPlyMesh()
        {
            if (m_PlyMeshObject == null)
            {
                m_PlyMeshObject = transform.GetComponentInChildren<PlyMeshLoader>(true);
            }
            if (m_PlyMeshObject != null)
            {
                m_PlyMeshObject.clearMesh();
                m_PlyMeshObject.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 清除八叉树点云
        /// </summary>
        private void removeOctree()
        {
            if (m_OctreeObject == null)
            {
                m_OctreeObject = transform.GetComponentInChildren<DynamicPointCloudSet>(true);
            }
            if (m_OctreeObject != null)
            {
                PointCloudLoader pcLoader = m_OctreeObject.GetComponent<PointCloudLoader>();
                pcLoader.RemovePointCloud();
                pcLoader.stopThread();

                m_OctreeObject.OnDisable();
                Debug.Log("SeengeneXDKSystem removeBaTree, succ");
                m_OctreeObject.gameObject.SetActive(false);

                DynamicPointCloudSet pointCloudSet = m_OctreeObject.GetComponent<DynamicPointCloudSet>();
                if (pointCloudSet)
                {
                    pointCloudSet.StopRendering();
                }

                for (int i = m_OctreeObject.transform.childCount - 1; i >= 0; i--)
                {
                    var child = m_OctreeObject.transform.GetChild(i);
#if UNITY_EDITOR
                    DestroyImmediate(child.gameObject);
#else
                    Destroy(child.gameObject);
#endif
                }

                m_OctreeObject.gameObject.SetActive(false);
            }
        }


        /// <summary>
        /// 获取文件的大小
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <returns></returns>
        private long getFileLength(string localFilePath)
        {
            if (File.Exists(localFilePath))
            {
                FileInfo info = new FileInfo(localFilePath);
                return info.Length;
            }
            return 0;
        }


        /// <summary>
        /// 读取配置文件，设定点云父节点的旋转和缩放
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="g"></param>
        private void UseScaleTxt(string localFilePath)
        {
            string m_Str = "";
            string[] strs = File.ReadAllLines(localFilePath);
            for (int i = 0; i < strs.Length; i++)
            {
                m_Str += strs[i];//读取每一行，并连起来
                m_Str += "\n";//每一行末尾换行
            }
            string[] str = m_Str.Split(',');
            float[] f = new float[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                f[i] = float.Parse(str[i]);
            }

            float W = f[0], X = f[1], Y = f[2], Z = f[3], Scale = f[4];
            Vector3 v = new Quaternion(X, Y, Z, W).eulerAngles;
            float change_X = 360 - v.x;
            float change_Y = v.y;
            float change_Z = 360 - v.z;
            transform.localEulerAngles = new Vector3(change_X, change_Y, change_Z);
            transform.localScale = new Vector3(Scale, Scale, Scale);
            Debug.Log("PointCloudPose localEulerAngles=" + transform.localEulerAngles);
            Debug.Log("PointCloudPose localScale=" + transform.localScale);
        }




        private string getScaleFilePath(string mapID)
        {
            string folder = Path.Combine(Application.persistentDataPath, "scale");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, mapID + "_scale.txt");
            return filePath;
        }



        /// <summary>
        /// 移除所有的缓存文件
        /// </summary>
        /// <returns></returns>
        public string ClearAllCachedFiles()
        {
            fileSize = 0;
            string folder = Path.Combine(Application.persistentDataPath, "ply");
            DeleteAllFile(folder);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            folder = Path.Combine(Application.persistentDataPath, "octree");
            DeleteAllFile(folder);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            folder = Path.Combine(Application.persistentDataPath, "scale");
            DeleteAllFile(folder);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            string str = "";
            if (fileSize >= 1000000)
            {
                str = (fileSize / 1000000) + "M ";
            }
            else if (fileSize >= 1000)
            {
                str = (fileSize / 1000) + "K ";
            }
            else
            {
                str = fileSize + " byte";
            }
            return str;
        }


        /// <summary>
        /// 删除文件夹中的文件
        /// </summary>
        /// <param name="fullPath"></param>
        private void DeleteAllFile(string fullPath)
        {
            if (Directory.Exists(fullPath))
            {
                //获取指定路径下面的所有文件，然后进行删除
                string[] files = Directory.GetFiles(fullPath);
                for (int i = 0; i < files.Length; i++)
                {
                    string path = files[i];
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                    if (File.Exists(path))
                    {
                        FileInfo info = new FileInfo(path);
                        fileSize += info.Length;

                        File.Delete(path);
                    }
                }

                // 递归删除子文件夹中的文件
                string[] folders = Directory.GetDirectories(fullPath);
                if (folders != null)
                {
                    for (int i = 0; i < folders.Length; i++)
                    {
                        DeleteAllFile(folders[i]);
                    }
                }
            }
        }


        public void UpdateByEditor()
        {
            if (m_OctreeObject != null)
            {
                if (m_OctreeObject.PointRenderer != null)
                {
                    m_OctreeObject.PointRenderer.Update();
                }
            }
        }
    }
}
