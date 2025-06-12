using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class RefectionUtil
{

    [MenuItem("Edit/Print Refection Info", false, 11)]
    private static void ReflectTypeInfo()
    {

        Type pt = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");



        BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        PropertyInfo[] allProperties = pt.GetProperties(bindingFlags);
        foreach (var item in allProperties)
        {
            Debug.Log("property -> " + item.Name + " " + item.PropertyType.ToString());
        }

        FieldInfo[] allFields = pt.GetFields(bindingFlags);
        foreach (var item in allFields)
        {
            Debug.Log("field -> " + item.Name + " " + item.FieldType.ToString());
        }

        MethodInfo[] allMethods = pt.GetMethods(bindingFlags);
        foreach (var item in allMethods)
        {
            Debug.Log(GetMethodInfoDesc(item));
        }
    }


    private static string GetMethodInfoDesc(MethodInfo item)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("method -> ");
        sb.Append(item.Name);
        sb.Append("(");
        var pps = item.GetParameters();
        for (int i = 0; i < pps.Length; i++)
        {
            var pp = pps[i];
            sb.Append(pp.ParameterType);
            sb.Append(" ");
            sb.Append(pp.Name);
            if (i < pps.Length - 1)
            {
                sb.Append(", ");
            }
        }
        sb.Append(")");
        return sb.ToString();
    }
}
