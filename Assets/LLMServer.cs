using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class LLMServer : MonoBehaviour
{
    private bool isServerStarted = false;
    private Process process;

    void OnEnable()
    {
        StartLLMServer();
    }

    private void StartLLMServer()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "LLM/llama.cpp/server",
            Arguments = "-m LLM/Models/llama-2-7b-chat.Q4_0.gguf -c 512 -b 1024 --port 13333 -ngl 128",
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
