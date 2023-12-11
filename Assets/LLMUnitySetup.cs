using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

[InitializeOnLoad]
public class LLMUnitySetup: MonoBehaviour
{
    public delegate void UpdatePath(string message);
    private static List<UpdatePath> serverPathLinks = new List<UpdatePath>();
    private static List<UpdatePath> modelPathLinks = new List<UpdatePath>();
    private static bool setupStarted = false;
    private static bool modelDownloading = false;

    public static void AddServerPathLinks(UpdatePath link)
    {
        serverPathLinks.Add(link);
    }
    public static void AddModelPathLinks(UpdatePath link)
    {
        modelPathLinks.Add(link);
    }

    public static void Setup(string serverPath)
    {
        if (File.Exists(serverPath)){
            foreach (UpdatePath link in serverPathLinks){
                link(serverPath);
            }
            Debug.Log("LLMUnity setup already completed!");
            return;
        }

        if (setupStarted) return;

        setupStarted = true;
        Debug.Log("LLMUnity setup started...");
        // Define the GitHub repository URL
        string os = SystemInfo.operatingSystem.ToLower();
        string repoURL = "https://github.com/ggerganov/llama.cpp.git";
        string repoVersion = "b1621";
        string exeName = "server";
        string buildPath = "llama.cpp";

        // Use a separate thread for the Git clone and setup operation
        System.Threading.Thread cloneThread = new System.Threading.Thread(() =>
        {
            // Clone the GitHub repository locally
            CloneRepository(repoURL, repoVersion, buildPath);

            // Perform setup actions now that cloning is complete
            SetupRepository(buildPath, exeName, os);

            // Set the flag to signal that the clone and setup are complete
            CheckSetup(buildPath, exeName, serverPath);
        });

        // Start the clone thread
        cloneThread.Start();

        // Register an update event to periodically check if the clone and setup are complete
        EditorApplication.update += Update;

        // Refresh the Asset Database to make Unity aware of the new files
        AssetDatabase.Refresh();
    }

    private static void CloneRepository(string repoURL, string repoVersion, string localPath)
    {
        if (Directory.Exists(localPath))
        {
            Debug.Log("Repository already exists. Skipping cloning.");
            return;
        }
        RunProcess("git", $"clone -b {repoVersion} {repoURL} {localPath}", "Clone llama.cpp");
    }

    private static void SetupRepository(string repoFolder, string exeName, string os)
    {
        string setupCommand = $"cd {repoFolder} && make -j 8 {exeName}";
        string command, commandArgs;

        if (os.Contains("win")){
            command = "cmd.exe";
            commandArgs = $"/c {setupCommand}";
        }
        else {
            command = "/bin/bash";
            commandArgs = $"-c \"{setupCommand}\"";
        }
        RunProcess(command, commandArgs, "Build llama.cpp");
    }

    public static bool SetupStarted(){
        return setupStarted;
    }

    private static void CheckSetup(string buildPath, string exeName, string serverPath){
        string exePath = buildPath + "/" + exeName;
        if (File.Exists(exePath)){
            string saveDir = Path.GetDirectoryName(serverPath);
            Directory.CreateDirectory(saveDir);
            File.Copy(exePath, serverPath);
            foreach (UpdatePath link in serverPathLinks){
                link(serverPath);
            }
            Directory.Delete(buildPath, true);
            Debug.Log("LLMUnity setup complete!");
        } else {
            Debug.Log("LLMUnity setup failed!");
        }
        EditorApplication.update -= Update;
        setupStarted = false;
    }

    private static void RunProcess(string command, string commandArgs, string debugMessage){
        Debug.Log(debugMessage + "...");
        Debug.Log("Running: " + command + " " + commandArgs);
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = commandArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processInfo };
        process.Start();
        process.ErrorDataReceived += (sender, e) => DebugErrors(e.Data);
        process.WaitForExit();
        Debug.Log(debugMessage + " completed");
    }

    private static void DebugErrors(string errorOutput){
        if(errorOutput.Contains("detached HEAD")) return;
        if(errorOutput.Trim() == "") return;
        Debug.LogError(errorOutput);
    }

    public static IEnumerator<UnityWebRequestAsyncOperation> DownloadFile(string fileUrl, string savePath)
    {
        if (!modelDownloading){
            if (File.Exists(savePath)){
                foreach (UpdatePath link in modelPathLinks){
                    link(savePath);
                }
                Debug.Log("Model already exists at: " + savePath);
            } else {
                Debug.Log("Downloading model from: " + fileUrl);
                string saveDir = Path.GetDirectoryName(savePath);
                Directory.CreateDirectory(saveDir);
                using (UnityWebRequest webRequest = UnityWebRequest.Get(fileUrl))
                {
                    modelDownloading = true;
                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
                        foreach (UpdatePath link in modelPathLinks){
                            link(savePath);
                        }
                        Debug.Log("Model downloaded and saved at: " + savePath);
                    }
                    else
                    {
                        Debug.LogError("Download failed: " + webRequest.error);
                    }
                    modelDownloading = false;
                }
            }
        }
    }
    public static bool ModelDownloading(){
        return modelDownloading;
    }

    private static void Update(){}
}
