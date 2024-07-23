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
        public string url;
        public string localPath;
        public bool includeInBuild;
    }

    [Serializable]
    public class ModelEntryList
    {
        public List<ModelEntry> modelEntries;
    }

    public class LLMManager
    {
        public static List<ModelEntry> modelEntries = new List<ModelEntry>();

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Load();
        }

        public static string ModelPathToName(string path)
        {
            return Path.GetFileNameWithoutExtension(path.Split("?")[0]);
        }

        public static async Task DownloadModel(ModelEntry entry, string url, string name = null)
        {
            string modelName = Path.GetFileName(url).Split("?")[0];
            string modelPath = Path.Combine(LLMUnitySetup.modelDownloadPath, modelName);
            await LLMUnitySetup.DownloadFile(url, modelPath);
            entry.name = name == null ? ModelPathToName(url) : name;
            entry.url = url;
            entry.localPath = modelPath;
        }

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LLMUnitySetup.modelListPath));
            File.WriteAllText(LLMUnitySetup.modelListPath, JsonUtility.ToJson(new ModelEntryList { modelEntries = modelEntries }));
        }

        public static void Load()
        {
            if (!File.Exists(LLMUnitySetup.modelListPath)) return;
            modelEntries = JsonUtility.FromJson<ModelEntryList>(File.ReadAllText(LLMUnitySetup.modelListPath)).modelEntries;
        }
    }
}
#endif
