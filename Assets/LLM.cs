using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class LLMSettings
{
    public string server;
    public string model;
}

public class LLM : LLMClient
{
    [HideInInspector] public bool modelHide = true;
    [HideInInspector] public bool serverHide = true;

    [ServerAttribute] public string server = "";
    [ServerAttribute] public int numThreads = -1;

    [ModelAttribute] public string model = "";
    [ModelAttribute] public int contextSize = 512;
    [ModelAttribute] public int batchSize = 512;

    [HideInInspector] public string modelUrl = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.1-GGUF/resolve/main/mistral-7b-instruct-v0.1.Q4_K_M.gguf?download=true";
    private LLMSettings settings;
    private string settingsPath = "settings.json";
    private bool isServerStarted = false;
    private bool modelCopying = false;
    private Process process;

    private string getAssetPath(string relPath=""){
        return Path.Combine(Application.streamingAssetsPath, relPath);
    }

    public LLM() {
        LoadSettings();

        #if UNITY_EDITOR
        LLMUnitySetup.AddServerPathLinks(SetServer);
        LLMUnitySetup.AddModelPathLinks(SetModel);
        #endif
    }

    #if UNITY_EDITOR
    public void RunSetup(){
        LLMUnitySetup.Setup(getAssetPath("server"));
    }

    public bool SetupStarted(){
        return LLMUnitySetup.SetupStarted();
    }

    public void DownloadModel(){
        string modelName = Path.GetFileName(modelUrl).Split("?")[0];
        StartCoroutine(LLMUnitySetup.DownloadFile(modelUrl, getAssetPath(modelName)));
    }

    public bool ModelDownloading(){
        return LLMUnitySetup.ModelDownloading();
    }
    public bool ModelCopying(){
        return modelCopying;
    }

    private void SaveSettings()
    {
        File.WriteAllText(getAssetPath(settingsPath), JsonUtility.ToJson(settings));
    }

    public async void SetServer(string serverPath){
        settings.server = await LLMUnitySetup.AddAsset(serverPath, getAssetPath());
        SaveSettings();
        server = getAssetPath(settings.server);
    }
    public async void SetModel(string modelPath){
        modelCopying = true;
        settings.model = await LLMUnitySetup.AddAsset(modelPath, getAssetPath());
        SaveSettings();
        model = getAssetPath(settings.model);
        modelCopying = false;
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

    private string ExtendString(string s, string s2){
        if (s=="") return s2;
        return s + "\n" + s2;
    }

    private async void StartLLMServer()
    {
        string error = "";
        string serverPath = getAssetPath(settings.server);
        string modelPath = getAssetPath(settings.model);
        if (settings.server == "") error = ExtendString(error, "No server file provided!");
        else if (!File.Exists(serverPath)) error = ExtendString(error, $"File {serverPath} not found!");
        if (settings.model == "") error = ExtendString(error, "No model file provided!");
        else if (!File.Exists(modelPath)) error = ExtendString(error, $"File {modelPath} not found!");
        if (error!="") throw new System.Exception(error);

        string arguments = $"--port {port} -m {modelPath} -c {contextSize} -b {batchSize} --log-disable";
        if (numThreads > 0) arguments += $" -t {numThreads}";
        Debug.Log($"Server command: {serverPath} {arguments}");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (sender, e) => { Debug.Log(e.Data); };
        process.Start();
        process.BeginOutputReadLine();

        // Wait until the server is started
        while (true){
            try {
                await Tokenize("");
            } catch (System.Exception e){
                continue;
            }
            break;
        }
        Debug.Log("LLM Server started!");
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
