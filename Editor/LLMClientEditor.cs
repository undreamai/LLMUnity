using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace LLMUnity
{
    [CustomEditor(typeof(LLMClient))]
    public class LLMClientEditor : PropertyEditor
    {
        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLMClient) };
        }

        public void AddClientSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(LLMAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(LLMAdvancedAttribute));
            }
            attributeClasses.Add(llmScriptSO.FindProperty("remote").boolValue ? typeof(RemoteAttribute) : typeof(LocalAttribute));
            ShowPropertiesOfClass("LLM Settings", llmScriptSO, attributeClasses, true);
        }

        public void AddModelSettings(SerializedObject llmScriptSO, LLMClient llmClientScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            ShowPropertiesOfClass("", llmScriptSO, new List<Type> { typeof(ModelAttribute) }, false);

            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Grammar", GUILayout.Width(EditorGUIUtility.labelWidth));
                if (GUILayout.Button("Load grammar", GUILayout.Width(buttonWidth)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string path = EditorUtility.OpenFilePanelWithFilters("Select a gbnf grammar file", "", new string[] { "Grammar Files", "gbnf" });
                        if (!string.IsNullOrEmpty(path))
                        {
                            llmClientScript.SetGrammar(path);
                        }
                    };
                }
                EditorGUILayout.EndHorizontal();

                ShowPropertiesOfClass("", llmScriptSO, new List<Type> { typeof(ModelAdvancedAttribute) }, false);
            }
            Space();
        }

        public void AddChatSettings(SerializedObject llmScriptSO)
        {
            ShowPropertiesOfClass("Chat Settings", llmScriptSO, new List<Type> { typeof(ChatAttribute) }, false);
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
            AddClientSettings(llmScriptSO);
            AddModelSettings(llmScriptSO, llmScript);
            AddChatSettings(llmScriptSO);

            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }
    }
}
