using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class CallLLM : MonoBehaviour
{
    #if UNITY_STANDALONE_WIN
    const string dllName = "llm.dll";
    #elif UNITY_STANDALONE_LINUX
    const string dllName = "libllm.so";
    #else
    const string dllName = null; // Unsupported platform
    #endif


    [DllImport(dllName)]
    public static extern IntPtr LLM_Create(string command);
    [DllImport(dllName)]
    public static extern void LLM_Answer(IntPtr LLM);
    [DllImport(dllName)]
    public static extern void LLM_Query(IntPtr LLM, string query);

    private IntPtr LLM;

    // Start is called before the first frame update
    void Start()
    {
        if (dllName == null) throw new Exception("Unsupported platform");
        // Call the function from the DLL
        string command = @"-m /home/benuix/codes/llama.cpp/llama-2-7b-chat.Q4_0.gguf -ngl 32 -s 1234 -c 512 -b 1024 -n 256 --keep 48 --repeat_penalty 1.0 -i -r ""User:"" -f /home/benuix/codes/llama.cpp/prompts/chat-with-bob.txt";
        Debug.Log("Create");
        LLM = LLM_Create(command);
        LLM_Answer(LLM);
        string question = "how old are you?";
        Debug.Log(question);
        LLM_Query(LLM, question);
        LLM_Answer(LLM);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}