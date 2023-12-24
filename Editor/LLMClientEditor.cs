using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(LLMClient))]
public class LLMClientEditor : Editor
{
    public void AddScript(SerializedObject llmScriptSO){
        var scriptProp = llmScriptSO.FindProperty("m_Script");
        EditorGUILayout.PropertyField(scriptProp);
        EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
    }

    public void AddServerSettings(SerializedObject llmScriptSO){
        EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
        ShowPropertiesOfClass(llmScriptSO, typeof(LLM), typeof(ServerAttribute));
        ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ServerAttribute));
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
    }

    public void AddModelSettings(SerializedObject llmScriptSO, bool showHeader=true){
        if (showHeader) EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
        ShowPropertiesOfClass(llmScriptSO, typeof(LLM), typeof(ModelAttribute));
        ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ModelAttribute));
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
    }

    public void AddChatSettings(SerializedObject llmScriptSO){
        EditorGUILayout.LabelField("Chat Settings", EditorStyles.boldLabel);
        ShowPropertiesOfClass(llmScriptSO, typeof(LLMClient), typeof(ChatAttribute));
    }

    public override void OnInspectorGUI()
    {
        LLMClient llmScript = (LLMClient)target;
        SerializedObject llmScriptSO = new SerializedObject(llmScript);
        llmScriptSO.Update();

        GUI.enabled = false;
        AddScript(llmScriptSO);
        GUI.enabled = true;

        EditorGUI.BeginChangeCheck();
        AddServerSettings(llmScriptSO);
        AddModelSettings(llmScriptSO);
        AddChatSettings(llmScriptSO);
        EditorGUI.EndChangeCheck();
        if (EditorGUI.EndChangeCheck())
            Repaint();

        llmScriptSO.ApplyModifiedProperties();
    }

    public void ShowPropertiesOfClass(SerializedObject so, System.Type targetClass, System.Type attributeClass = null){
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

    public bool PropertyInClass(SerializedProperty prop, System.Type targetClass, System.Type attributeClass = null)
    {
        // check if a property belongs to a certain class and/or has a specific attribute class
        FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && field.DeclaringType == targetClass && (attributeClass == null || AttributeInProperty(prop, attributeClass));
    }

    public bool AttributeInProperty(SerializedProperty prop, System.Type attributeClass)
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