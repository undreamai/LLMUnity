using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        [ServerAdvanced] public bool remote = false;
        [ServerAdvanced] public bool killExistingServersOnStart = true;

        [Model] public string model = "";
        [ModelAddonAdvanced] public string lora = "";
        [ModelAdvanced] public int contextSize = 512;
        [ModelAdvanced] public int batchSize = 512;

        [HideInInspector] public readonly (string, string)[] modelOptions = new(string, string)[]
        {
            ("Download model", null),
            ("Mistral 7B Instruct v0.2 (medium, best overall)", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true"),
            ("OpenHermes 2.5 7B (medium, best for conversation)", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true"),
            ("Phi 2 (small, decent)", "https://huggingface.co/TheBloke/phi-2-GGUF/resolve/main/phi-2.Q4_K_M.gguf?download=true"),
        };
        public int SelectedModel = 0;
        private static readonly string serverZipUrl = "https://github.com/Mozilla-Ocho/llamafile/releases/download/0.6.2/llamafile-0.6.2.zip";
        private static readonly string server = LLMUnitySetup.GetAssetPath("llamafile-0.6.2.exe");
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
        static object crashKillLock = new object();
        static bool crashKill = false;

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

                using (ZipArchive archive = ZipFile.OpenRead(serverZip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Name == "llamafile")
                        {
                            AssetDatabase.StartAssetEditing();
                            entry.ExtractToFile(server, true);
                            AssetDatabase.StopAssetEditing();
                            break;
                        }
                    }
                }
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
            SelectedModel = optionIndex;
            string modelUrl = modelOptions[optionIndex].Item2;
            if (modelUrl == null) return;
            modelProgress = 0;
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
            SetTemplate(ChatTemplate.FromGGUF(path));
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

        public override void SetTemplate(string templateName)
        {
            base.SetTemplate(templateName);
            foreach (LLMClient client in GetListeningClients())
            {
                client.chatTemplate = chatTemplate;
                client.template = template;
            }
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

        public List<LLMClient> GetListeningClients()
        {
            List<LLMClient> clients = new List<LLMClient>();
            foreach (LLMClient client in FindObjectsOfType<LLMClient>())
            {
                if (client.GetType() == typeof(LLM)) continue;
                if (client.host == host && client.port == port)
                {
                    clients.Add(client);
                }
            }
            return clients;
        }

        void KillServersAfterUnityCrash()
        {
            lock (crashKillLock) {
                if (crashKill) return;
                LLMUnitySetup.KillServerAfterUnityCrash(server);
                crashKill = true;
            }
        }

        new public async void Awake()
        {
            if (killExistingServersOnStart) KillServersAfterUnityCrash();
            if (asynchronousStartup) await StartLLMServer();
            else _ = StartLLMServer();
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
                if (status.message == "model loaded")
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
            process = LLMUnitySetup.CreateProcess(binary, arguments, CheckIfListening, ProcessError, ProcessExited);
        }

        private async Task StartLLMServer()
        {
            bool portInUse = asynchronousStartup ? await IsServerReachableAsync() : IsServerReachable();
            if (portInUse)
            {
                Debug.LogError($"Port {port} is already in use, please use another port or kill all llamafile processes using it!");
                return;
            }

            // Start the LLM server in a cross-platform way
            if (model == "")
            {
                Debug.LogError("No model file provided!");
                return;
            }
            string modelPath = LLMUnitySetup.GetAssetPath(model);
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"File {modelPath} not found!");
                return;
            }

            string loraPath = "";
            if (lora != "")
            {
                loraPath = LLMUnitySetup.GetAssetPath(lora);
                if (!File.Exists(loraPath))
                {
                    Debug.LogError($"File {loraPath} not found!");
                    return;
                }
            }

            int slots = parallelPrompts == -1 ? GetListeningClients().Count + 1 : parallelPrompts;
            string arguments = $" --port {port} -m {EscapeSpaces(modelPath)} -c {contextSize} -b {batchSize} --log-disable --nobrowser -np {slots}";
            if (remote) arguments += $" --host 0.0.0.0";
            if (numThreads > 0) arguments += $" -t {numThreads}";
            if (loraPath != "") arguments += $" --lora {EscapeSpaces(loraPath)}";

            string noGPUArgument = " -ngl 0 --gpu no";
            string GPUArgument = numGPULayers <= 0 ? noGPUArgument : $" -ngl {numGPULayers}";
            LLMUnitySetup.makeExecutable(server);

            RunServerCommand(server, arguments + GPUArgument);
            if (asynchronousStartup) await WaitOneASync(serverBlock, TimeSpan.FromSeconds(60));
            else serverBlock.WaitOne(60000);

            if (process.HasExited && mmapCrash)
            {
                Debug.Log("Mmap error, fallback to no mmap use");
                serverBlock.Reset();
                arguments += " --no-mmap";

                RunServerCommand(server, arguments + GPUArgument);
                if (asynchronousStartup) await WaitOneASync(serverBlock, TimeSpan.FromSeconds(60));
                else serverBlock.WaitOne(60000);
            }

            if (process.HasExited && numGPULayers > 0)
            {
                Debug.Log("GPU failed, fallback to CPU");
                serverBlock.Reset();

                RunServerCommand(server, arguments + noGPUArgument);
                if (asynchronousStartup) await WaitOneASync(serverBlock, TimeSpan.FromSeconds(60));
                else serverBlock.WaitOne(60000);
            }

            if (process.HasExited) Debug.LogError("Server could not be started!");
            else LLMUnitySetup.SaveServerPID(process.Id);
        }

        public void StopProcess()
        {
            // kill the llm server
            if (process != null)
            {
                int pid = process.Id;
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
                LLMUnitySetup.DeleteServerPID(pid);
            }
        }

        public void OnDestroy()
        {
            StopProcess();
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
