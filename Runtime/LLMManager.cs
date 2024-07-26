using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
#if UNITY_EDITOR
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
#endif

    public class LLMManager
    {
        public static float downloadProgress = 1;
        public static List<Callback<float>> downloadProgressCallbacks = new List<Callback<float>>();
        static Task downloadModelsTask;
        static readonly object lockObject = new object();
        static long totalSize;
        static long currFileSize;
        static long completedSize;

        public static void SetDownloadProgress(float progress)
        {
            downloadProgress = (completedSize + progress * currFileSize) / totalSize;
            foreach (Callback<float> downloadProgressCallback in downloadProgressCallbacks) downloadProgressCallback?.Invoke(downloadProgress);
        }

        public static Task DownloadModels()
        {
            lock (lockObject)
            {
                if (downloadModelsTask == null) downloadModelsTask = DownloadModelsOnce();
            }
            return downloadModelsTask;
        }

        public static async Task DownloadModelsOnce()
        {
            if (Application.platform == RuntimePlatform.Android) await LLMUnitySetup.AndroidExtractFile(LLMUnitySetup.BuildFilename);
            if (!File.Exists(LLMUnitySetup.BuildFile)) return;

            List<StringPair> downloads = new List<StringPair>();
            using (FileStream fs = new FileStream(LLMUnitySetup.BuildFile, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    List<StringPair> downloadsToDo = JsonUtility.FromJson<ListStringPair>(reader.ReadString()).pairs;
                    foreach (StringPair pair in downloadsToDo)
                    {
                        string target = LLMUnitySetup.GetAssetPath(pair.target);
                        if (!File.Exists(target)) downloads.Add(new StringPair {source = pair.source, target = target});
                    }
                }
            }
            if (downloads.Count == 0) return;

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
                    completedSize += currFileSize;
                }

                completedSize = totalSize;
                SetDownloadProgress(0);
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Error downloading the models");
                throw ex;
            }
        }

#if UNITY_EDITOR
        static string LLMManagerPref = "LLMManager";
        public static bool downloadOnStart = false;
        public static List<ModelEntry> modelEntries = new List<ModelEntry>();

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
            Save();
            return entry.filename;
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
                if (llm.model == entry.filename) llm.SetTemplate(chatTemplate);
            }
            Save();
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
            Save();
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
        }

        public static void SetLoraProgress(float progress)
        {
            loraProgress = progress;
        }

        public static void Save()
        {
            string pref = JsonUtility.ToJson(new LLMManagerStore { modelEntries = modelEntries, downloadOnStart = downloadOnStart }, true);
            PlayerPrefs.SetString(LLMManagerPref, pref);
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

        public static void Build(ActionCallback copyCallback)
        {
            List<StringPair> downloads = new List<StringPair>();
            foreach (ModelEntry modelEntry in modelEntries)
            {
                if (!modelEntry.includeInBuild) continue;
                string target = LLMUnitySetup.GetAssetPath(modelEntry.filename);
                if (File.Exists(target)) continue;
                if (!downloadOnStart) copyCallback(modelEntry.path, target);
                else downloads.Add(new StringPair { source = modelEntry.url, target = modelEntry.filename });
            }

            if (downloads.Count > 0)
            {
                string downloadJSON = JsonUtility.ToJson(new ListStringPair { pairs = downloads }, true);
                using (FileStream fs = new FileStream(LLMUnitySetup.BuildFile, FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter writer = new BinaryWriter(fs)) writer.Write(downloadJSON);
                }
            }
        }

#endif
    }
}
