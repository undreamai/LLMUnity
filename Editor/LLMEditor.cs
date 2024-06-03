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
            EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
            AddCUDALoaders(llmScriptSO, llmScript);
            AddServerSettings(llmScriptSO);
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
            AddModelAddonLoaders(llmScriptSO, llmScript);
            AddModelSettings(llmScriptSO);
        }

        public void AddCUDALoaders(SerializedObject llmScriptSO, LLM llmScript)
        {
            string[] options = new string[LLMUnitySetup.CUDAOptions.Length];
            for (int i = 0; i < LLMUnitySetup.CUDAOptions.Length; i++)
            {
                options[i] = LLMUnitySetup.CUDAOptions[i].Item1;
            }

            int newIndex = EditorGUILayout.Popup("CUDA", LLMUnitySetup.SelectedCUDA, options);
            if (newIndex != LLMUnitySetup.SelectedCUDA)
            {
                LLMUnitySetup.DownloadCUDA(newIndex);
            }
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
            List<Type> attributeClasses = new List<Type> { typeof(LLMAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(LLMAdvancedAttribute));
            }
            attributeClasses.Add(llmScriptSO.FindProperty("remote").boolValue ? typeof(RemoteAttribute) : typeof(LocalAttribute));
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
            ShowProgress(LLMUnitySetup.CUDAProgress, "CUDA Downloading");
            ShowProgress(llmScript.modelProgress, "Model Downloading");
            ShowProgress(llmScript.modelCopyProgress, "Model Copying");

            GUI.enabled = LLMUnitySetup.libraryProgress == 1 && LLMUnitySetup.CUDAProgress == 1f && llmScript.modelProgress == 1 && llmScript.modelCopyProgress == 1;
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
