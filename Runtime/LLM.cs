/// @file
/// @brief File implementing the LLM.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    /// \cond HIDE
    public class LLMException : Exception
    {
        public int ErrorCode { get; private set; }

        public LLMException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
    /// \endcond

    [DefaultExecutionOrder(-1)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM server.
    /// </summary>
    public class LLM : MonoBehaviour
    {
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> toggle to enable remote server functionality </summary>
        [LocalRemote] public bool remote = false;
        /// <summary> port to use for the LLM server </summary>
        [Remote] public int port = 13333;
        /// <summary> number of threads to use (-1 = all) </summary>
        [LLM] public int numThreads = -1;
        /// <summary> number of model layers to offload to the GPU (0 = GPU not used).
        /// Use a large number i.e. >30 to utilise the GPU as much as possible.
        /// If the user's GPU is not supported, the LLM will fall back to the CPU </summary>
        [LLM] public int numGPULayers = 0;
        /// <summary> select to log the output of the LLM in the Unity Editor. </summary>
        [LLM] public bool debug = false;
        /// <summary> number of prompts that can happen in parallel (-1 = number of LLMCharacter objects) </summary>
        [LLMAdvanced] public int parallelPrompts = -1;
        /// <summary> select to not destroy the LLM GameObject when loading a new Scene. </summary>
        [LLMAdvanced] public bool dontDestroyOnLoad = true;
        /// <summary> Size of the prompt context (0 = context size of the model).
        /// This is the number of tokens the model can take as input when generating responses. </summary>
        [ModelAdvanced] public int contextSize = 0;
        /// <summary> Batch size for prompt processing. </summary>
        [ModelAdvanced] public int batchSize = 512;
        /// <summary> a base prompt to use as a base for all LLMCharacter objects </summary>
        [TextArea(5, 10), ChatAdvanced] public string basePrompt = "";
        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public bool started { get; protected set; } = false;
        /// <summary> Boolean set to true if the server has failed to start. </summary>
        public bool failed { get; protected set; } = false;
        /// <summary> Boolean set to true if the models were not downloaded successfully. </summary>
        public static bool modelSetupFailed { get; protected set; } = false;
        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public static bool modelSetupComplete { get; protected set; } = false;

        /// <summary> the LLM model to use.
        /// Models with .gguf format are allowed.</summary>
        [ModelAdvanced] public string model = "";
        /// <summary> Chat template used for the model </summary>
        [ModelAdvanced] public string chatTemplate = ChatTemplate.DefaultTemplate;
        /// <summary> the paths of the LORA models being used (relative to the Assets/StreamingAssets folder).
        /// Models with .bin format are allowed.</summary>
        [ModelAdvanced] public string lora = "";

        /// \cond HIDE

        IntPtr LLMObject = IntPtr.Zero;
        List<LLMCharacter> clients = new List<LLMCharacter>();
        LLMLib llmlib;
        StreamWrapper logStreamWrapper = null;
        Thread llmThread = null;
        List<StreamWrapper> streamWrappers = new List<StreamWrapper>();
        public LLMManager llmManager = new LLMManager();
        List<float> loraWeights = new List<float>();

        /// \endcond

        public LLM()
        {
            LLMManager.Register(this);
        }

        /// <summary>
        /// The Unity Awake function that starts the LLM server.
        /// The server can be started asynchronously if the asynchronousStartup option is set.
        /// </summary>
        public async void Awake()
        {
            if (!enabled) return;
#if !UNITY_EDITOR
            modelSetupFailed = !await LLMManager.Setup();
#endif
            modelSetupComplete = true;
            if (modelSetupFailed)
            {
                failed = true;
                return;
            }
            string arguments = GetLlamaccpArguments();
            if (arguments == null)
            {
                failed = true;
                return;
            }
            await Task.Run(() => StartLLMServer(arguments));
            if (dontDestroyOnLoad) DontDestroyOnLoad(transform.root.gameObject);
            if (basePrompt != "") await SetBasePrompt(basePrompt);
        }

        public async Task WaitUntilReady()
        {
            while (!started) await Task.Yield();
        }

        public static async Task<bool> WaitUntilModelSetup(Callback<float> downloadProgressCallback = null)
        {
            if (downloadProgressCallback != null) LLMManager.downloadProgressCallbacks.Add(downloadProgressCallback);
            while (!modelSetupComplete) await Task.Yield();
            return !modelSetupFailed;
        }

        public string GetModelLoraPathRuntime(string path)
        {
            string assetPath = LLMManager.GetAssetPath(path);
            if (!string.IsNullOrEmpty(assetPath)) return assetPath;
            return path;
        }

        public string GetModelLoraPath(string path, bool lora)
        {
            if (string.IsNullOrEmpty(path)) return path;
            ModelEntry modelEntry = LLMManager.Get(path);
            if (modelEntry != null) return modelEntry.filename;

            string modelType = lora ? "Lora" : "Model";
            string assetPath = LLMUnitySetup.GetAssetPath(path);
            if (!File.Exists(assetPath))
            {
                LLMUnitySetup.LogError($"The {modelType} file {path} was not found.");
                return path;
            }

            if (!LLMUnitySetup.IsSubPath(assetPath, LLMUnitySetup.GetAssetPath()))
            {
                string errorMessage = $"The {modelType} file {path} was loaded locally. If you want to include it in the build:";
                errorMessage += $"\n-Copy the {modelType} inside the StreamingAssets folder and use its relative path or";
                errorMessage += $"\n-Load the {modelType} with the LLMManager: `string filename=LLMManager.Load{modelType}(path); llm.Set{modelType}(filename)`";
                LLMUnitySetup.LogWarning(errorMessage);
            }
            return assetPath;
        }

        /// <summary>
        /// Allows to set the model used by the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to model to use (.gguf format)</param>
        public void SetModel(string path)
        {
            model = GetModelLoraPath(path, false);
            if (!string.IsNullOrEmpty(model))
            {
                ModelEntry modelEntry = LLMManager.Get(model);
                if (modelEntry == null) modelEntry = new ModelEntry(GetModelLoraPathRuntime(model));
                SetTemplate(modelEntry.chatTemplate);
                if (contextSize == 0 && modelEntry.contextLength > 32768)
                {
                    LLMUnitySetup.LogWarning($"The model {path} has very large context size ({modelEntry.contextLength}), consider setting it to a smaller value (<=32768) to avoid filling up the RAM");
                }
            }
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Allows to set a LORA model to use in the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .bin format.
        /// </summary>
        /// <param name="path">path to LORA model to use (.bin format)</param>
        public void SetLora(string path)
        {
            lora = "";
            AddLora(path);
        }

        /// <summary>
        /// Allows to add a LORA model to use in the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .bin format.
        /// </summary>
        /// <param name="path">path to LORA model to use (.bin format)</param>
        public void AddLora(string path)
        {
            string loraPath = GetModelLoraPath(path, true);
            if (lora.Split(" ").Contains(loraPath)) return;
            if (lora != "") lora += " ";
            lora += loraPath;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Allows to remove a LORA model from the LLM.
        /// Models supported are in .bin format.
        /// </summary>
        /// <param name="path">path to LORA model to remove (.bin format)</param>
        public void RemoveLora(string path)
        {
            string loraPath = GetModelLoraPath(path, true);
            List<string> loras = new List<string>(lora.Split(" "));
            loras.Remove(loraPath);
            lora = "";
            for (int i = 0; i < loras.Count; i++)
            {
                if (i > 0) lora += " ";
                lora += loras[i];
            }
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Set the chat template for the LLM.
        /// </summary>
        /// <param name="templateName">the chat template to use. The available templates can be found in the ChatTemplate.templates.Keys array </param>
        public void SetTemplate(string templateName, bool setDirty = true)
        {
            chatTemplate = templateName;
            if (started) llmlib?.LLM_SetTemplate(LLMObject, chatTemplate);
#if UNITY_EDITOR
            if (setDirty && !EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Returns the chat template of the LLM.
        /// </summary>
        /// <returns>chat template of the LLM</returns>
        public string GetTemplate()
        {
            return chatTemplate;
        }

        protected virtual string GetLlamaccpArguments()
        {
            // Start the LLM server in a cross-platform way
            if (model == "")
            {
                LLMUnitySetup.LogError("No model file provided!");
                return null;
            }
            string modelPath = GetModelLoraPathRuntime(model);
            if (!File.Exists(modelPath))
            {
                LLMUnitySetup.LogError($"File {modelPath} not found!");
                return null;
            }
            string loraArgument = "";
            foreach (string lora in lora.Trim().Split(" "))
            {
                if (lora == "") continue;
                string loraPath = GetModelLoraPathRuntime(lora);
                if (!File.Exists(loraPath))
                {
                    LLMUnitySetup.LogError($"File {loraPath} not found!");
                    return null;
                }
                loraArgument += $" --lora \"{loraPath}\"";
            }

            int numThreadsToUse = numThreads;
            if (Application.platform == RuntimePlatform.Android && numThreads <= 0) numThreadsToUse = LLMUnitySetup.AndroidGetNumBigCores();

            int slots = GetNumClients();
            string arguments = $"-m \"{modelPath}\" -c {contextSize} -b {batchSize} --log-disable -np {slots}";
            if (remote) arguments += $" --port {port} --host 0.0.0.0";
            if (numThreadsToUse > 0) arguments += $" -t {numThreadsToUse}";
            arguments += loraArgument;
            arguments += $" -ngl {numGPULayers}";
            return arguments;
        }

        private void SetupLogging()
        {
            logStreamWrapper = ConstructStreamWrapper(LLMUnitySetup.LogWarning, true);
            llmlib?.Logging(logStreamWrapper.GetStringWrapper());
        }

        private void StopLogging()
        {
            if (logStreamWrapper == null) return;
            llmlib?.StopLogging();
            DestroyStreamWrapper(logStreamWrapper);
        }

        private void StartLLMServer(string arguments)
        {
            started = false;
            failed = false;
            bool useGPU = numGPULayers > 0;
            LLMUnitySetup.Log($"Server command: {arguments}");

            foreach (string arch in LLMLib.PossibleArchitectures(useGPU))
            {
                string error;
                try
                {
                    InitLib(arch);
                    InitServer(arguments);
                    LLMUnitySetup.Log($"Using architecture: {arch}");
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
                LLMUnitySetup.Log($"Tried architecture: {arch}, " + error);
            }
            if (llmlib == null)
            {
                LLMUnitySetup.LogError("LLM service couldn't be created");
                failed = true;
                return;
            }
            StartService();
            LLMUnitySetup.Log("LLM service created");
        }

        private void InitLib(string arch)
        {
            llmlib = new LLMLib(arch);
            CheckLLMStatus(false);
        }

        private void InitServer(string arguments)
        {
            if (debug) SetupLogging();
            LLMObject = llmlib.LLM_Construct(arguments);
            if (remote) llmlib.LLM_StartServer(LLMObject);
            llmlib.LLM_SetTemplate(LLMObject, chatTemplate);
            CheckLLMStatus(false);
        }

        private void StartService()
        {
            llmThread = new Thread(() => llmlib.LLM_Start(LLMObject));
            llmThread.Start();
            while (!llmlib.LLM_Started(LLMObject)) {}
            loraWeights = new List<float>();
            for (int i = 0; i < lora.Split(" ").Count(); i++) loraWeights.Add(1f);
            started = true;
        }

        /// <summary>
        /// Registers a local LLMCharacter object.
        /// This allows to bind the LLMCharacter "client" to a specific slot of the LLM.
        /// </summary>
        /// <param name="llmCharacter"></param>
        /// <returns></returns>
        public int Register(LLMCharacter llmCharacter)
        {
            clients.Add(llmCharacter);
            return clients.IndexOf(llmCharacter);
        }

        protected int GetNumClients()
        {
            return Math.Max(parallelPrompts == -1 ? clients.Count : parallelPrompts, 1);
        }

        /// \cond HIDE
        public delegate void LLMStatusCallback(IntPtr LLMObject, IntPtr stringWrapper);
        public delegate void LLMNoInputReplyCallback(IntPtr LLMObject, IntPtr stringWrapper);
        public delegate void LLMReplyCallback(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        /// \endcond

        StreamWrapper ConstructStreamWrapper(Callback<string> streamCallback = null, bool clearOnUpdate = false)
        {
            StreamWrapper streamWrapper = new StreamWrapper(llmlib, streamCallback, clearOnUpdate);
            streamWrappers.Add(streamWrapper);
            return streamWrapper;
        }

        void DestroyStreamWrapper(StreamWrapper streamWrapper)
        {
            streamWrappers.Remove(streamWrapper);
            streamWrapper.Destroy();
        }

        /// <summary>
        /// The Unity Update function. It is used to retrieve the LLM replies.
        public void Update()
        {
            foreach (StreamWrapper streamWrapper in streamWrappers) streamWrapper.Update();
        }

        void AssertStarted()
        {
            string error = null;
            if (failed) error = "LLM service couldn't be created";
            else if (!started) error = "LLM service not started";
            if (error != null)
            {
                LLMUnitySetup.LogError(error);
                throw new Exception(error);
            }
        }

        void CheckLLMStatus(bool log = true)
        {
            if (llmlib == null) {return;}
            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            int status = llmlib.LLM_Status(LLMObject, stringWrapper);
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            llmlib.StringWrapper_Delete(stringWrapper);
            string message = $"LLM {status}: {result}";
            if (status > 0)
            {
                if (log) LLMUnitySetup.LogError(message);
                throw new LLMException(message, status);
            }
            else if (status < 0)
            {
                if (log) LLMUnitySetup.LogWarning(message);
            }
        }

        async Task<string> LLMNoInputReply(LLMNoInputReplyCallback callback)
        {
            AssertStarted();
            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            await Task.Run(() => callback(LLMObject, stringWrapper));
            string result = llmlib?.GetStringWrapperResult(stringWrapper);
            llmlib?.StringWrapper_Delete(stringWrapper);
            CheckLLMStatus();
            return result;
        }

        async Task<string> LLMReply(LLMReplyCallback callback, string json)
        {
            AssertStarted();
            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            await Task.Run(() => callback(LLMObject, json, stringWrapper));
            string result = llmlib?.GetStringWrapperResult(stringWrapper);
            llmlib?.StringWrapper_Delete(stringWrapper);
            CheckLLMStatus();
            return result;
        }

        /// <summary>
        /// Tokenises the provided query.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>tokenisation result</returns>
        public async Task<string> Tokenize(string json)
        {
            AssertStarted();
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Tokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        /// <summary>
        /// Detokenises the provided query.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>detokenisation result</returns>
        public async Task<string> Detokenize(string json)
        {
            AssertStarted();
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Detokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        /// <summary>
        /// Computes the embeddings of the provided query.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>embeddings result</returns>
        public async Task<string> Embeddings(string json)
        {
            AssertStarted();
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Embeddings(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        /// <summary>
        /// Sets the lora scale, only works after the LLM service has started
        /// </summary>
        /// <returns>switch result</returns>
        public async Task<string> SetLoraScale(string loraToScale, float scale)
        {
            AssertStarted();
            List<string> loras = new List<string>(lora.Split(" "));
            string loraToScalePath = GetModelLoraPath(loraToScale, true);

            int index = loras.IndexOf(loraToScale);
            if (index == -1) index = loras.IndexOf(loraToScalePath);
            if (index == -1)
            {
                LLMUnitySetup.LogError($"LoRA {loraToScale} not loaded with the LLM");
                return "";
            }

            loraWeights[index] = scale;
            LoraWeightRequestList loraWeightRequest = new LoraWeightRequestList();
            loraWeightRequest.loraWeights = new List<LoraWeightRequest>();
            for (int i = 0; i < loraWeights.Count; i++)
            {
                loraWeightRequest.loraWeights.Add(new LoraWeightRequest() {id = i, scale = loraWeights[i]});
            }
            ;

            string json = JsonUtility.ToJson(loraWeightRequest);
            int startIndex = json.IndexOf("[");
            int endIndex = json.LastIndexOf("]") + 1;
            json = json.Substring(startIndex, endIndex - startIndex);

            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Lora_Weight(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        /// <summary>
        /// Gets a list of the lora adapters
        /// </summary>
        /// <returns>list of lara adapters</returns>
        public async Task<string> ListLora()
        {
            AssertStarted();
            LLMNoInputReplyCallback callback = (IntPtr LLMObject, IntPtr strWrapper) =>
            {
                llmlib.LLM_LoraList(LLMObject, strWrapper);
            };
            return await LLMNoInputReply(callback);
        }

        /// <summary>
        /// Allows to save / restore the state of a slot
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>slot result</returns>
        public async Task<string> Slot(string json)
        {
            AssertStarted();
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Slot(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        /// <summary>
        /// Allows to use the chat and completion functionality of the LLM.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <param name="streamCallback">callback function to call with intermediate responses</param>
        /// <returns>completion result</returns>
        public async Task<string> Completion(string json, Callback<string> streamCallback = null)
        {
            AssertStarted();
            if (streamCallback == null) streamCallback = (string s) => {};
            StreamWrapper streamWrapper = ConstructStreamWrapper(streamCallback);
            await Task.Run(() => llmlib.LLM_Completion(LLMObject, json, streamWrapper.GetStringWrapper()));
            if (!started) return null;
            streamWrapper.Update();
            string result = streamWrapper.GetString();
            DestroyStreamWrapper(streamWrapper);
            CheckLLMStatus();
            return result;
        }

        public async Task SetBasePrompt(string base_prompt)
        {
            AssertStarted();
            SystemPromptRequest request = new SystemPromptRequest(){system_prompt = base_prompt, prompt = " ", n_predict = 0};
            await Completion(JsonUtility.ToJson(request));
        }

        /// <summary>
        /// Allows to cancel the requests in a specific slot of the LLM
        /// </summary>
        /// <param name="id_slot">slot of the LLM</param>
        public void CancelRequest(int id_slot)
        {
            AssertStarted();
            llmlib?.LLM_Cancel(LLMObject, id_slot);
            CheckLLMStatus();
        }

        /// <summary>
        /// Stops and destroys the LLM
        /// </summary>
        public void Destroy()
        {
            try
            {
                if (llmlib != null)
                {
                    if (LLMObject != IntPtr.Zero)
                    {
                        llmlib.LLM_Stop(LLMObject);
                        if (remote) llmlib.LLM_StopServer(LLMObject);
                        StopLogging();
                        llmThread?.Join();
                        llmlib.LLM_Delete(LLMObject);
                        LLMObject = IntPtr.Zero;
                    }
                    llmlib.Destroy();
                }
                started = false;
                failed = false;
                llmlib = null;
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError(e.Message);
            }
        }

        /// <summary>
        /// The Unity OnDestroy function called when the onbject is destroyed.
        /// The function StopProcess is called to stop the LLM server.
        /// </summary>
        public void OnDestroy()
        {
            Destroy();
            LLMManager.Unregister(this);
        }
    }
}
