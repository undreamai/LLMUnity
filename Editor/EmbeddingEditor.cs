using UnityEditor;

namespace LLMUnity
{
    [CustomEditor(typeof(Embedding))]
    public class EmbeddingEditor : Editor
    {

        void ShowProgress(float progress, string progressText)
        {
            if (progress != 1) EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, progressText);
        }

        public override void OnInspectorGUI()
        {
            Embedding embdeddingScript = (Embedding)target;
            SerializedObject embdeddingScriptSO = new SerializedObject(embdeddingScript);
            embdeddingScriptSO.Update();

            embdeddingScriptSO.ApplyModifiedProperties();
            string[] options = new string[embdeddingScript.options.Length];
            for (int i = 0; i < embdeddingScript.options.Length; i++)
            {
                options[i] = embdeddingScript.options[i].Item1;
            }

            int newIndex = EditorGUILayout.Popup("Model", embdeddingScript.SelectedOption, options);
            if (newIndex != embdeddingScript.SelectedOption)
            {
                embdeddingScript.SelectModel(newIndex);
            }
            ShowProgress(embdeddingScript.downloadProgress, "Downloading model");
            EditorGUILayout.PropertyField(embdeddingScriptSO.FindProperty("GPU"));
            embdeddingScriptSO.ApplyModifiedProperties();
        }
    }
}
