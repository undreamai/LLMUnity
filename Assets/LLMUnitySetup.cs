using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class LLMUnitySetup: MonoBehaviour
{
    private static bool setupComplete = false;
    public static void Setup()
    {
        Debug.Log("LLMUnity setup started...");
        // Define the GitHub repository URL
        string os = SystemInfo.operatingSystem.ToLower();
        string repoURL = "https://github.com/ggerganov/llama.cpp.git";
        string repoVersion = "b1607";
        string localPath = "LLM/llama.cpp";

        // Use a separate thread for the Git clone and setup operation
        System.Threading.Thread cloneThread = new System.Threading.Thread(() =>
        {
            // Clone the GitHub repository locally
            CloneRepository(repoURL, repoVersion, localPath);

            // Perform setup actions now that cloning is complete
            SetupRepository(localPath, os);

            // Set the flag to signal that the clone and setup are complete
            setupComplete = true;
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
        Directory.CreateDirectory(Path.GetDirectoryName(localPath));
        if (Directory.Exists(localPath))
        {
            Debug.Log("Repository already exists. Skipping cloning.");
            return;
        }
        RunProcess("git", $"clone -b {repoVersion} {repoURL} {localPath}", "Clone llama.cpp");
    }

    private static void SetupRepository(string repoFolder, string os)
    {
        string setupCommand = $"cd {repoFolder} && make -j 8";
        string command, commandArgs;

        if (os.Contains("win")){
            command = "cmd.exe";
            commandArgs = $"/c {setupCommand}";
        }
        else {
            command = "/bin/bash";
            commandArgs = $"-c \"{setupCommand}\"";
        }
        RunProcess(command, commandArgs, "Setup llama.cpp");
    }

    private static void RunProcess(string command, string commandArgs, string debugMessage){
        Debug.Log(debugMessage + "...");
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

    private static void Update()
    {
        // Check if the clone and setup are complete
        if (setupComplete)
        {
            Debug.Log("LLMUnity setup completed!");
            // Unregister the update event
            EditorApplication.update -= Update;

            // Additional actions if needed
        }
    }
}
