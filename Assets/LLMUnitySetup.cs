using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using System.Collections.Generic;

public class LLMUnitySetup: MonoBehaviour
{
    public delegate void StringCallback(string message);

    public static Process CreateProcess(
        string command, string commandArgs="",
        StringCallback outputCallback=null, StringCallback errorCallback=null,
        List<(string, string)> environment = null,
        bool redirectOutput=false, bool redirectError=false
    ){
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = commandArgs,
            RedirectStandardOutput = redirectOutput || outputCallback != null,
            RedirectStandardError = redirectError || errorCallback != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (environment != null){
            foreach ((string name, string value) in environment){
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

    public static string RunProcess(string command, string commandArgs="", StringCallback outputCallback=null, StringCallback errorCallback=null){
        Process process = CreateProcess(command, commandArgs, null, null, null, true);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

#if UNITY_EDITOR
    public async static Task DownloadFile(string fileUrl, string savePath, bool debug=true)
    {
        if (File.Exists(savePath)){
            if(debug) Debug.Log($"File already exists at: {savePath}");
        } else {
            if (debug) Debug.Log($"Downloading file: {fileUrl}");
            string saveDir = Path.GetDirectoryName(savePath);
            Directory.CreateDirectory(saveDir);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(fileUrl))
            {
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    AssetDatabase.StartAssetEditing();
                    File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
                    AssetDatabase.StopAssetEditing();
                    if (debug) Debug.Log($"File downloaded and saved at: {savePath}");
                }
                else
                {
                    if (debug) Debug.LogError($"Download failed: {webRequest.error}");
                }
            }
        }
    }

    public static async Task<string> AddAsset(string assetPath, string basePath){
        string fullPath = Path.GetFullPath(assetPath);
        if (!fullPath.StartsWith(basePath)){
            // if the asset is not in the assets dir copy it over
            fullPath = Path.Combine(basePath, Path.GetFileName(assetPath));
            Debug.Log("copying " + assetPath + " to " + fullPath);
            AssetDatabase.StartAssetEditing();
            await Task.Run(() =>
            {
                foreach (string filename in new string[] {fullPath, fullPath + ".meta"}){
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
#endif
}