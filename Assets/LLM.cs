using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class LLMSettings
{
    public string model;
}

public class LLM : LLMClient
{
    [HideInInspector] public bool modelHide = true;

    private string server = getAssetPath("llamafile-server.exe");
    private string apeARM = getAssetPath("ape-arm64.elf");
    private string apeX86_64 = getAssetPath("ape-x86_64.elf");
    [ServerAttribute] public int numThreads = -1;
    [ServerAttribute] public bool debug = false;

    [ModelAttribute] public string model = "";
    [ModelAttribute] public int contextSize = 512;
    [ModelAttribute] public int batchSize = 512;

    [HideInInspector] public string modelUrl = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.1-GGUF/resolve/main/mistral-7b-instruct-v0.1.Q4_K_M.gguf?download=true";
    private LLMSettings settings;
    private string settingsPath = "settings.json";
    public bool modelWIP = false;
    private Process process;
    private bool serverListening = false;
    private static ManualResetEvent serverStarted = new ManualResetEvent(false);

    private static string getAssetPath(string relPath=""){
        return Path.Combine(Application.streamingAssetsPath, relPath);
    }

    public LLM() {
        LoadSettings();
    }

    #if UNITY_EDITOR
    private void SaveSettings()
    {
        File.WriteAllText(getAssetPath(settingsPath), JsonUtility.ToJson(settings));
    }

    public async void DownloadModel(){
        modelWIP = true;
        string modelName = Path.GetFileName(modelUrl).Split("?")[0];
        string modelPath = getAssetPath(modelName);
        await LLMUnitySetup.DownloadFile(modelUrl, modelPath);
        SetModel(modelName);
    }

    public async void LoadModel(string modelPath){
        modelWIP = true;
        SetModel(await LLMUnitySetup.AddAsset(modelPath, getAssetPath()));
    }

    public void SetModel(string modelPath){
        model = getAssetPath(modelPath);
        settings.model = modelPath;
        SaveSettings();
        modelWIP = false;
    }
    #endif

    public void LoadSettings()
    {
        string settingsFullPath = getAssetPath(settingsPath);
        if (File.Exists(settingsFullPath))
            settings = JsonUtility.FromJson<LLMSettings>(File.ReadAllText(settingsFullPath));
        else
            settings = new LLMSettings();
    }

    new void OnEnable()
    {
        StartLLMServer();
        base.OnEnable();
    }

    private string selectApeBinary(){
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
        if (!debug || message == null) return;
        if (logError) Debug.LogError(message);
        else Debug.Log(message);
    }

    private void DebugLogError(string message){
        DebugLog(message, true);
    }

    private void CheckIfListening(string message){
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
        string modelPath = getAssetPath(settings.model);
        if (settings.model == "") throw new System.Exception("No model file provided!");
        if (!File.Exists(modelPath)) throw new System.Exception($"File {modelPath} not found!");

        string binary = server;
        string arguments = $" --port {port} -m {modelPath} -c {contextSize} -b {batchSize} --log-disable --nobrowser";
        if (numThreads > 0) arguments += $" -t {numThreads}";

        if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer){
            arguments = $"{binary} {arguments}";
            binary = selectApeBinary();
        }
        Debug.Log($"Server command: {binary} {arguments}");
        process = LLMUnitySetup.CreateProcess(binary, arguments, CheckIfListening, DebugLogError);
        serverStarted.WaitOne();
    }

    public void StopProcess()
    {
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
