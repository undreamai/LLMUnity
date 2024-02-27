using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
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
        [ServerAdvanced] public bool asynchronousStartup = false;

        [Model] public string model = "";
        [ModelAddonAdvanced] public string lora = "";
        [ModelAdvanced] public int contextSize = 512;
        [ModelAdvanced] public int batchSize = 512;

        [HideInInspector] public readonly (string, string)[] modelOptions = new(string, string)[]
        {
            ("Download model", null),
            ("Mistral 7B Instruct v0.2 (medium, best overall)", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true"),
            ("OpenHermes 2.5 7B (medium, best for conversation)", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true"),
            ("Zephyr 7B (medium, alternative for conversation)", "https://huggingface.co/TheBloke/zephyr-7B-beta-GGUF/resolve/main/zephyr-7b-beta.Q4_K_M.gguf?download=true"),
            ("Phi 2 (small, decent)", "https://huggingface.co/TheBloke/phi-2-GGUF/resolve/main/phi-2.Q4_K_M.gguf?download=true"),
        };
        public int SelectedOption = 0;
        private static readonly string serverZipUrl = "https://github.com/Mozilla-Ocho/llamafile/releases/download/0.6/llamafile-0.6.zip";
        private static readonly string server = Path.Combine(LLMUnitySetup.GetAssetPath(Path.GetFileNameWithoutExtension(serverZipUrl)), "bin/llamafile");
        private static readonly string apeARMUrl = "https://cosmo.zip/pub/cosmos/bin/ape-arm64.elf";
        private static readonly string apeARM = LLMUnitySetup.GetAssetPath("ape-arm64.elf");
        private static readonly string apeX86_64Url = "https://cosmo.zip/pub/cosmos/bin/ape-x86_64.elf";
        private static readonly string apeX86_64 = LLMUnitySetup.GetAssetPath("ape-x86_64.elf");

        [HideInInspector] public static float binariesProgress = 1;
        [HideInInspector] public float modelProgress = 1;
        [HideInInspector] public float modelCopyProgress = 1;
        private static float binariesDone = 0;
        private Process process;
        private bool mmapCrash = false;
        public bool serverListening { get; private set; } = false;
        private ManualResetEvent serverBlock = new ManualResetEvent(false);

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static async Task InitializeOnLoad()
        {
            // Perform download when the build is finished
            await SetupBinaries();
        }

        private static async Task SetupBinaries()
        {
            if (binariesProgress < 1) return;
            binariesProgress = 0;
            binariesDone = 0;
            if (!File.Exists(apeARM)) await LLMUnitySetup.DownloadFile(apeARMUrl, apeARM, false, true, null, SetBinariesProgress);
            binariesDone += 1;
            if (!File.Exists(apeX86_64)) await LLMUnitySetup.DownloadFile(apeX86_64Url, apeX86_64, false, true, null, SetBinariesProgress);
            binariesDone += 1;
            if (!File.Exists(server))
            {
                string serverZip = Path.Combine(Application.temporaryCachePath, "llamafile.zip");
                await LLMUnitySetup.DownloadFile(serverZipUrl, serverZip, true, false, null, SetBinariesProgress);
                binariesDone += 1;
                LLMUnitySetup.ExtractZip(serverZip, LLMUnitySetup.GetAssetPath());
                File.Delete(serverZip);
                binariesDone += 1;
            }
            binariesProgress = 1;
        }

        public static void SetBinariesProgress(float progress)
        {
            binariesProgress = binariesDone / 4f + 1f / 4f * progress;
        }

        public void DownloadModel(int optionIndex)
        {
            // download default model and disable model editor properties until the model is set
            modelProgress = 0;
            SelectedOption = optionIndex;
            string modelUrl = modelOptions[optionIndex].Item2;
            string modelName = Path.GetFileName(modelUrl).Split("?")[0];
            string modelPath = LLMUnitySetup.GetAssetPath(modelName);
            Task downloadTask = LLMUnitySetup.DownloadFile(modelUrl, modelPath, false, false, SetModel, SetModelProgress);
        }

        public void SetModelProgress(float progress)
        {
            modelProgress = progress;
        }

        public async Task SetModel(string path)
        {
            // set the model and enable the model editor properties
            modelCopyProgress = 0;
            model = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

        public async Task SetLora(string path)
        {
            // set the lora and enable the model editor properties
            modelCopyProgress = 0;
            lora = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

#endif

        new public async void Awake()
        {
            // start the llm server and run the Awake of the client
            await StartLLMServer();

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

        public bool IsPortInUse()
        {
            try
            {
                using (TcpClient c = new TcpClient())
                {
                    c.Connect(host, port);
                }
                return true;
            }
            catch {}
            return false;
        }

        private void DebugLog(string message, bool logError = false)
        {
            // Debug log if debug is enabled
            if (!debug || message == null) return;
            if (logError) Debug.LogError(message);
            else Debug.Log(message);
        }

        private void ProcessError(string message)
        {
            // Debug log errors if debug is enabled
            DebugLog(message, true);
            if (message.Contains("assert(!isnan(x)) failed"))
            {
                mmapCrash = true;
            }
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
                    serverListening = true;
                    serverBlock.Set();
                }
            }
            catch {}
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            serverBlock.Set();
        }

        private string EscapeSpaces(string input)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                return input.Replace(" ", "\" \"");
            if (input.Contains(" "))
                return $"'{input}'";
            return input;
        }

        private void RunServerCommand(string exe, string args)
        {
            string binary = exe;
            string arguments = args;

            List<(string, string)> environment = null;
            if (numGPULayers <= 0)
            {
                // prevent nvcc building if not using GPU
                environment = new List<(string, string)> { ("PATH", ""), ("CUDA_PATH", "") };
            }

            if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                // use APE binary directly if on Linux
                arguments = $"{EscapeSpaces(binary)} {arguments}";
                binary = SelectApeBinary();
                LLMUnitySetup.makeExecutable(binary);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                arguments = $"-c \"{EscapeSpaces(binary)} {arguments}\"";
                binary = "sh";
            }
            Debug.Log($"Server command: {binary} {arguments}");
            process = LLMUnitySetup.CreateProcess(binary, arguments, CheckIfListening, ProcessError, ProcessExited, environment);
        }

        private async Task StartLLMServer()
        {
            if (IsPortInUse()) throw new Exception($"Port {port} is already in use, please use another port or kill all llamafile processes using it!");

            // Start the LLM server in a cross-platform way
            if (model == "") throw new Exception("No model file provided!");
            string modelPath = LLMUnitySetup.GetAssetPath(model);
            if (!File.Exists(modelPath)) throw new Exception($"File {modelPath} not found!");

            string loraPath = "";
            if (lora != "")
            {
                loraPath = LLMUnitySetup.GetAssetPath(lora);
                if (!File.Exists(loraPath)) throw new Exception($"File {loraPath} not found!");
            }

            int slots = parallelPrompts == -1 ? FindObjectsOfType<LLMClient>().Length : parallelPrompts;
            string arguments = $" --port {port} -m {EscapeSpaces(modelPath)} -c {contextSize} -b {batchSize} --log-disable --nobrowser -np {slots}";
            if (numThreads > 0) arguments += $" -t {numThreads}";
            if (loraPath != "") arguments += $" --lora {EscapeSpaces(loraPath)}";

            string GPUArgument = numGPULayers <= 0 ? "" : $" -ngl {numGPULayers}";
            LLMUnitySetup.makeExecutable(server);
            await RunAndWait(server, arguments + GPUArgument);

            if (process.HasExited && mmapCrash)
            {
                Debug.Log("Mmap error, fallback to no mmap use");
                serverBlock.Reset();
                arguments += " --no-mmap";
                await RunAndWait(server, arguments + GPUArgument);
            }

            if (process.HasExited && numGPULayers > 0)
            {
                Debug.Log("GPU failed, fallback to CPU");
                serverBlock.Reset();
                await RunAndWait(server, arguments);
            }

            if (process.HasExited) throw new Exception("Server could not be started!");
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

        private async Task RunAndWait(string exe, string args, int seconds = 60)
        {
            RunServerCommand(exe, args);
            if (asynchronousStartup) await WaitOneASync(serverBlock, TimeSpan.FromSeconds(seconds));
            else serverBlock.WaitOne(seconds * 1000);
        }

        /// Wrapper from https://stackoverflow.com/a/18766131
        private static Task WaitOneASync(WaitHandle handle, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
            {
                var localTcs = (TaskCompletionSource<object>)state;
                if (timedOut)
                    localTcs.TrySetCanceled();
                else
                    localTcs.TrySetResult(null);
            }, tcs, timeout, executeOnlyOnce: true);
            tcs.Task.ContinueWith((_, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
