using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class LLM : LLMClient
{
    public string model = "LLM/Models/llama-2-7b-chat.Q4_0.gguf";
    public string lora;
    public int contextSize = 512;
    public int batchSize = 1024;
    public int numGPULayers = 32;
    public int numThreads = 18;

    private bool isServerStarted = false;
    private Process process;

    new void OnEnable()
    {
        base.OnEnable();
        StartLLMServer();
    }

    private void StartLLMServer()
    {
        string arguments = $"-m {model} -c {contextSize} -b {batchSize} --port {port} -t {numThreads} -ngl {numGPULayers}";
        if (lora != null)
            arguments += $"--lora {lora}";
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
