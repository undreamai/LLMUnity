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
        static float templateColumnWidth = 100f;
        static float textColumnWidth = 150f;
        static float includeInBuildColumnWidth = 50f;
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
            float[] widths = GetColumnWidths();
            float listWidth = ReorderableList.Defaults.dragHandleWidth;
            foreach (float width in widths) listWidth += width + (listWidth == 0 ? 0 : elementPadding);
            EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
            modelList.DoLayoutList();
            bool downloadOnBuild = EditorGUILayout.Toggle("Download on Build", LLMManager.downloadOnBuild);
            if (downloadOnBuild != LLMManager.downloadOnBuild)
            {
                LLMManager.downloadOnBuild = downloadOnBuild;
                LLMManager.Save();
            }
            EditorGUILayout.EndVertical();
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

        float[] GetColumnWidths()
        {
            float[] widths = new float[] {actionColumnWidth, nameColumnWidth, templateColumnWidth, textColumnWidth, textColumnWidth, includeInBuildColumnWidth, actionColumnWidth};
            return widths;
        }

        List<Rect> CreateColumnRects(Rect rect)
        {
            float[] widths = GetColumnWidths();
            float offset = rect.x;
            List<Rect> rects = new List<Rect>();
            foreach (float width in widths)
            {
                rects.Add(new Rect(offset, rect.y, width, EditorGUIUtility.singleLineHeight));
                offset += width + elementPadding;
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

        async Task createCustomURLField(Rect rect)
        {
            bool submit;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                submit = true;
                e.Use();
            }
            else
            {
                Rect labelRect = new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight);
                Rect textRect = new Rect(rect.x + labelRect.width + elementPadding, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
                Rect submitRect = new Rect(rect.x + labelRect.width + buttonWidth + elementPadding * 2 , rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(labelRect, "Enter URL:");
                GUI.SetNextControlName("customURLFocus");
                customURL = EditorGUI.TextField(textRect, customURL);
                submit = GUI.Button(submitRect, "Submit");

                if (customURLFocus)
                {
                    customURLFocus = false;
                    elementFocus = "customURLFocus";
                }
            }

            if (submit)
            {
                showCustomURL = false;
                elementFocus = "dummy";
                Repaint();
                await LLMManager.Download(customURL, customURLLora);
                UpdateModels(true);
            }
        }

        async Task createButtons(Rect rect, LLM llmScript)
        {
            Rect downloadModelRect = new Rect(rect.x, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect loadModelRect = new Rect(rect.x + buttonWidth + elementPadding, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect downloadLoraRect = new Rect(rect.width - 2 * buttonWidth - elementPadding, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect loadLoraRect = new Rect(rect.width - buttonWidth, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);

            int modelIndex = EditorGUI.Popup(downloadModelRect, 0, modelOptions.ToArray());
            if (modelIndex == 1)
            {
                showCustomURLField(false);
            }
            else if (modelIndex > 1)
            {
                await LLMManager.DownloadModel(modelURLs[modelIndex], modelOptions[modelIndex]);
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

        void OnEnable()
        {
            LLM llmScript = (LLM)target;
            ResetModelOptions();
            templateOptions = ChatTemplate.templatesDescription.Keys.ToList().ToArray();
            trashIcon = new GUIContent(Resources.Load<Texture2D>("llmunity_trash_icon"), "Delete Model");
            modelList = new ReorderableList(LLMManager.modelEntries, typeof(ModelEntry), false, true, false, false)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index >= LLMManager.modelEntries.Count) return;

                    List<Rect> rects = CreateColumnRects(rect);
                    var selectRect = rects[0];
                    var nameRect = rects[1];
                    var templateRect = rects[2];
                    var urlRect = rects[3];
                    var pathRect = rects[4];
                    var includeInBuildRect = rects[5];
                    var actionRect = rects[6];
                    var entry = LLMManager.modelEntries[index];

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
                },
                drawHeaderCallback = (rect) =>
                {
                    List<Rect> rects = CreateColumnRects(rect);
                    EditorGUI.LabelField(rects[0], "");
                    EditorGUI.LabelField(rects[1], "Model");
                    EditorGUI.LabelField(rects[2], "Chat template");
                    EditorGUI.LabelField(rects[3], "URL");
                    EditorGUI.LabelField(rects[4], "Path");
                    EditorGUI.LabelField(rects[5], "Build");
                    EditorGUI.LabelField(rects[6], "");
                },
                drawFooterCallback = async(rect) =>
                {
                    if (showCustomURL) await createCustomURLField(rect);
                    else await createButtons(rect, llmScript);
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
