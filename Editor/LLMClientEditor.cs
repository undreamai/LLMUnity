using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace LLMUnity
{
    [CustomEditor(typeof(LLMClient))]
    public class LLMClientEditor : Editor
    {
        protected int buttonWidth = 150;
        Type[] orderedTypes = new Type[]{typeof(LLM), typeof(LLMClient)};

        public void AddScript(SerializedObject llmScriptSO){
            var scriptProp = llmScriptSO.FindProperty("m_Script");
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
        }
        public void AddAdvancedOptionsToggle(SerializedObject llmScriptSO){
            SerializedProperty advancedOptionsProp = llmScriptSO.FindProperty("advancedOptions");
            string toggleText = (advancedOptionsProp.boolValue? "Show":"Hide") + " Advanced Options";
            GUIStyle style = new GUIStyle("Button");
            if (!advancedOptionsProp.boolValue)
                style.normal = new GUIStyleState(){ background = Texture2D.grayTexture };
            if (GUILayout.Button(toggleText, style, GUILayout.Width(buttonWidth))){
                advancedOptionsProp.boolValue = !advancedOptionsProp.boolValue;
            }
            EditorGUILayout.Space();
        }

        public void AddServerSettings(SerializedObject llmScriptSO){
            ShowPropertiesOfClass("Server Settings", llmScriptSO, orderedTypes, typeof(ServerAttribute), typeof(ServerAdvancedAttribute), true);
        }

        public void AddModelSettings(SerializedObject llmScriptSO, bool showHeader=true){
            ShowPropertiesOfClass(showHeader? "Model Settings": "", llmScriptSO, orderedTypes, typeof(ModelAttribute), typeof(ModelAdvancedAttribute), true);
        }

        public void AddChatSettings(SerializedObject llmScriptSO){
            ShowPropertiesOfClass("Chat Settings", llmScriptSO, orderedTypes, typeof(ChatAttribute), null, false);
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
            AddAdvancedOptionsToggle(llmScriptSO);
            AddServerSettings(llmScriptSO);
            AddModelSettings(llmScriptSO);
            AddChatSettings(llmScriptSO);
            EditorGUI.EndChangeCheck();
            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }

        public List<SerializedProperty> GetPropertiesOfClass(SerializedObject so, Type[] targetClasses, Type attributeClass=null, Type attributeAdvancedClass=null){
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = new List<SerializedProperty>();
            List<Type> attributeClasses = new List<Type>{attributeClass};
            if (so.FindProperty("advancedOptions").boolValue)
                attributeClasses.Add(attributeAdvancedClass);
            foreach (Type attrClass in attributeClasses){
                if (attrClass == null) continue;
                foreach (Type targetClass in targetClasses){
                    SerializedProperty prop = so.GetIterator();
                    if (prop.NextVisible(true)) {
                        do {
                            if (PropertyInClass(prop, targetClass, attrClass))
                                properties.Add(so.FindProperty(prop.propertyPath));
                        }
                        while (prop.NextVisible(false));
                    }
                }
            }
            return properties;
        }

        public void ShowPropertiesOfClass(string title, SerializedObject so, Type[] targetClasses, Type attributeClass=null, Type attributeAdvancedClass=null, bool addSpace = true){
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = GetPropertiesOfClass(so, targetClasses, attributeClass, attributeAdvancedClass);
            if (properties.Count == 0) return;
            if (title != "") EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (SerializedProperty prop in properties) EditorGUILayout.PropertyField(prop);
            if (addSpace) EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        public bool PropertyInClass(SerializedProperty prop, Type targetClass, Type attributeClass = null)
        {
            // check if a property belongs to a certain class and/or has a specific attribute class
            FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null && field.DeclaringType == targetClass && (attributeClass == null || AttributeInProperty(prop, attributeClass));
        }

        public bool AttributeInProperty(SerializedProperty prop, Type attributeClass)
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
}