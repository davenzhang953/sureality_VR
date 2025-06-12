using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BAPointCloudRenderer.CloudController;

[CustomEditor(typeof(BAPreview))]
public class PreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BAPreview previewscript = (BAPreview)target;
        if (!EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Update Preview"))
            {
                previewscript.UpdatePreview();
            }
            if (GUILayout.Button("Hide Preview"))
            {
                previewscript.HidePreview();
            }
        }
    }
}
