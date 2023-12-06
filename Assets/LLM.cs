using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LLM : LLMClient
{
    // [HideAttribute("setupHide", "Server Settings")] 
    [HeaderAttribute("Server Settings")]
    [SerializeField]
    private string server = "";
    public string Server
    {
        get { return server; }
        set { if (server != value){ server = value; SetupGUI();} }
    }
    [SerializeField]
    private string model = "";
    public string Model
    {
        get { return model; }
        set { if (model != value){ model = value; SetupGUI();} }
    }
    [HideAttribute("setupHide")] public int contextSize = 512;
    [HideAttribute("setupHide")] public int batchSize = 1024;
    [HideAttribute("setupHide")] public int numGPULayers = 32;
    [HideAttribute("setupHide")] public int numThreads = 18;

    private bool isServerStarted = false;
    private Process process;

    public LLM() {
        LLMUnitySetup.AddServerPathLinks(SetServer);
        SetupGUI();
    }

    public void SetupGUI(){
        setupHide = (Server == "") || (Model == "");
    }

    public void RunSetup(){
        LLMUnitySetup.Setup();
    }

    public bool SetupStarted(){
        return LLMUnitySetup.SetupStarted();
    }

    public void SetServer(string serverPath){
        Server = serverPath;
    }

    new void OnEnable()
    {
        base.OnEnable();
        StartLLMServer();
    }

    private void StartLLMServer()
    {
        string arguments = $"-m {model} -c {contextSize} -b {batchSize} --port {port} -t {numThreads} -ngl {numGPULayers}";
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "LLM/llama.cpp/server",
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
