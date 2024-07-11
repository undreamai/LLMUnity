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

        public override void OnInspectorGUI()
        {
            LLMCharacter llmScript = (LLMCharacter)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);

            OnInspectorGUIStart(llmScriptSO);
            AddOptionsToggles(llmScriptSO);

            AddSetupSettings(llmScriptSO);
            AddChatSettings(llmScriptSO);
            Space();
            AddModelSettings(llmScriptSO, llmScript);

            OnInspectorGUIEnd(llmScriptSO);
        }
    }

    [CustomEditor(typeof(LLMClient))]
    public class LLMClientEditor : LLMCharacterEditor {}
}
