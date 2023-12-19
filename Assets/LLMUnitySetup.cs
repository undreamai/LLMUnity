#if UNITY_EDITOR
using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;

[InitializeOnLoad]
public class LLMUnitySetup: MonoBehaviour
{
    public delegate void Callback();
    public delegate void StringCallback(string message);

    public static void SetLinuxExecutable(string filePath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filePath){ IsReadOnly = false};
            fileInfo.Attributes |= FileAttributes.System; // Ensure the file is not marked as system file
            fileInfo.Attributes &= ~FileAttributes.ReadOnly; // Clear read-only attribute
            fileInfo.Attributes |= FileAttributes.Archive; // Ensure the file is not marked as archive
            fileInfo.Attributes |= FileAttributes.Normal;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting execute permissions: {e.Message}");
        }
    }

    public static Process CreateProcess(
        string command, string commandArgs="",
        StringCallback outputCallback=null, StringCallback errorCallback=null,
        bool beginOutputRead=true, bool beginErrorRead=true
    ){
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = commandArgs,
            RedirectStandardOutput = outputCallback != null,
            RedirectStandardError = errorCallback != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processInfo };
        if (outputCallback != null) process.OutputDataReceived += (sender, e) => outputCallback(e.Data);
        if (errorCallback != null) process.ErrorDataReceived += (sender, e) => errorCallback(e.Data);
        process.Start();
        if (outputCallback != null && beginOutputRead) process.BeginOutputReadLine();
        if (errorCallback != null && beginErrorRead) process.BeginErrorReadLine();
        return process;
    }

    public static string RunProcess(string command, string commandArgs="", StringCallback outputCallback=null, StringCallback errorCallback=null){
        Process process = CreateProcess(command, commandArgs, _=>{}, null, false, false);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

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

    private static void Update(){}
}
#endif