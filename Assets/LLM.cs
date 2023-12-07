using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LLM : LLMClient
{
    [HideInInspector] public bool modelHide = true;
    [HideInInspector] public bool serverHide = true;

    [ServerAttribute] public string server = "";
    [ServerAttribute] public int numGPULayers = 32;
    [ServerAttribute] public int numThreads = 18;

    [ModelAttribute] public string model = "";
    [ModelAttribute] public int contextSize = 512;
    [ModelAttribute] public int batchSize = 1024;

    private bool isServerStarted = false;
    private Process process;
    private string modelUrl = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.1-GGUF/resolve/main/mistral-7b-instruct-v0.1.Q4_K_M.gguf?download=true";
    private readonly string modelDir = "Assets/Models";

    public LLM() {
        LLMUnitySetup.AddServerPathLinks(SetServer);
        LLMUnitySetup.AddModelPathLinks(SetModel);
    }

    public void RunSetup(){
        LLMUnitySetup.Setup();
    }

    public bool SetupStarted(){
        return LLMUnitySetup.SetupStarted();
    }

    public void DownloadModel(){
        string modelName = Path.GetFileName(modelUrl).Split("?")[0];
        string modelPath = modelDir + '/' + modelName;
        StartCoroutine(LLMUnitySetup.DownloadFile(modelUrl, modelPath));
    }

    public void SetServer(string serverPath){
        server = serverPath;
    }
    public void SetModel(string modelPath){
        model = modelPath;
    }

    new void OnEnable()
    {
        base.OnEnable();
        StartLLMServer();
    }

    private void StartLLMServer()
    {
        if (server == "" || model == ""){
            if (server == "") Debug.LogError("No server executable provided!");
            if (model == "") Debug.LogError("No model provided!");
            return;
        }

        string arguments = $"-m {model} -c {contextSize} -b {batchSize} --port {port} -t {numThreads} -ngl {numGPULayers}";
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = server,
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
