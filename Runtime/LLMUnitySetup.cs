using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;

namespace LLMUnity
{
    public delegate void EmptyCallback();
    public delegate void Callback<T>(T message);
    public delegate Task TaskCallback<T>(T message);
    public delegate T2 ContentCallback<T, T2>(T message);

    public class LLMUnitySetup : MonoBehaviour
    {
        public static Process CreateProcess(
            string command, string commandArgs = "",
            Callback<string> outputCallback = null, Callback<string> errorCallback = null, System.EventHandler exitCallback = null,
            List<(string, string)> environment = null,
            bool redirectOutput = false, bool redirectError = false
        )
        {
            // create and start a process with output/error callbacks
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = commandArgs,
                RedirectStandardOutput = redirectOutput || outputCallback != null,
                RedirectStandardError = redirectError || errorCallback != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (environment != null)
            {
                foreach ((string name, string value) in environment)
                {
                    startInfo.EnvironmentVariables[name] = value;
                }
            }
            Process process = new Process { StartInfo = startInfo };
            if (outputCallback != null) process.OutputDataReceived += (sender, e) => outputCallback(e.Data);
            if (errorCallback != null) process.ErrorDataReceived += (sender, e) => errorCallback(e.Data);
            if (exitCallback != null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += exitCallback;
            }
            process.Start();
            if (outputCallback != null) process.BeginOutputReadLine();
            if (errorCallback != null) process.BeginErrorReadLine();
            return process;
        }

        public static string RunProcess(string command, string commandArgs = "", Callback<string> outputCallback = null, Callback<string> errorCallback = null)
        {
            // run a process and re#turn the output
            Process process = CreateProcess(command, commandArgs, null, null, null, null, true);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        public static void makeExecutable(string path)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
            {
                // macOS/Linux: Set executable permissions using chmod
                RunProcess("chmod", $"+x {path.Replace(" ", "' '")}");
            }
        }

#if UNITY_EDITOR
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
            string fileUrl, string savePath, bool overwrite = false, bool executable = false,
            TaskCallback<string> callback = null, Callback<float> progresscallback = null)
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
                await client.DownloadFileTaskAsync(fileUrl, tmpPath);
                if (executable) makeExecutable(tmpPath);

                AssetDatabase.StartAssetEditing();
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.Move(tmpPath, savePath);
                AssetDatabase.StopAssetEditing();
                Debug.Log($"Download complete!");

                progresscallback?.Invoke(1f);
                callback?.Invoke(savePath);
            }
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

        public static void ExtractZip(string zipPath, string extractToPath)
        {
            Debug.Log($"extracting {zipPath} to {extractToPath}");
            AssetDatabase.StartAssetEditing();
            if (!Directory.Exists(extractToPath))
            {
                Directory.CreateDirectory(extractToPath);
            }
            ZipFile.ExtractToDirectory(zipPath, extractToPath);
            AssetDatabase.StopAssetEditing();
            Debug.Log($"extraction complete!");
        }

#endif
    }
}
