using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class PBXProjectConfig : MonoBehaviour
{
    [PostProcessBuildAttribute(0)]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
#if UNITY_IOS
        // BuildTarget需为iOS
        if (buildTarget != BuildTarget.iOS) return;
        // 初始化
        string plistPath = Path.Combine(pathToBuiltProject, "info.plist");
        PlistDocument plist = new PlistDocument();
        Debug.Log("plistPath :" + plistPath);
        plist.ReadFromFile(plistPath);
        PlistElementDict _rootDic = plist.root;
        _rootDic.SetString(Utf8string("NSLocationAlwaysAndWhenInUseUsageDescription"), "我们需要您的位置信息，来展示附近的活动地点");
        _rootDic.SetString(Utf8string("NSLocationWhenInUseUsageDescription"), "我们需要您的位置信息，来展示附近的活动地点");
        string[] LSApplicationQueriesSchemesValueList = { "weixinULAPI", "weixin" };
        PlistElementArray LSApplicationQueriesSchemesArray = plist.root.CreateArray("LSApplicationQueriesSchemes");
        for (int i = 0; i < LSApplicationQueriesSchemesValueList.Length; i++)
        {
            LSApplicationQueriesSchemesArray.AddString(LSApplicationQueriesSchemesValueList[i]);
        }

        //PlistElementArray urlTypes = _rootDic.CreateArray("CFBundleURLTypes");
        //string[] urlSchemes = { "wx6bfce2e305642d83" };
        //foreach (string str in urlSchemes)
        //{
        //    PlistElementDict typeRole = urlTypes.AddDict();
        //    typeRole.SetString("CFBundleTypeRole", "Editor");
        //    PlistElementArray urlScheme = typeRole.CreateArray("CFBundleURLSchemes");
        //    urlScheme.AddString(str);
        //}
        plist.WriteToFile(plistPath);

        PBXProject pbxProject = new PBXProject();
        string projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

        // 读取当前配置
        pbxProject.ReadFromFile(projectPath);
        //pbxProject.
        string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();
        //ENABLE_BITCODE=False
        pbxProject.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "false");
        // 添加framework
        pbxProject.AddFrameworkToProject(targetGuid, "AssetsLibrary.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "MobileCoreServices.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "libz.tbd", false);
        pbxProject.AddFrameworkToProject(targetGuid, "CoreTelephony.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "WebKit.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "libsqlite3.tbd", false);
        pbxProject.AddFrameworkToProject(targetGuid, "SceneKit.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "Accelerate.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "ImageIO.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "Photos.framework", false);
        pbxProject.AddFrameworkToProject(targetGuid, "PhotosUI.framework", false);
        pbxProject.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-Objc");
        /// 注释掉了一些SDK用不到的依赖项
        //pbxProject.AddFrameworkToProject(targetGuid, "AuthenticationServices.framework", false);
        //pbxProject.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-force_load $(SRCROOT)/Libraries/Plugins/Seengene/OpenSDK1.8.7.1_NoPay/libWeChatSDK.a");

        pbxProject.WriteToFile(projectPath);

        // 删除无用文件
        string rawDataPath = pathToBuiltProject + "/Data/Raw/DB/Demo";
        string scaleFilePath = rawDataPath + "/scale.txt";
        string mapInfoFilePath = rawDataPath + "/MapInfo.json";
        if (File.Exists(scaleFilePath)) File.Delete(scaleFilePath);
        if (File.Exists(mapInfoFilePath)) File.Delete(mapInfoFilePath);
#endif
    }

    static string Utf8string(string s)
    {
        UTF8Encoding.UTF8.GetString(UTF8Encoding.UTF8.GetBytes(s));
        return s;
    }

}
