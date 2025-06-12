using System;
using System.Reflection;
using UnityEngine;


namespace Seengene.XDK
{

    public class ReflectTool
    {

        public static void DebugAllProperty(Type tp)
        {
            BindingFlags bindingFlags1 = BindingFlags.Public | BindingFlags.Instance;
            BindingFlags bindingFlags2 = BindingFlags.NonPublic | BindingFlags.Instance;

            PropertyInfo[] allProperty1 = tp.GetProperties(bindingFlags1);
            PropertyInfo[] allProperty2 = tp.GetProperties(bindingFlags2);
            int num1 = allProperty1.Length + allProperty2.Length;

            FieldInfo[] allFields1 = tp.GetFields(bindingFlags1);
            FieldInfo[] allFields2 = tp.GetFields(bindingFlags2);
            int num2 = allFields1.Length + allFields2.Length;

            Debug.Log("Type=" + tp.ToString() + " allProperty.Length=" + num1 + " allFields.Length=" + num2);
            int count = 0;
            for (int i = 0; i < allProperty1.Length; i++)
            {
                Debug.Log($"property [{count}], public  Name={allProperty1[i].Name} type={allProperty1[i].PropertyType}");
                count++;
            }
            for (int i = 0; i < allProperty2.Length; i++)
            {
                Debug.Log($"property [{count}], private Name={allProperty2[i].Name} type={allProperty2[i].PropertyType}");
                count++;
            }
            for (int i = 0; i < allFields1.Length; i++)
            {
                Debug.Log($"field [{count}], public  Name={allFields1[i].Name} type={allFields1[i].FieldType}");
                count++;
            }
            for (int i = 0; i < allFields2.Length; i++)
            {
                Debug.Log($"field [{count}], private Name={allFields2[i].Name} type={allFields2[i].FieldType}");
                count++;
            }
            
        }
    }
}
