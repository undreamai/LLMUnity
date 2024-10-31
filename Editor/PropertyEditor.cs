using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace LLMUnity
{
    public class PropertyEditor : Editor
    {
        public static int buttonWidth = 150;

        public virtual void AddScript(SerializedObject llmScriptSO)
        {
            var scriptProp = llmScriptSO.FindProperty("m_Script");
            EditorGUILayout.PropertyField(scriptProp);
        }

        public virtual bool ToggleButton(string text, bool activated)
        {
            GUIStyle style = new GUIStyle("Button");
            if (activated) style.normal = new GUIStyleState() { background = Texture2D.grayTexture };
            return GUILayout.Button(text, style, GUILayout.Width(buttonWidth));
        }

        public virtual void AddSetupSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type>(){typeof(LocalRemoteAttribute)};
            SerializedProperty remoteProperty = llmScriptSO.FindProperty("remote");
            if (remoteProperty != null) attributeClasses.Add(remoteProperty.boolValue ? typeof(RemoteAttribute) : typeof(LocalAttribute));
            attributeClasses.Add(typeof(LLMAttribute));
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(LLMAdvancedAttribute));
            }
            ShowPropertiesOfClass("Setup Settings", llmScriptSO, attributeClasses, true);
        }

        public virtual void AddModelSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type>(){typeof(ModelAttribute)};
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelAdvancedAttribute));
            }
            ShowPropertiesOfClass("Model Settings", llmScriptSO, attributeClasses, true);
        }

        public virtual void AddChatSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type>(){typeof(ChatAttribute)};
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ChatAdvancedAttribute));
            }
            ShowPropertiesOfClass("Chat Settings", llmScriptSO, attributeClasses, true);
        }

        public void AddDebugModeToggle()
        {
            LLMUnitySetup.SetDebugMode((LLMUnitySetup.DebugModeType)EditorGUILayout.EnumPopup("Log Level", LLMUnitySetup.DebugMode));
        }

        public void AddAdvancedOptionsToggle(SerializedObject llmScriptSO)
        {
            List<SerializedProperty> properties = GetPropertiesOfClass(llmScriptSO, new List<Type>(){typeof(AdvancedAttribute)});
            if (properties.Count == 0) return;

            SerializedProperty advancedOptionsProp = llmScriptSO.FindProperty("advancedOptions");
            string toggleText = (advancedOptionsProp.boolValue ? "Hide" : "Show") + " Advanced Options";
            if (ToggleButton(toggleText, advancedOptionsProp.boolValue)) advancedOptionsProp.boolValue = !advancedOptionsProp.boolValue;
        }

        public virtual void AddOptionsToggles(SerializedObject llmScriptSO)
        {
            AddAdvancedOptionsToggle(llmScriptSO);
            Space();
        }

        public virtual void Space()
        {
            EditorGUILayout.Space((int)EditorGUIUtility.singleLineHeight / 2);
        }

        protected virtual Type[] GetPropertyTypes()
        {
            List<Type> types = new List<Type>();
            Type currentType = target.GetType();
            while (currentType != null)
            {
                types.Add(currentType);
                currentType = currentType.BaseType;
                if (currentType == typeof(MonoBehaviour)) break;
            }
            types.Reverse();
            return types.ToArray();
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

        public bool ShowPropertiesOfClass(string title, SerializedObject so, List<Type> attributeClasses, bool addSpace = true, List<Type> excludeAttributeClasses = null)
        {
            // display a property if it belongs to a certain class and/or has a specific attribute class
            List<SerializedProperty> properties = GetPropertiesOfClass(so, attributeClasses);
            if (excludeAttributeClasses != null)
            {
                List<SerializedProperty> excludeProperties = GetPropertiesOfClass(so, excludeAttributeClasses);
                List<SerializedProperty> removeProperties = new List<SerializedProperty>();
                foreach (SerializedProperty excprop in excludeProperties)
                {
                    foreach (SerializedProperty prop in properties)
                    {
                        if (prop.displayName == excprop.displayName) removeProperties.Add(prop);
                    }
                }
                foreach (SerializedProperty prop in removeProperties) properties.Remove(prop);
            }
            if (properties.Count == 0) return false;
            if (title != "") EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (SerializedProperty prop in properties)
            {
                Attribute floatAttr = GetPropertyAttribute(prop, typeof(FloatAttribute));
                Attribute intAttr = GetPropertyAttribute(prop, typeof(IntAttribute));
                Attribute dynamicRangeAttr = GetPropertyAttribute(prop, typeof(DynamicRangeAttribute));
                if (floatAttr != null)
                {
                    FloatAttribute attr = (FloatAttribute)floatAttr;
                    EditorGUILayout.Slider(prop, attr.Min, attr.Max, new GUIContent(prop.displayName));
                }
                else if (intAttr != null)
                {
                    IntAttribute attr = (IntAttribute)intAttr;
                    EditorGUILayout.IntSlider(prop, attr.Min, attr.Max, new GUIContent(prop.displayName));
                }
                else if (dynamicRangeAttr != null)
                {
                    DynamicRangeAttribute attr = (DynamicRangeAttribute)dynamicRangeAttr;
                    if (attr.intOrFloat)
                    {
                        float minValue = so.FindProperty(attr.minVariable).floatValue;
                        float maxValue = so.FindProperty(attr.maxVariable).floatValue;
                        EditorGUILayout.Slider(prop, minValue, maxValue, new GUIContent(prop.displayName));
                    }
                    else
                    {
                        int minValue = so.FindProperty(attr.minVariable).intValue;
                        int maxValue = so.FindProperty(attr.maxVariable).intValue;
                        EditorGUILayout.IntSlider(prop, minValue, maxValue, new GUIContent(prop.displayName));
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(prop);
                }
            }
            if (addSpace) Space();
            return true;
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
                            if (attributeClass.IsAssignableFrom(attr.GetType()))
                                return attr;
                        }
                    }
                    targetType = targetType.BaseType;
                }
            }
            return null;
        }

        public virtual void OnInspectorGUIStart(SerializedObject scriptSO)
        {
            scriptSO.Update();
            GUI.enabled = false;
            AddScript(scriptSO);
            GUI.enabled = true;
            EditorGUI.BeginChangeCheck();
        }

        public virtual void OnInspectorGUIEnd(SerializedObject scriptSO)
        {
            if (EditorGUI.EndChangeCheck())
                Repaint();

            scriptSO.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            SerializedObject llmScriptSO = new SerializedObject(target);

            OnInspectorGUIStart(llmScriptSO);
            AddOptionsToggles(llmScriptSO);

            AddSetupSettings(llmScriptSO);
            AddChatSettings(llmScriptSO);
            AddModelSettings(llmScriptSO);

            OnInspectorGUIEnd(llmScriptSO);
        }
    }
}
