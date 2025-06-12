using BAPointCloudRenderer.CloudData;
using System.Collections.Generic;
using UnityEngine;
using BAPointCloudRenderer.CloudController;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BAPointCloudRenderer.ObjectCreation {

    /// <summary>
    /// What kind of interpolation to use
    /// </summary>
    public enum FragInterpolationMode {
        /// <summary>
        /// No interpolation
        /// </summary>
        OFF,
        /// <summary>
        /// Paraboloids
        /// </summary>
        PARABOLOIDS,
        /// <summary>
        /// Cones
        /// </summary>
        CONES
    }

    /// <summary>
    /// This is the default Mesh Configuration, that is able to render points as pixels, quads, circles and also provides fragment and cone interpolations using the fragment shader (see Thesis chapter 3.3.4 "Interpolation").
    /// This works using Geometry Shader Quad Rendering, as described in the Bachelor Thesis in chapter 3.3.3.
    /// This configuration also supports changes of the parameters while the application is running. Just change the parameters and check the checkbox "reload".
    /// This class replaces GeoQuadMeshConfiguration in Version 1.2.
    /// </summary>
    public class DefaultMeshConfiguration : MeshConfiguration {
        
        /// <summary>
        /// Whether the quads should be rendered as circles (true) or as squares (false)
        /// </summary>
        public bool renderCircles = false;
        /// <summary>
        /// True, if pointRadius should be interpreted as pixels, false if it should be interpreted as world units
        /// </summary>
        public bool screenSize = true;
        /// <summary>
        /// Wether and how to use interpolation
        /// </summary>
        public FragInterpolationMode interpolation = FragInterpolationMode.OFF;
        /// <summary>
        /// If changing the parameters should be possible during execution, this variable has to be set to true in the beginning! Later changes to this variable will not change anything
        /// </summary>
        public const bool reloadingPossible = true;
        /// <summary>
        /// Set this to true to reload the shaders according to the changed parameters. After applying the changes, the variable will set itself back to false.
        /// </summary>
        public bool reload = false;
        
        /// <summary>
        /// If set to true, the Bounding Boxes of the individual octree nodes will be displayed.
        /// </summary>
        public bool displayLOD = false;


        public Material material;

        /// <summary>
        /// Radius of the point (in pixel or world units, depending on variable screenSize)
        /// </summary>
        public float pointRadius = 0.01f;

        private HashSet<GameObject> gameObjectCollection = null;

        private void LoadShaders() {
            if (material == null)
            {
                material = new Material(Shader.Find("PCX/Point Primitive"));
            }
            material.SetFloat("_PointSize", pointRadius);
            material.SetInt("_Circles", renderCircles ? 1 : 0);
            CheckCamera();
        }

        public void Start() {
            if (reloadingPossible) {
                gameObjectCollection = new HashSet<GameObject>();
            }
            LoadShaders();
        }

        private void CheckCamera()
        {
#if UNITY_EDITOR
            if (renderCamera == null)
            {
                SceneView view = SceneView.lastActiveSceneView;
                renderCamera = view.camera;
            }
#else
            if (renderCamera == null)
            {
                renderCamera = Camera.main;
            }
#endif
        }

        public void Update() {
            CheckCamera();
            if (renderCamera == null)
            {
                return;
            }
            if (reload && gameObjectCollection != null) {
                LoadShaders();
                foreach (GameObject go in gameObjectCollection) {
                    go.GetComponent<MeshRenderer>().material = material;
                }
                reload = false;
            }
            if (displayLOD)
            {
                if(gameObjectCollection != null)
                {
                    foreach (GameObject go in gameObjectCollection)
                    {
                        BoundingBoxComponent bbc = go.GetComponent<BoundingBoxComponent>();
                        Utility.BBDraw.DrawBoundingBox(bbc.boundingBox, bbc.parent, Color.red, false);
                    }
                }
            }
            if (screenSize) {
                if (interpolation != FragInterpolationMode.OFF) {
                    Matrix4x4 invP = (GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, true)).inverse;
                    material.SetMatrix("_InverseProjMatrix", invP);
                    material.SetFloat("_FOV", Mathf.Deg2Rad * renderCamera.fieldOfView);
                }
                Rect screen = renderCamera.pixelRect;
                material.SetInt("_ScreenWidth", (int)screen.width);
                material.SetInt("_ScreenHeight", (int)screen.height);
            }
        }

        public override GameObject CreateGameObject(string name, Vector3[] vertexData, Color[] colorData, BoundingBox boundingBox, Transform parent) {
            GameObject gameObject = new GameObject(name);

            Mesh mesh = new Mesh();

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;

            int[] indecies = new int[vertexData.Length];
            for (int i = 0; i < vertexData.Length; ++i) {
                indecies[i] = i;
            }
            mesh.vertices = vertexData;
            mesh.colors = colorData;
            mesh.SetIndices(indecies, MeshTopology.Points, 0);

            //Set Translation
            gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
            gameObject.transform.SetParent(parent, false);

            BoundingBoxComponent bbc = gameObject.AddComponent<BoundingBoxComponent>();
            bbc.boundingBox = boundingBox; ;
            bbc.parent = parent;

            if (gameObjectCollection != null) {
                gameObjectCollection.Add(gameObject);
            }
            CheckPointNum();
            return gameObject;
        }

        public override int GetMaximumPointsPerMesh() {
            return 65000;
        }

        public override void RemoveGameObject(GameObject gameObject) {
            if (gameObjectCollection != null) {
                gameObjectCollection.Remove(gameObject);
            }
            if (gameObject != null) {
#if UNITY_EDITOR
                DestroyImmediate(gameObject.GetComponent<MeshFilter>().sharedMesh);
                DestroyImmediate(gameObject);
#else
                Destroy(gameObject.GetComponent<MeshFilter>().sharedMesh);
                Destroy(gameObject);
#endif
            }
        }


        public void CheckPointNum()
        {
#if UNITY_EDITOR
            var pcSet = GetComponent<AbstractPointCloudSet>();
            uint pointCount = pcSet.GetPointCount();
            Debug.Log("PointNum = " + pointCount + " NodeNum="+transform.childCount);
#endif
        }

    }
}
