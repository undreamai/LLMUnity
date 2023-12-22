using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LLM))]
public class LLMEditor : LLMClientEditor
{
    public void AddModelLoaders(SerializedObject llmScriptSO, LLM llmScript, int buttonWidth = 150){
        EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
        GUI.enabled = !llmScript.modelWIP;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Download model", GUILayout.Width(buttonWidth)))
        {
            llmScript.DownloadModel();
        }
        if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.SetModel(path);
                }
            };
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load lora", GUILayout.Width(buttonWidth)))
        {
            EditorApplication.delayCall += () => {
                string path = EditorUtility.OpenFilePanelWithFilters("Select a bin lora file", "", new string[] { "Model Files", "bin" });
                if (!string.IsNullOrEmpty(path))
                {
                    llmScript.SetLora(path);
                }
            };
        }
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;
    }

    public override void OnInspectorGUI()
    {
        LLM llmScript = (LLM)target;
        SerializedObject llmScriptSO = new SerializedObject(llmScript);
        llmScriptSO.Update();

        AddScript(llmScriptSO);
        EditorGUI.BeginChangeCheck();
        AddServerSettings(llmScriptSO);
        AddModelLoaders(llmScriptSO, llmScript);
        if (llmScript.model != ""){
            AddModelSettings(llmScriptSO, false);
            AddChatSettings(llmScriptSO);
        }
        EditorGUI.EndChangeCheck();
        if (EditorGUI.EndChangeCheck())
            Repaint();

        llmScriptSO.ApplyModifiedProperties();
    }
}
