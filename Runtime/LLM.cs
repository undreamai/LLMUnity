using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public class LLM : LLMClient
    {
        [HideInInspector] public bool modelHide = true;

        [Server] public int numThreads = -1;
        [Server] public int numGPULayers = 0;
        [ServerAdvanced] public int parallelPrompts = -1;
        [ServerAdvanced] public bool debug = false;

        [Model] public string model = "";
        [Model] public string lora = "";
        [ModelAdvanced] public int contextSize = 512;
        [ModelAdvanced] public int batchSize = 512;

        [HideInInspector] public string modelUrl = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true";
        private static readonly string serverUrl = "https://github.com/Mozilla-Ocho/llamafile/releases/download/0.4.1/llamafile-server-0.4.1";
        private static readonly string server = GetAssetPath("llamafile-server.exe");
        private static readonly string apeARMUrl = "https://cosmo.zip/pub/cosmos/bin/ape-arm64.elf";
        private static readonly string apeARM = GetAssetPath("ape-arm64.elf");
        private static readonly string apeX86_64Url = "https://cosmo.zip/pub/cosmos/bin/ape-x86_64.elf";
        private static readonly string apeX86_64 = GetAssetPath("ape-x86_64.elf");

        [HideInInspector] public static float binariesProgress = 1;
        [HideInInspector] public float modelProgress = 1;
        [HideInInspector] public float modelCopyProgress = 1;
        private static float binariesDone = 0;
        private Process process;
        private bool serverListening = false;
        public ManualResetEvent serverStarted = new ManualResetEvent(false);

        private static string GetAssetPath(string relPath = "")
        {
            // Path to store llm server binaries and models
            return Path.Combine(Application.streamingAssetsPath, relPath);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static async void InitializeOnLoad()
        {
            // Perform download when the build is finished
            await DownloadBinaries();
        }

        private static async Task DownloadBinaries(){
            if (binariesProgress == 0) return;
            binariesProgress = 0;
            binariesDone = 0;
            foreach ((string url, string path) in new[] {(serverUrl, server), (apeARMUrl, apeARM), (apeX86_64Url, apeX86_64)}){
                if (!File.Exists(path)) await LLMUnitySetup.DownloadFile(url, path, true, null, SetBinariesProgress);
                binariesDone += 1;
            }
            binariesProgress = 1;
        }

        public static void SetBinariesProgress(float progress){
            binariesProgress = binariesDone / 3f + 1f / 3f * progress;
        }

        public void DownloadModel(){
            // download default model and disable model editor properties until the model is set
            modelProgress = 0;
            string modelName = Path.GetFileName(modelUrl).Split("?")[0];
            string modelPath = GetAssetPath(modelName);
            Task downloadTask = LLMUnitySetup.DownloadFile(modelUrl, modelPath, false, SetModel, SetModelProgress);
        }

        public void SetModelProgress(float progress){
            modelProgress = progress;
        }

        public async void SetModel(string path){
            // set the model and enable the model editor properties
            modelCopyProgress = 0;
            model = await LLMUnitySetup.AddAsset(path, GetAssetPath());
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

        public async void SetLora(string path){
            // set the lora and enable the model editor properties
            modelCopyProgress = 0;
            lora = await LLMUnitySetup.AddAsset(path, GetAssetPath());
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }
#endif

        new public void Awake()
        {
            // start the llm server and run the OnEnable of the client
            StartLLMServer();
            base.Awake();
        }

        private string SelectApeBinary()
        {
            // select the corresponding APE binary for the system architecture
            string arch = LLMUnitySetup.RunProcess("uname", "-m");
            Debug.Log($"architecture: {arch}");
            string apeExe;
            if (arch.Contains("arm64") || arch.Contains("aarch64"))
            {
                apeExe = apeARM;
            }
            else
            {
                apeExe = apeX86_64;
                if (!arch.Contains("x86_64"))
                    Debug.Log($"Unknown architecture of processor {arch}! Falling back to x86_64");
            }
            return apeExe;
        }

        private void DebugLog(string message, bool logError = false)
        {
            // Debug log if debug is enabled
            if (!debug || message == null) return;
            if (logError) Debug.LogError(message);
            else Debug.Log(message);
        }

        private void DebugLogError(string message)
        {
            // Debug log errors if debug is enabled
            DebugLog(message, true);
        }

        private void CheckIfListening(string message)
        {
            // Read the output of the llm binary and check if the server has been started and listening
            DebugLog(message);
            if (serverListening) return;
            try
            {
                ServerStatus status = JsonUtility.FromJson<ServerStatus>(message);
                if (status.message == "HTTP server listening")
                {
                    Debug.Log("LLM Server started!");
                    serverStarted.Set();
                    serverListening = true;
                }
            }
            catch { }
        }

        private void StartLLMServer()
        {
            // Start the LLM server in a cross-platform way
            if (model == "") throw new System.Exception("No model file provided!");
            string modelPath = GetAssetPath(model);
            if (!File.Exists(modelPath)) throw new System.Exception($"File {modelPath} not found!");

            string loraPath = "";
            if (lora != "")
            {
                loraPath = GetAssetPath(lora);
                if (!File.Exists(loraPath)) throw new System.Exception($"File {loraPath} not found!");
            }

            int slots = parallelPrompts == -1 ? FindObjectsOfType<LLMClient>().Length : parallelPrompts;
            string binary = server;
            string arguments = $" --port {port} -m \"{modelPath}\" -c {contextSize} -b {batchSize} --log-disable --nobrowser -np {slots}";
            if (numThreads > 0) arguments += $" -t {numThreads}";
            if (numGPULayers > 0) arguments += $" -ngl {numGPULayers}";
            if (loraPath != "") arguments += $" --lora \"{loraPath}\"";
            List<(string, string)> environment = null;

            if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
            {
                // use APE binary directly if not on Windows
                arguments = $"\"{binary}\" {arguments}";
                binary = SelectApeBinary();
                if (numGPULayers <= 0)
                {
                    // prevent nvcc building if not using GPU
                    environment = new List<(string, string)> { ("PATH", ""), ("CUDA_PATH", "") };
                }
            }
            Debug.Log($"Server command: {binary} {arguments}");
            process = LLMUnitySetup.CreateProcess(binary, arguments, CheckIfListening, DebugLogError, environment);
            // wait for at most 2'
            serverStarted.WaitOne(60000);
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
}