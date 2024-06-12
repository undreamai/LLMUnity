using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace LLMUnity
{
    public class PropertyEditor : Editor
    {
        protected int buttonWidth = 150;

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
            EditorGUILayout.EndHorizontal();
            Space();
        }

        public void Space()
        {
            EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
        }

        protected virtual Type[] GetPropertyTypes()
        {
            return new Type[] {};
        }

        public List<SerializedProperty> GetPropertiesOfClass(SerializedObject so, List<Type> attributeClasses)
        {
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = new List<SerializedProperty>();
            foreach (Type attrClass in attributeClasses)
            {
                if (attrClass == null) continue;
                foreach (Type targetClass in GetPropertyTypes())
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

        public void ShowPropertiesOfClass(string title, SerializedObject so, List<Type> attributeClasses, bool addSpace = true)
        {
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = GetPropertiesOfClass(so, attributeClasses);
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
