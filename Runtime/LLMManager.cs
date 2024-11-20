/// @file
/// @brief File implementing the LLM model manager
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [Serializable]
    /// @ingroup utils
    /// <summary>
    /// Class implementing a LLM model entry
    /// </summary>
    public class ModelEntry
    {
        public string label;
        public string filename;
        public string path;
        public bool lora;
        public string chatTemplate;
        public string url;
        public bool embeddingOnly;
        public int embeddingLength;
        public bool includeInBuild;
        public int contextLength;

        static List<string> embeddingOnlyArchs = new List<string> {"bert", "nomic-bert", "jina-bert-v2", "t5", "t5encoder"};

        /// <summary>
        /// Returns the relative asset path if it is in the AssetPath folder (StreamingAssets or persistentPath), otherwise the filename.
        /// </summary>
        /// <param name="path">asset path</param>
        /// <returns>relative asset path or filename</returns>
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

        /// <summary>
        /// Constructs a LLM model entry
        /// </summary>
        /// <param name="path">model path</param>
        /// <param name="lora">if it is a LORA or LLM</param>
        /// <param name="label">label to show in the model manager in the Editor</param>
        /// <param name="url">model url</param>
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
            embeddingOnly = false;
            embeddingLength = 0;
            if (!lora)
            {
                GGUFReader reader = new GGUFReader(this.path);
                string arch = reader.GetStringField("general.architecture");
                if (arch != null)
                {
                    contextLength = reader.GetIntField($"{arch}.context_length");
                    embeddingLength = reader.GetIntField($"{arch}.embedding_length");
                }
                embeddingOnly = embeddingOnlyArchs.Contains(arch);
                chatTemplate = embeddingOnly ? default : ChatTemplate.FromGGUF(reader, this.path);
            }
        }

        /// <summary>
        /// Returns only the required fields for bundling the model in the build
        /// </summary>
        /// <returns>Adapted model entry</returns>
        public ModelEntry OnlyRequiredFields()
        {
            ModelEntry entry = (ModelEntry)MemberwiseClone();
            entry.label = null;
            entry.path = entry.filename;
            return entry;
        }
    }

    /// \cond HIDE
    [Serializable]
    public class LLMManagerStore
    {
        public bool downloadOnStart;
        public List<ModelEntry> modelEntries;
    }
    /// \endcond

    [DefaultExecutionOrder(-2)]
    /// @ingroup utils
    /// <summary>
    /// Class implementing the LLM model manager
    /// </summary>
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

        /// <summary>
        /// Sets the model download progress in all registered callbacks
        /// </summary>
        /// <param name="progress">model download progress</param>
        public static void SetDownloadProgress(float progress)
        {
            downloadProgress = (completedSize + progress * currFileSize) / totalSize;
            foreach (Callback<float> downloadProgressCallback in downloadProgressCallbacks) downloadProgressCallback?.Invoke(downloadProgress);
        }

        /// <summary>
        /// Setup of the models
        /// </summary>
        /// <returns>bool specifying if the setup was successful</returns>
        public static Task<bool> Setup()
        {
            lock (lockObject)
            {
                if (SetupTask == null) SetupTask = SetupOnce();
            }
            return SetupTask;
        }

        /// <summary>
        /// Task performing the setup of the models
        /// </summary>
        /// <returns>bool specifying if the setup was successful</returns>
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
                    target = LLMUnitySetup.GetDownloadAssetPath(modelEntry.filename);
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

        /// <summary>
        /// Sets the chat template for a model and distributes it to all LLMs using it
        /// </summary>
        /// <param name="filename">model path</param>
        /// <param name="chatTemplate">chat template</param>
        public static void SetTemplate(string filename, string chatTemplate)
        {
            SetTemplate(Get(filename), chatTemplate);
        }

        /// <summary>
        /// Sets the chat template for a model and distributes it to all LLMs using it
        /// </summary>
        /// <param name="entry">model entry</param>
        /// <param name="chatTemplate">chat template</param>
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

        /// <summary>
        /// Gets the model entry for a model path
        /// </summary>
        /// <param name="path">model path</param>
        /// <returns>model entry</returns>
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

        /// <summary>
        /// Gets the asset path based on whether the application runs locally in the editor or in a build
        /// </summary>
        /// <param name="filename">model filename or relative path</param>
        /// <returns>asset path</returns>
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

        /// <summary>
        /// Returns the number of LLM/LORA models
        /// </summary>
        /// <param name="lora">whether to return number of LORA or LLM models</param>
        /// <returns>number of LLM/LORA models</returns>
        public static int Num(bool lora)
        {
            int num = 0;
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.lora == lora) num++;
            }
            return num;
        }

        /// <summary>
        /// Returns the number of LLM models
        /// </summary>
        /// <returns>number of LLM models</returns>
        public static int NumModels()
        {
            return Num(false);
        }

        /// <summary>
        /// Returns the number of LORA models
        /// </summary>
        /// <returns>number of LORA models</returns>
        public static int NumLoras()
        {
            return Num(true);
        }

        /// <summary>
        /// Registers a LLM to the model manager
        /// </summary>
        /// <param name="llm">LLM</param>
        public static void Register(LLM llm)
        {
            llms.Add(llm);
        }

        /// <summary>
        /// Removes a LLM from the model manager
        /// </summary>
        /// <param name="llm">LLM</param>
        public static void Unregister(LLM llm)
        {
            llms.Remove(llm);
        }

        /// <summary>
        /// Loads the model manager from a file
        /// </summary>
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

        /// <summary>
        /// Adds a model entry to the model manager
        /// </summary>
        /// <param name="entry">model entry</param>
        /// <returns>model filename</returns>
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

        /// <summary>
        /// Creates and adds a model entry to the model manager
        /// </summary>
        /// <param name="path">model path</param>
        /// <param name="lora">if it is a LORA or LLM</param>
        /// <param name="label">label to show in the model manager in the Editor</param>
        /// <param name="url">model url</param>
        /// <returns>model filename</returns>
        public static string AddEntry(string path, bool lora = false, string label = null, string url = null)
        {
            return AddEntry(new ModelEntry(path, lora, label, url));
        }

        /// <summary>
        /// Downloads a model and adds a model entry to the model manager
        /// </summary>
        /// <param name="url">model url</param>
        /// <param name="lora">if it is a LORA or LLM</param>
        /// <param name="log">whether to log</param>
        /// <param name="label">model label</param>
        /// <returns>model filename</returns>
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

        /// <summary>
        /// Loads a model from disk and adds a model entry to the model manager
        /// </summary>
        /// <param name="path">model path</param>
        /// <param name="lora">if it is a LORA or LLM</param>
        /// <param name="log">whether to log</param>
        /// <param name="label">model label</param>
        /// <returns>model filename</returns>
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

        /// <summary>
        /// Downloads a LLM model from disk and adds a model entry to the model manager
        /// </summary>
        /// <param name="url">model url</param>
        /// <param name="log">whether to log</param>
        /// <param name="label">model label</param>
        /// <returns>model filename</returns>
        public static async Task<string> DownloadModel(string url, bool log = false, string label = null)
        {
            return await Download(url, false, log, label);
        }

        /// <summary>
        /// Downloads a Lora model from disk and adds a model entry to the model manager
        /// </summary>
        /// <param name="url">model url</param>
        /// <param name="log">whether to log</param>
        /// <param name="label">model label</param>
        /// <returns>model filename</returns>
        public static async Task<string> DownloadLora(string url, bool log = false, string label = null)
        {
            return await Download(url, true, log, label);
        }

        /// <summary>
        /// Loads a LLM model from disk and adds a model entry to the model manager
        /// </summary>
        /// <param name="path">model path</param>
        /// <param name="log">whether to log</param>
        /// <param name="label">model label</param>
        /// <returns>model filename</returns>
        public static string LoadModel(string path, bool log = false, string label = null)
        {
            return Load(path, false, log, label);
        }

        /// <summary>
        /// Loads a LORA model from disk and adds a model entry to the model manager
        /// </summary>
        /// <param name="path">model path</param>
        /// <param name="log">whether to log</param>
        /// <param name="label">model label</param>
        /// <returns>model filename</returns>
        public static string LoadLora(string path, bool log = false, string label = null)
        {
            return Load(path, true, log, label);
        }

        /// <summary>
        /// Sets the URL for a model
        /// </summary>
        /// <param name="filename">model filename</param>
        /// <param name="url">model URL</param>
        public static void SetURL(string filename, string url)
        {
            SetURL(Get(filename), url);
        }

        /// <summary>
        /// Sets the URL for a model
        /// </summary>
        /// <param name="entry">model entry</param>
        /// <param name="url">model URL</param>
        public static void SetURL(ModelEntry entry, string url)
        {
            if (entry == null) return;
            entry.url = url;
            Save();
        }

        /// <summary>
        /// Sets whether to include a model to the build
        /// </summary>
        /// <param name="filename">model filename</param>
        /// <param name="includeInBuild">whether to include it</param>
        public static void SetIncludeInBuild(string filename, bool includeInBuild)
        {
            SetIncludeInBuild(Get(filename), includeInBuild);
        }

        /// <summary>
        /// Sets whether to include a model to the build
        /// </summary>
        /// <param name="entry">model entry</param>
        /// <param name="includeInBuild">whether to include it</param>
        public static void SetIncludeInBuild(ModelEntry entry, bool includeInBuild)
        {
            if (entry == null) return;
            entry.includeInBuild = includeInBuild;
            Save();
        }

        /// <summary>
        /// Sets whether to download files on start
        /// </summary>
        /// <param name="value">whether to download files</param>
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

        /// <summary>
        /// Removes a model from the model manager
        /// </summary>
        /// <param name="filename">model filename</param>
        public static void Remove(string filename)
        {
            Remove(Get(filename));
        }

        /// <summary>
        /// Removes a model from the model manager
        /// </summary>
        /// <param name="filename">model entry</param>
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

        /// <summary>
        /// Sets the LLM download progress
        /// </summary>
        /// <param name="progress">download progress</param>
        public static void SetModelProgress(float progress)
        {
            modelProgress = progress;
        }

        /// <summary>
        /// Sets the LORA download progress
        /// </summary>
        /// <param name="progress">download progress</param>
        public static void SetLoraProgress(float progress)
        {
            loraProgress = progress;
        }

        /// <summary>
        /// Serialises and saves the model manager
        /// </summary>
        public static void Save()
        {
            string json = JsonUtility.ToJson(new LLMManagerStore { modelEntries = modelEntries, downloadOnStart = downloadOnStart }, true);
            PlayerPrefs.SetString(LLMManagerPref, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Deserialises and loads the model manager
        /// </summary>
        public static void Load()
        {
            string pref = PlayerPrefs.GetString(LLMManagerPref);
            if (pref == null || pref == "") return;
            LLMManagerStore store = JsonUtility.FromJson<LLMManagerStore>(pref);
            downloadOnStart = store.downloadOnStart;
            modelEntries = store.modelEntries;
        }

        /// <summary>
        /// Saves the model manager to disk for the build
        /// </summary>
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

        /// <summary>
        /// Saves the model manager to disk along with models that are not (or can't) be downloaded for the build
        /// </summary>
        /// <param name="copyCallback">copy function</param>
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
