#if UNITY_EDITOR
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
    }

    [Serializable]
    public class LLMManagerStore
    {
        public bool downloadOnStart;
        public List<ModelEntry> modelEntries;
    }

    public class LLMManager
    {
        public static bool downloadOnStart = false;
        public static List<ModelEntry> modelEntries = new List<ModelEntry>();

        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public static bool modelsDownloaded { get; protected set; } = false;
        static List<Callback<float>> modelProgressCallbacks = new List<Callback<float>>();
        static List<Callback<float>> loraProgressCallbacks = new List<Callback<float>>();

        [HideInInspector] public static float modelProgress = 1;
        [HideInInspector] public static float loraProgress = 1;
        static List<LLM> llms = new List<LLM>();

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Load();
        }

        public static string AddEntry(string path, bool lora = false, string label = null, string url = null)
        {
            ModelEntry entry = new ModelEntry();
            entry.filename = Path.GetFileName(path.Split("?")[0]);
            entry.label = label == null ? entry.filename : label;
            entry.lora = lora;
            entry.chatTemplate = lora ? null : ChatTemplate.FromGGUF(path);
            entry.url = url;
            entry.path = Path.GetFullPath(path).Replace('\\', '/');
            entry.includeInBuild = true;
            int indexToInsert = modelEntries.Count;
            if (!lora)
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
            modelEntries.Insert(indexToInsert, entry);
            return entry.filename;
        }

        public static async Task WaitUntilModelsDownloaded(Callback<float> modelProgressCallback = null, Callback<float> loraProgressCallback = null)
        {
            if (modelProgressCallback != null) modelProgressCallbacks.Add(modelProgressCallback);
            if (loraProgressCallback != null) loraProgressCallbacks.Add(loraProgressCallback);
            while (!modelsDownloaded) await Task.Yield();
            if (modelProgressCallback != null) modelProgressCallbacks.Remove(modelProgressCallback);
            if (loraProgressCallback != null) loraProgressCallbacks.Remove(loraProgressCallback);
        }

        public static async Task<string> Download(string url, bool lora = false, string label = null)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.url == url) return entry.filename;
            }
            string modelName = Path.GetFileName(url).Split("?")[0];
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

        public static string Load(string path, bool lora = false, string label = null)
        {
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.path == fullPath) return entry.filename;
            }
            return AddEntry(fullPath, lora, label);
        }

        public static async Task<string> DownloadModel(string url, string label = null)
        {
            return await Download(url, false, label);
        }

        public static async Task<string> DownloadLora(string url, string label = null)
        {
            return await Download(url, true, label);
        }

        public static string LoadModel(string url, string label = null)
        {
            return Load(url, false, label);
        }

        public static string LoadLora(string url, string label = null)
        {
            return Load(url, true, label);
        }

        public static void SetModelTemplate(string filename, string chatTemplate)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.filename == filename)
                {
                    entry.chatTemplate = chatTemplate;
                    break;
                }
            }
        }

        public static ModelEntry Get(string filename)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.filename == filename) return entry;
            }
            return null;
        }

        public static void Remove(string filename)
        {
            Remove(Get(filename));
        }

        public static void Remove(ModelEntry entry)
        {
            if (entry == null) return;
            modelEntries.Remove(entry);
            foreach (LLM llm in llms)
            {
                if (!entry.lora && llm.model == entry.filename) llm.model = "";
                else if (entry.lora && llm.lora == entry.filename) llm.lora = "";
            }
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

        public static void SetModelProgress(float progress)
        {
            modelProgress = progress;
            foreach (Callback<float> modelProgressCallback in modelProgressCallbacks) modelProgressCallback?.Invoke(progress);
        }

        public static void SetLoraProgress(float progress)
        {
            loraProgress = progress;
            foreach (Callback<float> loraProgressCallback in loraProgressCallbacks) loraProgressCallback?.Invoke(progress);
        }

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LLMUnitySetup.modelListPath));
            File.WriteAllText(LLMUnitySetup.modelListPath, JsonUtility.ToJson(new LLMManagerStore { modelEntries = modelEntries, downloadOnStart = downloadOnStart }, true));
        }

        public static void Load()
        {
            if (!File.Exists(LLMUnitySetup.modelListPath)) return;
            LLMManagerStore store = JsonUtility.FromJson<LLMManagerStore>(File.ReadAllText(LLMUnitySetup.modelListPath));
            downloadOnStart = store.downloadOnStart;
            modelEntries = store.modelEntries;
        }
    }
}
#endif
