using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [Serializable]
    public class ModelEntry
    {
        public string label;
        public string filename;
        public string path;
        public bool lora;
        public string chatTemplate;
        public string url;
        public bool includeInBuild;
        public int contextLength;

        public static string GetFilenameOrRelativeAssetPath(string path)
        {
            string assetPath = LLMUnitySetup.GetAssetPath(path); // Note: this will return the full path if a full path is passed
            string basePath = LLMUnitySetup.GetAssetPath();
            if (File.Exists(assetPath) && LLMUnitySetup.IsSubPath(assetPath, basePath))
            {
                return LLMUnitySetup.RelativePath(assetPath, basePath);
            }
            return Path.GetFileName(path);
        }

        public ModelEntry(string path, bool lora = false, string label = null, string url = null)
        {
            filename = GetFilenameOrRelativeAssetPath(path);
            this.label = label == null ? Path.GetFileName(filename) : label;
            this.lora = lora;
            this.path = LLMUnitySetup.GetFullPath(path);
            this.url = url;
            includeInBuild = true;
            chatTemplate = null;
            contextLength = -1;
            if (!lora)
            {
                GGUFReader reader = new GGUFReader(this.path);
                chatTemplate = ChatTemplate.FromGGUF(reader, this.path);
                string arch = reader.GetStringField("general.architecture");
                if (arch != null) contextLength = reader.GetIntField($"{arch}.context_length");
            }
        }

        public ModelEntry OnlyRequiredFields()
        {
            ModelEntry entry = (ModelEntry)MemberwiseClone();
            entry.label = null;
            entry.path = entry.filename;
            return entry;
        }
    }

    [Serializable]
    public class LLMManagerStore
    {
        public bool downloadOnStart;
        public List<ModelEntry> modelEntries;
    }

    [DefaultExecutionOrder(-2)]
    public class LLMManager
    {
        public static bool downloadOnStart = false;
        public static List<ModelEntry> modelEntries = new List<ModelEntry>();
        static List<LLM> llms = new List<LLM>();

        public static float downloadProgress = 1;
        public static List<Callback<float>> downloadProgressCallbacks = new List<Callback<float>>();
        static Task<bool> SetupTask;
        static readonly object lockObject = new object();
        static long totalSize;
        static long currFileSize;
        static long completedSize;

        public static void SetDownloadProgress(float progress)
        {
            downloadProgress = (completedSize + progress * currFileSize) / totalSize;
            foreach (Callback<float> downloadProgressCallback in downloadProgressCallbacks) downloadProgressCallback?.Invoke(downloadProgress);
        }

        public static Task<bool> Setup()
        {
            lock (lockObject)
            {
                if (SetupTask == null) SetupTask = SetupOnce();
            }
            return SetupTask;
        }

        public static async Task<bool> SetupOnce()
        {
            await LLMUnitySetup.AndroidExtractAsset(LLMUnitySetup.LLMManagerPath, true);
            LoadFromDisk();

            List<StringPair> downloads = new List<StringPair>();
            foreach (ModelEntry modelEntry in modelEntries)
            {
                string target = LLMUnitySetup.GetAssetPath(modelEntry.filename);
                if (File.Exists(target)) continue;

                if (!downloadOnStart || string.IsNullOrEmpty(modelEntry.url))
                {
                    await LLMUnitySetup.AndroidExtractFile(modelEntry.filename);
                    if (!File.Exists(target)) LLMUnitySetup.LogError($"Model {modelEntry.filename} could not be found!");
                }
                else
                {
                    downloads.Add(new StringPair {source = modelEntry.url, target = target});
                }
            }
            if (downloads.Count == 0) return true;

            try
            {
                downloadProgress = 0;
                totalSize = 0;
                completedSize = 0;

                ResumingWebClient client = new ResumingWebClient();
                Dictionary<string, long> fileSizes = new Dictionary<string, long>();
                foreach (StringPair pair in downloads)
                {
                    long size = client.GetURLFileSize(pair.source);
                    fileSizes[pair.source] = size;
                    totalSize += size;
                }

                foreach (StringPair pair in downloads)
                {
                    currFileSize = fileSizes[pair.source];
                    await LLMUnitySetup.DownloadFile(pair.source, pair.target, false, null, SetDownloadProgress);
                    await LLMUnitySetup.AndroidExtractFile(Path.GetFileName(pair.target));
                    completedSize += currFileSize;
                }

                completedSize = totalSize;
                SetDownloadProgress(0);
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Error downloading the models: {ex.Message}");
                return false;
            }
            return true;
        }

        public static void SetTemplate(string filename, string chatTemplate)
        {
            SetTemplate(Get(filename), chatTemplate);
        }

        public static void SetTemplate(ModelEntry entry, string chatTemplate)
        {
            if (entry == null) return;
            entry.chatTemplate = chatTemplate;
            foreach (LLM llm in llms)
            {
                if (llm != null && llm.model == entry.filename) llm.SetTemplate(chatTemplate);
            }
#if UNITY_EDITOR
            Save();
#endif
        }

        public static ModelEntry Get(string path)
        {
            string filename = Path.GetFileName(path);
            string fullPath = LLMUnitySetup.GetFullPath(path);
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.filename == filename || entry.path == fullPath) return entry;
            }
            return null;
        }

        public static string GetAssetPath(string filename)
        {
            ModelEntry entry = Get(filename);
            if (entry == null) return "";
#if UNITY_EDITOR
            return entry.path;
#else
            return LLMUnitySetup.GetAssetPath(entry.filename);
#endif
        }

        public static int Num(bool lora)
        {
            int num = 0;
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.lora == lora) num++;
            }
            return num;
        }

        public static int NumModels()
        {
            return Num(false);
        }

        public static int NumLoras()
        {
            return Num(true);
        }

        public static void Register(LLM llm)
        {
            llms.Add(llm);
        }

        public static void Unregister(LLM llm)
        {
            llms.Remove(llm);
        }

        public static void LoadFromDisk()
        {
            if (!File.Exists(LLMUnitySetup.LLMManagerPath)) return;
            LLMManagerStore store = JsonUtility.FromJson<LLMManagerStore>(File.ReadAllText(LLMUnitySetup.LLMManagerPath));
            downloadOnStart = store.downloadOnStart;
            modelEntries = store.modelEntries;
        }

#if UNITY_EDITOR
        static string LLMManagerPref = "LLMManager";

        [HideInInspector] public static float modelProgress = 1;
        [HideInInspector] public static float loraProgress = 1;

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Load();
        }

        public static string AddEntry(ModelEntry entry)
        {
            int indexToInsert = modelEntries.Count;
            if (!entry.lora)
            {
                if (modelEntries.Count > 0 && modelEntries[0].lora) indexToInsert = 0;
                else
                {
                    for (int i = modelEntries.Count - 1; i >= 0; i--)
                    {
                        if (!modelEntries[i].lora)
                        {
                            indexToInsert = i + 1;
                            break;
                        }
                    }
                }
            }
            modelEntries.Insert(indexToInsert, entry);
            Save();
            return entry.filename;
        }

        public static string AddEntry(string path, bool lora = false, string label = null, string url = null)
        {
            return AddEntry(new ModelEntry(path, lora, label, url));
        }

        public static async Task<string> Download(string url, bool lora = false, bool log = false, string label = null)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.url == url)
                {
                    if (log) LLMUnitySetup.Log($"Found existing entry for {url}");
                    return entry.filename;
                }
            }

            string modelName = Path.GetFileName(url).Split("?")[0];
            ModelEntry entryPath = Get(modelName);
            if (entryPath != null)
            {
                if (log) LLMUnitySetup.Log($"Found existing entry for {modelName}");
                return entryPath.filename;
            }

            string modelPath = Path.Combine(LLMUnitySetup.modelDownloadPath, modelName);
            float preModelProgress = modelProgress;
            float preLoraProgress = loraProgress;
            try
            {
                if (!lora)
                {
                    modelProgress = 0;
                    await LLMUnitySetup.DownloadFile(url, modelPath, false, null, SetModelProgress);
                }
                else
                {
                    loraProgress = 0;
                    await LLMUnitySetup.DownloadFile(url, modelPath, false, null, SetLoraProgress);
                }
            }
            catch (Exception ex)
            {
                modelProgress = preModelProgress;
                loraProgress = preLoraProgress;
                LLMUnitySetup.LogError($"Error downloading the model from URL '{url}': " + ex.Message);
                return null;
            }
            return AddEntry(modelPath, lora, label, url);
        }

        public static string Load(string path, bool lora = false, bool log = false, string label = null)
        {
            ModelEntry entry = Get(path);
            if (entry != null)
            {
                if (log) LLMUnitySetup.Log($"Found existing entry for {entry.filename}");
                return entry.filename;
            }
            return AddEntry(path, lora, label);
        }

        public static async Task<string> DownloadModel(string url, bool log = false, string label = null)
        {
            return await Download(url, false, log, label);
        }

        public static async Task<string> DownloadLora(string url, bool log = false, string label = null)
        {
            return await Download(url, true, log, label);
        }

        public static string LoadModel(string path, bool log = false, string label = null)
        {
            return Load(path, false, log, label);
        }

        public static string LoadLora(string path, bool log = false, string label = null)
        {
            return Load(path, true, log, label);
        }

        public static void SetURL(string filename, string url)
        {
            SetURL(Get(filename), url);
        }

        public static void SetURL(ModelEntry entry, string url)
        {
            if (entry == null) return;
            entry.url = url;
            Save();
        }

        public static void SetIncludeInBuild(string filename, bool includeInBuild)
        {
            SetIncludeInBuild(Get(filename), includeInBuild);
        }

        public static void SetIncludeInBuild(ModelEntry entry, bool includeInBuild)
        {
            if (entry == null) return;
            entry.includeInBuild = includeInBuild;
            Save();
        }

        public static void SetDownloadOnStart(bool value)
        {
            downloadOnStart = value;
            if (downloadOnStart)
            {
                bool warn = false;
                foreach (ModelEntry entry in modelEntries)
                {
                    if (entry.url == null || entry.url == "") warn = true;
                }
                if (warn) LLMUnitySetup.LogWarning("Some models do not have a URL and will be copied in the build. To resolve this fill in the URL field in the expanded view of the LLM Model list.");
            }
            Save();
        }

        public static void Remove(string filename)
        {
            Remove(Get(filename));
        }

        public static void Remove(ModelEntry entry)
        {
            if (entry == null) return;
            modelEntries.Remove(entry);
            Save();
            foreach (LLM llm in llms)
            {
                if (!entry.lora && llm.model == entry.filename) llm.model = "";
                else if (entry.lora) llm.RemoveLora(entry.filename);
            }
        }

        public static void SetModelProgress(float progress)
        {
            modelProgress = progress;
        }

        public static void SetLoraProgress(float progress)
        {
            loraProgress = progress;
        }

        public static void Save()
        {
            string json = JsonUtility.ToJson(new LLMManagerStore { modelEntries = modelEntries, downloadOnStart = downloadOnStart }, true);
            PlayerPrefs.SetString(LLMManagerPref, json);
            PlayerPrefs.Save();
        }

        public static void Load()
        {
            string pref = PlayerPrefs.GetString(LLMManagerPref);
            if (pref == null || pref == "") return;
            LLMManagerStore store = JsonUtility.FromJson<LLMManagerStore>(pref);
            downloadOnStart = store.downloadOnStart;
            modelEntries = store.modelEntries;
        }

        public static void SaveToDisk()
        {
            List<ModelEntry> modelEntriesBuild = new List<ModelEntry>();
            foreach (ModelEntry modelEntry in modelEntries)
            {
                if (!modelEntry.includeInBuild) continue;
                modelEntriesBuild.Add(modelEntry.OnlyRequiredFields());
            }
            string json = JsonUtility.ToJson(new LLMManagerStore { modelEntries = modelEntriesBuild, downloadOnStart = downloadOnStart }, true);
            File.WriteAllText(LLMUnitySetup.LLMManagerPath, json);
        }

        public static void Build(ActionCallback copyCallback)
        {
            SaveToDisk();

            foreach (ModelEntry modelEntry in modelEntries)
            {
                string target = LLMUnitySetup.GetAssetPath(modelEntry.filename);
                if (!modelEntry.includeInBuild || File.Exists(target)) continue;
                if (!downloadOnStart || string.IsNullOrEmpty(modelEntry.url)) copyCallback(modelEntry.path, target);
            }
        }

#endif
    }
}
