using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM))]
    public class LLMEditor : PropertyEditor
    {
        private ReorderableList modelList;
        static float nameColumnWidth = 250f;
        static float textColumnWidth = 150f;
        static float includeInBuildColumnWidth = 50f;
        static float actionColumnWidth = 30f;
        static int elementPadding = 10;
        static GUIContent trashIcon;
        static List<string> modelOptions;
        static List<string> modelURLs;

        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLM) };
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
            modelList.DoLayoutList();
            string[] templateOptions = ChatTemplate.templatesDescription.Keys.ToList().ToArray();
            int index = Array.IndexOf(ChatTemplate.templatesDescription.Values.ToList().ToArray(), llmScript.chatTemplate);
            int newIndex = EditorGUILayout.Popup("Chat Template", index, templateOptions);
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
            List<Type> excludeAttributeClasses = new List<Type> { typeof(ModelDownloadAttribute), typeof(ModelDownloadAdvancedAttribute) };
            if (llmScriptSO.FindProperty("downloadOnBuild").boolValue) excludeAttributeClasses.Remove(typeof(ModelDownloadAttribute));
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelAdvancedAttribute));
                if (llmScriptSO.FindProperty("downloadOnBuild").boolValue) excludeAttributeClasses.Remove(typeof(ModelDownloadAdvancedAttribute));
            }
            ShowPropertiesOfClass("", llmScriptSO, attributeClasses, false, excludeAttributeClasses);
            Space();
        }

        void ShowProgress(float progress, string progressText)
        {
            if (progress != 1) EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, progressText);
        }

        static void ResetModelOptions()
        {
            List<string> existingOptions = new List<string>();
            foreach (ModelEntry entry in LLMManager.modelEntries) existingOptions.Add(entry.url);
            modelOptions = new List<string>();
            modelURLs = new List<string>();
            for (int i = 0; i < LLMUnitySetup.modelOptions.Length; i++)
            {
                string url = LLMUnitySetup.modelOptions[i].Item2;
                if (existingOptions.Contains(url)) continue;
                modelOptions.Add(LLMUnitySetup.modelOptions[i].Item1);
                modelURLs.Add(url);
            }
        }

        List<float[]> getColumnPositions(float offsetX)
        {
            List<float> offsets = new List<float>();
            float[] widths = new float[] {actionColumnWidth, nameColumnWidth, textColumnWidth, textColumnWidth, includeInBuildColumnWidth};
            float offset = offsetX;
            foreach (float width in widths)
            {
                offsets.Add(offset);
                offset += width + elementPadding;
            }
            return new List<float[]>(){offsets.ToArray(), widths};
        }

        void UpdateModels(bool resetOptions = false)
        {
            LLMManager.Save();
            if (resetOptions) ResetModelOptions();
            Repaint();
        }

        void OnEnable()
        {
            ResetModelOptions();
            trashIcon = new GUIContent(Resources.Load<Texture2D>("llmunity_trash_icon"), "Delete Model");

            modelList = new ReorderableList(LLMManager.modelEntries, typeof(ModelEntry), true, true, true, true)
            {
                drawElementCallback = async(rect, index, isActive, isFocused) =>
                {
                    if (index >= LLMManager.modelEntries.Count) return;

                    List<float[]> positions = getColumnPositions(rect.x);
                    float[] offsets = positions[0];
                    float[] widths = positions[1];
                    var actionRect = new Rect(offsets[0], rect.y, widths[0], EditorGUIUtility.singleLineHeight);
                    var nameRect = new Rect(offsets[1], rect.y, widths[1], EditorGUIUtility.singleLineHeight);
                    var urlRect = new Rect(offsets[2], rect.y, widths[2], EditorGUIUtility.singleLineHeight);
                    var pathRect = new Rect(offsets[3], rect.y, widths[3], EditorGUIUtility.singleLineHeight);
                    var includeInBuildRect = new Rect(offsets[4], rect.y, widths[4], EditorGUIUtility.singleLineHeight);
                    var entry = LLMManager.modelEntries[index];

                    bool hasPath = entry.localPath != null && entry.localPath != "";
                    bool hasURL = entry.url != null && entry.url != "";

                    if (GUI.Button(actionRect, trashIcon))
                    {
                        LLMManager.modelEntries.Remove(entry);
                        UpdateModels(true);
                    }

                    DrawCopyableLabel(nameRect, entry.name);

                    if (hasURL)
                    {
                        DrawCopyableLabel(urlRect, entry.url);
                    }
                    else if (hasPath)
                    {
                        string newURL = EditorGUI.TextField(urlRect, entry.url);
                        if (newURL != entry.url)
                        {
                            entry.url = newURL;
                            UpdateModels();
                        }
                    }
                    else
                    {
                        urlRect.width = buttonWidth;
                        int newIndex = EditorGUI.Popup(urlRect, 0, modelOptions.ToArray());
                        if (newIndex != 0)
                        {
                            await LLMManager.DownloadModel(entry, modelURLs[newIndex], modelOptions[newIndex]);
                            UpdateModels(true);
                        }
                    }

                    if (hasPath)
                    {
                        DrawCopyableLabel(pathRect, entry.localPath);
                    }
                    else
                    {
                        pathRect.width = buttonWidth;
                        if (GUI.Button(pathRect, "Load model"))
                        {
                            EditorApplication.delayCall += () =>
                            {
                                string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                                if (!string.IsNullOrEmpty(path))
                                {
                                    entry.localPath = path;
                                    entry.name = LLMManager.ModelPathToName(path);
                                    UpdateModels();
                                }
                            };
                        }
                    }

                    bool includeInBuild = EditorGUI.ToggleLeft(includeInBuildRect, "", entry.includeInBuild);
                    if (includeInBuild != entry.includeInBuild)
                    {
                        entry.includeInBuild = includeInBuild;
                        UpdateModels();
                    }
                },
                drawHeaderCallback = (rect) =>
                {
                    List<float[]> positions = getColumnPositions(rect.x + ReorderableList.Defaults.dragHandleWidth - ReorderableList.Defaults.padding + 1);
                    float[] offsets = positions[0];
                    float[] widths = positions[1];
                    EditorGUI.LabelField(new Rect(offsets[0], rect.y, widths[0], EditorGUIUtility.singleLineHeight), "");
                    EditorGUI.LabelField(new Rect(offsets[1], rect.y, widths[1], EditorGUIUtility.singleLineHeight), "Model");
                    EditorGUI.LabelField(new Rect(offsets[2], rect.y, widths[2], EditorGUIUtility.singleLineHeight), "URL");
                    EditorGUI.LabelField(new Rect(offsets[3], rect.y, widths[3], EditorGUIUtility.singleLineHeight), "Local Path");
                    EditorGUI.LabelField(new Rect(offsets[4], rect.y, widths[4], EditorGUIUtility.singleLineHeight), "Build");
                }
            };
        }

        private void DrawCopyableLabel(Rect rect, string text)
        {
            EditorGUI.LabelField(rect, text);
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy"), false, () => CopyToClipboard(text));
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private void CopyToClipboard(string text)
        {
            TextEditor te = new TextEditor
            {
                text = text
            };
            te.SelectAll();
            te.Copy();
        }

        public override void OnInspectorGUI()
        {
            LLM llmScript = (LLM)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);

            OnInspectorGUIStart(llmScriptSO);

            ShowProgress(LLMUnitySetup.libraryProgress, "Setup Library");
            ShowProgress(llmScript.modelProgress, "Model Downloading");
            ShowProgress(llmScript.modelCopyProgress, "Model Copying");

            GUI.enabled = LLMUnitySetup.libraryProgress == 1 && llmScript.modelProgress == 1 && llmScript.modelCopyProgress == 1;
            AddOptionsToggles(llmScriptSO);
            AddSetupSettings(llmScriptSO);
            AddModelLoadersSettings(llmScriptSO, llmScript);
            GUI.enabled = true;
            AddChatSettings(llmScriptSO);

            OnInspectorGUIEnd(llmScriptSO);
        }
    }
}
