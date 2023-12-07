using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(LLM))]
public class LLMEditor : Editor
{
    private int buttonWidth = 150;

    public override void OnInspectorGUI()
    {        
        LLM llmScript = (LLM)target;
        SerializedObject llmScriptSO = new SerializedObject(llmScript);

        // Add script property
        GUI.enabled = false;
        var scriptProp = llmScriptSO.FindProperty("m_Script");
        EditorGUILayout.PropertyField(scriptProp);
        GUI.enabled = true;
        EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);

        EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
        // Add buttons
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
        
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
        
        ShowPropertiesOfClass(llmScriptSO, typeof(LLM));
        EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);

        EditorGUILayout.LabelField("Client Settings", EditorStyles.boldLabel);
        ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient));
    }

    private bool IsPropertyDeclaredInClass(SerializedProperty prop, System.Type targetClass)
    {
        FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.DeclaringType == targetClass)
            return true;
        return false;
    }

    private void ShowPropertiesOfClass(SerializedObject so, System.Type targetClass){
        SerializedProperty prop = so.GetIterator();
        if (prop.NextVisible(true)) {
            do {
                if (IsPropertyDeclaredInClass(prop, targetClass))
                    EditorGUILayout.PropertyField(prop);
            }
            while (prop.NextVisible(false));
        }
    }
}
