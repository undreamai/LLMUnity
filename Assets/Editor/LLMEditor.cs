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
        ShowPropertiesOfClass(llmScriptSO, typeof(LLM), typeof(ServerAttribute));
        ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ServerAttribute));
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        // MODEL SETTINGS
        EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
        GUI.enabled = !llmScript.modelWIP;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Download model", GUILayout.Width(buttonWidth)))
        {
            llmScript.DownloadModel();
        }
        if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.LoadModel(path);
                }
            };
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load lora", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanelWithFilters("Select a bin lora file", "", new string[] { "Model Files", "bin" });
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.LoadLora(path);
                }
            };
        }
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;
        if (llmScript.model != ""){
            ShowPropertiesOfClass(llmScriptSO, typeof(LLM), typeof(ModelAttribute));
            ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ModelAttribute));
        }
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        // CLIENT SETTINGS
        if (llmScript.model != ""){
            EditorGUILayout.LabelField("Chat Settings", EditorStyles.boldLabel);
            ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ChatAttribute));
        }

        EditorGUI.EndChangeCheck();
        if (EditorGUI.EndChangeCheck())
            Repaint();

        llmScriptSO.ApplyModifiedProperties();
    }

    private void ShowPropertiesOfClass(SerializedObject so, System.Type targetClass, System.Type attributeClass = null){
        // display a property if it belongs to a certain class and/or has a specific attribute class
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
        // check if a property belongs to a certain class and/or has a specific attribute class
        FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && field.DeclaringType == targetClass && (attributeClass == null || AttributeInProperty(prop, attributeClass));
    }

    private bool AttributeInProperty(SerializedProperty prop, System.Type attributeClass)
    {
        // check if a property has a specific attribute class
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
