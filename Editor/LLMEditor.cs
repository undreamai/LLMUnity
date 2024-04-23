using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM))]
    public class LLMEditor : LLMClientEditor
    {
        Type[] propertyTypes = new Type[] { typeof(LLMBase), typeof(LLM) };

        public void AddModelLoaders(SerializedObject llmScriptSO, LLM llmClientScript)
        {
            LLM llmScript = (LLM)llmClientScript;
            EditorGUILayout.BeginHorizontal();

            string[] options = new string[llmScript.modelOptions.Length];
            for (int i = 0; i < llmScript.modelOptions.Length; i++)
            {
                options[i] = llmScript.modelOptions[i].Item1;
            }

            int newIndex = EditorGUILayout.Popup("Model", llmScript.SelectedModel, options);
            if (newIndex != llmScript.SelectedModel)
            {
                llmScript.DownloadModel(newIndex);
            }

            if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
            {
                EditorApplication.delayCall += () =>
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                    if (!string.IsNullOrEmpty(path))
                    {
                        llmScript.SelectedModel = 0;
                        llmScript.SetModel(path);
                    }
                };
            }
            EditorGUILayout.EndHorizontal();

            string[] templateOptions = ChatTemplate.templatesDescription.Keys.ToList().ToArray();
            int index = Array.IndexOf(ChatTemplate.templatesDescription.Values.ToList().ToArray(), llmScript.chatTemplate);
            newIndex = EditorGUILayout.Popup("Chat Template", index, templateOptions);
            if (newIndex != index)
            {
                llmScript.SetTemplate(ChatTemplate.templatesDescription[templateOptions[newIndex]]);
            }
        }

        public void AddModelAddonLoaders(SerializedObject llmScriptSO, LLM llmScript, bool layout = true)
        {
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Lora / Grammar", GUILayout.Width(EditorGUIUtility.labelWidth));

                if (GUILayout.Button("Load lora", GUILayout.Width(buttonWidth)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string path = EditorUtility.OpenFilePanelWithFilters("Select a bin lora file", "", new string[] { "Model Files", "bin" });
                        if (!string.IsNullOrEmpty(path))
                        {
                            llmScript.SetLora(path);
                        }
                    };
                }
                EditorGUILayout.EndHorizontal();
            }
            // ShowProgress(LLM.binariesProgress, "Setup Binaries");
            ShowProgress(llmScript.modelProgress, "Model Downloading");
            ShowProgress(llmScript.modelCopyProgress, "Model Copying");
        }

        public void AddModelSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ModelAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelAdvancedAttribute));
            }
            ShowPropertiesOfClass("", llmScriptSO, propertyTypes, attributeClasses, false);
            Space();
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
            AddModelAddonLoaders(llmScriptSO, llmScript);
            AddModelSettings(llmScriptSO);
        }

        public void AddServerSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ServerAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ServerAdvancedAttribute));
            }
            ShowPropertiesOfClass("Server Settings", llmScriptSO, propertyTypes, attributeClasses, true);
        }

        void ShowProgress(float progress, string progressText)
        {
            if (progress != 1) EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, progressText);
        }

        public override void OnInspectorGUI()
        {
            LLM llmScript = (LLM)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);
            llmScriptSO.Update();

            GUI.enabled = false;
            AddScript(llmScriptSO);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            AddOptionsToggles(llmScriptSO);
            // GUI.enabled = LLM.binariesProgress == 1;
            AddServerSettings(llmScriptSO);
            // GUI.enabled = LLM.binariesProgress == 1 && llmScript.modelProgress == 1 && llmScript.modelCopyProgress == 1;
            AddModelLoadersSettings(llmScriptSO, llmScript);
            GUI.enabled = true;
            AddChatSettings(llmScriptSO);

            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }
    }
}
