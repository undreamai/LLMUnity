using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LLM))]
public class LLMEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        LLM llmScript = (LLM)target;

        if (!llmScript.checkSetup())
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Setup server", GUILayout.Width(150)))
            {
                llmScript.runSetup();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties(); // Apply changes to serializedObject

            Repaint();
        }
    }
}
