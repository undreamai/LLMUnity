using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;

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
            Callback<string> outputCallback = null, Callback<string> errorCallback = null,
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
            process.Start();
            if (outputCallback != null) process.BeginOutputReadLine();
            if (errorCallback != null) process.BeginErrorReadLine();
            return process;
        }

        public static string RunProcess(string command, string commandArgs = "", Callback<string> outputCallback = null, Callback<string> errorCallback = null)
        {
            // run a process and re#turn the output
            Process process = CreateProcess(command, commandArgs, null, null, null, true);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

#if UNITY_EDITOR
        public static void makeExecutable(string path)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
            {
                // macOS/Linux: Set executable permissions using chmod
                RunProcess("chmod", $"+x \"{path}\"");
            }
        }

        public static async Task DownloadFile(
            string fileUrl, string savePath, bool executable = false,
            TaskCallback<string> callback = null, Callback<float> progresscallback = null,
            int chunkSize = 1024 * 1024)
        {
            // download a file to the specified path
            if (File.Exists(savePath))
            {
                Debug.Log($"File already exists at: {savePath}");
            }
            else
            {
                Debug.Log($"Downloading {fileUrl}...");

                UnityWebRequest www = UnityWebRequest.Head(fileUrl);
                UnityWebRequestAsyncOperation asyncOperation = www.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    await Task.Delay(100); // Adjust the delay as needed
                }

                if (www.result != UnityWebRequest.Result.Success)
                    throw new System.Exception("Failed to get file size. Error: " + www.error);

                long fileSize = long.Parse(www.GetResponseHeader("Content-Length"));

                AssetDatabase.StartAssetEditing();
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    for (long i = 0; i < fileSize; i += chunkSize)
                    {
                        long startByte = i;
                        long endByte = (long)Mathf.Min(i + chunkSize - 1, fileSize - 1);

                        www = UnityWebRequest.Get(fileUrl);
                        www.SetRequestHeader("Range", "bytes=" + startByte + "-" + endByte);

                        asyncOperation = www.SendWebRequest();

                        while (!asyncOperation.isDone)
                        {
                            await Task.Delay(100); // Adjust the delay as needed
                        }

                        if (www.result != UnityWebRequest.Result.Success)
                            throw new System.Exception("Download failed. Error: " + www.error);

                        fs.Write(www.downloadHandler.data, 0, www.downloadHandler.data.Length);

                        int progressPercentage = Mathf.FloorToInt((float)i / fileSize * 100);
                        if (progressPercentage % 1 == 0)
                            progresscallback((float)progressPercentage / 100);
                    }
                }

                if (executable) makeExecutable(savePath);
                AssetDatabase.StopAssetEditing();
                Debug.Log($"Download complete!");
            }
            progresscallback(1f);
            callback?.Invoke(savePath);
        }

        public static async Task<string> AddAsset(string assetPath, string basePath)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogError($"{assetPath} does not exist!");
                return null;
            }
            // add an asset to the basePath directory if it is not already there and return the relative path
            Directory.CreateDirectory(basePath);
            string fullPath = Path.GetFullPath(assetPath);
            if (!fullPath.StartsWith(basePath))
            {
                // if the asset is not in the assets dir copy it over
                fullPath = Path.Combine(basePath, Path.GetFileName(assetPath));
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
            return fullPath.Substring(basePath.Length + 1);
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
