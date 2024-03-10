using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM))]
    public class LLMEditor : LLMClientEditor
    {
        public override void AddModelLoaders(SerializedObject llmScriptSO, LLMClient llmClientScript)
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
            base.AddModelLoaders(llmScriptSO, llmScript);
        }

        public override void AddModelAddonLoaders(SerializedObject llmScriptSO, LLMClient llmClientScript, bool layout)
        {
            LLM llmScript = (LLM)llmClientScript;
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
                base.AddModelAddonLoaders(llmScriptSO, llmScript, false);
                EditorGUILayout.EndHorizontal();
            }
            ShowProgress(LLM.binariesProgress, "Setup Binaries");
            ShowProgress(llmScript.modelProgress, "Model Downloading");
            ShowProgress(llmScript.modelCopyProgress, "Model Copying");
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
            GUI.enabled = LLM.binariesProgress == 1;
            AddServerSettings(llmScriptSO);
            GUI.enabled = LLM.binariesProgress == 1 && llmScript.modelProgress == 1 && llmScript.modelCopyProgress == 1;
            AddModelLoadersSettings(llmScriptSO, llmScript);
            GUI.enabled = true;
            AddChatSettings(llmScriptSO);
            
            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }
    }
}
