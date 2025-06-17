using System;
using System.Collections;
using System.Collections.Generic;
using Seengene.XDK;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(XDKCloudSession))]
public class XDKCloudSessionEditor : Editor
{
    private GUIStyle m_DefaultLabel;
    private GUIStyle m_Label14;
    private GUIStyle m_Label12;
    private bool CreateStyleDone;

    //private string[] NotDraw = new string[] { "configServer",  };
    private string[] NotDraw = new string[] {  };
    [NonSerialized] private readonly GUIContent m_MapIDLabel = EditorGUIUtility.TrTextContent("Map ID");


    private GUIContent LabelRelocationServer = new GUIContent("定位服务器地址");
    private GUIContent LabelVideoQuility = new GUIContent("视频流质量");
    private GUIContent LabelStartOnMono = new GUIContent("启动立即开始定位");
    private GUIContent LabelSetFrame60 = new GUIContent("设置目标帧数为60");
    private GUIContent LabelEvtStartWork = new GUIContent("开始定位时调用");
    private GUIContent LabelEvtFirstSucc = new GUIContent("首次定位成功调用");
    private GUIContent LabelEvtUseResult = new GUIContent("每次应用定位结果调用");
    private GUIContent LabelEvtRelocateTimeout = new GUIContent("定位超时调用");
    private GUIContent LabelEvtMapInfoFailed = new GUIContent("获取地图信息失败调用");
    private GUIContent LabelRelocateStrategy = new GUIContent("定位策略");


    [NonSerialized] private SerializedProperty m_videoStreamingQuality;
    [NonSerialized] private SerializedProperty m_relocateServer;
    [NonSerialized] private SerializedProperty m_AutoStartWork;
    [NonSerialized] private SerializedProperty m_SetFrame60;
    [NonSerialized] private SerializedProperty m_RelocateStrategy;
    [NonSerialized] private SerializedProperty m_MapItem;
    [NonSerialized] private SerializedProperty m_OnStartWork;
    [NonSerialized] private SerializedProperty OnFirstLocateSuccessEvent;
    [NonSerialized] private SerializedProperty OnScenePoseUsed;
    [NonSerialized] private SerializedProperty OnMapInfoFail;
    [NonSerialized] private SerializedProperty OnRelocateTimeout;

    private void OnEnable()
    {
        m_videoStreamingQuality = serializedObject.FindProperty(nameof(XDKCloudSession.videoStreamingQuality));
        m_relocateServer = serializedObject.FindProperty(nameof(XDKCloudSession.relocateServer));
        m_AutoStartWork = serializedObject.FindProperty(nameof(XDKCloudSession.StartOnMonoStart));
        m_SetFrame60 = serializedObject.FindProperty(nameof(XDKCloudSession.SetFrame60));
        m_RelocateStrategy = serializedObject.FindProperty(nameof(XDKCloudSession.relocateStrategy));
        m_MapItem = serializedObject.FindProperty(nameof(XDKCloudSession.mapItem));
        m_OnStartWork = serializedObject.FindProperty(nameof(XDKCloudSession.EvtStartWork));
        OnFirstLocateSuccessEvent = serializedObject.FindProperty(nameof(XDKCloudSession.EvtFirstLocateSuccess));
        OnScenePoseUsed = serializedObject.FindProperty(nameof(XDKCloudSession.EvtScenePoseUsed));
        OnMapInfoFail = serializedObject.FindProperty(nameof(XDKCloudSession.EvtMapInfoFail));
        OnRelocateTimeout = serializedObject.FindProperty(nameof(XDKCloudSession.EvtRelocateTimeout));

    }


    private void InitGUIStyle()
    {
        if (CreateStyleDone)
        {
            return;
        }
        m_DefaultLabel = new GUIStyle(GUI.skin.label);

        m_Label14 = new GUIStyle(m_DefaultLabel);
        m_Label14.alignment = TextAnchor.MiddleLeft;
        m_Label14.fontStyle = m_DefaultLabel.fontStyle;
        m_Label14.fontSize = 14;
        m_Label14.normal.textColor = new Color(0.89f, 0.47f, 0.10f, 1.00f);


        m_Label12 = new GUIStyle(m_DefaultLabel);
        m_Label12.alignment = TextAnchor.MiddleLeft;
        m_Label12.fontStyle = m_DefaultLabel.fontStyle;
        m_Label12.fontSize = 12;
        m_Label12.normal.textColor = Color.white;

        CreateStyleDone = true;
    }


    public override void OnInspectorGUI()
    {
        InitGUIStyle();

        var labelFontSize = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = 12;

        DrawAboutXDK();
        DrawMapItem();
        DrawRelocateStrategy();
        DrawEvents();

        GUI.skin.label.fontSize = labelFontSize;
    }



    private void DrawAboutXDK()
    {
        XDKCloudSession xdkScript = (XDKCloudSession)target;
        EditorGUILayout.LabelField("基本信息 ", m_Label14); 

        ++EditorGUI.indentLevel;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("XDK版本号");
        EditorGUILayout.LabelField(XDKCloudSession.XDKVersion);
        EditorGUILayout.EndHorizontal();

        //EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.PrefixLabel("Secret Key");
        //EditorGUILayout.LabelField(xdkScript.secretKey);
        //EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("配置服务器地址");
        EditorGUILayout.LabelField(xdkScript.configServer);
        EditorGUILayout.EndHorizontal();



        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_relocateServer, LabelRelocationServer);
        EditorGUILayout.PropertyField(m_videoStreamingQuality, LabelVideoQuility);
        EditorGUILayout.PropertyField(m_AutoStartWork, LabelStartOnMono);
        EditorGUILayout.PropertyField(m_SetFrame60, LabelSetFrame60);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }


        --EditorGUI.indentLevel;
    }



    private int hhDayStart;
    private int mmDayStart;
    private int hhNightStart;
    private int mmNightStart;

    private void DrawMapItem()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("地图配置", m_Label14);

        ++EditorGUI.indentLevel;

        var MapIdDaytime = m_MapItem.FindPropertyRelative("MapIdDaytime").longValue;
        var UseTwoMaps = m_MapItem.FindPropertyRelative("UseTwoMaps").boolValue;

        var MapIdNight = m_MapItem.FindPropertyRelative("MapIdNight").longValue;
        var OffsetPos = m_MapItem.FindPropertyRelative("OffsetPos").vector3Value;
        var OffsetEuler = m_MapItem.FindPropertyRelative("OffsetEuler").vector3Value;

        var UseAutoDaytime = m_MapItem.FindPropertyRelative("UseAutoDaytime").boolValue;
        var DaytimeStart = m_MapItem.FindPropertyRelative("DaytimeStart").intValue;
        var NightStart = m_MapItem.FindPropertyRelative("NightStart").intValue;

        
        if (UseTwoMaps)
        {
            EditorGUI.BeginChangeCheck();
            MapIdDaytime = EditorGUILayout.LongField("主地图ID", MapIdDaytime);
            UseTwoMaps = EditorGUILayout.Toggle("使用双地图", UseTwoMaps);
            MapIdNight = EditorGUILayout.LongField("夜间地图ID", MapIdNight);
            OffsetPos = EditorGUILayout.Vector3Field("夜间地图偏移", OffsetPos);
            OffsetEuler = EditorGUILayout.Vector3Field("夜间地图朝向", OffsetEuler);
            UseAutoDaytime = EditorGUILayout.Toggle("自动判定白昼时间", UseAutoDaytime);
            if (!UseAutoDaytime)
            {
                hhDayStart = DaytimeStart / 100;
                mmDayStart = DaytimeStart % 100;
                hhNightStart = NightStart / 100;
                mmNightStart = NightStart % 100;
                hhDayStart = EditorGUILayout.IntSlider("白昼开始时间-小时", hhDayStart, 4, 12);
                mmDayStart = EditorGUILayout.IntSlider("白昼开始时间-分钟", mmDayStart, 0, 59);
                hhNightStart = EditorGUILayout.IntSlider("夜晚开始时间-小时", hhNightStart, 16, 20);
                mmNightStart = EditorGUILayout.IntSlider("夜晚开始时间-分钟", mmNightStart, 0, 59);
                DaytimeStart = hhDayStart * 100 + mmDayStart;
                NightStart = hhNightStart * 100 + mmNightStart;
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_MapItem.FindPropertyRelative("MapIdDaytime").longValue = MapIdDaytime;
                m_MapItem.FindPropertyRelative("UseTwoMaps").boolValue = UseTwoMaps;
                m_MapItem.FindPropertyRelative("MapIdNight").longValue = MapIdNight;
                m_MapItem.FindPropertyRelative("OffsetPos").vector3Value = checkEulurValue(OffsetPos);
                m_MapItem.FindPropertyRelative("OffsetEuler").vector3Value = checkEulurValue(OffsetEuler);

                m_MapItem.FindPropertyRelative("UseAutoDaytime").boolValue = UseAutoDaytime;
                m_MapItem.FindPropertyRelative("DaytimeStart").intValue = DaytimeStart;
                m_MapItem.FindPropertyRelative("NightStart").intValue = NightStart;
                serializedObject.ApplyModifiedProperties();
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            MapIdDaytime = EditorGUILayout.LongField("地图ID", MapIdDaytime);
            UseTwoMaps = EditorGUILayout.Toggle("使用双地图", UseTwoMaps);
            if (EditorGUI.EndChangeCheck())
            {
                m_MapItem.FindPropertyRelative("MapIdDaytime").longValue = MapIdDaytime;
                m_MapItem.FindPropertyRelative("UseTwoMaps").boolValue = UseTwoMaps;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        --EditorGUI.indentLevel;
    }





    private Vector3 checkEulurValue(Vector3 euler)
    {
        while(euler.x > 360)
        {
            euler.x -= 360;
        }
        while (euler.y > 360)
        {
            euler.y -= 360;
        }
        while (euler.z > 360)
        {
            euler.z -= 360;
        }
        while (euler.x < 0)
        {
            euler.x += 360;
        }
        while (euler.y < 0)
        {
            euler.y += 360;
        }
        while (euler.z < 0)
        {
            euler.z += 360;
        }
        return euler;
    }





    private void DrawRelocateStrategy()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("定位策略", m_Label14);

        ++EditorGUI.indentLevel;


        EditorGUILayout.LabelField("新一轮定位的触发条件", m_Label12);
        ++EditorGUI.indentLevel;
        EditorGUI.BeginChangeCheck();
        var RelocateDistance = m_RelocateStrategy.FindPropertyRelative("RelocateDistance").floatValue;
        var RelocatePeriod = m_RelocateStrategy.FindPropertyRelative("RelocatePeriod").floatValue;
        var CameraMoveThreshold = m_RelocateStrategy.FindPropertyRelative("CameraMoveThreshold").floatValue;
        RelocateDistance = EditorGUILayout.Slider("累计距离", RelocateDistance, 2, 60);
        RelocatePeriod = EditorGUILayout.Slider("累计时间", RelocatePeriod, 0, 99);
        CameraMoveThreshold = EditorGUILayout.Slider("相机位置跳变阈值", CameraMoveThreshold, 0.3f, 10);
        if (EditorGUI.EndChangeCheck())
        {
            m_RelocateStrategy.FindPropertyRelative("RelocateDistance").floatValue = RelocateDistance;
            m_RelocateStrategy.FindPropertyRelative("RelocatePeriod").floatValue = RelocatePeriod;
            m_RelocateStrategy.FindPropertyRelative("CameraMoveThreshold").floatValue = CameraMoveThreshold;
            serializedObject.ApplyModifiedProperties();
        }
        --EditorGUI.indentLevel;


        EditorGUILayout.LabelField("暂时阻止定位的条件", m_Label12);
        ++EditorGUI.indentLevel;
        EditorGUI.BeginChangeCheck();
        var AngularSpeedLimit = m_RelocateStrategy.FindPropertyRelative("AngularSpeedLimit").floatValue;
        AngularSpeedLimit = EditorGUILayout.Slider("角速度阈值", AngularSpeedLimit, 1, 180);
        if (EditorGUI.EndChangeCheck())
        {
            m_RelocateStrategy.FindPropertyRelative("AngularSpeedLimit").floatValue = AngularSpeedLimit;
            serializedObject.ApplyModifiedProperties();
        }
        --EditorGUI.indentLevel;


        EditorGUILayout.LabelField("一轮定位的参数", m_Label12);
        ++EditorGUI.indentLevel;
        EditorGUI.BeginChangeCheck();
        var RelocateCountInRound = m_RelocateStrategy.FindPropertyRelative("RelocateCountInRound").intValue;
        var FrameIntervalInRound = m_RelocateStrategy.FindPropertyRelative("FrameIntervalInRound").floatValue;
        var RoundTimeLimit = m_RelocateStrategy.FindPropertyRelative("RoundTimeLimit").intValue;
        RelocateCountInRound = EditorGUILayout.IntSlider("每轮定位次数", RelocateCountInRound, 1, 10);
        FrameIntervalInRound = EditorGUILayout.Slider("定位时间间隔", FrameIntervalInRound, 0.5f, 50);
        RoundTimeLimit = EditorGUILayout.IntSlider("每轮定位最大时间", RoundTimeLimit, 5, 80);
        if (EditorGUI.EndChangeCheck())
        {
            m_RelocateStrategy.FindPropertyRelative("RelocateCountInRound").intValue = RelocateCountInRound;
            m_RelocateStrategy.FindPropertyRelative("FrameIntervalInRound").floatValue = FrameIntervalInRound;
            m_RelocateStrategy.FindPropertyRelative("RoundTimeLimit").intValue = RoundTimeLimit;
            serializedObject.ApplyModifiedProperties();
        }
        --EditorGUI.indentLevel;


        EditorGUILayout.LabelField("定位结果应用的参数", m_Label12);
        ++EditorGUI.indentLevel;
        EditorGUI.BeginChangeCheck();
        var ResultCountUsing = m_RelocateStrategy.FindPropertyRelative("ResultCountUsing").intValue;
        var ResultDropRatio = m_RelocateStrategy.FindPropertyRelative("ResultDropRatio").floatValue;
        ResultCountUsing = EditorGUILayout.IntSlider("定位结果采用数量", ResultCountUsing, 1, 20);
        ResultDropRatio = EditorGUILayout.Slider("定位结果丢弃比例", ResultDropRatio, 0.0f, 0.5f);
        if (EditorGUI.EndChangeCheck())
        {
            m_RelocateStrategy.FindPropertyRelative("ResultCountUsing").intValue = ResultCountUsing;
            m_RelocateStrategy.FindPropertyRelative("ResultDropRatio").floatValue = ResultDropRatio;
            serializedObject.ApplyModifiedProperties();
        }
        --EditorGUI.indentLevel;


        EditorGUILayout.LabelField("场景根节点平滑方式", m_Label12);
        ++EditorGUI.indentLevel;
        EditorGUI.BeginChangeCheck();
        var SmoothMoveType = m_RelocateStrategy.FindPropertyRelative("SmoothMoveType").enumValueFlag;
        var SmoothMoveTime = m_RelocateStrategy.FindPropertyRelative("SmoothMoveTime").floatValue;
        var cc = EditorGUILayout.EnumPopup("平滑方式", (SmoothMoveType)SmoothMoveType);
        SmoothMoveTime = EditorGUILayout.Slider("平滑时间", SmoothMoveTime, 0.0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            m_RelocateStrategy.FindPropertyRelative("SmoothMoveType").enumValueIndex = (int)(SmoothMoveType)cc;
            m_RelocateStrategy.FindPropertyRelative("SmoothMoveTime").floatValue = SmoothMoveTime;
            serializedObject.ApplyModifiedProperties();
        }
        --EditorGUI.indentLevel;


        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("将定位策略重置为默认参数", GUILayout.MaxWidth(200), GUILayout.MinHeight(24)))
        {
            OnStrategyChangedToDefault();
            serializedObject.ApplyModifiedProperties();
        }

        GUILayout.EndHorizontal();
        --EditorGUI.indentLevel;
    }


    private void OnStrategyChangedToDefault()
    {
        m_RelocateStrategy.FindPropertyRelative("RelocateDistance").floatValue = 10;
        m_RelocateStrategy.FindPropertyRelative("RelocatePeriod").floatValue = 15;
        m_RelocateStrategy.FindPropertyRelative("CameraMoveThreshold").floatValue = 1;

        m_RelocateStrategy.FindPropertyRelative("AngularSpeedLimit").floatValue = 40;

        m_RelocateStrategy.FindPropertyRelative("RelocateCountInRound").intValue = 3;
        m_RelocateStrategy.FindPropertyRelative("FrameIntervalInRound").floatValue = 1;
        m_RelocateStrategy.FindPropertyRelative("RoundTimeLimit").intValue = 15;

        m_RelocateStrategy.FindPropertyRelative("ResultCountUsing").intValue = 5;
        m_RelocateStrategy.FindPropertyRelative("ResultDropRatio").floatValue = 0.41f;

        m_RelocateStrategy.FindPropertyRelative("SmoothMoveType").enumValueIndex = (int)SmoothMoveType.Linear;
        m_RelocateStrategy.FindPropertyRelative("SmoothMoveTime").floatValue = 1f;
    }


    private void DrawEvents()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("事件定义", m_Label14);

        ++EditorGUI.indentLevel;


        EditorGUI.BeginChangeCheck();   //////////
        EditorGUILayout.PropertyField(m_OnStartWork, LabelEvtStartWork);
        EditorGUILayout.PropertyField(OnFirstLocateSuccessEvent, LabelEvtFirstSucc);
        EditorGUILayout.PropertyField(OnMapInfoFail, LabelEvtMapInfoFailed);
        EditorGUILayout.PropertyField(OnRelocateTimeout, LabelEvtRelocateTimeout);
        EditorGUILayout.PropertyField(OnScenePoseUsed, LabelEvtUseResult);
        if (EditorGUI.EndChangeCheck()) //////////
        {
            serializedObject.ApplyModifiedProperties();
        }

        --EditorGUI.indentLevel;
    }



}
