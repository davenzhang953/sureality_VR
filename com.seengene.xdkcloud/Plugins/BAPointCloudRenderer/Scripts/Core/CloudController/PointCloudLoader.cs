using BAPointCloudRenderer.CloudData;
using BAPointCloudRenderer.Loading;
using System;
using System.Threading;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace BAPointCloudRenderer.CloudController {
    /// <summary>
    /// Use this script to load a single PointCloud from a directory.
    ///
    /// Streaming Assets support provided by Pablo Vidaurre
    /// </summary>
    public class PointCloudLoader : MonoBehaviour {

        [HideInInspector]
        public string cloudPath;

        [HideInInspector]
        public string cloudFullPath;

        /// <summary>
        /// The PointSetController to use
        /// </summary>
        [HideInInspector]
        public AbstractPointCloudSet setController;

        [HideInInspector]
        public Transform setControllerObj;

        private Node rootNode;

        [HideInInspector]
        public string loadingPath;


        private Thread thread1;

        private void Awake()
        {
            if (setController == null)
            {
                setController = GetComponent<AbstractPointCloudSet>();
            }
        }

        void Start() {
            if (setController == null)
            {
                setController = GetComponent<AbstractPointCloudSet>();
            }
        }

        private void OnDestroy()
        {
            stopThread();
        }

        private void OnApplicationQuit()
        {
            stopThread();
        }


        public void stopThread()
        {
            if (thread1 != null)
            {
                try
                {
                    thread1.Abort();
                }
                catch (Exception ee)
                {
                    Debug.LogError("OnDestroy StopThread " + Thread.CurrentThread.Name + "\n" + ee);
                }
                thread1 = null;
            }
        }

        private void LoadHierarchy() {
            try {
                if (!cloudPath.EndsWith("/")) {
                    cloudPath = cloudPath + "/";
                }
                
                PointCloudMetaData metaData = CloudLoader.LoadMetaData(cloudPath, cloudFullPath, false);
                //Debug.Log("LoadHierarchy 13");

                setController.UpdateBoundingBox(this, metaData.boundingBox, metaData.tightBoundingBox);
                //Debug.Log("LoadHierarchy 14");

                rootNode = CloudLoader.LoadHierarchyOnly(metaData);
                //Debug.Log("LoadHierarchy 15");
                
                setController.AddRootNode(this, rootNode, metaData);
                //Debug.Log("LoadHierarchy 16");
                loadingPath = null;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                Debug.LogError("Could not find file: " + ex.FileName);
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                Debug.LogError("Could not find directory: " + ex.Message);
            }
            catch (System.Net.WebException ex)
            {
                Debug.LogError("Could not access web address. " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex + Thread.CurrentThread.Name);
            }
        }

        /// <summary>
        /// Starts loading the point cloud. When the hierarchy is loaded it is registered at the corresponding point cloud set
        /// </summary>
        public void LoadPointCloud(string filePath = null) {
            if (filePath != null)
            {
                cloudPath = filePath;
            }
            if(string.Equals(loadingPath, cloudPath))
            {
                return;
            }
            loadingPath = cloudPath;
            Debug.Log("loadingPath=" + cloudPath);
            if (rootNode == null && setController != null && cloudPath != null)
            {
                cloudFullPath = Path.Combine(Application.persistentDataPath, cloudPath);
                setController.resetAll();
                setController.RegisterController(this);
                stopThread();
                thread1 = new Thread(LoadHierarchy);
                thread1.Name = "Loader for " + cloudPath;
                thread1.Start();

                if (setControllerObj == null)
                {
                    setControllerObj = setController.transform;
                }
                setControllerObj.localEulerAngles = new Vector3(90,0,0); // cuilichen 20210804 这里的旋转是为了和unity的坐标系对应上。
                setControllerObj.localPosition = Vector3.zero;
                setControllerObj.localScale = Vector3.one;

            }
        }


        /// <summary>
        /// Removes the point cloud from the scene. Should only be called from the main thread!
        /// </summary>
        /// <returns>True if the cloud was removed. False, when the cloud hasn't even been loaded yet.</returns>
        public bool RemovePointCloud() {
            if (rootNode == null) {
                return false;
            }
            setController.RemoveRootNode(this, rootNode);
            rootNode = null;
            return true;
        }

    }
}
