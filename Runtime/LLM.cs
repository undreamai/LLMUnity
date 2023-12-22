using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LLM : LLMClient
{
    [HideInInspector] public bool modelHide = true;

    [ServerAttribute] public int numThreads = -1;
    [ServerAttribute] public int numGPULayers = 0;
    [ServerAttribute] public bool debug = false;

    [ModelAttribute] public string model = "";
    [ModelAttribute] public string lora = "";
    [ModelAttribute] public int contextSize = 512;
    [ModelAttribute] public int batchSize = 512;

    [HideInInspector] public string modelUrl = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.1-GGUF/resolve/main/mistral-7b-instruct-v0.1.Q4_K_M.gguf?download=true";
    private readonly string server = GetAssetPath("llamafile-server.exe");
    private readonly string apeARM = GetAssetPath("ape-arm64.elf");
    private readonly string apeX86_64 = GetAssetPath("ape-x86_64.elf");

    public bool modelWIP = false;
    private Process process;
    private bool serverListening = false;
    private static ManualResetEvent serverStarted = new ManualResetEvent(false);

    private static string GetAssetPath(string relPath=""){
        // Path to store llm server binaries and models
        return Path.Combine(Application.streamingAssetsPath, relPath);
    }

    #if UNITY_EDITOR
    public void DownloadModel(){
        // download default model and disable model editor properties until the model is set
        modelWIP = true;
        string modelName = Path.GetFileName(modelUrl).Split("?")[0];
        string modelPath = GetAssetPath(modelName);
        LLMUnitySetup.DownloadFile(modelUrl, modelPath, SetModel);
    }

    public async void SetModel(string path){
        // set the model and enable the model editor properties
        modelWIP = true;
        model = await LLMUnitySetup.AddAsset(path, GetAssetPath());
        modelWIP = false;
    }

    public async void SetLora(string path){
        // set the lora and enable the model editor properties
        modelWIP = true;
        lora = await LLMUnitySetup.AddAsset(path, GetAssetPath());
        modelWIP = false;
    }
    #endif

    new void OnEnable()
    {
        // start the llm server and run the OnEnable of the client
        StartLLMServer();
        base.OnEnable();
    }

    private string SelectApeBinary(){
        // select the corresponding APE binary for the system architecture
        string arch = LLMUnitySetup.RunProcess("uname", "-m");
        Debug.Log($"architecture: {arch}");
        string apeExe;
        if (arch.Contains("arm64") || arch.Contains("aarch64")) {
            apeExe = apeARM;
        } else {
            apeExe = apeX86_64;
            if (!arch.Contains("x86_64"))
                Debug.Log($"Unknown architecture of processor {arch}! Falling back to x86_64");
        }
        return apeExe;
    }

    private void DebugLog(string message, bool logError = false){
        // Debug log if debug is enabled
        if (!debug || message == null) return;
        if (logError) Debug.LogError(message);
        else Debug.Log(message);
    }

    private void DebugLogError(string message){
        // Debug log errors if debug is enabled
        DebugLog(message, true);
    }

    private void CheckIfListening(string message){
        // Read the output of the llm binary and check if the server has been started and listening
        DebugLog(message);
        if (serverListening) return;
        try {
            ServerStatus status = JsonUtility.FromJson<ServerStatus>(message);
            if (status.message == "HTTP server listening"){
                Debug.Log("LLM Server started!");
                serverStarted.Set();
                serverListening = true;
            }
        } catch {}
    }

    private void StartLLMServer()
    {
        // Start the LLM server in a cross-platform way
        if (model == "") throw new System.Exception("No model file provided!");
        string modelPath = GetAssetPath(model);
        if (!File.Exists(modelPath)) throw new System.Exception($"File {modelPath} not found!");

        string loraPath = "";
        if (lora != ""){
            loraPath = GetAssetPath(lora);
            if (!File.Exists(loraPath)) throw new System.Exception($"File {loraPath} not found!");
        }

        string binary = server;
        string arguments = $" --port {port} -m {modelPath} -c {contextSize} -b {batchSize} --log-disable --nobrowser";
        if (numThreads > 0) arguments += $" -t {numThreads}";
        if (numGPULayers > 0) arguments += $" -ngl {numGPULayers}";
        if (loraPath != "") arguments += $" --lora {loraPath}";
        List<(string, string)> environment = null;

        if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer){
            // use APE binary directly if not on Windows
            arguments = $"{binary} {arguments}";
            binary = SelectApeBinary();
            if (numGPULayers <= 0){
                // prevent nvcc building if not using GPU
                environment = new List<(string, string)> {("PATH", ""), ("CUDA_PATH", "")};
            }
        }
        Debug.Log($"Server command: {binary} {arguments}");
        process = LLMUnitySetup.CreateProcess(binary, arguments, CheckIfListening, DebugLogError, environment);
        serverStarted.WaitOne();
    }

    public void StopProcess()
    {
        // kill the llm server
        if (process != null && !process.HasExited)
        {
            process.Kill();
            process.WaitForExit();
        }
    }

    public void OnDestroy()
    {
        StopProcess();
    }
}
