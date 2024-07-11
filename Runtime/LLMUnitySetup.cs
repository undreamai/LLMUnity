/// @file
/// @brief File implementing helper functions for setup and process management.
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using System.Net;
using System;
using System.IO.Compression;
using System.Collections.Generic;

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
            return Path.Combine(Application.streamingAssetsPath, relPath).Replace('\\', '/');
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

#if UNITY_EDITOR
        [HideInInspector] public static float libraryProgress = 1;

        public class DownloadStatus
        {
            Callback<float> progresscallback;

            public DownloadStatus(Callback<float> progresscallback = null)
            {
                this.progresscallback = progresscallback;
            }

            public void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
            {
                progresscallback?.Invoke(e.ProgressPercentage / 100.0f);
            }
        }

        public static async Task DownloadFile(
            string fileUrl, string savePath, bool overwrite = false,
            TaskCallback<string> callback = null, Callback<float> progresscallback = null,
            bool async = true
        )
        {
            // download a file to the specified path
            if (File.Exists(savePath) && !overwrite)
            {
                Log($"File already exists at: {savePath}");
            }
            else
            {
                Log($"Downloading {fileUrl}...");
                string tmpPath = Path.Combine(Application.temporaryCachePath, Path.GetFileName(savePath));

                WebClient client = new WebClient();
                DownloadStatus downloadStatus = new DownloadStatus(progresscallback);
                client.DownloadProgressChanged += downloadStatus.DownloadProgressChanged;
                if (async)
                {
                    await client.DownloadFileTaskAsync(fileUrl, tmpPath);
                }
                else
                {
                    client.DownloadFile(fileUrl, tmpPath);
                }

                AssetDatabase.StartAssetEditing();
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.Move(tmpPath, savePath);
                AssetDatabase.StopAssetEditing();
                Log($"Download complete!");
            }

            progresscallback?.Invoke(1f);
            if (callback != null) await callback.Invoke(savePath);
        }

        public static async Task<string> AddAsset(string assetPath, string basePath)
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
                Log($"copying {assetPath} to {fullPath}");
                AssetDatabase.StartAssetEditing();
                await Task.Run(() =>
                {
                    foreach (string filename in new string[] {fullPath, fullPath + ".meta"})
                    {
                        if (File.Exists(fullPath))
                            File.Delete(fullPath);
                    }
                    File.Copy(assetPath, fullPath);
                });
                AssetDatabase.StopAssetEditing();
                Log("copying complete!");
            }
            return fullPath.Substring(basePathSlash.Length + 1);
        }

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

        public static void DownloadModel(LLM llm, int optionIndex)
        {
            // download default model and disable model editor properties until the model is set
            llm.SelectedModel = optionIndex;
            string modelUrl = modelOptions[optionIndex].Item2;
            if (modelUrl == null) return;
            llm.modelProgress = 0;
            string modelName = Path.GetFileName(modelUrl).Split("?")[0];
            string modelPath = GetAssetPath(modelName);
            Task downloadTask = DownloadFile(modelUrl, modelPath, false, llm.SetModel, llm.SetModelProgress);
        }

#endif
        /// \endcond
    }
}
