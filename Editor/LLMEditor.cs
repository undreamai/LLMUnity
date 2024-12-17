using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LLMUnity
{
    [CustomEditor(typeof(LLM), true)]
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
        static List<string> modelNames;
        static List<string> modelOptions;
        static List<string> modelLicenses;
        static List<string> modelURLs;
        string elementFocus = "";
        bool showCustomURL = false;
        string customURL = "";
        bool customURLLora = false;
        bool customURLFocus = false;
        bool expandedView = false;

        public void AddSecuritySettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            void AddSSLLoad(string type, Callback<string> setterCallback)
            {
                if (GUILayout.Button("Load SSL " + type, GUILayout.Width(buttonWidth)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string path = EditorUtility.OpenFilePanel("Select a SSL " + type + " file", "", "");
                        if (!string.IsNullOrEmpty(path)) setterCallback(path);
                    };
                }
            }

            void AddSSLInfo(string propertyName, string type, Callback<string> setterCallback)
            {
                string path = llmScriptSO.FindProperty(propertyName).stringValue;
                if (path != "")
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("SSL " + type + " path", path);
                    if (GUILayout.Button(trashIcon, GUILayout.Height(actionColumnWidth), GUILayout.Width(actionColumnWidth))) setterCallback("");
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.LabelField("Server Security Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(llmScriptSO.FindProperty("APIKey"));

            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                AddSSLLoad("certificate", llmScript.SetSSLCert);
                AddSSLLoad("key", llmScript.SetSSLKey);
                EditorGUILayout.EndHorizontal();
                AddSSLInfo("SSLCertPath", "certificate", llmScript.SetSSLCert);
                AddSSLInfo("SSLKeyPath", "key", llmScript.SetSSLKey);
            }
            Space();
        }

        public void AddModelLoadersSettings(SerializedObject llmScriptSO, LLM llmScript)
        {
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            AddModelLoaders(llmScriptSO, llmScript);
            AddModelSettings(llmScriptSO);
        }

        public void AddModelLoaders(SerializedObject llmScriptSO, LLM llmScript)
        {
            if (LLMManager.modelEntries.Count > 0)
            {
                float[] widths = GetColumnWidths(expandedView);
                float listWidth = 2 * ReorderableList.Defaults.padding;
                foreach (float width in widths) listWidth += width + (listWidth == 0 ? 0 : elementPadding);
                EditorGUILayout.BeginHorizontal(GUILayout.Width(listWidth + actionColumnWidth));

                EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
                modelList.DoLayoutList();
                EditorGUILayout.EndVertical();

                Rect expandedRect = GUILayoutUtility.GetRect(actionColumnWidth, modelList.elementHeight + ReorderableList.Defaults.padding);
                expandedRect.y += modelList.GetHeight() - modelList.elementHeight - ReorderableList.Defaults.padding;
                if (GUI.Button(expandedRect, expandedView ? "«" : "»"))
                {
                    expandedView = !expandedView;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            _ = AddLoadButtons();
            bool downloadOnStart = EditorGUILayout.Toggle("Download on Start", LLMManager.downloadOnStart);
            if (downloadOnStart != LLMManager.downloadOnStart) LLMManager.SetDownloadOnStart(downloadOnStart);
        }

        public override void AddModelSettings(SerializedObject llmScriptSO)
        {
            List<Type> attributeClasses = new List<Type> { typeof(ModelAttribute) };
            if (llmScriptSO.FindProperty("advancedOptions").boolValue)
            {
                attributeClasses.Add(typeof(ModelAdvancedAttribute));
                if (LLMUnitySetup.FullLlamaLib) attributeClasses.Add(typeof(ModelExtrasAttribute));
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
            modelOptions = new List<string>(){"Download model", "Custom URL"};
            modelNames = new List<string>(){null, null};
            modelURLs = new List<string>(){null, null};
            modelLicenses = new List<string>(){null, null};
            foreach (var entry in LLMUnitySetup.modelOptions)
            {
                string category = entry.Key;
                foreach ((string name, string url, string license) in entry.Value)
                {
                    if (url != null && existingOptions.Contains(url)) continue;
                    modelOptions.Add(category + "/" + name);
                    modelNames.Add(name);
                    modelURLs.Add(url);
                    modelLicenses.Add(license);
                }
            }
        }

        float[] GetColumnWidths(bool expandedView)
        {
            List<float> widths = new List<float>(){actionColumnWidth, nameColumnWidth, templateColumnWidth};
            if (expandedView) widths.AddRange(new List<float>(){textColumnWidth, textColumnWidth});
            widths.AddRange(new List<float>(){includeInBuildColumnWidth, actionColumnWidth});
            return widths.ToArray();
        }

        List<Rect> CreateColumnRects(Rect rect)
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

        void SetModelIfNone(string filename, bool lora)
        {
            LLM llmScript = (LLM)target;
            int num = LLMManager.Num(lora);
            if (!lora && llmScript.model == "" && num == 1) llmScript.SetModel(filename);
            if (lora) llmScript.AddLora(filename);
        }

        async Task createCustomURLField()
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
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enter URL", GUILayout.Width(100));
                GUI.SetNextControlName("customURLFocus");
                customURL = EditorGUILayout.TextField(customURL, GUILayout.Width(buttonWidth));
                submit = GUILayout.Button("Submit", GUILayout.Width(buttonWidth / 2));
                exit = GUILayout.Button("Back", GUILayout.Width(buttonWidth / 2));
                EditorGUILayout.EndHorizontal();

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
                    string filename = await LLMManager.Download(customURL, customURLLora, true);
                    SetModelIfNone(filename, customURLLora);
                    UpdateModels(true);
                }
            }
        }

        async Task createButtons()
        {
            LLM llmScript = (LLM)target;
            EditorGUILayout.BeginHorizontal();

            GUIStyle centeredPopupStyle = new GUIStyle(EditorStyles.popup);
            centeredPopupStyle.alignment = TextAnchor.MiddleCenter;
            int modelIndex = EditorGUILayout.Popup(0, modelOptions.ToArray(), centeredPopupStyle, GUILayout.Width(buttonWidth));
            if (modelIndex == 1)
            {
                showCustomURLField(false);
            }
            else if (modelIndex > 1)
            {
                if (modelLicenses[modelIndex] != null) LLMUnitySetup.LogWarning($"The {modelNames[modelIndex]} model is released under the following license: {modelLicenses[modelIndex]}. By using this model, you agree to the terms of the license.");
                string filename = await LLMManager.DownloadModel(modelURLs[modelIndex], true, modelNames[modelIndex]);
                SetModelIfNone(filename, false);
                UpdateModels(true);
            }

            if (GUILayout.Button("Load model", GUILayout.Width(buttonWidth)))
            {
                EditorApplication.delayCall += () =>
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf model file", "", new string[] { "Model Files", "gguf" });
                    if (!string.IsNullOrEmpty(path))
                    {
                        string filename = LLMManager.LoadModel(path, true);
                        SetModelIfNone(filename, false);
                        UpdateModels();
                    }
                };
            }
            EditorGUILayout.EndHorizontal();

            if (llmScript.advancedOptions)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Download LoRA", GUILayout.Width(buttonWidth)))
                {
                    showCustomURLField(true);
                }
                if (GUILayout.Button("Load LoRA", GUILayout.Width(buttonWidth)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string path = EditorUtility.OpenFilePanelWithFilters("Select a gguf lora file", "", new string[] { "Model Files", "gguf" });
                        if (!string.IsNullOrEmpty(path))
                        {
                            string filename = LLMManager.LoadLora(path, true);
                            SetModelIfNone(filename, true);
                            UpdateModels();
                        }
                    };
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        async Task AddLoadButtons()
        {
            if (showCustomURL) await createCustomURLField();
            else await createButtons();
        }

        void OnEnable()
        {
            LLM llmScript = (LLM)target;
            ResetModelOptions();
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

                    List<Rect> rects = CreateColumnRects(rect);
                    int col = 0;
                    Rect selectRect = rects[col++];
                    Rect nameRect = rects[col++];
                    Rect templateRect = rects[col++];
                    Rect urlRect = new Rect();
                    Rect pathRect = new Rect();
                    if (expandedView)
                    {
                        urlRect = rects[col++];
                        pathRect = rects[col++];
                    }
                    Rect includeInBuildRect = rects[col++];
                    Rect actionRect = rects[col++];

                    bool hasPath = entry.path != null && entry.path != "";
                    bool hasURL = entry.url != null && entry.url != "";

                    bool isSelected = false;
                    if (!entry.lora)
                    {
                        isSelected = llmScript.model == entry.filename;
                        bool newSelected = EditorGUI.Toggle(selectRect, isSelected, EditorStyles.radioButton);
                        if (newSelected && !isSelected) llmScript.SetModel(entry.filename);
                    }
                    else
                    {
                        isSelected = llmScript.loraManager.Contains(entry.filename);
                        bool newSelected = EditorGUI.Toggle(selectRect, isSelected);
                        if (newSelected && !isSelected) llmScript.AddLora(entry.filename);
                        else if (!newSelected && isSelected) llmScript.RemoveLora(entry.filename);
                    }

                    DrawCopyableLabel(nameRect, entry.label, entry.filename);

                    if (!entry.lora)
                    {
                        string[] templateDescriptions = ChatTemplate.templatesDescription.Keys.ToList().ToArray();
                        string[] templates = ChatTemplate.templatesDescription.Values.ToList().ToArray();
                        int templateIndex = Array.IndexOf(templates, entry.chatTemplate);
                        int newTemplateIndex = EditorGUI.Popup(templateRect, templateIndex, templateDescriptions);
                        if (newTemplateIndex != templateIndex)
                        {
                            LLMManager.SetTemplate(entry.filename, templates[newTemplateIndex]);
                            UpdateModels();
                        }
                    }

                    if (expandedView)
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
                                LLMManager.SetURL(entry, newURL);
                                UpdateModels();
                            }
                        }
                        DrawCopyableLabel(pathRect, entry.path);
                    }

                    bool includeInBuild = EditorGUI.ToggleLeft(includeInBuildRect, "", entry.includeInBuild);
                    if (includeInBuild != entry.includeInBuild)
                    {
                        LLMManager.SetIncludeInBuild(entry, includeInBuild);
                        UpdateModels();
                    }

                    if (GUI.Button(actionRect, trashIcon))
                    {
                        if (isSelected)
                        {
                            if (!entry.lora) llmScript.SetModel("");
                            else llmScript.RemoveLora(entry.filename);
                        }
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
                    List<Rect> rects = CreateColumnRects(rect);
                    int col = 0;
                    EditorGUI.LabelField(rects[col++], "");
                    EditorGUI.LabelField(rects[col++], "Model");
                    EditorGUI.LabelField(rects[col++], "Chat template");
                    if (expandedView)
                    {
                        EditorGUI.LabelField(rects[col++], "URL");
                        EditorGUI.LabelField(rects[col++], "Path");
                    }
                    EditorGUI.LabelField(rects[col++], "Build");
                    EditorGUI.LabelField(rects[col++], "");
                },
                drawFooterCallback = {},
                footerHeight = 0,
            };
        }

        private void DrawCopyableLabel(Rect rect, string label, string text = "")
        {
            if (text == "") text = label;
            EditorGUI.LabelField(rect, label);
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

        public void AddExtrasToggle()
        {
            if (ToggleButton("Use extras", LLMUnitySetup.FullLlamaLib)) LLMUnitySetup.SetFullLlamaLib(!LLMUnitySetup.FullLlamaLib);
        }

        public override void AddOptionsToggles(SerializedObject llmScriptSO)
        {
            AddDebugModeToggle();

            EditorGUILayout.BeginHorizontal();
            AddAdvancedOptionsToggle(llmScriptSO);
            AddExtrasToggle();
            EditorGUILayout.EndHorizontal();
            Space();
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
            if (llmScriptSO.FindProperty("remote").boolValue) AddSecuritySettings(llmScriptSO, llmScript);
            AddModelLoadersSettings(llmScriptSO, llmScript);
            AddChatSettings(llmScriptSO);

            OnInspectorGUIEnd(llmScriptSO);
        }
    }
}
