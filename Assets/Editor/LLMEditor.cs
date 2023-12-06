using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LLM))]
public class LLMEditor : Editor
{
    private int buttonWidth = 150;

    public override void OnInspectorGUI()
    {        
        LLM llmScript = (LLM)target;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !llmScript.SetupStarted();
        if (GUILayout.Button("Setup server", GUILayout.Width(buttonWidth)))
        {
            llmScript.RunSetup();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Load server", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanel("Select a llama.cpp server exe", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.Server = path;
                }
            };
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Download model", GUILayout.Width(buttonWidth)))
        {
            Debug.Log("Download model");
        }
        if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.Model = path;
                }
            };
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
        
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
        base.OnInspectorGUI();
    }
}
