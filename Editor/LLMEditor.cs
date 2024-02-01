using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM))]
    public class LLMEditor : LLMClientEditor
    {
        public void AddModelLoaders(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Download model", GUILayout.Width(buttonWidth)))
            {
                llmScript.DownloadModel();
            }
            if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
            {
                EditorApplication.delayCall += () =>
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                    if (!string.IsNullOrEmpty(path))
                    {
                        llmScript.SetModel(path);
                    }
                };
            }
            EditorGUILayout.EndHorizontal();
        }

        public void AddModelAddonLoaders(SerializedObject llmScriptSO, LLM llmScript)
        {
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                EditorGUILayout.BeginHorizontal();
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
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
            AddModelAddonLoaders(llmScriptSO, llmScript);
            ShowProgress(LLM.binariesProgress, "Setup Binaries");
            ShowProgress(llmScript.modelProgress, "Model Downloading");
            ShowProgress(llmScript.modelCopyProgress, "Model Copying");
            AddModelSettings(llmScriptSO);
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

            EditorGUI.EndChangeCheck();
            if (EditorGUI.EndChangeCheck())
                Repaint();

            llmScriptSO.ApplyModifiedProperties();
        }
    }
}
