using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
    private bool isServerStarted = false;
    private bool modelCopying = false;
    private Process process;

    private string getAssetPath(string relPath=""){
        return Path.Combine(Application.streamingAssetsPath, relPath);
    }

    #if UNITY_EDITOR
    public LLM() {
        LLMUnitySetup.AddServerPathLinks(SetServer);
        LLMUnitySetup.AddModelPathLinks(SetModel);
    }

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

    public async void SetServer(string serverPath){
        string relPath = await LLMUnitySetup.AddAsset(serverPath, getAssetPath());
        server = getAssetPath(relPath);
        PlayerPrefs.SetString("serverPath", relPath);
        PlayerPrefs.Save();
    }
    public async void SetModel(string modelPath){
        modelCopying = true;
        string relPath = await LLMUnitySetup.AddAsset(modelPath, getAssetPath());
        model = getAssetPath(relPath);
        PlayerPrefs.SetString("modelPath", relPath);
        PlayerPrefs.Save();
        modelCopying = false;
    }
    #endif

    new void OnEnable()
    {
        StartLLMServer();
        base.OnEnable();
    }

    private string ExtendString(string s, string s2){
        if (s=="") return s2;
        return s + "\n" + s2;
    }

    private (string, string) CheckAsset(string asset){
        string path = PlayerPrefs.GetString(asset);
        if (path == "") return ("", $"No {asset} provided!");
        string fullPath = getAssetPath(path);
        if (!File.Exists(fullPath)) return ("", $"File {fullPath} not found!");
        return (fullPath, "");
    }

    private void StartLLMServer()
    {

        string serverPath, modelPath, serverPathError, modelPathError;
        (serverPath, serverPathError) = CheckAsset("serverPath");
        (modelPath, modelPathError) = CheckAsset("modelPath");
        string error = ExtendString(serverPathError, modelPathError);
        if (error!="") throw new System.Exception(error);

        if (numThreads == -1)
            numThreads = System.Environment.ProcessorCount;
        string arguments = $"--port {port} -m {modelPath} -c {contextSize} -b {batchSize}";
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
        process.OutputDataReceived += (sender, e) => { HandleOutput(e.Data); };
        process.Start();
        process.BeginOutputReadLine();

        // Wait until the server is started
        while (!isServerStarted){}
        Debug.Log("LLM Server started!");
    }

    private void HandleOutput(string data)
    {
        if (data != null && data.Contains("HTTP server listening"))
            isServerStarted = true;
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
