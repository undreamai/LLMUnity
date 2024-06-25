using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM))]
    public class LLMEditor : PropertyEditor
    {
        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLM) };
        }

        public void AddServerLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Setup Settings", EditorStyles.boldLabel);
            AddServerSettings(llmScriptSO);
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
            AddModelAddonLoaders(llmScriptSO, llmScript);
            AddModelSettings(llmScriptSO);
        }

        public void AddModelLoaders(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.BeginHorizontal();

            string[] options = new string[LLMUnitySetup.modelOptions.Length];
            for (int i = 0; i < LLMUnitySetup.modelOptions.Length; i++)
            {
                options[i] = LLMUnitySetup.modelOptions[i].Item1;
            }

            int newIndex = EditorGUILayout.Popup("Model", llmScript.SelectedModel, options);
            if (newIndex != llmScript.SelectedModel)
            {
                LLMUnitySetup.DownloadModel(llmScript, newIndex);
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
                GUILayout.Label("Lora", GUILayout.Width(EditorGUIUtility.labelWidth));

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
        }

        public void AddModelSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ModelAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelAdvancedAttribute));
            }
            ShowPropertiesOfClass("", llmScriptSO, attributeClasses, false);
            Space();
        }

        public void AddServerSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type>(){typeof(LocalRemoteAttribute)};
            attributeClasses.Add(llmScriptSO.FindProperty("remote").boolValue ? typeof(RemoteAttribute) : typeof(LocalAttribute));
            attributeClasses.Add(typeof(LLMAttribute));
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(LLMAdvancedAttribute));
            }
            ShowPropertiesOfClass("", llmScriptSO, attributeClasses, true);
            Space();
        }

        public void AddChatSettings(SerializedObject llmScriptSO)
        {
            ShowPropertiesOfClass("Chat Settings", llmScriptSO, new List<Type> { typeof(ChatAttribute) }, false);
        }

        void ShowProgress(float progress, string progressText)
        {
            if (progress != 1) EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, progressText);
        }

        public override void OnInspectorGUI()
        {
            LLM llmScript = (LLM)target;
            // LLM llmScript = (LLM)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);
            llmScriptSO.Update();

            GUI.enabled = false;
            AddScript(llmScriptSO);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();

            ShowProgress(LLMUnitySetup.libraryProgress, "Setup Library");
            ShowProgress(llmScript.modelProgress, "Model Downloading");
            ShowProgress(llmScript.modelCopyProgress, "Model Copying");

            GUI.enabled = LLMUnitySetup.libraryProgress == 1 && llmScript.modelProgress == 1 && llmScript.modelCopyProgress == 1;
            AddOptionsToggles(llmScriptSO);
            AddServerLoadersSettings(llmScriptSO, llmScript);
            AddModelLoadersSettings(llmScriptSO, llmScript);
            GUI.enabled = true;
            AddChatSettings(llmScriptSO);

            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }
    }
}
