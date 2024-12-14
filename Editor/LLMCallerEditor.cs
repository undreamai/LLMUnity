using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace LLMUnity
{
    [CustomEditor(typeof(LLMCaller), true)]
    public class LLMCallerEditor : PropertyEditor {}

    [CustomEditor(typeof(LLMCharacter), true)]
    public class LLMCharacterEditor : LLMCallerEditor
    {
        public override void AddModelSettings(SerializedObject llmScriptSO)
        {
            if (!llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                base.AddModelSettings(llmScriptSO);
            }
            else
            {
                EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
                ShowPropertiesOfClass("", llmScriptSO, new List<Type> { typeof(ModelAttribute) }, false);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Grammar", GUILayout.Width(EditorGUIUtility.labelWidth));
                if (GUILayout.Button("Load grammar", GUILayout.Width(buttonWidth)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string path = EditorUtility.OpenFilePanelWithFilters("Select a gbnf grammar file", "", new string[] { "Grammar Files", "gbnf" });
                        if (!string.IsNullOrEmpty(path))
                        {
                            ((LLMCharacter)target).SetGrammar(path);
                        }
                    };
                }
                EditorGUILayout.EndHorizontal();

                ShowPropertiesOfClass("", llmScriptSO, new List<Type> { typeof(ModelAdvancedAttribute) }, false);
            }
        }
    }

    [CustomEditor(typeof(DBSearch), true)]
    public class DBSearchEditor : PropertyEditor {}
}
