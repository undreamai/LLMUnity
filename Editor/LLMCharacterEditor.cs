using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace LLMUnity
{
    [CustomEditor(typeof(LLMCharacter))]
    public class LLMCharacterEditor : PropertyEditor
    {
        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLMCharacter) };
        }

        public void AddClientSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type>(){typeof(LocalRemoteAttribute)};
            attributeClasses.Add(llmScriptSO.FindProperty("remote").boolValue ? typeof(RemoteAttribute) : typeof(LocalAttribute));
            attributeClasses.Add(typeof(LLMAttribute));
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(LLMAdvancedAttribute));
            }
            ShowPropertiesOfClass("Setup Settings", llmScriptSO, attributeClasses, true);
        }

        public void AddModelSettings(SerializedObject llmScriptSO, LLMCharacter llmCharacterScript)
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
                            llmCharacterScript.SetGrammar(path);
                        }
                    };
                }
                EditorGUILayout.EndHorizontal();

                ShowPropertiesOfClass("", llmScriptSO, new List<Type> { typeof(ModelAdvancedAttribute) }, false);
            }
        }

        public void AddChatSettings(SerializedObject llmScriptSO)
        {
            ShowPropertiesOfClass("Chat Settings", llmScriptSO, new List<Type> { typeof(ChatAttribute) }, true);
        }

        public override void OnInspectorGUI()
        {
            LLMCharacter llmScript = (LLMCharacter)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);
            llmScriptSO.Update();

            GUI.enabled = false;
            AddScript(llmScriptSO);
            GUI.enabled = true;
            EditorGUI.BeginChangeCheck();
            AddOptionsToggles(llmScriptSO);
            AddClientSettings(llmScriptSO);
            AddChatSettings(llmScriptSO);
            AddModelSettings(llmScriptSO, llmScript);

            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(LLMClient))]
    public class LLMClientEditor : LLMCharacterEditor {}
}
