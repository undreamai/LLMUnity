/// @file
/// @brief File implementing the LLM server.
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
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM server.
    /// </summary>
    public class LLMRemote : LLMBase
    {
        /// <summary> Select to kill existing servers created by the package at startup.
        /// Useful in case of game crashes where the servers didn't have the chance to terminate</summary>
        [ServerAdvanced] public bool killExistingServersOnStart = true;
        /// <summary> host to use for the LLMClient object </summary>
        [ClientAdvanced] public string host = "localhost";
        /// <summary> port to use for the server (LLM) or client (LLMClient) </summary>
        [ServerAdvanced] public int port = 13333;

        private static readonly string serverZipUrl = "https://github.com/Mozilla-Ocho/llamafile/releases/download/0.6.2/llamafile-0.6.2.zip";
        private static readonly string server = LLMUnitySetup.GetAssetPath("llamafile-0.6.2.exe");
        private static readonly string apeARMUrl = "https://cosmo.zip/pub/cosmos/bin/ape-arm64.elf";
        private static readonly string apeARM = LLMUnitySetup.GetAssetPath("ape-arm64.elf");
        private static readonly string apeX86_64Url = "https://cosmo.zip/pub/cosmos/bin/ape-x86_64.elf";
        private static readonly string apeX86_64 = LLMUnitySetup.GetAssetPath("ape-x86_64.elf");

        [HideInInspector] public static float binariesProgress = 1;
        private static float binariesDone = 0;
        static object crashKillLock = new object();
        static bool crashKill = false;
        private Process process;
        private bool mmapCrash = false;
        private ManualResetEvent serverBlock = new ManualResetEvent(false);

        public async void Awake()
        {
            if (killExistingServersOnStart) KillServersAfterUnityCrash();
            if (asynchronousStartup) await StartLLMServer();
            else _ = StartLLMServer();
            serverStarted = true;
        }

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

        static void SetBinariesProgress(float progress)
        {
            binariesProgress = binariesDone / 4f + 1f / 4f * progress;
        }

        void KillServersAfterUnityCrash()
        {
            lock (crashKillLock) {
                if (crashKill) return;
                LLMUnitySetup.KillServerAfterUnityCrash(server);
                crashKill = true;
            }
        }

#endif
        /// <summary>
        /// The Unity Awake function that initializes the state before the application starts.
        /// The following actions are executed:
        /// - existing servers are killed (if killExistingServersOnStart=true)
        /// - the LLM server is started (async if asynchronousStartup, synchronous otherwise)
        /// Additionally the Awake of the LLMClient is called to initialise the client part of the LLM object.
        /// </summary>
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
            serverListening = false;
            serverBlock.Set();
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

        protected override string GetLlamaccpArguments()
        {
            string arguments = base.GetLlamaccpArguments();
            arguments += $" --host 0.0.0.0 --port {port} --log-disable --nobrowser";
            return arguments;
        }

        private async Task StartLLMServer()
        {
            // bool portInUse = asynchronousStartup ? await IsServerReachableAsync() : IsServerReachable();
            // if (portInUse)
            // {
            //     Debug.LogError($"Port {port} is already in use, please use another port or kill all llamafile processes using it!");
            //     return;
            // }

            string arguments = GetLlamaccpArguments();
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

        /// <summary>
        /// Allows to stop the LLM server.
        /// </summary>
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

        /// <summary>
        /// The Unity OnDestroy function called when the onbject is destroyed.
        /// The function StopProcess is called to stop the LLM server.
        /// </summary>
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
