
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;

public class FindReferences
{

    [MenuItem("Assets/Find References", false, 10)]
    static private void Find()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        if (Selection.activeObject == null)
        {
            Debug.Log("Nothing is selected.");
            return;
        }
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Can not get the path of the selected Object");
            return;
        }

        EditorGUI.hyperLinkClicked -= EditorGUI_hyperLinkClicked;
        EditorGUI.hyperLinkClicked += EditorGUI_hyperLinkClicked;

        string extension = Path.GetExtension(path).ToLower();
        if (string.Equals(extension, ".shader") || string.Equals(extension, ".shadergraph"))
        {
            FindMaterialWithShader(path);
        }
        else if (string.Equals(extension, ".cs"))
        {
            FindOthers(path, true);
        }
        else
        {
            FindOthers(path, false);
        }

    }

    /// <summary>
    /// Find materials which is using the shader
    /// </summary>
    /// <param name="shaderPath"></param>
    static private void FindMaterialWithShader(string shaderPath)
    {
        string shaderName = GetShaderName(shaderPath);
        if (shaderName == null)
        {
            Debug.Log($"No shaderName, path={shaderPath}");
            return;
        }
        Debug.Log($"--> start searching: shaderName={shaderName}, path={shaderPath}");

        EditorSettings.serializationMode = SerializationMode.ForceText;
        List<string> withExtensions = new List<string>() { ".mat" };
        List<string> allFiles = new List<string>();
        GetFilesWithExtension(withExtensions, allFiles);
        if (allFiles.Count == 0)
        {
            Debug.Log("No file is found with the suffix.");
            return;
        }

        int startIndex = 0;
        List<string> searchResult = new();
        EditorApplication.update = delegate ()
        {
            string file = allFiles[startIndex];
            bool isCancel = EditorUtility.DisplayCancelableProgressBar("Searching References", file, (float)startIndex / (float)allFiles.Count);
            try
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(file);
                if (mat != null && mat.shader != null)
                {
                    if (mat.shader.name == shaderName)
                    {
                        searchResult.Add(file);
                    }
                }
            }
            catch (System.Exception ee)
            {
                Debug.LogError($"--> error {searchResult.Count}, path = {file}");
            }


            startIndex++;
            if (isCancel || startIndex >= allFiles.Count)
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                WriteSearchResult(searchResult);
            }
        };
    }



    /// <summary>
    /// find refrence objects which is using the asset
    /// </summary>
    /// <param name="assetPath"></param>
    static private void FindOthers(string assetPath, bool isScript)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        List<string> allFiles = new List<string>();
        List<string> withExtensions = new List<string>() { ".prefab", ".unity", ".mat", ".asset" };
        if (isScript)
        {
            withExtensions = new List<string>() { ".prefab", ".unity", ".asset" };
        }

        GetFilesWithExtension(withExtensions, allFiles);
        if (allFiles.Count == 0)
        {
            Debug.Log("No file is found with the suffix.");
            return;
        }
        Debug.Log($"--> start searching: path={assetPath}");

        int startIndex = 0;
        List<string> searchResult = new();
        EditorApplication.update = delegate ()
        {
            string file = allFiles[startIndex];
            bool isCancel = EditorUtility.DisplayCancelableProgressBar("Searching References", file, (float)startIndex / (float)allFiles.Count);
            try
            {
                string fileContent = File.ReadAllText(file);
                if (Regex.IsMatch(fileContent, guid))
                {
                    searchResult.Add(file);
                    //Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                }
            }
            catch (System.Exception ee)
            {
                Debug.LogError($"--> error {searchResult.Count}, path = {file}");
            }
            
            startIndex++;
            if (isCancel || startIndex >= allFiles.Count)
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                WriteSearchResult(searchResult);
            }

        };

    }


    /// <summary>
    /// 将搜索结果打印到 Console
    /// </summary>
    /// <param name="searchResult"></param>
    private static void WriteSearchResult(List<string> searchResult)
    {
        for (int i = 0; i < searchResult.Count; i++)
        {
            var filePath = searchResult[i];
            filePath = TrimProjectPath(filePath);
            Debug.Log($"-->{i} <a selectFile=\"{filePath}\">{filePath}</a>");
            if (i == 0 && searchResult.Count == 1)
            {
                FocusToFile(filePath);
            }
        }
        if(searchResult.Count == 0)
        {
            Debug.Log($"--> <color=yellow>done! but no matchs</color>");
        }
    }

    private static string TrimProjectPath(string path)
    {
        var projectPath = GetProjectDirectory();
        if (path.StartsWith(projectPath))
        {
            path = path.Substring(projectPath.Length); // 这里的+1对应的是字符串中的“/”
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }
        }
        return path;
    }



    private static void FocusToFile(string filePath)
    {
        EditorUtility.FocusProjectWindow();
        
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(UnityEngine.Object));
        if(obj == null)
        {
            return;
        }
        
        var pt = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
        var ins = pt.GetField("s_LastInteractedProjectBrowser", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        var showSelectionMeth = pt.GetMethod("AssetTreeSelectionCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        
        int[] ids = new int[] { obj.GetInstanceID() };
        showSelectionMeth.Invoke(ins, new object[] { ids });
    }

    private static void FocusToFolder(string folderPath)
    {
        EditorUtility.FocusProjectWindow();

        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(folderPath, typeof(UnityEngine.Object));

        var pt = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
        var ins = pt.GetField("s_LastInteractedProjectBrowser", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        var showDirMeth = pt.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);

        showDirMeth.Invoke(ins, new object[] { obj.GetInstanceID(), true });
    }



    private static void EditorGUI_hyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
    {
        if (window.titleContent.text == "Console")
        {
            var hyperLinkData = args.hyperLinkData;
            foreach (var item in hyperLinkData.Keys)
            {
                if (string.Equals("selectFile", item))
                {
                    var filePath = hyperLinkData[item];
                    FocusToFile(filePath);
                    break;
                }
            }
        }
    }


    /// <summary>
    /// get shader name in the asset file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static string GetShaderName(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        if (string.Equals(extension, ".shader"))
        {
            var projectFolder = GetProjectDirectory();
            var fullPath = Path.Combine(projectFolder, filePath);
            return GetNameFromShaderFile(fullPath);
        }
        if (string.Equals(extension, ".shadergraph"))
        {
            var projectFolder = GetProjectDirectory();
            var fullPath = Path.Combine(projectFolder, filePath);
            return GetNameFromShaderGraph(fullPath);
        }
        return null;
    }



    /// <summary>
    /// get shader name in the shader file
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    private static string GetNameFromShaderFile(string fullPath)
    {
        string fileContent = File.ReadAllText(fullPath);
        var shaderName = FindTheSection(fileContent, "Shader \"", "\"", 0, out int endIndex);
        if (shaderName != null)
        {
            return shaderName;
        }
        var shaderName1 = FindTheSection(fileContent, "Shader\"", "\"", 0, out int endIndex1);
        if (shaderName1 != null)
        {
            return shaderName1;
        }
        var shaderName2 = FindTheSection(fileContent, "Shader\t\"", "\"", 0, out int endIndex2);
        if (shaderName2 != null)
        {
            return shaderName2;
        }
        return null;
    }


    /// <summary>
    /// get shader name in the shadergraph file
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    private static string GetNameFromShaderGraph(string fullPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(fullPath);

        string fileContent = File.ReadAllText(fullPath);
        var pathName = FindTheSection(fileContent, "\"m_Path\":", ",", 0, out int endIndex);
        if (pathName != null)
        {
            int ind2 = pathName.IndexOf("\"");
            pathName = pathName.Substring(ind2 + 1, pathName.Length - ind2 - 2);
            return pathName+"/"+fileName;
        }
        else
        {
            return "Shader Graphs/" + fileName;
        }
    }

    /// <summary>
    /// get the directory for this unity project
    /// </summary>
    /// <returns></returns>
    static private string GetProjectDirectory()
    {
        string projectFolder = Application.dataPath.Replace('\\', '/');
        int index = projectFolder.LastIndexOf('/');
        string path2 = projectFolder.Substring(0, index);
        return path2;
    }

    /// <summary>
    /// get the files with the extensions
    /// </summary>
    /// <param name="extensions"></param>
    /// <param name="fileList"></param>
    static private void GetFilesWithExtension(List<string> extensions, List<string> fileList)
    {
        string searchFolder = Application.dataPath;
        string[] files = Directory.GetFiles(searchFolder, "*.*", SearchOption.AllDirectories)
                .Where(s => extensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
        foreach (var file in files)
        {
            if (file.StartsWith(searchFolder))
            {
                fileList.Add(file.Substring(searchFolder.Length - 6));
            }
            else if (file.StartsWith("/"))
            {
                fileList.Add("Assets" + file);
            }
            else
            {
                fileList.Add("Assets/" + file);
            }
        }
       

        string projectFolder = GetProjectDirectory();
        string packagesFolder = Path.Combine(projectFolder, "Packages");
        var packages = ReadPackagesManifest();
        foreach (var packageName in packages.Keys)
        {
            var version = packages[packageName];
            //Debug.Log("packageName=" + packageName + " packageVersion=" + version);
            if (version.StartsWith("file:"))
            {
                searchFolder = DealPathMatch(packagesFolder, version.Substring(5));
                if (Directory.Exists(searchFolder))
                {
                    string[] filesInPackage = Directory.GetFiles(searchFolder, "*.*", SearchOption.AllDirectories)
                       .Where(s => extensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
                    foreach (var file in filesInPackage)
                    {
                        var assetPath = "Packages/" + packageName + file.Substring(searchFolder.Length);
                        //Debug.Log("assetPath=" + assetPath);
                        fileList.Add(assetPath);
                    }
                }
                else
                {
                    Debug.Log("Directory not exists, path=" + searchFolder);
                }
            }
        }
        
        var folders = Directory.GetDirectories(packagesFolder);
        foreach (var subFolder in folders)
        {
            string[] filesInPackage = Directory.GetFiles(subFolder, "*.*", SearchOption.AllDirectories)
                    .Where(s => extensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
            for (int i = 0; i < filesInPackage.Length; i++)
            {
                var filePath = filesInPackage[i];
                if (fileList.Contains(filePath))
                {
                    continue;
                }
                fileList.Add(filePath);
            }
        }
    }



    /// <summary>
    /// read the manifest file in Packages folder
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, string> ReadPackagesManifest()
    {
        var ret = new Dictionary<string, string>();
        string projectFolder = GetProjectDirectory();
        string packagesFolder = Path.Combine(projectFolder, "Packages");
        string packagesFile = Path.Combine(packagesFolder, "manifest.json");
        Regex regex = new Regex("\"(.+\\.)+.+\":\\s+\".+\"");
        foreach (var line in File.ReadLines(packagesFile))
        {
            if (regex.IsMatch(line))
            {
                int ind = line.IndexOf(":");
                if (ind > 0)
                {
                    string left = line.Substring(0, ind);
                    string right = line.Substring(ind + 1);
                    string packageName = left.Trim().Replace("\"", "");
                    string packageVersion = right.Trim().Replace("\"", "").Replace(",", "");
                    ret[packageName] = packageVersion;
                }
            }
        }
        return ret;
    }


    /// <summary>
    /// find content, specify with prefix and suffix
    /// </summary>
    /// <param name="content"></param>
    /// <param name="startStr"></param>
    /// <param name="endStr"></param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <returns></returns>
    static private string FindTheSection(string content, string startStr, string endStr, int startIndex, out int endIndex)
    {
        int ind2 = content.IndexOf(startStr, startIndex);
        if (ind2 >= startIndex)
        {
            int ind3 = content.IndexOf(endStr, ind2 + startStr.Length);
            if (ind3 >= ind2 + startStr.Length)
            {
                ind2 += startStr.Length;
                string subPath = content.Substring(ind2, ind3 - ind2);
                endIndex = ind3 + endStr.Length;
                return subPath;
            }
        }
        endIndex = startIndex + startStr.Length;
        return null;
    }

    /// <summary>
    /// fix with path string
    /// </summary>
    /// <param name="currentFolder"></param>
    /// <param name="subPath"></param>
    /// <returns></returns>
    private static string DealPathMatch(string currentFolder, string subPath)
    {
        while (subPath.StartsWith("../"))
        {
            subPath = subPath.Substring(3);
            int index = currentFolder.LastIndexOf('/');
            currentFolder = currentFolder.Substring(0, index);
        }
        return Path.Combine(currentFolder, subPath);
    }



}