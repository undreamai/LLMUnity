using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(LLM))]
public class LLMEditor : Editor
{
    private int buttonWidth = 150;

    public void OnEnable()
    {
        LLM llmScript = (LLM)target;
        if (llmScript.numThreads == -1){
            llmScript.numThreads = SystemInfo.processorCount;
        }
    }

    public override void OnInspectorGUI()
    {
        LLM llmScript = (LLM)target;
        SerializedObject llmScriptSO = new SerializedObject(llmScript);
        llmScriptSO.Update();

        // SCRIPT PROPERTY
        GUI.enabled = false;
        var scriptProp = llmScriptSO.FindProperty("m_Script");
        EditorGUILayout.PropertyField(scriptProp);
        GUI.enabled = true;
        EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);

        EditorGUI.BeginChangeCheck();

        // SERVER SETTINGS
        EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
        // Add buttons
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
                    llmScript.SetServer(path);
                }
            };
        }
        EditorGUILayout.EndHorizontal();
        if (llmScript.server != ""){
            ShowPropertiesOfClass(llmScriptSO, typeof(LLM), typeof(ServerAttribute));
            ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ServerAttribute));
        }
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        // MODEL SETTINGS
        EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !llmScript.ModelDownloading();
        if (GUILayout.Button("Download model", GUILayout.Width(buttonWidth)))
        {
            llmScript.DownloadModel();
        }
        GUI.enabled = true;
        GUI.enabled = !llmScript.ModelCopying();
        if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.SetModel(path);
                }
            };
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        if (llmScript.model != ""){
            ShowPropertiesOfClass(llmScriptSO, typeof(LLM), typeof(ModelAttribute));
            ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ModelAttribute));
        }
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        // CLIENT SETTINGS
        if (llmScript.server != "" && llmScript.model != ""){
            EditorGUILayout.LabelField("Chat Settings", EditorStyles.boldLabel);
            ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ChatAttribute));
        }

        EditorGUI.EndChangeCheck();
        if (EditorGUI.EndChangeCheck())
            Repaint();

        llmScriptSO.ApplyModifiedProperties();
    }

    private void ShowPropertiesOfClass(SerializedObject so, System.Type targetClass, System.Type attributeClass = null){
        SerializedProperty prop = so.GetIterator();
        if (prop.NextVisible(true)) {
            do {
                if (PropertyInClass(prop, targetClass, attributeClass))
                    EditorGUILayout.PropertyField(prop);
            }
            while (prop.NextVisible(false));
        }
    }

    private bool PropertyInClass(SerializedProperty prop, System.Type targetClass, System.Type attributeClass = null)
    {
        FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && field.DeclaringType == targetClass && (attributeClass == null || AttributeInProperty(prop, attributeClass));
    }

    private bool AttributeInProperty(SerializedProperty prop, System.Type attributeClass)
    {
        foreach (var pathSegment in prop.propertyPath.Split('.')){
            var targetType = prop.serializedObject.targetObject.GetType();
            while (targetType != null){
                var fieldInfo = targetType.GetField(pathSegment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo != null){
                    foreach (Attribute attr in fieldInfo.GetCustomAttributes(attributeClass, true)){
                        if (attr.GetType() == attributeClass)
                            return true;
                    }
                }
                targetType = targetType.BaseType;
            }
        }
        return false;
    }

}
