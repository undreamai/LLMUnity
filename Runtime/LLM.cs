/// @file
/// @brief File implementing the LLM server.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    public class LLMException : Exception
    {
        public int ErrorCode { get; private set; }

        public LLMException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    [DefaultExecutionOrder(-1)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM server.
    /// </summary>
    public class LLM : UnityMainThreadDispatcher
    {
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> number of threads to use (-1 = all) </summary>
        [Server] public int numThreads = -1;
        /// <summary> number of model layers to offload to the GPU (0 = GPU not used).
        /// Use a large number i.e. >30 to utilise the GPU as much as possible.
        /// If the user's GPU is not supported, the LLM will fall back to the CPU </summary>
        [Server] public int numGPULayers = 0;
        /// <summary> select to log the output of the LLM in the Unity Editor. </summary>
        [ServerAdvanced] public bool debug = false;
        /// <summary> number of prompts that can happen in parallel (-1 = number of LLM/LLMClient objects) </summary>
        [ServerAdvanced] public int parallelPrompts = -1;
        /// <summary> port to use for the LLM server </summary>
        [ServerAdvanced] public int port = 13333;
        /// <summary> the path of the model being used (relative to the Assets/StreamingAssets folder).
        /// Models with .gguf format are allowed.</summary>
        [Model] public string model = "";
        /// <summary> the path of the LORA model being used (relative to the Assets/StreamingAssets folder).
        /// Models with .bin format are allowed.</summary>
        [ModelAdvanced] public string lora = "";
        /// <summary> Size of the prompt context (0 = context size of the model).
        /// This is the number of tokens the model can take as input when generating responses. </summary>
        [ModelAdvanced] public int contextSize = 512;
        /// <summary> Batch size for prompt processing. </summary>
        [ModelAdvanced] public int batchSize = 512;
        /// <summary> select to log the output of the LLM in the Unity Editor. </summary>
        [Server] public bool remote = false;
        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public bool started { get; protected set; } = false;

        /// \cond HIDE
        [HideInInspector] public readonly (string, string)[] modelOptions = new(string, string)[]
        {
            ("Download model", null),
            ("Mistral 7B Instruct v0.2 (medium, best overall)", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true"),
            ("OpenHermes 2.5 7B (medium, best for conversation)", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true"),
            ("Phi 2 (small, decent)", "https://huggingface.co/TheBloke/phi-2-GGUF/resolve/main/phi-2.Q4_K_M.gguf?download=true"),
        };
        public int SelectedModel = 0;
        [HideInInspector] public float modelProgress = 1;
        [HideInInspector] public float modelCopyProgress = 1;
        [HideInInspector] public bool modelHide = true;

        public string chatTemplate = ChatTemplate.DefaultTemplate;

        IntPtr LLMObject;
        List<LLMClient> clients = new List<LLMClient>();
        LLMLib llmlib;
        StreamWrapper logStreamWrapper;
        Thread llmThread;
        /// \endcond

#if UNITY_EDITOR
        /// \cond HIDE
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

        /// \endcond

        void SetModelProgress(float progress)
        {
            modelProgress = progress;
        }

        /// <summary>
        /// Allows to set the model used by the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to model to use (.gguf format)</param>
        public async Task SetModel(string path)
        {
            // set the model and enable the model editor properties
            modelCopyProgress = 0;
            model = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
            SetTemplate(ChatTemplate.FromGGUF(path));
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

        /// <summary>
        /// Allows to set a LORA model to use in the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .bin format.
        /// </summary>
        /// <param name="path">path to LORA model to use (.bin format)</param>
        public async Task SetLora(string path)
        {
            // set the lora and enable the model editor properties
            modelCopyProgress = 0;
            lora = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

#endif

        public void SetTemplate(string templateName)
        {
            chatTemplate = templateName;
            llmlib?.LLM_SetTemplate(LLMObject, chatTemplate);
        }

        public string GetTemplate()
        {
            return chatTemplate;
        }

        protected string EscapeSpaces(string input)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                return input.Replace(" ", "\" \"");
            if (input.Contains(" "))
                return $"'{input}'";
            return input;
        }

        protected virtual string GetLlamaccpArguments()
        {
            // Start the LLM server in a cross-platform way
            if (model == "")
            {
                Debug.LogError("No model file provided!");
                return null;
            }
            string modelPath = LLMUnitySetup.GetAssetPath(model);
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"File {modelPath} not found!");
                return null;
            }
            string loraPath = "";
            if (lora != "")
            {
                loraPath = LLMUnitySetup.GetAssetPath(lora);
                if (!File.Exists(loraPath))
                {
                    Debug.LogError($"File {loraPath} not found!");
                    return null;
                }
            }

            int slots = GetNumClients();
            string arguments = $"-m {EscapeSpaces(modelPath)} -c {contextSize} -b {batchSize} --log-disable -np {slots}";
            if (remote) arguments += $" --port {port} --host 0.0.0.0";
            if (numThreads > 0) arguments += $" -t {numThreads}";
            if (loraPath != "") arguments += $" --lora {EscapeSpaces(loraPath)}";
            arguments += $" -ngl {numGPULayers}";
            return arguments;
        }

        public void Awake()
        {
            if (!gameObject.activeSelf) return;
            llmThread = new Thread(StartLLMServer);
            llmThread.Start();
        }

        private void LLMLibLog(string log)
        {
            if (!debug) return;
            Debug.LogWarning(log);
        }

        private void SetupLogging()
        {
            logStreamWrapper = new StreamWrapper(this, LLMLibLog);
            llmlib?.Logging(logStreamWrapper.GetStringWrapper(), logStreamWrapper.GetCallPointer());
        }

        private void StopLogging()
        {
            llmlib?.StopLogging();
            logStreamWrapper.Destroy();
        }

        private void StartLLMServer()
        {
            string arguments = GetLlamaccpArguments();
            bool useGPU = numGPULayers > 0;
            Debug.Log($"Server command: {arguments}");

            foreach (string arch in LLMLib.PossibleArchitectures(useGPU))
            {
                llmlib = new LLMLib(arch);
                string error;
                try
                {
                    SetupLogging();
                    LLMObject = llmlib.LLM_Construct(arguments);
                    if (remote) llmlib.LLM_SetupServer(LLMObject);
                    SetTemplate(chatTemplate);
                    CheckLLMStatus(false);
                    Debug.Log($"Using architecture: {arch}");
                    break;
                }
                catch (LLMException e)
                {
                    error = e.Message;
                    Destroy();
                }
                catch (Exception e)
                {
                    error = $"{e.GetType()}: {e.Message}";
                }
                Debug.Log($"Tried architecture: {arch}, " + error);
            }
            if (llmlib == null)
            {
                Debug.LogError("LLM service couldn't be created");
                return;
            }
            Debug.Log("LLM service created");
            started = true;
            llmlib.LLM_Start(LLMObject);
        }

        public int Register(LLMClient llmClient)
        {
            clients.Add(llmClient);
            return clients.IndexOf(llmClient);
        }

        protected int GetNumClients()
        {
            return Math.Max(parallelPrompts == -1 ? clients.Count : parallelPrompts, 1);
        }

        public delegate void LLMStatusCallback(IntPtr LLMObject, IntPtr stringWrapper);
        public delegate void LLMReplyCallback(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        public delegate void StreamingCallback();
        public delegate void LLMReplyStreamingCallback(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);

        class StreamWrapper
        {
            LLM llm;
            IntPtr stringWrapper = IntPtr.Zero;
            Callback<string> streamCallback;

            public StreamWrapper(LLM llm, Callback<string> streamCallback)
            {
                this.llm = llm;
                this.streamCallback = streamCallback;
                if (llm.llmlib == null) Debug.LogError("LLM service not started");
                else  stringWrapper = llm.llmlib.StringWrapper_Construct();
            }

            public void Call()
            {
                string result = llm.llmlib?.GetStringWrapperResult(stringWrapper);
                if (result != null) llm.EnqueueAsync(() => {streamCallback(result);});
            }

            public IntPtr GetStringWrapper()
            {
                return stringWrapper;
            }

            public IntPtr GetCallPointer()
            {
                return Marshal.GetFunctionPointerForDelegate((StreamingCallback)Call);
            }

            public void Destroy()
            {
                if (stringWrapper != IntPtr.Zero) llm.llmlib?.StringWrapper_Delete(stringWrapper);
            }
        }

        void CheckLLMStatus(bool log = true)
        {
            if (llmlib == null) {Debug.LogError("LLM service not started"); return;}

            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            int status = llmlib.LLM_Status(LLMObject, stringWrapper);
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            llmlib.StringWrapper_Delete(stringWrapper);
            string message = $"LLM {status}: {result}";
            if (status > 0)
            {
                if (log) Debug.LogError(message);
                throw new LLMException(message, status);
            }
            else if (status < 0)
            {
                if (log) Debug.LogWarning(message);
            }
        }

        async Task<string> LLMReply(LLMReplyCallback callback, string json)
        {
            if (llmlib == null) {Debug.LogError("LLM service not started"); return null;}

            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            await Task.Run(() => callback(LLMObject, json, stringWrapper));
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            llmlib.StringWrapper_Delete(stringWrapper);
            CheckLLMStatus();
            return result;
        }

        async Task<string> LLMStreamReply(LLMReplyStreamingCallback callback, string json, Callback<string> streamCallback = null)
        {
            if (llmlib == null) {Debug.LogError("LLM service not started"); return null;}

            IntPtr stringWrapper = IntPtr.Zero;
            IntPtr streamCallbackPointer = IntPtr.Zero;
            StreamWrapper streamWrapper = null;
            if (streamCallback != null)
            {
                streamWrapper = new StreamWrapper(this, streamCallback);
                streamCallbackPointer = streamWrapper.GetCallPointer();
                stringWrapper = streamWrapper.GetStringWrapper();
            }
            await Task.Run(() => callback(LLMObject, json, stringWrapper, streamCallbackPointer));
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            if (streamWrapper != null) streamWrapper.Destroy();
            CheckLLMStatus();
            return result;
        }

        public async Task<string> Tokenize(string json)
        {
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Tokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        public async Task<string> Detokenize(string json)
        {
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Detokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        public async Task<string> Completion(string json, Callback<string> streamCallback = null)
        {
            LLMReplyStreamingCallback callback = (IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer) =>
            {
                llmlib.LLM_Completion(LLMObject, json_data, stringWrapper, streamCallbackPointer);
            };
            return await LLMStreamReply(callback, json, streamCallback);
        }

        public void CancelRequest(int id_slot)
        {
            llmlib?.LLM_Cancel(LLMObject, id_slot);
            CheckLLMStatus();
        }

        private void Destroy()
        {
            if (llmlib != null)
            {
                StopLogging();
                llmlib.LLM_Stop(LLMObject);
                llmlib.LLM_Delete(LLMObject);
            }
            llmlib = null;
        }

        /// <summary>
        /// The Unity OnDestroy function called when the onbject is destroyed.
        /// The function StopProcess is called to stop the LLM server.
        /// </summary>
        public void OnDestroy()
        {
            Destroy();
        }
    }
}
