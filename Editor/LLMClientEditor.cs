using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace LLMUnity
{
    [CustomEditor(typeof(LLMClientBase))]
    public class LLMClientBaseEditor : PropertyEditor
    {
        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLMClientBase) };
        }

        public void AddClientSettings(SerializedObject llmScriptSO)
        {
            ShowPropertiesOfClass("Client Settings", llmScriptSO, new List<Type> { typeof(ClientAttribute) }, true);
        }

        public void AddModelSettings(SerializedObject llmScriptSO, LLMClientBase llmClientScript)
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
            LLMClientBase llmScript = (LLMClientBase)target;
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

    [CustomEditor(typeof(LLMClient))]
    public class LLMClientEditor : LLMClientBaseEditor
    {
        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLMClient), typeof(LLMClientBase) };
        }
    }

    [CustomEditor(typeof(LLMRemoteClient))]
    public class LLMRemoteClientEditor : LLMClientBaseEditor
    {
        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLMRemoteClient), typeof(LLMClientBase) };
        }
    }
}
