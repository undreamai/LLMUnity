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

        public void AddOptionsToggle(SerializedObject llmScriptSO, string propertyName, string name)
        {
            SerializedProperty advancedOptionsProp = llmScriptSO.FindProperty(propertyName);
            string toggleText = (advancedOptionsProp.boolValue ? "Hide" : "Show") + " " + name;
            GUIStyle style = new GUIStyle("Button");
            if (advancedOptionsProp.boolValue)
                style.normal = new GUIStyleState() { background = Texture2D.grayTexture };
            if (GUILayout.Button(toggleText, style, GUILayout.Width(buttonWidth)))
            {
                advancedOptionsProp.boolValue = !advancedOptionsProp.boolValue;
            }
        }

        public void AddOptionsToggles(SerializedObject llmScriptSO)
        {
            EditorGUILayout.BeginHorizontal();
            AddOptionsToggle(llmScriptSO, "advancedOptions", "Advanced Options");
            AddOptionsToggle(llmScriptSO, "expertOptions", "Expert Options");
            EditorGUILayout.EndHorizontal();
            Space();
        }

        public void AddServerSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ServerAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                if (llmScriptSO.targetObject.GetType() == typeof(LLMClient)) attributeClasses.Add(typeof(ClientAdvancedAttribute));
                attributeClasses.Add(typeof(ServerAdvancedAttribute));
            }
            ShowPropertiesOfClass("Server Settings", llmScriptSO, orderedTypes, attributeClasses, true);
        }

        public virtual void AddModelLoaders(SerializedObject llmScriptSO, LLMClient llmScript)
        {
            string[] templateOptions = ChatTemplate.templatesDescription.Keys.ToList().ToArray();
            int index = Array.IndexOf(ChatTemplate.templatesDescription.Values.ToList().ToArray(), llmScript.chatTemplate);
            int newIndex = EditorGUILayout.Popup("Chat Template", index, templateOptions);
            if (newIndex != index)
            {
                llmScript.SetTemplate(ChatTemplate.templatesDescription[templateOptions[newIndex]]);
            }
        }

        public virtual void AddModelAddonLoaders(SerializedObject llmScriptSO, LLMClient llmScript, bool layout = true)
        {
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                if (layout)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Grammar", GUILayout.Width(EditorGUIUtility.labelWidth));
                }
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
            if (llmScriptSO.FindProperty("expertOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelExpertAttribute));
            }
            ShowPropertiesOfClass("", llmScriptSO, orderedTypes, attributeClasses, false);
            Space();
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLMClient llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
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
            AddOptionsToggles(llmScriptSO);
            AddServerSettings(llmScriptSO);
            AddModelLoadersSettings(llmScriptSO, llmScript);
            AddChatSettings(llmScriptSO);
            
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
            foreach (SerializedProperty prop in properties)
            {
                Attribute floatAttr = GetPropertyAttribute(prop, typeof(FloatAttribute));
                Attribute intAttr = GetPropertyAttribute(prop, typeof(IntAttribute));
                if (floatAttr != null)
                {
                    EditorGUILayout.Slider(prop, ((FloatAttribute)floatAttr).Min, ((FloatAttribute)floatAttr).Max, new GUIContent(prop.displayName));
                }
                else if (intAttr != null)
                {
                    EditorGUILayout.IntSlider(prop, ((IntAttribute)intAttr).Min, ((IntAttribute)intAttr).Max, new GUIContent(prop.displayName));
                }
                else
                {
                    EditorGUILayout.PropertyField(prop);
                }
            }
            if (addSpace) Space();
        }

        public bool PropertyInClass(SerializedProperty prop, Type targetClass, Type attributeClass = null)
        {
            // check if a property belongs to a certain class and/or has a specific attribute class
            FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null && field.DeclaringType == targetClass && (attributeClass == null || GetPropertyAttribute(prop, attributeClass) != null);
        }

        public Attribute GetPropertyAttribute(SerializedProperty prop, Type attributeClass)
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
                                return attr;
                        }
                    }
                    targetType = targetType.BaseType;
                }
            }
            return null;
        }
    }
}
