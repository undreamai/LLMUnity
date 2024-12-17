/// @file
/// @brief File implementing helper functions for setup and process management.
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

/// @defgroup llm LLM
/// @defgroup template Chat Templates
/// @defgroup utils Utils
namespace LLMUnity
{
    /// \cond HIDE
    public sealed class FloatAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }

        public FloatAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
    public sealed class IntAttribute : PropertyAttribute
    {
        public int Min { get; private set; }
        public int Max { get; private set; }

        public IntAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class DynamicRangeAttribute : PropertyAttribute
    {
        public readonly string minVariable;
        public readonly string maxVariable;
        public bool intOrFloat;

        public DynamicRangeAttribute(string minVariable, string maxVariable, bool intOrFloat)
        {
            this.minVariable = minVariable;
            this.maxVariable = maxVariable;
            this.intOrFloat = intOrFloat;
        }
    }

    public class LLMAttribute : PropertyAttribute {}
    public class LocalRemoteAttribute : PropertyAttribute {}
    public class RemoteAttribute : PropertyAttribute {}
    public class LocalAttribute : PropertyAttribute {}
    public class ModelAttribute : PropertyAttribute {}
    public class ModelExtrasAttribute : PropertyAttribute {}
    public class ChatAttribute : PropertyAttribute {}
    public class LLMUnityAttribute : PropertyAttribute {}

    public class AdvancedAttribute : PropertyAttribute {}
    public class LLMAdvancedAttribute : AdvancedAttribute {}
    public class ModelAdvancedAttribute : AdvancedAttribute {}
    public class ChatAdvancedAttribute : AdvancedAttribute {}

    public class NotImplementedException : Exception
    {
        public NotImplementedException() : base("The method needs to be implemented by subclasses.") {}
    }

    public delegate void EmptyCallback();
    public delegate void Callback<T>(T message);
    public delegate Task TaskCallback<T>(T message);
    public delegate T2 ContentCallback<T, T2>(T message);
    public delegate void ActionCallback(string source, string target);

    [Serializable]
    public struct StringPair
    {
        public string source;
        public string target;
    }

    [Serializable]
    public class ListStringPair
    {
        public List<StringPair> pairs;
    }
    /// \endcond

    /// @ingroup utils
    /// <summary>
    /// Class implementing helper functions for setup and process management.
    /// </summary>
    public class LLMUnitySetup
    {
        // DON'T CHANGE! the version is autocompleted with a GitHub action
        /// <summary> LLM for Unity version </summary>
        public static string Version = "v2.4.1";
        /// <summary> LlamaLib version </summary>
        public static string LlamaLibVersion = "v1.2.1";
        /// <summary> LlamaLib release url </summary>
        public static string LlamaLibReleaseURL = $"https://github.com/undreamai/LlamaLib/releases/download/{LlamaLibVersion}";
        /// <summary> LlamaLib name </summary>
        public static string libraryName = GetLibraryName(LlamaLibVersion);
        /// <summary> LlamaLib path </summary>
        public static string libraryPath = GetAssetPath(libraryName);
        /// <summary> LlamaLib url </summary>
        public static string LlamaLibURL = $"{LlamaLibReleaseURL}/{libraryName}.zip";
        /// <summary> LlamaLib extension url </summary>
        public static string LlamaLibExtensionURL = $"{LlamaLibReleaseURL}/{libraryName}-full.zip";
        /// <summary> LLMnity store path </summary>
        public static string LLMUnityStore = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LLMUnity");
        /// <summary> Model download path </summary>
        public static string modelDownloadPath = Path.Combine(LLMUnityStore, "models");
        /// <summary> Path of file with build information for runtime </summary>
        public static string LLMManagerPath = GetAssetPath("LLMManager.json");

        /// <summary> Default models for download </summary>
        [HideInInspector] public static readonly Dictionary<string, (string, string, string)[]> modelOptions = new Dictionary<string, (string, string, string)[]>()
        {
            {"Medium models", new(string, string, string)[]
             {
                 ("Llama 3.1 8B", "https://huggingface.co/bartowski/Meta-Llama-3.1-8B-Instruct-GGUF/resolve/main/Meta-Llama-3.1-8B-Instruct-Q4_K_M.gguf?download=true", "https://huggingface.co/meta-llama/Meta-Llama-3.1-8B/blob/main/LICENSE"),
                 ("Mistral 7B Instruct v0.2", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true", null),
                 ("Gemma 2 9B it", "https://huggingface.co/bartowski/gemma-2-9b-it-GGUF/resolve/main/gemma-2-9b-it-Q4_K_M.gguf?download=true", "https://ai.google.dev/gemma/terms"),
                 ("OpenHermes 2.5 7B", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true", null),
             }},
            {"Small models", new(string, string, string)[]
             {
                 ("Llama 3.2 3B", "https://huggingface.co/hugging-quants/Llama-3.2-3B-Instruct-Q4_K_M-GGUF/resolve/main/llama-3.2-3b-instruct-q4_k_m.gguf", null),
                 ("Phi 3.5 4B", "https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf", null),
             }},
            {"Tiny models", new(string, string, string)[]
             {
                 ("Llama 3.2 1B", "https://huggingface.co/hugging-quants/Llama-3.2-1B-Instruct-Q4_K_M-GGUF/resolve/main/llama-3.2-1b-instruct-q4_k_m.gguf", null),
                 ("Qwen 2 0.5B", "https://huggingface.co/Qwen/Qwen2-0.5B-Instruct-GGUF/resolve/main/qwen2-0_5b-instruct-q4_k_m.gguf?download=true", null),
             }},
            {"RAG models", new(string, string, string)[]
             {
                 ("All MiniLM L12 v2", "https://huggingface.co/leliuga/all-MiniLM-L12-v2-GGUF/resolve/main/all-MiniLM-L12-v2.Q4_K_M.gguf", null),
                 ("BGE large en v1.5", "https://huggingface.co/CompendiumLabs/bge-large-en-v1.5-gguf/resolve/main/bge-large-en-v1.5-q4_k_m.gguf", null),
                 ("BGE base en v1.5", "https://huggingface.co/CompendiumLabs/bge-base-en-v1.5-gguf/resolve/main/bge-base-en-v1.5-q4_k_m.gguf", null),
                 ("BGE small en v1.5", "https://huggingface.co/CompendiumLabs/bge-small-en-v1.5-gguf/resolve/main/bge-small-en-v1.5-q4_k_m.gguf", null),
             }},
        };

        /// \cond HIDE
        [LLMUnity] public static DebugModeType DebugMode = DebugModeType.All;
        static string DebugModeKey = "DebugMode";
        public static bool FullLlamaLib = false;
        static string FullLlamaLibKey = "FullLlamaLib";
        static List<Callback<string>> errorCallbacks = new List<Callback<string>>();
        static readonly object lockObject = new object();
        static Dictionary<string, Task> androidExtractTasks = new Dictionary<string, Task>();

        public enum DebugModeType
        {
            All,
            Warning,
            Error,
            None
        }

        public static void Log(string message)
        {
            if ((int)DebugMode > (int)DebugModeType.All) return;
            Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if ((int)DebugMode > (int)DebugModeType.Warning) return;
            Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if ((int)DebugMode > (int)DebugModeType.Error) return;
            Debug.LogError(message);
            foreach (Callback<string> errorCallback in errorCallbacks) errorCallback(message);
        }

        static void LoadPlayerPrefs()
        {
            DebugMode = (DebugModeType)PlayerPrefs.GetInt(DebugModeKey, (int)DebugModeType.All);
            FullLlamaLib = PlayerPrefs.GetInt(FullLlamaLibKey, 0) == 1;
        }

        public static void SetDebugMode(DebugModeType newDebugMode)
        {
            if (DebugMode == newDebugMode) return;
            DebugMode = newDebugMode;
            PlayerPrefs.SetInt(DebugModeKey, (int)DebugMode);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        public static void SetFullLlamaLib(bool value)
        {
            if (FullLlamaLib == value) return;
            FullLlamaLib = value;
            PlayerPrefs.SetInt(FullLlamaLibKey, value ? 1 : 0);
            PlayerPrefs.Save();
            _ = DownloadLibrary();
        }

#endif

        public static string GetLibraryName(string version)
        {
            return $"undreamai-{version}-llamacpp";
        }

        public static string GetAssetPath(string relPath = "")
        {
            string assetsDir = Application.platform == RuntimePlatform.Android? Application.persistentDataPath : Application.streamingAssetsPath;
            return Path.Combine(assetsDir, relPath).Replace('\\', '/');
        }

        public static string GetDownloadAssetPath(string relPath = "")
        {
            string assetsDir = (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)? Application.persistentDataPath : Application.streamingAssetsPath;
            return Path.Combine(assetsDir, relPath).Replace('\\', '/');
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static async Task InitializeOnLoad()
        {
            LoadPlayerPrefs();
            await DownloadLibrary();
        }

#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        void InitializeOnLoad()
        {
            LoadPlayerPrefs();
        }

#endif

        static Dictionary<string, ResumingWebClient> downloadClients = new Dictionary<string, ResumingWebClient>();

        public static void CancelDownload(string savePath)
        {
            if (!downloadClients.ContainsKey(savePath)) return;
            downloadClients[savePath].CancelDownloadAsync();
            downloadClients.Remove(savePath);
        }

        public static async Task DownloadFile(
            string fileUrl, string savePath, bool overwrite = false,
            Callback<string> callback = null, Callback<float> progressCallback = null
        )
        {
            if (File.Exists(savePath) && !overwrite)
            {
                Log($"File already exists at: {savePath}");
            }
            else
            {
                Log($"Downloading {fileUrl} to {savePath}...");
                string tmpPath = Path.Combine(Application.temporaryCachePath, Path.GetFileName(savePath));

                ResumingWebClient client = new ResumingWebClient();
                downloadClients[savePath] = client;
                await client.DownloadFileTaskAsyncResume(new Uri(fileUrl), tmpPath, !overwrite, progressCallback);
                downloadClients.Remove(savePath);
#if UNITY_EDITOR
                AssetDatabase.StartAssetEditing();
#endif
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.Move(tmpPath, savePath);
#if UNITY_EDITOR
                AssetDatabase.StopAssetEditing();
#endif
                Log($"Download complete!");
            }

            progressCallback?.Invoke(1f);
            callback?.Invoke(savePath);
        }

        public static async Task AndroidExtractFile(string assetName, bool overwrite = false, bool log = true, int chunkSize = 1024*1024)
        {
            Task extractionTask;
            lock (lockObject)
            {
                if (!androidExtractTasks.TryGetValue(assetName, out extractionTask))
                {
#if UNITY_ANDROID
                    extractionTask = AndroidExtractFileOnce(assetName, overwrite, log, chunkSize);
#else
                    extractionTask = Task.CompletedTask;
#endif
                    androidExtractTasks[assetName] = extractionTask;
                }
            }
            await extractionTask;
        }

        public static async Task AndroidExtractFileOnce(string assetName, bool overwrite = false, bool log = true, int chunkSize = 1024*1024)
        {
            string source = "jar:file://" + Application.dataPath + "!/assets/" + assetName;
            string target = GetAssetPath(assetName);
            if (!overwrite && File.Exists(target))
            {
                if (log) Log($"File {target} already exists");
                return;
            }

            Log($"Extracting {source} to {target}");

            // UnityWebRequest to read the file from StreamingAssets
            UnityWebRequest www = UnityWebRequest.Get(source);
            // Send the request and await its completion
            var operation = www.SendWebRequest();

            while (!operation.isDone) await Task.Delay(1);
            if (www.result != UnityWebRequest.Result.Success)
            {
                LogError("Failed to load file from StreamingAssets: " + www.error);
            }
            else
            {
                byte[] buffer = new byte[chunkSize];
                using (Stream responseStream = new MemoryStream(www.downloadHandler.data))
                using (FileStream fileStream = new FileStream(target, FileMode.Create, FileAccess.Write))
                {
                    int bytesRead;
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }
            }
        }

        public static async Task AndroidExtractAsset(string path, bool overwrite = false)
        {
            if (Application.platform != RuntimePlatform.Android) return;
            await AndroidExtractFile(Path.GetFileName(path), overwrite);
        }

        public static string GetFullPath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }

        public static bool IsSubPath(string childPath, string parentPath)
        {
            return GetFullPath(childPath).StartsWith(GetFullPath(parentPath), StringComparison.OrdinalIgnoreCase);
        }

        public static string RelativePath(string fullPath, string basePath)
        {
            // Get the full paths and replace backslashes with forward slashes (or vice versa)
            string fullParentPath = GetFullPath(basePath).TrimEnd('/');
            string fullChildPath = GetFullPath(fullPath);

            string relativePath = fullChildPath;
            if (fullChildPath.StartsWith(fullParentPath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = fullChildPath.Substring(fullParentPath.Length);
                while (relativePath.StartsWith("/")) relativePath = relativePath.Substring(1);
            }
            return relativePath;
        }

#if UNITY_EDITOR

        [HideInInspector] public static float libraryProgress = 1;

        public static void CreateEmptyFile(string path)
        {
            File.Create(path).Dispose();
        }

        static void ExtractInsideDirectory(string zipPath, string extractPath, bool overwrite = true)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string destinationPath = Path.Combine(extractPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath, overwrite);
                }
            }
        }

        static async Task DownloadAndExtractInsideDirectory(string url, string path, string setupDir)
        {
            string urlName = Path.GetFileName(url);
            string setupFile = Path.Combine(setupDir, urlName + ".complete");
            if (File.Exists(setupFile)) return;

            string zipPath = Path.Combine(Application.temporaryCachePath, urlName);
            await DownloadFile(url, zipPath, true, null, SetLibraryProgress);

            AssetDatabase.StartAssetEditing();
            ExtractInsideDirectory(zipPath, path);
            CreateEmptyFile(setupFile);
            AssetDatabase.StopAssetEditing();

            File.Delete(zipPath);
        }


        static void DeleteEarlierVersions()
        {
            List<string> assetPathSubDirs = new List<string>();
            foreach (string dir in new string[]{GetAssetPath(), Path.Combine(Application.dataPath, "Plugins", "Android")})
            {
                if(Directory.Exists(dir)) assetPathSubDirs.AddRange(Directory.GetDirectories(dir));
            }

            Regex regex = new Regex(GetLibraryName("(.+)"));
            foreach (string assetPathSubDir in assetPathSubDirs)
            {
                Match match = regex.Match(Path.GetFileName(assetPathSubDir));
                if (match.Success)
                {
                    string version = match.Groups[1].Value;
                    if (version != LlamaLibVersion)
                    {
                        Debug.Log($"Deleting other LLMUnity version folder: {assetPathSubDir}");
                        Directory.Delete(assetPathSubDir, true);
                        if (File.Exists(assetPathSubDir + ".meta")) File.Delete(assetPathSubDir + ".meta");
                    }
                }
            }
        }

        static async Task DownloadLibrary()
        {
            if (libraryProgress < 1) return;
            libraryProgress = 0;

            try
            {
                DeleteEarlierVersions();

                string setupDir = Path.Combine(libraryPath, "setup");
                Directory.CreateDirectory(setupDir);

                // setup LlamaLib in StreamingAssets
                await DownloadAndExtractInsideDirectory(LlamaLibURL, libraryPath, setupDir);

                // setup LlamaLib extras in StreamingAssets
                if (FullLlamaLib) await DownloadAndExtractInsideDirectory(LlamaLibExtensionURL, libraryPath, setupDir);
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }

            libraryProgress = 1;
        }

        private static void SetLibraryProgress(float progress)
        {
            libraryProgress = Math.Min(0.99f, progress);
        }

        public static string AddAsset(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                LogError($"{assetPath} does not exist!");
                return null;
            }
            string assetDir = GetAssetPath();
            if (IsSubPath(assetPath, assetDir)) return RelativePath(assetPath, assetDir);

            string filename = Path.GetFileName(assetPath);
            string fullPath = GetAssetPath(filename);
            AssetDatabase.StartAssetEditing();
            foreach (string path in new string[] {fullPath, fullPath + ".meta"})
            {
                if (File.Exists(path)) File.Delete(path);
            }
            File.Copy(assetPath, fullPath);
            AssetDatabase.StopAssetEditing();
            return filename;
        }

#endif
        /// \endcond

        /// <summary> Add callback function to call for error logs </summary>
        public static void AddErrorCallBack(Callback<string> callback)
        {
            errorCallbacks.Add(callback);
        }

        /// <summary> Remove callback function added for error logs </summary>
        public static void RemoveErrorCallBack(Callback<string> callback)
        {
            errorCallbacks.Remove(callback);
        }

        /// <summary> Remove all callback function added for error logs </summary>
        public static void ClearErrorCallBacks()
        {
            errorCallbacks.Clear();
        }

        public static int GetMaxFreqKHz(int cpuId)
        {
            string[] paths = new string[]
            {
                $"/sys/devices/system/cpu/cpufreq/stats/cpu{cpuId}/time_in_state",
                $"/sys/devices/system/cpu/cpu{cpuId}/cpufreq/stats/time_in_state",
                $"/sys/devices/system/cpu/cpu{cpuId}/cpufreq/cpuinfo_max_freq"
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;

                int maxFreqKHz = 0;
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] parts = line.Split(' ');
                        if (parts.Length > 0 && int.TryParse(parts[0], out int freqKHz))
                        {
                            if (freqKHz > maxFreqKHz)
                            {
                                maxFreqKHz = freqKHz;
                            }
                        }
                    }
                }
                if (maxFreqKHz != 0) return maxFreqKHz;
            }
            return -1;
        }

        public static bool IsSmtCpu(int cpuId)
        {
            string[] paths = new string[]
            {
                $"/sys/devices/system/cpu/cpu{cpuId}/topology/core_cpus_list",
                $"/sys/devices/system/cpu/cpu{cpuId}/topology/thread_siblings_list"
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains(",") || line.Contains("-"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Calculates the number of big cores in Android similarly to ncnn (https://github.com/Tencent/ncnn)
        /// </summary>
        /// <returns></returns>
        public static int AndroidGetNumBigCores()
        {
            int maxFreqKHzMin = int.MaxValue;
            int maxFreqKHzMax = 0;
            List<int> cpuMaxFreqKHz = new List<int>();
            List<bool> cpuIsSmtCpu = new List<bool>();

            try
            {
                string cpuPath = "/sys/devices/system/cpu/";
                int coreIndex;
                if (Directory.Exists(cpuPath))
                {
                    foreach (string cpuDir in Directory.GetDirectories(cpuPath))
                    {
                        string dirName = Path.GetFileName(cpuDir);
                        if (!dirName.StartsWith("cpu")) continue;
                        if (!int.TryParse(dirName.Substring(3), out coreIndex)) continue;

                        int maxFreqKHz = GetMaxFreqKHz(coreIndex);
                        cpuMaxFreqKHz.Add(maxFreqKHz);
                        if (maxFreqKHz > maxFreqKHzMax) maxFreqKHzMax = maxFreqKHz;
                        if (maxFreqKHz < maxFreqKHzMin)  maxFreqKHzMin = maxFreqKHz;
                        cpuIsSmtCpu.Add(IsSmtCpu(coreIndex));
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }

            int numBigCores = 0;
            int numCores = SystemInfo.processorCount;
            int maxFreqKHzMedium = (maxFreqKHzMin + maxFreqKHzMax) / 2;
            if (maxFreqKHzMedium == maxFreqKHzMax) numBigCores = numCores;
            else
            {
                for (int i = 0; i < cpuMaxFreqKHz.Count; i++)
                {
                    if (cpuIsSmtCpu[i] || cpuMaxFreqKHz[i] >= maxFreqKHzMedium) numBigCores++;
                }
            }

            if (numBigCores == 0) numBigCores = SystemInfo.processorCount / 2;
            else numBigCores = Math.Min(numBigCores, SystemInfo.processorCount);

            return numBigCores;
        }

        /// <summary>
        /// Calculates the number of big cores in Android similarly to Unity (https://docs.unity3d.com/2022.3/Documentation/Manual/android-thread-configuration.html)
        /// </summary>
        /// <returns></returns>
        public static int AndroidGetNumBigCoresCapacity()
        {
            List<int> capacities = new List<int>();
            int minCapacity = int.MaxValue;
            try
            {
                string cpuPath = "/sys/devices/system/cpu/";
                int coreIndex;
                if (Directory.Exists(cpuPath))
                {
                    foreach (string cpuDir in Directory.GetDirectories(cpuPath))
                    {
                        string dirName = Path.GetFileName(cpuDir);
                        if (!dirName.StartsWith("cpu")) continue;
                        if (!int.TryParse(dirName.Substring(3), out coreIndex)) continue;

                        string capacityPath = Path.Combine(cpuDir, "cpu_capacity");
                        if (!File.Exists(capacityPath)) break;

                        int capacity = int.Parse(File.ReadAllText(capacityPath).Trim());
                        capacities.Add(capacity);
                        if (minCapacity > capacity) minCapacity = capacity;
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }

            int numBigCores = 0;
            foreach (int capacity in capacities)
            {
                if (capacity >= 2 * minCapacity) numBigCores++;
            }

            if (numBigCores == 0 || numBigCores > SystemInfo.processorCount) numBigCores = SystemInfo.processorCount;
            return numBigCores;
        }
    }
}
