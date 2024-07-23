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
        public string name;
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
        // [HideInInspector] public static float loraProgress = 1;

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Load();
        }

        public static string ModelPathToName(string path)
        {
            return Path.GetFileNameWithoutExtension(path.Split("?")[0]);
        }

        public static ModelEntry CreateEntry(string path, string url = null, string name = null)
        {
            ModelEntry entry = new ModelEntry();
            entry.name = name == null ? ModelPathToName(url) : name;
            entry.chatTemplate = ChatTemplate.FromGGUF(path);
            entry.url = url;
            entry.localPath = Path.GetFullPath(path).Replace('\\', '/');
            return entry;
        }

        public static ModelEntry AddEntry(string path, string url = null, string name = null)
        {
            ModelEntry entry = CreateEntry(path, url, name);
            modelEntries.Add(entry);
            return entry;
        }

        public static async Task WaitUntilModelsDownloaded(Callback<float> modelProgressCallback = null, Callback<float> loraProgressCallback = null)
        {
            if (modelProgressCallback != null) modelProgressCallbacks.Add(modelProgressCallback);
            if (loraProgressCallback != null) loraProgressCallbacks.Add(loraProgressCallback);
            while (!modelsDownloaded) await Task.Yield();
            if (modelProgressCallback != null) modelProgressCallbacks.Remove(modelProgressCallback);
            if (loraProgressCallback != null) loraProgressCallbacks.Remove(loraProgressCallback);
        }

        public static async Task<ModelEntry> DownloadModel(string url, string name = null)
        {
            foreach (ModelEntry modelEntry in modelEntries)
            {
                if (modelEntry.url == url) return modelEntry;
            }
            string modelName = Path.GetFileName(url).Split("?")[0];
            string modelPath = Path.Combine(LLMUnitySetup.modelDownloadPath, modelName);
            modelProgress = 0;
            await LLMUnitySetup.DownloadFile(url, modelPath, false, null, SetModelProgress);
            return AddEntry(modelPath, url, name);
        }

        public static ModelEntry LoadModel(string path)
        {
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            foreach (ModelEntry modelEntry in modelEntries)
            {
                if (modelEntry.localPath == fullPath) return modelEntry;
            }
            return AddEntry(path);
        }

        public static void SetModelProgress(float progress)
        {
            modelProgress = progress;
            foreach (Callback<float> modelProgressCallback in modelProgressCallbacks) modelProgressCallback?.Invoke(progress);
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
            modelEntries = store.modelEntries;
            downloadOnBuild = store.downloadOnBuild;
        }
    }
}
#endif
