/// @file
/// @brief File implementing the LLM.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
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
        /// Models with .gguf format are allowed.</summary>
        [ModelAdvanced] public string lora = "";
        /// <summary> the weights of the LORA models being used.</summary>
        [ModelAdvanced] public string loraWeights = "";
        /// <summary> enable use of flash attention </summary>
        [ModelExtras] public bool flashAttention = false;

        /// \cond HIDE

        IntPtr LLMObject = IntPtr.Zero;
        List<LLMCharacter> clients = new List<LLMCharacter>();
        LLMLib llmlib;
        StreamWrapper logStreamWrapper = null;
        Thread llmThread = null;
        List<StreamWrapper> streamWrappers = new List<StreamWrapper>();
        public LLMManager llmManager = new LLMManager();
        private readonly object startLock = new object();
        public LoraManager loraManager = new LoraManager();
        string loraPre = "";
        string loraWeightsPre = "";

        /// \endcond

        public LLM()
        {
            LLMManager.Register(this);
        }

        void OnValidate()
        {
            if (lora != loraPre || loraWeights != loraWeightsPre)
            {
                loraManager.FromStrings(lora, loraWeights);
                (loraPre, loraWeightsPre) = (lora, loraWeights);
            }
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
            if (!started) return;
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

        public static string GetLLMManagerAsset(string path)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) return GetLLMManagerAssetEditor(path);
#endif
            return GetLLMManagerAssetRuntime(path);
        }

        public static string GetLLMManagerAssetEditor(string path)
        {
            // empty
            if (string.IsNullOrEmpty(path)) return path;
            // LLMManager - return location the file will be stored in StreamingAssets
            ModelEntry modelEntry = LLMManager.Get(path);
            if (modelEntry != null) return modelEntry.filename;
            // StreamingAssets - return relative location within StreamingAssets
            string assetPath = LLMUnitySetup.GetAssetPath(path); // Note: this will return the full path if a full path is passed
            string basePath = LLMUnitySetup.GetAssetPath();
            if (File.Exists(assetPath))
            {
                if (LLMUnitySetup.IsSubPath(assetPath, basePath)) return LLMUnitySetup.RelativePath(assetPath, basePath);
            }
            // full path
            if (!File.Exists(assetPath))
            {
                LLMUnitySetup.LogError($"Model {path} was not found.");
            }
            else
            {
                string errorMessage = $"The model {path} was loaded locally. You can include it in the build in one of these ways:";
                errorMessage += $"\n-Copy the model inside the StreamingAssets folder and use its StreamingAssets path";
                errorMessage += $"\n-Load the model with the model manager inside the LLM GameObject and use its filename";
                LLMUnitySetup.LogWarning(errorMessage);
            }
            return path;
        }

        public static string GetLLMManagerAssetRuntime(string path)
        {
            // empty
            if (string.IsNullOrEmpty(path)) return path;
            // LLMManager
            string managerPath = LLMManager.GetAssetPath(path);
            if (!string.IsNullOrEmpty(managerPath) && File.Exists(managerPath)) return managerPath;
            // StreamingAssets
            string assetPath = LLMUnitySetup.GetAssetPath(path);
            if (File.Exists(assetPath)) return assetPath;
            // give up
            return path;
        }

        /// <summary>
        /// Allows to set the model used by the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to model to use (.gguf format)</param>
        public void SetModel(string path)
        {
            model = GetLLMManagerAsset(path);
            if (!string.IsNullOrEmpty(model))
            {
                ModelEntry modelEntry = LLMManager.Get(model);
                if (modelEntry == null) modelEntry = new ModelEntry(GetLLMManagerAssetRuntime(model));
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
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to LORA model to use (.gguf format)</param>
        public void SetLora(string path, float weight = 1)
        {
            AssertNotStarted();
            loraManager.Clear();
            AddLora(path, weight);
        }

        /// <summary>
        /// Allows to add a LORA model to use in the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to LORA model to use (.gguf format)</param>
        public void AddLora(string path, float weight = 1)
        {
            AssertNotStarted();
            loraManager.Add(path, weight);
            UpdateLoras();
        }

        /// <summary>
        /// Allows to remove a LORA model from the LLM.
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to LORA model to remove (.gguf format)</param>
        public void RemoveLora(string path)
        {
            AssertNotStarted();
            loraManager.Remove(path);
            UpdateLoras();
        }

        /// <summary>
        /// Allows to remove all LORA models from the LLM.
        /// </summary>
        public void RemoveLoras()
        {
            AssertNotStarted();
            loraManager.Clear();
            UpdateLoras();
        }

        /// <summary>
        /// Allows to change the weight (scale) of a LORA model in the LLM.
        /// </summary>
        /// <param name="path">path of LORA model to change (.gguf format)</param>
        /// <param name="weight">weight of LORA</param>
        public void SetLoraWeight(string path, float weight)
        {
            loraManager.SetWeight(path, weight);
            UpdateLoras();
            if (started) ApplyLoras();
        }

        /// <summary>
        /// Allows to change the weights (scale) of the LORA models in the LLM.
        /// </summary>
        /// <param name="loraToWeight">Dictionary (string, float) mapping the path of LORA models with weights to change</param>
        public void SetLoraWeights(Dictionary<string, float> loraToWeight)
        {
            foreach (KeyValuePair<string, float> entry in loraToWeight) loraManager.SetWeight(entry.Key, entry.Value);
            UpdateLoras();
            if (started) ApplyLoras();
        }

        public void UpdateLoras()
        {
            (lora, loraWeights) = loraManager.ToStrings();
            (loraPre, loraWeightsPre) = (lora, loraWeights);
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
            string modelPath = GetLLMManagerAssetRuntime(model);
            if (!File.Exists(modelPath))
            {
                LLMUnitySetup.LogError($"File {modelPath} not found!");
                return null;
            }
            string loraArgument = "";
            foreach (string lora in lora.Trim().Split(" "))
            {
                if (lora == "") continue;
                string loraPath = GetLLMManagerAssetRuntime(lora);
                if (!File.Exists(loraPath))
                {
                    LLMUnitySetup.LogError($"File {loraPath} not found!");
                    return null;
                }
                loraArgument += $" --lora \"{loraPath}\"";
            }
            loraManager.FromStrings(lora, loraWeights);

            int numThreadsToUse = numThreads;
            if (Application.platform == RuntimePlatform.Android && numThreads <= 0) numThreadsToUse = LLMUnitySetup.AndroidGetNumBigCores();

            int slots = GetNumClients();
            string arguments = $"-m \"{modelPath}\" -c {contextSize} -b {batchSize} --log-disable -np {slots}";
            if (remote) arguments += $" --port {port} --host 0.0.0.0";
            if (numThreadsToUse > 0) arguments += $" -t {numThreadsToUse}";
            arguments += loraArgument;
            arguments += $" -ngl {numGPULayers}";
            if (LLMUnitySetup.FullLlamaLib && flashAttention) arguments += $" --flash-attn";
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
                    InitService(arguments);
                    LLMUnitySetup.Log($"Using architecture: {arch}");
                    break;
                }
                catch (LLMException e)
                {
                    error = e.Message;
                    Destroy();
                }
                catch (DestroyException)
                {
                    break;
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
            CallWithLock(StartService);
            LLMUnitySetup.Log("LLM service created");
        }

        private void InitLib(string arch)
        {
            llmlib = new LLMLib(arch);
            CheckLLMStatus(false);
        }

        void CallWithLock(EmptyCallback fn, bool checkNull = true)
        {
            lock (startLock)
            {
                if (checkNull && llmlib == null) throw new DestroyException();
                fn();
            }
        }

        private void InitService(string arguments)
        {
            if (debug) CallWithLock(SetupLogging);
            CallWithLock(() => { LLMObject = llmlib.LLM_Construct(arguments); });
            CallWithLock(() => llmlib.LLM_SetTemplate(LLMObject, chatTemplate));
            if (remote) CallWithLock(() => llmlib.LLM_StartServer(LLMObject));
            CallWithLock(() => CheckLLMStatus(false));
        }

        private void StartService()
        {
            llmThread = new Thread(() => llmlib.LLM_Start(LLMObject));
            llmThread.Start();
            while (!llmlib.LLM_Started(LLMObject)) {}
            ApplyLoras();
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
            int index = clients.IndexOf(llmCharacter);
            if (parallelPrompts != -1) return index % parallelPrompts;
            return index;
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

        void AssertNotStarted()
        {
            if (started)
            {
                string error = "This method can't be called when the LLM has started";
                LLMUnitySetup.LogError(error);
                throw new Exception(error);
            }
        }

        void CheckLLMStatus(bool log = true)
        {
            if (llmlib == null) { return; }
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
        public void ApplyLoras()
        {
            LoraWeightRequestList loraWeightRequest = new LoraWeightRequestList();
            loraWeightRequest.loraWeights = new List<LoraWeightRequest>();
            float[] weights = loraManager.GetWeights();
            for (int i = 0; i < weights.Length; i++)
            {
                loraWeightRequest.loraWeights.Add(new LoraWeightRequest() { id = i, scale = weights[i] });
            }

            string json = JsonUtility.ToJson(loraWeightRequest);
            int startIndex = json.IndexOf("[");
            int endIndex = json.LastIndexOf("]") + 1;
            json = json.Substring(startIndex, endIndex - startIndex);

            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            llmlib.LLM_Lora_Weight(LLMObject, json, stringWrapper);
            llmlib.StringWrapper_Delete(stringWrapper);
        }

        /// <summary>
        /// Gets a list of the lora adapters
        /// </summary>
        /// <returns>list of lara adapters</returns>
        public async Task<List<LoraWeightResult>> ListLoras()
        {
            AssertStarted();
            LLMNoInputReplyCallback callback = (IntPtr LLMObject, IntPtr strWrapper) =>
            {
                llmlib.LLM_LoraList(LLMObject, strWrapper);
            };
            string json = await LLMNoInputReply(callback);
            if (String.IsNullOrEmpty(json)) return null;
            LoraWeightResultList loraRequest = JsonUtility.FromJson<LoraWeightResultList>("{\"loraWeights\": " + json + "}");
            return loraRequest.loraWeights;
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
            SystemPromptRequest request = new SystemPromptRequest() { system_prompt = base_prompt, prompt = " ", n_predict = 0 };
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
            CallWithLock(() =>
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
                        llmlib = null;
                    }
                    started = false;
                    failed = false;
                }
                catch (Exception e)
                {
                    LLMUnitySetup.LogError(e.Message);
                }
            }, false);
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
