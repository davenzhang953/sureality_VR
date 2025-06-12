
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor.SceneManagement;
using Application = UnityEngine.Application;
using Seengene.XDK;
using UnityEngine.SceneManagement;
using Dummiesman;
using Unity.EditorCoroutines.Editor;

public class SeengeneXDKSystem : EditorWindow
{
    static XDKCloudSession xdkSession;
    static PointCloudPose m_PointCloudPose;
    static PointCloudPose m_PointCloudNight;
    static Material m_CustomMaterial;
    static SceneAsset m_Scene;

    static long mapIdInDaytime = 1;
    static long mapIdAtNight = 0;
    static XDKMapInfo mapInfo1 = null;
    static XDKMapInfo mapInfo2 = null;
    private static int width = 800;
    private static int height = 500;

    private Vector2 scrollPos;
    private bool IsSceneReady = false;

    const float w1 = 120f;
    float w2 = 110f;
    float w3 = 110f;

    enum ETabs
    {
        MapIDs,             
        SceneObjects,        
        PointClouds,        
    }

    private ETabs currentTab = ETabs.MapIDs;

    private string[] TabTitles = new string[] { "Map Config", "Scene Objects", "Point Cloud" };
    private float tabWidth = 250;
    private float tabHeight = 38;
    GUIStyle m_tabBtnStyle;
    GUIStyle m_tabBtnOnStyle;
    GUIStyle m_textStyle;
    GUIStyle m_lineLeftStyle;
    GUIStyle m_lineRightStyle;
    GUIStyle m_tabTextSytel;
    GUIStyle m_tabStyle;
    GUIStyle m_iconNameStyle;
    GUIStyle m_saveBtnStyle;
    GUIStyle m_toggleStyle;
    GUIStyle m_titleStyle;

    private static SeengeneXDKSystem m_Instance;
    public static SeengeneXDKSystem Instance
    {
        get { return m_Instance; }
        private set { m_Instance = value; }
    }

    private void InitGUIStyle()
    {
        tabWidth = (int)(position.width - 20) / 3;
        m_tabTextSytel = new GUIStyle("Label");


        m_tabBtnStyle = new GUIStyle("Tooltip"); // ButtonMid
        m_tabBtnStyle.alignment = TextAnchor.MiddleCenter;
        m_tabBtnStyle.fontStyle = m_tabTextSytel.fontStyle;
        m_tabBtnStyle.fontSize = 14;
        m_tabBtnStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1);

        /// TE NodeBox / TE NodeBoxSelected
        /// Tooltip /  U2D.createRect
        /// LightmapEditor / MeTransOnLeft / PR Ping / RL Header/ SelectionRect / U2D.createRect

        m_tabBtnOnStyle = new GUIStyle("SelectionRect");
        m_tabBtnOnStyle.alignment = TextAnchor.MiddleCenter;
        m_tabBtnOnStyle.fontStyle = m_tabTextSytel.fontStyle;
        m_tabBtnOnStyle.fontSize = 14;
        m_tabBtnStyle.normal.textColor = new Color(1f, 1f, 1f, 1);

        m_textStyle = new GUIStyle("HeaderLabel");
        m_textStyle.fontSize = 13;
        m_textStyle.alignment = TextAnchor.MiddleCenter;


        m_lineLeftStyle = new GUIStyle("CenteredLabel");
        m_lineLeftStyle.alignment = TextAnchor.MiddleRight;
        m_lineLeftStyle.fontSize = 12;
        m_lineLeftStyle.normal.textColor = new Color(1f, 1f, 1f, 1);
        m_lineLeftStyle.onNormal.textColor = new Color(1f, 1f, 1f, 1);

        m_lineRightStyle = new GUIStyle("CenteredLabel");
        m_lineRightStyle.alignment = TextAnchor.MiddleLeft;
        m_lineRightStyle.fontSize = 12;
        m_lineRightStyle.normal.textColor = new Color(1f, 1f, 1f, 1);
        m_lineRightStyle.onNormal.textColor = new Color(1f, 1f, 1f, 1);


        m_titleStyle = new GUIStyle("AM VuValue");
        m_titleStyle.alignment = TextAnchor.MiddleRight;
        m_titleStyle.fontSize = 15;

        m_iconNameStyle = new GUIStyle("WarningOverlay");
        m_iconNameStyle.fontSize = 12;

        m_toggleStyle = new GUIStyle("OL ToggleWhite");

        m_saveBtnStyle = new GUIStyle("flow node 1");
        m_saveBtnStyle.fontSize = 20;
        m_saveBtnStyle.fixedHeight = 40;
        m_saveBtnStyle.alignment = TextAnchor.MiddleCenter;
    }




    [MenuItem("Window/Seengene XDK")]
    static void CreatWindows()
    {
        int x = (Screen.currentResolution.width - width) / 2;
        int y = (Screen.currentResolution.height - height) / 2;

        Debug.Log("Screen.currentResolution: " + Screen.currentResolution.width + "," +
                  Screen.currentResolution.height + " dpi=" + Screen.dpi);
        m_Instance = (SeengeneXDKSystem)GetWindow(typeof(SeengeneXDKSystem), false);
        m_Instance.titleContent = new GUIContent("Seengene XDK");
        m_Instance.position = new Rect(x, y, width, height);
        m_Instance.minSize = new Vector2(width, height * 0.5f);

        m_Instance.InitGUIStyle();
        Instance.DestroyPointCloudNight();
        Texture titleTexture = AssetDatabase.LoadAssetAtPath<Texture>(EditorConfigs.Path_TitleIcon);
        if (titleTexture != null)
        {
            m_Instance.titleContent.image = titleTexture;
        }
        Scene temp = SceneManager.GetActiveScene();
        m_Scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(temp.path);


        AssetDatabase.Refresh();
    }


    void OnGUI()
    {
        /// save font size
        var labelFontSize = GUI.skin.label.fontSize;

        tabWidth = (position.width - 20) / 3;
        if (IfMapInfoValid())
        {
            if (IsSceneReady)
            {
                DrawThreeTabs();
            }
            else
            {
                DrawTwoTabs();
            }
        }
        else
        {
            DrawOneTab();
            IsSceneReady = false;
        }
        

        /// set back
        GUI.skin.label.fontSize = labelFontSize;
    }

    public SeengeneXDKSystem()
    {
        if (m_Instance == null)
        {
            m_Instance = this;
        }
        SetDefault();
    }

    static void SetDefault()
    {
        mapIdInDaytime = 1;
        mapInfo1 = null;
        mapInfo2 = null;
    }


    private void FindPointCloudObjects()
    {
        SceneRoot RootObj = ObjectTool.FindAnyObjectByType<SceneRoot>(true);
        if (RootObj == null)
        {
            GameObject prefabSceneRoot = PrefabUtility.LoadPrefabContents(EditorConfigs.Path_SceneRoot);
            RootObj = Instantiate(prefabSceneRoot).GetComponent<SceneRoot>();
            RootObj.name = "XJ_SceneRoot";
            RootObj.transform.localEulerAngles = Vector3.zero;
            RootObj.transform.localScale = Vector3.one;
            RootObj.transform.localPosition = Vector3.zero;
        }


        var items = RootObj.GetComponentsInChildren<PointCloudPose>(true);
        foreach(var item in items)
        {
            if(item.transform.name == "PointCloudPose")
            {
                m_PointCloudPose = item;
                return;
            }
        }
        if (m_PointCloudPose == null && items.Length > 0)
        {
            m_PointCloudPose = items[0];
        }
    }



    private void OnDestroy()
    {
        DestroyPointCloudNight();

        if (m_PointCloudPose != null)
        {
            m_PointCloudPose.RemoveAllPointCloud();
        }

        removeSceneMesh();

        SaveActiveScene();
    }

    private void SaveActiveScene()
    {
        var currScene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(currScene, currScene.path);
    }

    /// <summary>
    /// 
    /// </summary>
    private void DrawOneTab()
    {
        GUILayout.Space(4);
        GUILayout.BeginHorizontal(GUILayout.Height(35));
        for (int i = 0; i < 1; ++i)
        {
            m_tabStyle = m_tabBtnStyle;
            GUILayout.Space(5);
            GUILayout.Button(TabTitles[i], m_tabStyle, GUILayout.Width(tabWidth), GUILayout.Height(tabHeight));
        }
        currentTab = (ETabs)0;
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        w2 = position.width - 40 - w1;
        w3 = position.width - 40;
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

        GUI_MapIDs();

        GUILayout.EndScrollView();
    }





    private void GUI_MapIDs()
    {
        if (xdkSession == null)
        {
            xdkSession = ObjectTool.FindAnyObjectByType<XDKCloudSession>(true);
        }
        if (xdkSession != null && mapIdInDaytime <= 1)
        {
            if (xdkSession.mapItem.MapIdDaytime > 0)
            {
                mapIdInDaytime = xdkSession.mapItem.MapIdDaytime;
                mapIdAtNight = xdkSession.mapItem.MapIdNight;
            }
        }
        GUILayout.Space(10);
        GUILayout.BeginHorizontal(GUILayout.Height(60));
        GUILayout.Space(5);
        {
            GUILayout.BeginVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Main MapId:", GUILayout.Width(w1));
            mapIdInDaytime = EditorGUILayout.LongField(mapIdInDaytime, GUILayout.Width(w2 - 180), GUILayout.MaxHeight(18));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Secondary MapId:", GUILayout.Width(w1));
            mapIdAtNight = EditorGUILayout.LongField(mapIdAtNight, GUILayout.Width(w2 - 180), GUILayout.MaxHeight(18));
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Get Map Infomation", GUILayout.Height(45), GUILayout.Width(165)))//
            {
                FindPointCloudObjects();

                EditorUtility.ClearProgressBar();
                
                EditorCoroutineUtility.StartCoroutine(downloadMapInfoDaytime(), this);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


    }

    private void DrawThreeTabs()
    {
        GUILayout.Space(4);
        GUILayout.BeginHorizontal(GUILayout.Height(35));
        for (int i = 0; i < TabTitles.Length; ++i)
        {
            if (i == (int)currentTab)
            {
                m_tabStyle = m_tabBtnOnStyle;
            }
            else
            {
                m_tabStyle = m_tabBtnStyle;
            }
            GUILayout.Space(5);
            if (GUILayout.Button(TabTitles[i], m_tabStyle, GUILayout.Width(tabWidth), GUILayout.Height(tabHeight)))
            {
                currentTab = (ETabs)i;
            }
            
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        w2 = position.width - 40 - w1;
        w3 = position.width - 40;
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width),
                GUILayout.Height(position.height));
        switch (currentTab)
        {
            case ETabs.MapIDs:
                GUI_MapInfo();
                break;
            case ETabs.SceneObjects:
                GUI_SceneSetUp();
                break;
            case ETabs.PointClouds:
                GUI_PointCloud();
                break;
        }
        GUILayout.EndScrollView();
    }


    private void DrawTwoTabs()
    {
        GUILayout.Space(4);
        GUILayout.BeginHorizontal(GUILayout.Height(35));
        for (int i = 0; i < 2; ++i)
        {
            if (i == (int)currentTab)
            {
                m_tabStyle = m_tabBtnOnStyle;
            }
            else
            {
                m_tabStyle = m_tabBtnStyle;
            }
            GUILayout.Space(5);
            if (GUILayout.Button(TabTitles[i], m_tabStyle, GUILayout.Width(tabWidth), GUILayout.Height(tabHeight)))
            {
                currentTab = (ETabs)i;
            }

        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        w2 = position.width - 40 - w1;
        w3 = position.width - 40;
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width),
                GUILayout.Height(position.height));
        switch (currentTab)
        {
            case ETabs.MapIDs:
                GUI_MapInfo();
                break;
            case ETabs.SceneObjects:
                GUI_SceneSetUp();
                break;
        }
        GUILayout.EndScrollView();
    }



    private void GUI_MapInfo()
    {
        GUILayout.Space(10);
        GUILayout.BeginHorizontal(GUILayout.Height(60));
        GUILayout.Space(5);
        {
            GUILayout.BeginVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Main MapId:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapIdInDaytime.ToString(), GUILayout.Width(w2 - 180), GUILayout.MaxHeight(18));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Secondary MapId:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapIdAtNight.ToString(), GUILayout.Width(w2 - 180), GUILayout.MaxHeight(18));
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("- RESET ALL -", GUILayout.Height(45), GUILayout.Width(165)))//
            {
                SetDefault();
                m_PointCloudPose.RemoveAllPointCloud();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();



        if (mapInfo1 != null)
        {
            GUILayout.Space(10);
            GUILayout.Button(" ", GUILayout.Width(position.width - 20), GUILayout.Height(2));
            GUILayout.Label("Main MapId:" + mapIdInDaytime, m_textStyle, GUILayout.Width(position.width - 20), GUILayout.MaxHeight(18));

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Company:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo1.data.companyName, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("CompanyID:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo1.data.companyId, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Programe:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo1.data.programName, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("PlyFileUrl:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo1.data.dense_model_unity_url, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("ScaleFileUrl:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo1.data.scaleUrl, GUILayout.Width(w2));
            GUILayout.EndHorizontal();
        }

        if (mapInfo2 != null)
        {
            GUILayout.Space(10);
            GUILayout.Button(" ", GUILayout.Width(position.width - 20), GUILayout.Height(2));
            GUILayout.Label("Secondary MapID:" + mapIdAtNight, m_textStyle, GUILayout.Width(position.width - 20), GUILayout.MaxHeight(18));

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Company:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo2.data.companyName, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("CompanyID:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo2.data.companyId, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Programe:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo2.data.programName, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("PlyFileUrl:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo2.data.dense_model_unity_url, GUILayout.Width(w2));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("ScaleFileUrl:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(mapInfo2.data.scaleUrl, GUILayout.Width(w2));
            GUILayout.EndHorizontal();
        }
    }



    private void GUI_SceneSetUp()
    {
        GUILayout.Space(10);

        //绘制当前正在编辑的场景
        string sceneName = null;
        GUILayout.Space(10);
        if (m_Scene == null)
        {
            Scene temp = SceneManager.GetActiveScene();
            Debug.Log("temppath=" + temp.path);
            m_Scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(XDKTools.GetRelativePathFromFullPath(temp.path));
        }
        else
        {
            sceneName = m_Scene.name;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        m_Scene = (SceneAsset)EditorGUILayout.ObjectField("Source Scene", m_Scene, typeof(SceneAsset), true, GUILayout.Width(w3));
        GUILayout.EndHorizontal();

        //绘制八叉树显示对象
        GUILayout.Space(10);
        GameObject obj1 = null;
        if (m_PointCloudPose != null)
        {
            obj1 = m_PointCloudPose.gameObject;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        obj1 = (GameObject)EditorGUILayout.ObjectField("PointCloudContianer", obj1, typeof(GameObject), true, GUILayout.Width(w3));
        GUILayout.EndHorizontal();


        // 点云自定义材质
        GUILayout.Space(10);
        if (m_CustomMaterial == null)
        {
            m_CustomMaterial = AssetDatabase.LoadAssetAtPath<Material>(EditorConfigs.Path_PLYMaterial);
        }
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        m_CustomMaterial = (Material)EditorGUILayout.ObjectField("Custom Material", m_CustomMaterial, typeof(Material), true, GUILayout.Width(w3));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        if (GUILayout.Button("Setup Scene", GUILayout.Width(w3), GUILayout.Height(24)))
        {
            checkSceneReady();
        }
        GUILayout.EndHorizontal();
    }




    private void GUI_PointCloud()
    {
        GUILayout.Space(10);

        GUILayout.Space(10);
        GUILayout.Label("Main MapId:" + mapIdInDaytime, m_textStyle, GUILayout.Width(position.width - 20), GUILayout.MaxHeight(18));
        GUILayout.Space(10);


        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        float btnW3 = w3 / 3f;
        if (GUILayout.Button("Show Point Cloud（Octree）", GUILayout.Width(btnW3), GUILayout.Height(24)))
        {
            m_PointCloudPose.UsePotree = true;
            m_PointCloudPose.scaleTxtUrl = mapInfo1.data.scaleUrl;
            m_PointCloudPose.plyFileUrl = mapInfo1.data.dense_model_unity_url;
            m_PointCloudPose.mapID = mapIdInDaytime.ToString();
            m_PointCloudPose.onLoadEnd = onRemoteZipLoaded;
            m_PointCloudPose.onProgress = onRemoteZipProgress;
            m_PointCloudPose.ResetPose();
            m_PointCloudPose.PrepareLoaders();
            this.StartCoroutine(m_PointCloudPose.loadRemoteFile());
        }
        if (GUILayout.Button("Show Point Cloud（Ply）", GUILayout.Width(btnW3), GUILayout.Height(24)))
        {
            m_PointCloudPose.UsePotree = false;
            m_PointCloudPose.scaleTxtUrl = mapInfo1.data.scaleUrl;
            m_PointCloudPose.plyFileUrl = mapInfo1.data.dense_model_unity_url;
            m_PointCloudPose.mapID = mapIdInDaytime.ToString();
            m_PointCloudPose.onLoadEnd = onPlyMeshLoaded;
            m_PointCloudPose.onProgress = onPlyMeshProgress;
            m_PointCloudPose.ResetPose();
            m_PointCloudPose.PrepareLoaders();
            this.StartCoroutine(m_PointCloudPose.loadRemoteFile());
        }
        if (GUILayout.Button("Remove Point Cloud", GUILayout.Width(btnW3), GUILayout.Height(24)))
        {
            m_PointCloudPose.RemoveAllPointCloud();
        }
        GUILayout.EndHorizontal();


        GUILayout.Space(10);
        GUILayout.Label("Scene Mesh: " + mapIdInDaytime, m_textStyle, GUILayout.Width(position.width - 20), GUILayout.MaxHeight(18));
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        if (GUILayout.Button("Show Scene Mesh", GUILayout.Width(btnW3), GUILayout.Height(24)))
        {
            removeSceneMesh();
            EditorApplication.delayCall += () =>
            {
                downloadSceneMesh();
            };
        }
        if (GUILayout.Button("Remove Scene Mesh", GUILayout.Width(btnW3), GUILayout.Height(24)))
        {
            removeSceneMesh();
        }
        if (GUILayout.Button("Clear All Cache", GUILayout.Width(btnW3), GUILayout.Height(24)))
        {
            string desc = m_PointCloudPose.ClearAllCachedFiles();
            EditorUtility.DisplayDialog("成功", "已成功删除 " + desc + "缓存文件", "确定");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (mapIdAtNight > 0 && mapIdAtNight != mapIdInDaytime)
        {
            GUILayout.Space(10);
            GUILayout.Label("Secondary MapID:" + mapIdAtNight, m_textStyle, GUILayout.Width(position.width - 20), GUILayout.MaxHeight(18));
            GUILayout.Space(10);

            if (m_PointCloudNight == null)
            {
                CreatePointCloudNight();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("Show Point Cloud（Octree）", GUILayout.Width(btnW3), GUILayout.Height(24)))
            {
                m_PointCloudNight.UsePotree = true;
                m_PointCloudNight.scaleTxtUrl = mapInfo2.data.scaleUrl;
                m_PointCloudNight.plyFileUrl = mapInfo2.data.dense_model_unity_url;
                m_PointCloudNight.mapID = mapIdAtNight.ToString();
                m_PointCloudNight.onLoadEnd = onRemoteZipLoaded;
                m_PointCloudNight.onProgress = onRemoteZipProgress;
                m_PointCloudNight.ResetPose();
                m_PointCloudNight.PrepareLoaders();
                this.StartCoroutine(m_PointCloudNight.loadRemoteFile());
            }
            if (GUILayout.Button("Show Point Cloud（Ply）", GUILayout.Width(btnW3), GUILayout.Height(24)))
            {
                m_PointCloudNight.UsePotree = false;
                m_PointCloudNight.scaleTxtUrl = mapInfo2.data.scaleUrl;
                m_PointCloudNight.plyFileUrl = mapInfo2.data.dense_model_unity_url;
                m_PointCloudNight.mapID = mapIdAtNight.ToString();
                m_PointCloudNight.onLoadEnd = onPlyMeshLoaded;
                m_PointCloudNight.onProgress = onPlyMeshProgress;
                m_PointCloudNight.ResetPose();
                m_PointCloudNight.PrepareLoaders();
                this.StartCoroutine(m_PointCloudNight.loadRemoteFile());
            }
            if (GUILayout.Button("Remove Point Cloud", GUILayout.Width(btnW3), GUILayout.Height(24)))
            {
                m_PointCloudNight.RemoveAllPointCloud();
            }
            GUILayout.EndHorizontal();

            float tempW1 = position.width - 250;
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(55);
            GUILayout.Label("Offset Position:",  GUILayout.Width(w1));
            EditorGUILayout.LabelField(m_PointCloudNight.transform.localPosition.ToString(), GUILayout.Width(tempW1));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(55);
            GUILayout.Label("Offset EulerAngles:", GUILayout.Width(w1));
            EditorGUILayout.LabelField(m_PointCloudNight.transform.localEulerAngles.ToString(), GUILayout.Width(tempW1));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("Save Offset To current scene", GUILayout.Width(position.width - 33), GUILayout.Height(24)))
            {
                xdkSession.mapItem.OffsetPos = m_PointCloudNight.transform.localPosition;
                xdkSession.mapItem.OffsetEuler = m_PointCloudNight.transform.localEulerAngles;
                ShowNotification(new GUIContent("Set Parameters Complete! Please save scene manually! "));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("Save Scene and Close this window", GUILayout.Width(position.width - 33), GUILayout.Height(24)))
            {
                DestroyPointCloudNight();
                removeSceneMesh();
                SaveActiveScene();
                Close();
            }
            GUILayout.EndHorizontal();
        }


    }



    private void ResetNightMapOffset(string ss)
    {
        if (xdkSession.mapItem.MapIdNight > 0)
        {
            if (m_PointCloudNight)
            {
                m_PointCloudNight.transform.localPosition = xdkSession.mapItem.OffsetPos;
                m_PointCloudNight.transform.localEulerAngles = xdkSession.mapItem.OffsetEuler;
                m_PointCloudNight.transform.localScale = Vector3.one;
                Debug.Log("Reset NightMapOffset successfully!");
            }
            else
            {
                Debug.Log("Reset NightMapOffset failed, object is null");
            }
        }
    }




    /// <summary>
    /// 
    /// </summary>
    private void CreatePointCloudNight()
    {
        var trans = m_PointCloudPose.transform.parent;
        var child = trans.Find("PointCloudPose_1");
        if (child != null)
        {
            m_PointCloudNight = child.GetComponent<PointCloudPose>();
        }
        else
        {
            var obj = GameObject.Instantiate(m_PointCloudPose.gameObject, trans);
            obj.name = "PointCloudPose_1";
            obj.transform.SetAsFirstSibling();
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            m_PointCloudNight = obj.GetComponent<PointCloudPose>();
        }

        var renders = m_PointCloudNight.GetComponentsInChildren<Renderer>(true);
        //Debug.Log("SetMaterial  renders.Length=" + renders.Length);
        var colorGray = new Color(1f, 0.4f, 0.4f, 1);
        for (int i = 0; i < renders.Length; i++)
        {
            var mat = new Material(m_CustomMaterial);
            mat.SetColor("_ExtraColor", colorGray);
            renders[i].material = mat;
            //Debug.Log("SetMaterial [" + i + "] obj.name=" + renders[i].name);
        }

        m_PointCloudNight.transform.localPosition = xdkSession.mapItem.OffsetPos;
        m_PointCloudNight.transform.localEulerAngles = xdkSession.mapItem.OffsetEuler;
        m_PointCloudNight.transform.localScale = Vector3.one;
    }


    private void DestroyPointCloudNight()
    {
        var obj = GameObject.Find("PointCloudPose_1");
        if (obj)
        {
            DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// 检测场景中的设置
    /// </summary>
    private void checkSceneReady()
    {
        if (m_PointCloudPose == null)
        {
            ShowNotification(new GUIContent("Error: No PointCloudPose in scene"));
            return;
        }
        if (m_CustomMaterial == null)
        {
            ShowNotification(new GUIContent("Error: No material is setted?!"));
            return;
        }
        ShowNotification(new GUIContent("Setup Scene Complete！"));
        IsSceneReady = true;
    }



    private void onRemoteZipProgress(float val)
    {
        EditorUtility.DisplayProgressBar("Loading point cloud", "正在加载八叉树点云...", val);
    }

    private void onPlyMeshProgress(float val)
    {
        EditorUtility.DisplayProgressBar("Loading point cloud", "正在加载点云ply文件...", val);
    }

    private void onSceneMeshProgress(float val)
    {
        EditorUtility.DisplayProgressBar("Loading scene mesh", "正在加载场景mesh文件...", val);
    }

    private void onSceneJpgLoadProgress(float val)
    {
        EditorUtility.DisplayProgressBar("Loading scene texture", "正在加载场景模型的贴图文件...", val);
    }

    private void onSceneMeshCreateProgress(float val)
    {
        EditorUtility.DisplayProgressBar("Creating scene mesh", "正在将场景模型加载到场景中...", val);
    }

    private void onRemoteZipLoaded(string msg)
    {
        EditorUtility.ClearProgressBar();
        if (string.IsNullOrEmpty(msg))
        {
            ShowNotification(new GUIContent("加载远程的点云tar.gz包成功"));
            ResetNightMapOffset(msg);
        }
        else
        {
            Debug.Log("加载点云zip包失败：\n" + msg);
            ShowNotification(new GUIContent("加载点云tar.gz包失败"), 3.0d);
        }
    }



    private void downloadSceneMesh()
    {
        if(mapInfo1 == null)
        {
            EditorUtility.DisplayDialog("错误", "mapInfo1 字段是空的", "确定");
            return;
        }
        var plyFileUrl = mapInfo1.data.dense_model_unity_url;
        if (string.IsNullOrEmpty(plyFileUrl))
        {
            EditorUtility.DisplayDialog("错误", "plyFileUrl 字段是空的", "确定");
            return;
        }
        int ind = plyFileUrl.LastIndexOf("/");
        string modelUrl = plyFileUrl.Substring(0, ind) + "/model/model.obj";
        string filePath = getModelFilePath();

        if (File.Exists(filePath))
        {
            onSceneMeshLoaded(0);
        }
        else
        {
            onSceneMeshProgress(0);
            this.StartCoroutine(ToolHttp.loadRemoteFile(modelUrl, filePath, onSceneMeshLoaded, onSceneMeshProgress));
        }
    }


    private void onSceneMeshLoaded(int code)
    {
        EditorUtility.ClearProgressBar();
        Debug.Log("onSceneMeshLoaded code=" + code);
        if (code != 0)
        {
            EditorUtility.DisplayDialog("错误", "加载场景的 mesh 对象失败", "确定");
            return;
        }
        if (mapInfo1 == null)
        {
            EditorUtility.DisplayDialog("错误", "mapInfo1 字段是空的", "确定");
            return;
        }
        var plyFileUrl = mapInfo1.data.dense_model_unity_url;
        int ind = plyFileUrl.LastIndexOf("/");
        string mtlUrl = plyFileUrl.Substring(0, ind) + "/model/model.mtl";
        string localMtlFile = getMTLFilePath();
        if (File.Exists(localMtlFile))
        {
            onMtlFileLoaded(0);
        }
        else
        {
            this.StartCoroutine(ToolHttp.loadRemoteFile(mtlUrl, localMtlFile, onMtlFileLoaded));
        }

    }


    private void onMtlFileLoaded(int code)
    {
        EditorUtility.ClearProgressBar();
        Debug.Log("onMtlFileLoaded code=" + code);
        if (code != 0)
        {
            EditorUtility.DisplayDialog("错误", "加载场景的 mtl 对象失败", "确定");
            return;
        }
        string localMtlFile = getMTLFilePath();
        string jpgFileName = getJpgFileName(localMtlFile);
        if (string.IsNullOrEmpty(jpgFileName))
        {
            EditorUtility.DisplayDialog("错误", "mtl文件解析失败，请删除缓存文件后重试。", "确定");
            return;
        }
        if (mapInfo1 == null)
        {
            EditorUtility.DisplayDialog("错误", "mapInfo1 字段是空的", "确定");
            return;
        }
        var plyFileUrl = mapInfo1.data.dense_model_unity_url;
        int ind = plyFileUrl.LastIndexOf("/");
        string jpgUrl = plyFileUrl.Substring(0, ind) + "/model/" + jpgFileName;

        string folder2 = getMapFolder();
        string localJpgFile = Path.Combine(folder2, jpgFileName);
        if (File.Exists(localJpgFile))
        {
            onJpgFileLoaded(0);
        }
        else
        {
            this.StartCoroutine(ToolHttp.loadRemoteFile(jpgUrl, localJpgFile, onJpgFileLoaded, onSceneJpgLoadProgress));
        }
    }


    private void onJpgFileLoaded(int code)
    {
        EditorUtility.ClearProgressBar();
        Debug.Log("onMtlFileLoaded code=" + code);
        if (code != 0)
        {
            EditorUtility.DisplayDialog("错误", "加载场景的 mesh 的贴图文件失败", "确定");
            return;
        }
        SceneRoot RootObj = ObjectTool.FindAnyObjectByType<SceneRoot>(true);
        GameObject SceneMesh = GameObject.Find("SceneMesh");
        if (SceneMesh != null)
        {
            DestroyImmediate(SceneMesh);
        }
        SceneMesh = new GameObject("SceneMesh");
        SceneMesh.transform.SetParent(m_PointCloudPose.transform.parent);
        SceneMesh.transform.localPosition = Vector3.zero;
        SceneMesh.transform.localEulerAngles = Vector3.zero;
        SceneMesh.transform.localScale = Vector3.one;
        string filePath = getModelFilePath();
        string localMtlFile = getMTLFilePath();
        var aLoader = new OBJLoader();
        this.StartCoroutine(aLoader.Load(filePath, localMtlFile, (obj) =>
        {
            EditorUtility.ClearProgressBar();
            if (obj)
            {
                obj.transform.SetParent(SceneMesh.transform);
            }
        }, onSceneMeshCreateProgress));

    }





    private string getModelFilePath()
    {
        string folder2 = getMapFolder();
        string filePath = Path.Combine(folder2, "model" + mapIdInDaytime + ".obj");
        return filePath;
    }



    private string getMTLFilePath()
    {
        string folder2 = getMapFolder();
        string filePath = Path.Combine(folder2, "model" + mapIdInDaytime + ".mtl");
        return filePath;
    }



    private string getJpgFileName(string mtlFilePath)
    {
        string content = File.ReadAllText(mtlFilePath);
        int ind1 = content.IndexOf("map_Kd ");
        if (ind1 > 0)
        {
            int ind2 = content.IndexOf("\n", ind1);
            int start = ind1 + 7;
            if (ind2 > 0)
            {
                return content.Substring(start, ind2 - start);
            }
            else
            {
                return content.Substring(start);
            }
        }
        return null;
    }

    private string getMapFolder()
    {
        string folder = Path.Combine(Application.persistentDataPath, "ply");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string folder2 = Path.Combine(folder, mapIdInDaytime.ToString());
        if (!Directory.Exists(folder2))
        {
            Directory.CreateDirectory(folder2);
        }
        return folder2;
    }

    private void removeSceneMesh()
    {
        GameObject meshObj = GameObject.Find("SceneMesh");
        if(meshObj == null && m_PointCloudPose != null)
        {
            for (int i = 0; i < m_PointCloudPose.transform.childCount; i++)
            {
                var child = m_PointCloudPose.transform.GetChild(i);
                if (child.name == "SceneMesh")
                {
                    meshObj = child.gameObject;
                    break;
                }
            }
        }
        if (meshObj != null)
        {
            DestroyImmediate(meshObj);
        }
        else
        {
            Debug.Log("Can not find SceneMesh");
        }
    }

    private void onPlyMeshLoaded(string msg)
    {
        EditorUtility.ClearProgressBar();
        if (string.IsNullOrEmpty(msg))
        {
            ShowNotification(new GUIContent("加载点云ply成功"));
            ResetNightMapOffset(msg);
        }
        else
        {
            ShowNotification(new GUIContent("加载点云ply失败：" + msg), 3.0d);
        }
    }

    private bool IfMapInfoValid()
    {
        return mapInfo1 != null && mapInfo1.code == 200 && mapInfo1.data != null;
    }



    /// <summary>
    /// 请求地图数据信息
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    IEnumerator downloadMapInfoDaytime()
    {
        mapInfo1 = null;
        string url = EditorConfigs.ConfigServer;
        if (!url.EndsWith("/"))
        {
            url += "/";
        }
        url += EditorConfigs.GetMapInfoURL + mapIdInDaytime;
        WWWForm form = new WWWForm();
        Debug.Log("downloadMapInfo1 url=" + url);
        UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string mes = webRequest.downloadHandler.text;
            try
            {
                mapInfo1 = JsonUtility.FromJson<XDKMapInfo>(mes);
                Debug.Log(mes);
            }
            catch (Exception e)
            {
                mapInfo1 = null;
                Debug.LogFormat("Map Infomation Anylizition Error, MapInfor: {0},  error: {1}", mes, e);
                ShowNotification(new GUIContent("获取地图信息失败，mapID=" + mapIdInDaytime));
            }
        }
        else
        {
            Debug.LogError(webRequest.error);
        }
        if (mapInfo1 == null)
        {
            SetDefault();
        }
        else
        {
            if (mapInfo1.code != 200)
            {
                Debug.LogErrorFormat("Map Infomation Error, code:{0}, msg:{1}", mapInfo1.code, mapInfo1.msg);
            }
            else
            {
                if (mapIdAtNight > 0 && mapIdAtNight != mapIdInDaytime)
                {
                    yield return downloadMapInfoNight();
                }
                else
                {
                    Repaint();
                }
            }
        }
    }



    /// <summary>
    /// 请求地图数据信息
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    IEnumerator downloadMapInfoNight()
    {
        mapInfo2 = null;

        string url = EditorConfigs.ConfigServer;
        if (!url.EndsWith("/"))
        {
            url += "/";
        }
        url += EditorConfigs.GetMapInfoURL + mapIdAtNight;
        Debug.Log("downloadMapInfoNight url=" + url);

        UnityWebRequest webRequest = UnityWebRequest.Post(url, new WWWForm());
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string mes = webRequest.downloadHandler.text;
            try
            {
                mapInfo2 = JsonUtility.FromJson<XDKMapInfo>(mes);
                Debug.Log(mes);
            }
            catch (Exception e)
            {
                mapInfo2 = null;
                Debug.LogFormat("Map Infomation Anylizition Error, MapInfor: {0},  error: {1}", mes, e);
                ShowNotification(new GUIContent("获取地图信息失败，mapID=" + mapIdAtNight));
            }
        }
        else
        {
            Debug.LogError(webRequest.error);
        }
        if (mapInfo2 == null)
        {
            SetDefault();
        }
        else
        {
            if (mapInfo2.code != 200)
            {
                Debug.LogErrorFormat("Map Infomation Error, code:{0}, msg:{1}", mapInfo2.code, mapInfo2.msg);
            }
            else
            {
                Repaint();
            }
        }
    }



    private void Update()
    {
        if (m_PointCloudPose != null) // 八叉树方式需要不断刷新 PointRenderer 对象
        {
            m_PointCloudPose.UpdateByEditor();
        }
    }

}