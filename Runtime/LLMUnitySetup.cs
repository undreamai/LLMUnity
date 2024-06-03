/// @file
/// @brief File implementing helper functions for setup and process management.
using UnityEditor;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
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
    public class RemoteAttribute : PropertyAttribute {}
    public class LocalAttribute : PropertyAttribute {}
    public class ModelAttribute : PropertyAttribute {}
    public class ModelAdvancedAttribute : PropertyAttribute {}
    public class ChatAttribute : PropertyAttribute {}

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
    public class LLMUnitySetup : MonoBehaviour
    {
        // DON'T CHANGE! the version is autocompleted with a GitHub action
        /// <summary> LLM for Unity version </summary>
        public static string Version = "v1.2.9";
        public static string LlamaLibVersion = "v1.1.1";
        public static string LlamaLibURL = $"https://github.com/undreamai/LlamaLib/releases/download/{LlamaLibVersion}/undreamai-{LlamaLibVersion}-llamacpp.zip";
        public static string CUDA12WindowsURL = $"https://github.com/undreamai/LlamaLib/releases/download/{LlamaLibVersion}/cuda-12.2.0-windows.zip";
        public static string CUDA12LinuxURL = $"https://github.com/undreamai/LlamaLib/releases/download/{LlamaLibVersion}/cuda-12.2.0-linux.zip";
        public static string libraryPath = Path.Combine(Application.dataPath, "Plugins", Path.GetFileName(LlamaLibURL).Replace(".zip", ""));

        [HideInInspector] public static readonly (string, string)[] modelOptions = new(string, string)[]
        {
            ("Download model", null),
            ("Mistral 7B Instruct v0.2 (medium, best overall)", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true"),
            ("OpenHermes 2.5 7B (medium, best for conversation)", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true"),
            ("Phi 2 (small, decent)", "https://huggingface.co/TheBloke/phi-2-GGUF/resolve/main/phi-2.Q4_K_M.gguf?download=true"),
        };
        [HideInInspector] public static readonly (string, string[])[] CUDAOptions = new(string, string[])[]
        {
            ("Download CUDA", new string[] {}),
            ("CUDA 12 (Windows)", new string[] {CUDA12WindowsURL}),
            ("CUDA 12 (Linux)", new string[] {CUDA12LinuxURL}),
            ("CUDA 12 (Windows and Linux)", new string[] {CUDA12WindowsURL, CUDA12LinuxURL}),
        };

        private static int selectedCUDA = -1;
        public static float CUDAbinariesWIP = 0;
        public static float CUDAbinariesDone = 0;
        [HideInInspector] public static float libraryProgress = 1;
        [HideInInspector] public static float CUDAProgress = 1;

        public static string GetAssetPath(string relPath = "")
        {
            // Path to store llm server binaries and models
            return Path.Combine(Application.streamingAssetsPath, relPath).Replace('\\', '/');
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static async Task InitializeOnLoad()
        {
            // Perform download when the build is finished
            await DownloadLibrary();
            if (SelectedCUDA > 0) await DownloadCUDA(SelectedCUDA);
        }

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
                Debug.Log($"File already exists at: {savePath}");
            }
            else
            {
                Debug.Log($"Downloading {fileUrl}...");
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
                Debug.Log($"Download complete!");
            }

            progresscallback?.Invoke(1f);
            await callback?.Invoke(savePath);
        }

        public static async Task<string> AddAsset(string assetPath, string basePath)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogError($"{assetPath} does not exist!");
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
                Debug.Log($"copying {assetPath} to {fullPath}");
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
                Debug.Log("copying complete!");
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

        /// \cond HIDE
        public static int SelectedCUDA
        {
            get
            {
                if (selectedCUDA == -1)
                {
                    selectedCUDA = EditorPrefs.GetInt("selectedCUDA", 0);
                }
                return selectedCUDA;
            }
            set
            {
                selectedCUDA = value;
                EditorPrefs.SetInt("selectedCUDA", value);
            }
        }

        public static string GetCUDAPath(string CUDAUrl)
        {
            return Path.Combine(Application.persistentDataPath, Path.GetFileName(CUDAUrl)).Replace('\\', '/');
        }

        public static string GetCUDAFilePath(string path)
        {
            return Path.Combine(libraryPath, path);
        }

        public static async Task DownloadCUDA(int optionIndex)
        {
            List<string> CUDAUrls = new List<string>();
            CUDAUrls.AddRange(CUDAOptions[optionIndex].Item2);
            if (SelectedCUDA > 0)
            {
                string[] currentCUDAUrls = CUDAOptions[SelectedCUDA].Item2;
                foreach (string CUDAUrl in currentCUDAUrls)
                {
                    if (!CUDAUrls.Contains(CUDAUrl)) RemoveCUDA(GetCUDAPath(CUDAUrl));
                }
            }

            SelectedCUDA = optionIndex;
            if (CUDAUrls == null) return;
            CUDAProgress = 0;
            CUDAbinariesWIP = CUDAUrls.Count * 1.2f; // 0.2 for extraction
            CUDAbinariesDone = 0;
            foreach (string CUDAUrl in CUDAUrls)
            {
                string CUDAPath = GetCUDAPath(CUDAUrl);
                if (!File.Exists(CUDAPath)) await DownloadFile(CUDAUrl, CUDAPath, false, null, SetCUDAProgress);
                CUDAbinariesDone += 1f;
                await SetupCUDA(CUDAPath);
                CUDAbinariesDone += 0.2f;
            }
            CUDAProgress = 1f;
        }

        public static Task SetupCUDA(string path)
        {
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                float progress = 0f;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destination = GetCUDAFilePath(entry.FullName);
                    if (!File.Exists(destination))
                    {
                        AssetDatabase.StartAssetEditing();
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        entry.ExtractToFile(destination);
                        AssetDatabase.StopAssetEditing();
                    }
                    progress += 0.2f / archive.Entries.Count;
                    SetCUDAProgress(progress);
                }
            }
            return Task.CompletedTask;
        }

        public static void RemoveCUDA(string path)
        {
            if (!File.Exists(path)) return;
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destination = GetCUDAFilePath(entry.FullName);
                    if (File.Exists(destination + ".meta")) File.Delete(destination + ".meta");
                    if (File.Exists(destination)) File.Delete(destination);
                }
            }
        }

        static void SetCUDAProgress(float progress)
        {
            CUDAProgress = (CUDAbinariesDone + progress) / CUDAbinariesWIP;
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
