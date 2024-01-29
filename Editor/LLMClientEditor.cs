using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace LLMUnity
{
    [CustomEditor(typeof(LLMClient))]
    public class LLMClientEditor : Editor
    {
        protected int buttonWidth = 150;
        Type[] orderedTypes = new Type[] { typeof(LLM), typeof(LLMClient) };

        public void Space()
        {
            EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
        }

        public void AddScript(SerializedObject llmScriptSO)
        {
            var scriptProp = llmScriptSO.FindProperty("m_Script");
            EditorGUILayout.PropertyField(scriptProp);
        }

        public void AddAdvancedOptionsToggle(SerializedObject llmScriptSO)
        {
            SerializedProperty advancedOptionsProp = llmScriptSO.FindProperty("advancedOptions");
            string toggleText = (advancedOptionsProp.boolValue ? "Hide" : "Show") + " Advanced Options";
            GUIStyle style = new GUIStyle("Button");
            if (advancedOptionsProp.boolValue)
                style.normal = new GUIStyleState() { background = Texture2D.grayTexture };
            if (GUILayout.Button(toggleText, style, GUILayout.Width(buttonWidth)))
            {
                advancedOptionsProp.boolValue = !advancedOptionsProp.boolValue;
            }
            Space();
        }

        public void AddServerSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ServerAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue) attributeClasses.Add(typeof(ServerAdvancedAttribute));
            ShowPropertiesOfClass("Server Settings", llmScriptSO, orderedTypes, attributeClasses, true);
        }

        public void AddModelAddonLoaders(SerializedObject llmScriptSO, LLMClient llmScript, bool layout = true)
        {
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                if (layout) EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Load grammar", GUILayout.Width(buttonWidth)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string path = EditorUtility.OpenFilePanelWithFilters("Select a gbnf grammar file", "", new string[] { "Grammar Files", "gbnf" });
                        if (!string.IsNullOrEmpty(path))
                        {
                            llmScript.SetGrammar(path);
                        }
                    };
                }
                if (layout) EditorGUILayout.EndHorizontal();
            }
        }

        public void AddModelSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ModelAttribute), typeof(ModelAddonAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelAddonAdvancedAttribute));
                attributeClasses.Add(typeof(ModelAdvancedAttribute));
            }
            ShowPropertiesOfClass("", llmScriptSO, orderedTypes, attributeClasses, true);
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLMClient llmScript)
        {
            if (!llmScriptSO.FindProperty("advancedOptions").boolValue) return; // at the moment we only have advanced parameters here
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelAddonLoaders(llmScriptSO, llmScript);
            AddModelSettings(llmScriptSO);
        }

        public void AddChatSettings(SerializedObject llmScriptSO)
        {
            ShowPropertiesOfClass("Chat Settings", llmScriptSO, orderedTypes, new List<Type> { typeof(ChatAttribute) }, false);
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
            AddModelLoadersSettings(llmScriptSO, llmScript);
            AddChatSettings(llmScriptSO);
            EditorGUI.EndChangeCheck();
            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }

        public List<SerializedProperty> GetPropertiesOfClass(SerializedObject so, Type[] targetClasses, List<Type> attributeClasses)
        {
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = new List<SerializedProperty>();
            foreach (Type attrClass in attributeClasses)
            {
                if (attrClass == null) continue;
                foreach (Type targetClass in targetClasses)
                {
                    SerializedProperty prop = so.GetIterator();
                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            if (PropertyInClass(prop, targetClass, attrClass))
                                properties.Add(so.FindProperty(prop.propertyPath));
                        }
                        while (prop.NextVisible(false));
                    }
                }
            }
            return properties;
        }

        public void ShowPropertiesOfClass(string title, SerializedObject so, Type[] targetClasses, List<Type> attributeClasses, bool addSpace = true)
        {
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = GetPropertiesOfClass(so, targetClasses, attributeClasses);
            if (properties.Count == 0) return;
            if (title != "") EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (SerializedProperty prop in properties) EditorGUILayout.PropertyField(prop);
            if (addSpace) Space();
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
            foreach (var pathSegment in prop.propertyPath.Split('.'))
            {
                var targetType = prop.serializedObject.targetObject.GetType();
                while (targetType != null)
                {
                    var fieldInfo = targetType.GetField(pathSegment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fieldInfo != null)
                    {
                        foreach (Attribute attr in fieldInfo.GetCustomAttributes(attributeClass, true))
                        {
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
