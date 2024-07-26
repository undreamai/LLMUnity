/// @file
/// @brief File implementing helper functions for setup and process management.
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

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

    public class LLMAttribute : PropertyAttribute {}
    public class LLMAdvancedAttribute : PropertyAttribute {}
    public class LocalRemoteAttribute : PropertyAttribute {}
    public class RemoteAttribute : PropertyAttribute {}
    public class LocalAttribute : PropertyAttribute {}
    public class ModelAttribute : PropertyAttribute {}
    public class ModelDownloadAttribute : ModelAttribute {}
    public class ModelDownloadAdvancedAttribute : ModelAdvancedAttribute {}
    public class ModelAdvancedAttribute : PropertyAttribute {}
    public class ChatAttribute : PropertyAttribute {}
    public class ChatAdvancedAttribute : PropertyAttribute {}
    public class LLMUnityAttribute : PropertyAttribute {}

    public class NotImplementedException : Exception
    {
        public NotImplementedException() : base("The method needs to be implemented by subclasses.") {}
    }

    public delegate void EmptyCallback();
    public delegate void Callback<T>(T message);
    public delegate Task TaskCallback<T>(T message);
    public delegate T2 ContentCallback<T, T2>(T message);
    /// \endcond

    /// @ingroup utils
    /// <summary>
    /// Class implementing helper functions for setup and process management.
    /// </summary>
    public class LLMUnitySetup
    {
        // DON'T CHANGE! the version is autocompleted with a GitHub action
        /// <summary> LLM for Unity version </summary>
        public static string Version = "v2.0.3";
        /// <summary> LlamaLib version </summary>
        public static string LlamaLibVersion = "v1.1.5";
        /// <summary> LlamaLib url </summary>
        public static string LlamaLibURL = $"https://github.com/undreamai/LlamaLib/releases/download/{LlamaLibVersion}/undreamai-{LlamaLibVersion}-llamacpp.zip";
        /// <summary> LlamaLib path </summary>
        public static string libraryPath = GetAssetPath(Path.GetFileName(LlamaLibURL).Replace(".zip", ""));

        /// <summary> Default models for download </summary>
        [HideInInspector] public static readonly (string, string)[] modelOptions = new(string, string)[]
        {
            ("Download model", null),
            ("Mistral 7B Instruct v0.2 (medium, best overall)", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true"),
            ("OpenHermes 2.5 7B (medium, best for conversation)", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true"),
            ("Phi 3 (small, great)", "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf?download=true"),
        };

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

        /// \cond HIDE
        public enum DebugModeType
        {
            All,
            Warning,
            Error,
            None
        }
        [LLMUnity] public static DebugModeType DebugMode = DebugModeType.All;
        static List<Callback<string>> errorCallbacks = new List<Callback<string>>();

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

        static string DebugModeKey = "DebugMode";
        static void LoadDebugMode()
        {
            DebugMode = (DebugModeType)PlayerPrefs.GetInt(DebugModeKey, (int)DebugModeType.All);
        }

        public static void SetDebugMode(DebugModeType newDebugMode)
        {
            if (DebugMode == newDebugMode) return;
            DebugMode = newDebugMode;
            PlayerPrefs.SetInt(DebugModeKey, (int)DebugMode);
            PlayerPrefs.Save();
        }

        public static string GetAssetPath(string relPath = "")
        {
            // Path to store llm server binaries and models
            string assetsDir = Application.platform == RuntimePlatform.Android ? Application.persistentDataPath : Application.streamingAssetsPath;
            return Path.Combine(assetsDir, relPath).Replace('\\', '/');
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static async Task InitializeOnLoad()
        {
            await DownloadLibrary();
            LoadDebugMode();
        }

#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        void InitializeOnLoad()
        {
            LoadDebugMode();
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

        public static async Task AndroidExtractFile(string assetName, bool overwrite = false, int chunkSize = 1024*1024)
        {
            string source = "jar:file://" + Application.dataPath + "!/assets/" + assetName;
            string target = GetAssetPath(assetName);
            if (!overwrite && File.Exists(target))
            {
                Debug.Log($"File {target} already exists");
                return;
            }

            Debug.Log($"Extracting {source} to {target}");

            // UnityWebRequest to read the file from StreamingAssets
            UnityWebRequest www = UnityWebRequest.Get(source);
            // Send the request and await its completion
            var operation = www.SendWebRequest();

            while (!operation.isDone) await Task.Delay(1);
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load file from StreamingAssets: " + www.error);
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

#if UNITY_EDITOR
        [HideInInspector] public static float libraryProgress = 1;

        private static async Task DownloadLibrary()
        {
            if (libraryProgress < 1) return;
            libraryProgress = 0;
            string libZip = Path.Combine(Application.temporaryCachePath, Path.GetFileName(LlamaLibURL));
            if (!Directory.Exists(libraryPath))
            {
                await DownloadFile(LlamaLibURL, libZip, true, null, SetLibraryProgress);
                ZipFile.ExtractToDirectory(libZip, libraryPath);
                File.Delete(libZip);
            }
            libraryProgress = 1;
        }

        private static void SetLibraryProgress(float progress)
        {
            libraryProgress = progress;
        }

        public static string AddAsset(string assetPath, string basePath)
        {
            if (!File.Exists(assetPath))
            {
                LogError($"{assetPath} does not exist!");
                return null;
            }
            // add an asset to the basePath directory if it is not already there and return the relative path
            string basePathSlash = basePath.Replace('\\', '/');
            string fullPath = Path.GetFullPath(assetPath).Replace('\\', '/');
            Directory.CreateDirectory(basePathSlash);
            if (!fullPath.StartsWith(basePathSlash))
            {
                // if the asset is not in the assets dir copy it over
                fullPath = Path.Combine(basePathSlash, Path.GetFileName(assetPath));
                AssetDatabase.StartAssetEditing();
                foreach (string filename in new string[] {fullPath, fullPath + ".meta"})
                {
                    if (File.Exists(filename)) File.Delete(filename);
                }
                CreateSymlink(assetPath, fullPath);
                AssetDatabase.StopAssetEditing();
            }
            return fullPath.Substring(basePathSlash.Length + 1);
        }

        public static void CreateSymlink(string sourcePath, string targetPath)
        {
            bool isDirectory = Directory.Exists(sourcePath);
            if (!isDirectory && !File.Exists(sourcePath)) throw new FileNotFoundException($"Source path does not exist: {sourcePath}");

            bool success;
#if UNITY_STANDALONE_WIN
            success = CreateSymbolicLink(targetPath, sourcePath, (int)isDirectory);
#else
            success = symlink(sourcePath, targetPath) == 0;
#endif
            if (!success) throw new IOException($"Failed to create symbolic link: {targetPath}");
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(string oldpath, string newpath);

#endif
        /// \endcond
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
                Debug.LogError(e.Message);
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
                Debug.LogError(e.Message);
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
