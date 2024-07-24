#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [Serializable]
    public class ModelEntry
    {
        public string name;
        public bool lora;
        public string chatTemplate;
        public string url;
        public string localPath;
        public bool includeInBuild;
    }

    [Serializable]
    public class LLMManagerStore
    {
        public bool downloadOnBuild;
        public List<ModelEntry> modelEntries;
    }

    public class LLMManager
    {
        public static bool downloadOnBuild = false;
        public static List<ModelEntry> modelEntries = new List<ModelEntry>();

        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public static bool modelsDownloaded { get; protected set; } = false;
        static List<Callback<float>> modelProgressCallbacks = new List<Callback<float>>();
        static List<Callback<float>> loraProgressCallbacks = new List<Callback<float>>();

        [HideInInspector] public static float modelProgress = 1;
        [HideInInspector] public static float loraProgress = 1;

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Load();
        }

        public static string ModelPathToName(string path)
        {
            return Path.GetFileNameWithoutExtension(path.Split("?")[0]);
        }

        public static string AddEntry(string path, bool lora = false, string name = null, string url = null)
        {
            string key = name == null ? ModelPathToName(url) : name;
            ModelEntry entry = new ModelEntry();
            entry.name = key;
            entry.lora = lora;
            entry.chatTemplate = lora ? null : ChatTemplate.FromGGUF(path);
            entry.url = url;
            entry.localPath = Path.GetFullPath(path).Replace('\\', '/');
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
            return key;
        }

        public static async Task WaitUntilModelsDownloaded(Callback<float> modelProgressCallback = null, Callback<float> loraProgressCallback = null)
        {
            if (modelProgressCallback != null) modelProgressCallbacks.Add(modelProgressCallback);
            if (loraProgressCallback != null) loraProgressCallbacks.Add(loraProgressCallback);
            while (!modelsDownloaded) await Task.Yield();
            if (modelProgressCallback != null) modelProgressCallbacks.Remove(modelProgressCallback);
            if (loraProgressCallback != null) loraProgressCallbacks.Remove(loraProgressCallback);
        }

        public static async Task<string> Download(string url, bool lora = false, string name = null)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.url == url) return entry.name;
            }
            string modelName = Path.GetFileName(url).Split("?")[0];
            string modelPath = Path.Combine(LLMUnitySetup.modelDownloadPath, modelName);
            if (!lora)
            {
                modelProgress = 0;
                try
                {
                    await LLMUnitySetup.DownloadFile(url, modelPath, false, null, SetModelProgress);
                }
                catch (Exception ex)
                {
                    modelProgress = 1;
                    throw ex;
                }
            }
            else
            {
                loraProgress = 0;
                try
                {
                    await LLMUnitySetup.DownloadFile(url, modelPath, false, null, SetLoraProgress);
                }
                catch (Exception ex)
                {
                    loraProgress = 1;
                    throw ex;
                }
            }
            return AddEntry(modelPath, lora, name, url);
        }

        public static string Load(string path, bool lora = false, string name = null)
        {
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.localPath == fullPath) return entry.name;
            }
            return AddEntry(path, lora, name);
        }

        public static async Task<string> DownloadModel(string url, string name = null)
        {
            return await Download(url, false, name);
        }

        public static async Task<string> DownloadLora(string url, string name = null)
        {
            return await Download(url, true, name);
        }

        public static string LoadModel(string url, string name = null)
        {
            return Load(url, false, name);
        }

        public static string LoadLora(string url, string name = null)
        {
            return Load(url, true, name);
        }

        public static void SetModelTemplate(string name, string chatTemplate)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.name == name)
                {
                    entry.chatTemplate = chatTemplate;
                    break;
                }
            }
        }

        public static ModelEntry Get(string name)
        {
            foreach (ModelEntry entry in modelEntries)
            {
                if (entry.name == name) return entry;
            }
            return null;
        }

        public static void Remove(string name)
        {
            Remove(Get(name));
        }

        public static void Remove(ModelEntry entry)
        {
            if (entry == null) return;
            modelEntries.Remove(entry);
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
            File.WriteAllText(LLMUnitySetup.modelListPath, JsonUtility.ToJson(new LLMManagerStore { modelEntries = modelEntries, downloadOnBuild = downloadOnBuild }));
        }

        public static void Load()
        {
            if (!File.Exists(LLMUnitySetup.modelListPath)) return;
            LLMManagerStore store = JsonUtility.FromJson<LLMManagerStore>(File.ReadAllText(LLMUnitySetup.modelListPath));
            downloadOnBuild = store.downloadOnBuild;
            modelEntries = store.modelEntries;
        }
    }
}
#endif
