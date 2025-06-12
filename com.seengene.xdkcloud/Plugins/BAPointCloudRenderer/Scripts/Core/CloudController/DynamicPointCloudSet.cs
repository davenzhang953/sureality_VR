using BAPointCloudRenderer.Loading;
using BAPointCloudRenderer.ObjectCreation;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BAPointCloudRenderer.CloudController
{

    /// <summary>
    /// Point Cloud Set to display a large point cloud. All the time, only the points which are needed for the current camera position are loaded from the disk (as described in the thesis).
    /// </summary>
    public class DynamicPointCloudSet : AbstractPointCloudSet
    {
        /// <summary>
        /// Point Budget - Maximum Number of Points in Memory / to Render
        /// </summary>
        public uint pointBudget = 1000000;
        /// <summary>
        /// Minimum Node Size
        /// </summary>
        public int minNodeSize = 10;
        /// <summary>
        /// Maximum number of nodes loaded per frame
        /// </summary>
        public uint nodesLoadedPerFrame = 15;
        /// <summary>
        /// Maximum number of nodes having their gameobjects created per frame
        /// </summary>
        public uint nodesGOsPerFrame = 30;
        /// <summary>
        /// Cache Size in POints
        /// </summary>
        public uint cacheSizeInPoints = 1000000;
        /// <summary>
        /// Camera to use. If none is specified, Camera.main is used
        /// </summary>
        public Camera userCamera;


        // Use this for initialization
        protected override void Initialize()
        {
#if UNITY_EDITOR
            if (userCamera == null)
            {
                SceneView view = SceneView.lastActiveSceneView;
                userCamera = view.camera;
            }
#else
            if (userCamera == null)
            {
                userCamera = Camera.main;
            }
#endif


            if (myGameObject == null)
            {
                myGameObject = this.gameObject;
            }
            PointRenderer = new V2Renderer(this, minNodeSize, pointBudget, nodesLoadedPerFrame, nodesGOsPerFrame, userCamera, meshConfiguration, cacheSizeInPoints, myGameObject);
        }


        // Update is called once per frame
        void Update()
        {
            if (!CheckReady())
            {
                return;
            }
            PointRenderer.Update();
            DrawDebugInfo();
        }
    }
}
