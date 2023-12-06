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
        EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
        
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
        
        // Add properties from child class and then parent
        foreach (Type type in new Type[]{typeof(LLM), typeof(LLMClient)}) {
            SerializedProperty prop = llmScriptSO.GetIterator();
            if (prop.NextVisible(true)) {
                do {
                    if (IsPropertyDeclaredInClass(prop, type))
                        EditorGUILayout.PropertyField(prop);
                }
                while (prop.NextVisible(false));
            }
        }
    }

    private bool IsPropertyDeclaredInClass(SerializedProperty prop, System.Type targetClass)
    {
        FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field != null && field.DeclaringType == targetClass)
        {
            return true;
        }

        return false;
    }
}
