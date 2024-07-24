using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM))]
    public class LLMEditor : PropertyEditor
    {
        private ReorderableList modelList;
        static float nameColumnWidth = 150f;
        static float templateColumnWidth = 150f;
        static float textColumnWidth = 150f;
        static float includeInBuildColumnWidth = 30f;
        static float actionColumnWidth = 20f;
        static int elementPadding = 10;
        static GUIContent trashIcon;
        static List<string> modelOptions;
        static List<string> modelURLs;
        string[] templateOptions;
        string elementFocus = "";
        bool showCustomURL = false;
        string customURL = "";
        bool customURLLora = false;
        bool customURLFocus = false;

        protected override Type[] GetPropertyTypes()
        {
            return new Type[] { typeof(LLM) };
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
            AddModelSettings(llmScriptSO);
        }

        public void AddModelLoaders(SerializedObject llmScriptSO, LLM llmScript)
        {
            if (LLMManager.modelEntries.Count == 0)
            {
                DrawFooter(EditorGUILayout.GetControlRect());
            }
            else
            {
                float[] widths = GetColumnWidths(llmScript.advancedOptions);
                float listWidth = 2 * ReorderableList.Defaults.padding * 2;
                foreach (float width in widths) listWidth += width + (listWidth == 0 ? 0 : elementPadding);
                EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
                modelList.DoLayoutList();
                EditorGUILayout.EndVertical();
            }
            bool downloadOnStart = EditorGUILayout.Toggle("Download on Start", LLMManager.downloadOnStart);
            if (downloadOnStart != LLMManager.downloadOnStart)
            {
                LLMManager.downloadOnStart = downloadOnStart;
                LLMManager.Save();
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
            foreach ((string name, string url) in LLMUnitySetup.modelOptions)
            {
                if (url != null && existingOptions.Contains(url)) continue;
                modelOptions.Add(name);
                modelURLs.Add(url);
            }
        }

        float[] GetColumnWidths(bool expandedView)
        {
            List<float> widths = new List<float>(){actionColumnWidth, nameColumnWidth, templateColumnWidth};
            if (expandedView) widths.AddRange(new List<float>(){textColumnWidth, textColumnWidth});
            widths.AddRange(new List<float>(){includeInBuildColumnWidth, actionColumnWidth});
            return widths.ToArray();
        }

        List<Rect> CreateColumnRects(Rect rect, bool expandedView)
        {
            float[] widths = GetColumnWidths(expandedView);
            float offsetX = rect.x;
            float offsetY = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2;
            List<Rect> rects = new List<Rect>();
            foreach (float width in widths)
            {
                rects.Add(new Rect(offsetX, offsetY, width, EditorGUIUtility.singleLineHeight));
                offsetX += width + elementPadding;
            }
            return rects;
        }

        void UpdateModels(bool resetOptions = false)
        {
            LLMManager.Save();
            if (resetOptions) ResetModelOptions();
            Repaint();
        }

        void showCustomURLField(bool lora)
        {
            customURL = "";
            customURLLora = lora;
            showCustomURL = true;
            customURLFocus = true;
            Repaint();
        }

        void SetModelIfNone()
        {
            LLM llmScript = (LLM)target;
            if (llmScript.model == "" && LLMManager.modelEntries.Count == 1) llmScript.SetModel(LLMManager.modelEntries[0].localPath);
        }

        async Task createCustomURLField(Rect rect)
        {
            bool submit = false;
            bool exit = false;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                submit = true;
                e.Use();
            }
            else if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape))
            {
                exit = true;
                e.Use();
            }
            else
            {
                Rect labelRect = new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight);
                Rect textRect = new Rect(rect.x + labelRect.width + elementPadding, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
                Rect submitRect = new Rect(rect.x + labelRect.width + buttonWidth + elementPadding * 2, rect.y, buttonWidth / 2f, EditorGUIUtility.singleLineHeight);
                Rect backRect = new Rect(rect.x + labelRect.width + buttonWidth * 1.5f + elementPadding * 3, rect.y, buttonWidth / 2f, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(labelRect, "Enter URL:");
                GUI.SetNextControlName("customURLFocus");
                customURL = EditorGUI.TextField(textRect, customURL);
                submit = GUI.Button(submitRect, "Submit");
                exit = GUI.Button(backRect, "Back");

                if (customURLFocus)
                {
                    customURLFocus = false;
                    elementFocus = "customURLFocus";
                }
            }

            if (exit || submit)
            {
                showCustomURL = false;
                elementFocus = "dummy";
                Repaint();
                if (submit && customURL != "")
                {
                    await LLMManager.Download(customURL, customURLLora);
                    SetModelIfNone();
                    UpdateModels(true);
                }
            }
        }

        async Task createButtons(Rect rect, LLM llmScript)
        {
            Rect downloadModelRect = new Rect(rect.x, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect loadModelRect = new Rect(rect.x + buttonWidth + elementPadding, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect downloadLoraRect = new Rect(rect.xMax - 2 * buttonWidth - elementPadding, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect loadLoraRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            int modelIndex = EditorGUI.Popup(downloadModelRect, 0, modelOptions.ToArray());
            if (modelIndex == 1)
            {
                showCustomURLField(false);
            }
            else if (modelIndex > 1)
            {
                await LLMManager.DownloadModel(modelURLs[modelIndex], modelOptions[modelIndex]);
                SetModelIfNone();
                UpdateModels(true);
            }

            if (GUI.Button(loadModelRect, "Load model"))
            {
                EditorApplication.delayCall += () =>
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                    if (!string.IsNullOrEmpty(path))
                    {
                        LLMManager.LoadModel(path);
                        UpdateModels();
                    }
                };
            }

            if (llmScript.advancedOptions)
            {
                if (GUI.Button(downloadLoraRect, "Download LoRA"))
                {
                    showCustomURLField(true);
                }
                if (GUI.Button(loadLoraRect, "Load LoRA"))
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
            }
        }

        async void DrawFooter(Rect rect)
        {
            LLM llmScript = (LLM)target;
            if (showCustomURL) await createCustomURLField(rect);
            else await createButtons(rect, llmScript);
        }

        void OnEnable()
        {
            LLM llmScript = (LLM)target;
            ResetModelOptions();
            templateOptions = ChatTemplate.templatesDescription.Keys.ToList().ToArray();
            trashIcon = new GUIContent(Resources.Load<Texture2D>("llmunity_trash_icon"), "Delete Model");
            Texture2D loraLineTexture = new Texture2D(1, 1);
            loraLineTexture.SetPixel(0, 0, Color.black);
            loraLineTexture.Apply();

            modelList = new ReorderableList(LLMManager.modelEntries, typeof(ModelEntry), false, true, false, false)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index >= LLMManager.modelEntries.Count) return;
                    ModelEntry entry = LLMManager.modelEntries[index];

                    List<Rect> rects = CreateColumnRects(rect, llmScript.advancedOptions);
                    int col = 0;
                    Rect selectRect = rects[col++];
                    Rect nameRect = rects[col++];
                    Rect templateRect = rects[col++];
                    Rect urlRect = new Rect();
                    Rect pathRect = new Rect();
                    if (llmScript.advancedOptions)
                    {
                        urlRect = rects[col++];
                        pathRect = rects[col++];
                    }
                    Rect includeInBuildRect = rects[col++];
                    Rect actionRect = rects[col++];

                    bool hasPath = entry.localPath != null && entry.localPath != "";
                    bool hasURL = entry.url != null && entry.url != "";

                    bool isSelected = false;
                    if (!entry.lora)
                    {
                        isSelected = llmScript.model == entry.localPath;
                        bool newSelected = EditorGUI.Toggle(selectRect, isSelected, EditorStyles.radioButton);
                        if (newSelected && !isSelected) llmScript.SetModel(entry.localPath);
                    }
                    else
                    {
                        isSelected = llmScript.lora == entry.localPath;
                        bool newSelected = EditorGUI.Toggle(selectRect, isSelected, EditorStyles.radioButton);
                        if (newSelected && !isSelected) llmScript.SetLora(entry.localPath);
                        else if (!newSelected && isSelected) llmScript.SetLora("");
                    }

                    DrawCopyableLabel(nameRect, entry.name);

                    if (!entry.lora)
                    {
                        int templateIndex = Array.IndexOf(ChatTemplate.templatesDescription.Values.ToList().ToArray(), entry.chatTemplate);
                        int newTemplateIndex = EditorGUI.Popup(templateRect, templateIndex, templateOptions);
                        if (newTemplateIndex != templateIndex)
                        {
                            entry.chatTemplate = ChatTemplate.templatesDescription[templateOptions[newTemplateIndex]];
                            if (isSelected) llmScript.SetTemplate(entry.chatTemplate);
                            UpdateModels();
                        }
                    }

                    if (llmScript.advancedOptions)
                    {
                        if (hasURL)
                        {
                            DrawCopyableLabel(urlRect, entry.url);
                        }
                        else
                        {
                            string newURL = EditorGUI.TextField(urlRect, entry.url);
                            if (newURL != entry.url)
                            {
                                entry.url = newURL;
                                UpdateModels();
                            }
                        }
                        DrawCopyableLabel(pathRect, entry.localPath);
                    }

                    bool includeInBuild = EditorGUI.ToggleLeft(includeInBuildRect, "", entry.includeInBuild);
                    if (includeInBuild != entry.includeInBuild)
                    {
                        entry.includeInBuild = includeInBuild;
                        UpdateModels();
                    }

                    if (GUI.Button(actionRect, trashIcon))
                    {
                        LLMManager.Remove(entry);
                        UpdateModels(true);
                    }

                    if (!entry.lora && index < LLMManager.modelEntries.Count - 1 && LLMManager.modelEntries[index + 1].lora)
                    {
                        GUI.DrawTexture(new Rect(rect.x - ReorderableList.Defaults.padding, rect.yMax, rect.width + ReorderableList.Defaults.padding * 2, 1), loraLineTexture);
                    }
                },
                drawHeaderCallback = (rect) =>
                {
                    List<Rect> rects = CreateColumnRects(rect, llmScript.advancedOptions);
                    int col = 0;
                    EditorGUI.LabelField(rects[col++], "");
                    EditorGUI.LabelField(rects[col++], "Model");
                    EditorGUI.LabelField(rects[col++], "Chat template");
                    if (llmScript.advancedOptions)
                    {
                        EditorGUI.LabelField(rects[col++], "URL");
                        EditorGUI.LabelField(rects[col++], "Path");
                    }
                    EditorGUI.LabelField(rects[col++], "Build");
                    EditorGUI.LabelField(rects[col++], "");
                },
                drawFooterCallback = DrawFooter,
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
            TextEditor te = new TextEditor {text = text};
            te.SelectAll();
            te.Copy();
        }

        public override void OnInspectorGUI()
        {
            if (elementFocus != "")
            {
                EditorGUI.FocusTextInControl(elementFocus);
                elementFocus = "";
            }

            LLM llmScript = (LLM)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);

            OnInspectorGUIStart(llmScriptSO);

            ShowProgress(LLMUnitySetup.libraryProgress, "Setup Library");
            ShowProgress(LLMManager.modelProgress, "Model Downloading");
            ShowProgress(LLMManager.loraProgress, "LoRA Downloading");
            GUI.enabled = LLMUnitySetup.libraryProgress == 1 && LLMManager.modelProgress == 1 && LLMManager.loraProgress == 1;

            AddOptionsToggles(llmScriptSO);
            AddSetupSettings(llmScriptSO);
            AddModelLoadersSettings(llmScriptSO, llmScript);
            AddChatSettings(llmScriptSO);

            OnInspectorGUIEnd(llmScriptSO);
        }
    }
}
