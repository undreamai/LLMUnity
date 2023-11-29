using System.Diagnostics;
using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunExecutable : MonoBehaviour
{
    private Process process;

    void Start()
    {
        // Replace "full/path/to/your_executable" and the parameters with your actual values
        RunProcess(
            "Assets/Plugins/llama.cpp/main",
            @"-m /home/benuix/codes/llama.cpp/llama-2-7b-chat.Q4_0.gguf -ngl 32 -s 1234 -c 512 -b 1024 -n 256 --keep 48 --repeat_penalty 1.0 -i -r ""User:"" -f /home/benuix/codes/llama.cpp/prompts/chat-with-bob.txt"
        );
    }

    void RunProcess(string processPath, string arguments)
    {
        process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();

        startInfo.FileName = processPath;
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = true;

        process.StartInfo = startInfo;
        process.OutputDataReceived += ProcessOutput;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Start a coroutine to check if the process has exited
        StartCoroutine(CheckProcessExit());
    }

    IEnumerator CheckProcessExit()
    {
        while (!process.HasExited)
        {
            yield return null;
        }

        // Process has exited, you can handle any cleanup here
        Debug.Log("Process has exited.");

        process.Close();
    }

    // You can send input to the process like this
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            WriteToInput("YourInputHere");
        }
    }


    private void ProcessOutput(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            // Handle the output data here
            Debug.Log("Output from external process: " + e.Data);
        }
    }

    void WriteToInput(string input)
    {
        process.StandardInput.WriteLine(input);
    }

    void OnApplicationQuit()
    {
        if (process != null && !process.HasExited)
        {
            process.CloseMainWindow();
            process.Kill();
        }
    }
}
