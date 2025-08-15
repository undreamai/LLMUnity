/// @file
/// @brief File implementing the LLM.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
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
        /// <summary> show/hide advanced options in the GameObject </summary>
        [Tooltip("show/hide advanced options in the GameObject")]
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> enable remote server functionality </summary>
        [Tooltip("enable remote server functionality")]
        [LocalRemote] public bool remote = false;
        /// <summary> port to use for the remote LLM server </summary>
        [Tooltip("port to use for the remote LLM server")]
        [Remote] public int port = 13333;
        /// <summary> number of threads to use (-1 = all) </summary>
        [Tooltip("number of threads to use (-1 = all)")]
        [LLM] public int numThreads = -1;
        /// <summary> number of model layers to offload to the GPU (0 = GPU not used).
        /// If the user's GPU is not supported, the LLM will fall back to the CPU </summary>
        [Tooltip("number of model layers to offload to the GPU (0 = GPU not used). If the user's GPU is not supported, the LLM will fall back to the CPU")]
        [LLM] public int numGPULayers = 0;
        /// <summary> log the output of the LLM in the Unity Editor. </summary>
        [Tooltip("log the output of the LLM in the Unity Editor.")]
        [LLM] public bool debug = false;
        /// <summary> number of prompts that can happen in parallel (-1 = number of LLMCaller objects) </summary>
        [Tooltip("number of prompts that can happen in parallel (-1 = number of LLMCaller objects)")]
        [LLMAdvanced] public int parallelPrompts = -1;
        /// <summary> do not destroy the LLM GameObject when loading a new Scene. </summary>
        [Tooltip("do not destroy the LLM GameObject when loading a new Scene.")]
        [LLMAdvanced] public bool dontDestroyOnLoad = true;
        /// <summary> Size of the prompt context (0 = context size of the model).
        /// This is the number of tokens the model can take as input when generating responses. </summary>
        [Tooltip("Size of the prompt context (0 = context size of the model). This is the number of tokens the model can take as input when generating responses.")]
        [DynamicRange("minContextLength", "maxContextLength", false), Model] public int contextSize = 8192;
        /// <summary> Batch size for prompt processing. </summary>
        [Tooltip("Batch size for prompt processing.")]
        [ModelAdvanced] public int batchSize = 512;
        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public bool started { get; protected set; } = false;
        /// <summary> Boolean set to true if the server has failed to start. </summary>
        public bool failed { get; protected set; } = false;
        /// <summary> Boolean set to true if the models were not downloaded successfully. </summary>
        public static bool modelSetupFailed { get; protected set; } = false;
        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public static bool modelSetupComplete { get; protected set; } = false;
        /// <summary> LLM model to use (.gguf format) </summary>
        [Tooltip("LLM model to use (.gguf format)")]
        [ModelAdvanced] public string model = "";
        /// <summary> Chat template for the model </summary>
        [Tooltip("Chat template for the model")]
        [ModelAdvanced] public string chatTemplate = ChatTemplate.DefaultTemplate;
        /// <summary> LORA models to use (.gguf format) </summary>
        [Tooltip("LORA models to use (.gguf format)")]
        [ModelAdvanced] public string lora = "";
        /// <summary> the weights of the LORA models being used.</summary>
        [Tooltip("the weights of the LORA models being used.")]
        [ModelAdvanced] public string loraWeights = "";
        /// <summary> enable use of flash attention </summary>
        [Tooltip("enable use of flash attention")]
        [ModelExtras] public bool flashAttention = false;
        /// <summary> API key to use for the server </summary>
        [Tooltip("API key to use for the server")]
        public string APIKey;

        // SSL certificate
        [SerializeField]
        private string SSLCert = "";
        public string SSLCertPath = "";
        // SSL key
        [SerializeField]
        private string SSLKey = "";
        public string SSLKeyPath = "";

        /// \cond HIDE
        public int minContextLength = 0;
        public int maxContextLength = 0;

        public string architecture => llmlib?.architecture;
        List<LLMCaller> clients = new List<LLMCaller>();
        LlamaLib llmlib;
        LLMService llmService = null;
        Thread llmThread = null;
        public LLMManager llmManager = new LLMManager();
        private readonly object startLock = new object();
        static readonly object staticLock = new object();
        public LoraManager loraManager = new LoraManager();
        string loraPre = "";
        string loraWeightsPre = "";
        public bool embeddingsOnly = false;
        public int embeddingLength = 0;

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
        /// </summary>
        public void Awake()
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

            StartService();
            if (!started) return;
            if (dontDestroyOnLoad) DontDestroyOnLoad(transform.root.gameObject);
        }

        private void CheckParameters()
        {
            if ((SSLCert != "" && SSLKey == "") || (SSLCert == "" && SSLKey != ""))
            {
                throw new ArgumentException($"Both SSL certificate and key need to be provided!");
            }
        }

        private string GetModelPath()
        {
            if (model == "")
            {
                throw new ArgumentException("No model file provided!");
            }

            string modelPath = GetLLMManagerAssetRuntime(model);
            if (!File.Exists(modelPath))
            {
                throw new ArgumentException($"File {modelPath} not found!");
            }
            return modelPath;
        }

        private List<string> GetLoraPaths()
        {
            loraManager.FromStrings(lora, loraWeights);
            List<string> loraPaths = new List<string>();
            foreach (string lora in loraManager.GetLoras())
            {
                string loraPath = GetLLMManagerAssetRuntime(lora);
                if (!File.Exists(loraPath))
                {
                    throw new ArgumentException($"File {loraPath} not found!");
                }
                loraPaths.Add(loraPath);
            }
            return loraPaths;
        }

        private void StartService()
        {
            started = false;
            failed = false;

            string modelPath;
            List<string> loraPaths;
            try
            {
                CheckParameters();
                modelPath = GetModelPath();
                loraPaths = GetLoraPaths();
            }
            catch (ArgumentException ex)
            {
                LLMUnitySetup.LogError(ex.Message);
                return;
            }

            try
            {
                CreateLib();
                CreateService(modelPath, loraPaths);
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"LLM service couldn't be created, error: {e.Message}");
                Destroy();
                failed = true;
                return;
            }
            LLMUnitySetup.Log($"LLM service created, using {architecture}");
        }

        private void CreateLib()
        {
            bool useGPU = numGPULayers > 0;
            llmlib = new LlamaLibUnity(useGPU);
            if (debug)
            {
                //todo
                LlamaLibUnity.Debug(2);
                LlamaLibUnity.LoggingCallback(LLMUnitySetup.Log);
            }
        }

        // the following is the equivalent for running from command line
        // string serverCommand;
        // if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) serverCommand = "undreamai_server.exe";
        // else serverCommand = "./undreamai_server";
        // serverCommand += " " + arguments;
        // serverCommand += $" --template \"{chatTemplate}\"";
        // if (remote && SSLCert != "" && SSLKey != "") serverCommand += $" --ssl-cert-file {SSLCertPath} --ssl-key-file {SSLKeyPath}";
        // LLMUnitySetup.Log($"Deploy server command: {serverCommand}");

        private void CreateService(string modelPath, List<string> loraPaths)
        {
            int numSlots = GetNumClients();
            int numThreadsToUse = numThreads;
            if (Application.platform == RuntimePlatform.Android && numThreads <= 0) numThreadsToUse = LLMUnitySetup.AndroidGetNumBigCores();

            lock (staticLock)
            {
                System.IntPtr llm = LLMService.CreateLLM(
                    llmlib,
                    modelPath, numSlots, numThreadsToUse, numGPULayers,
                    flashAttention, contextSize, batchSize, embeddingsOnly, loraPaths.ToArray());

                llmService = new LLMService(llmlib, llm);
                if (remote)
                {
                    if (SSLCert != "" && SSLKey != "")
                    {
                        LLMUnitySetup.Log("Using SSL");
                        llmService.SetSSL(SSLCert, SSLKey);
                    }
                    llmService.StartServer("", port, APIKey);
                }

                llmService.Start();
            }

            started = llmService.Started();
            if (!started) return;

            ApplyLoras();
            //todo
            // llmService.SetTemplate(chatTemplate);
        }

        /// <summary>
        /// Allows to wait until the LLM is ready
        /// </summary>
        public async Task WaitUntilReady()
        {
            while (!started) await Task.Yield();
        }

        /// <summary>
        /// Allows to wait until the LLM models are downloaded and ready
        /// </summary>
        /// <param name="downloadProgressCallback">function to call with the download progress (float)</param>
        public static async Task<bool> WaitUntilModelSetup(Callback<float> downloadProgressCallback = null)
        {
            if (downloadProgressCallback != null) LLMManager.downloadProgressCallbacks.Add(downloadProgressCallback);
            while (!modelSetupComplete) await Task.Yield();
            return !modelSetupFailed;
        }

        /// \cond HIDE
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
            // download path
            assetPath = LLMUnitySetup.GetDownloadAssetPath(path);
            if (File.Exists(assetPath)) return assetPath;
            // give up
            return path;
        }

        /// \endcond

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

                maxContextLength = modelEntry.contextLength;
                if (contextSize > maxContextLength) contextSize = maxContextLength;
                SetEmbeddings(modelEntry.embeddingLength, modelEntry.embeddingOnly);
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
            if (started) llmService.SetTemplate(chatTemplate);
#if UNITY_EDITOR
            if (setDirty && !EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Set LLM Embedding parameters
        /// </summary>
        /// <param name="embeddingLength"> number of embedding dimensions </param>
        /// <param name="embeddingsOnly"> if true, the LLM will be used only for embeddings </param>
        public void SetEmbeddings(int embeddingLength, bool embeddingsOnly)
        {
            this.embeddingsOnly = embeddingsOnly;
            this.embeddingLength = embeddingLength;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// \cond HIDE

        string ReadFileContents(string path)
        {
            if (String.IsNullOrEmpty(path)) return "";
            else if (!File.Exists(path))
            {
                LLMUnitySetup.LogError($"File {path} not found!");
                return "";
            }
            return File.ReadAllText(path);
        }

        /// \endcond

        /// <summary>
        /// Use a SSL certificate for the LLM server.
        /// </summary>
        /// <param name="templateName">the SSL certificate path </param>
        public void SetSSLCert(string path)
        {
            SSLCertPath = path;
            SSLCert = ReadFileContents(path);
        }

        /// <summary>
        /// Use a SSL key for the LLM server.
        /// </summary>
        /// <param name="templateName">the SSL key path </param>
        public void SetSSLKey(string path)
        {
            SSLKeyPath = path;
            SSLKey = ReadFileContents(path);
        }

        /// <summary>
        /// Returns the chat template of the LLM.
        /// </summary>
        /// <returns>chat template of the LLM</returns>
        public string GetTemplate()
        {
            return chatTemplate;
        }

        /// <summary>
        /// Registers a local LLMCaller object.
        /// This allows to bind the LLMCaller "client" to a specific slot of the LLM.
        /// </summary>
        /// <param name="llmCaller"></param>
        /// <returns></returns>
        public int Register(LLMCaller llmCaller)
        {
            clients.Add(llmCaller);
            int index = clients.IndexOf(llmCaller);
            if (parallelPrompts != -1) return index % parallelPrompts;
            return index;
        }

        protected int GetNumClients()
        {
            return Math.Max(parallelPrompts == -1 ? clients.Count : parallelPrompts, 1);
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

        /// <summary>
        /// Sets the lora scale, only works after the LLM service has started
        /// </summary>
        /// <returns>switch result</returns>
        public void ApplyLoras()
        {
            List<LoraIdScale> loras = new List<LoraIdScale>();
            float[] weights = loraManager.GetWeights();
            if (weights.Length == 0) return;
            for (int i = 0; i < weights.Length; i++)
            {
                loras.Add(new LoraIdScale(i, weights[i]));
            }
            llmService.LoraWeight(loras);
        }

        /// <summary>
        /// Tokenises the provided query.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>tokenisation result</returns>
        public List<int> Tokenize(string content)
        {
            AssertStarted();
            return llmService.Tokenize(content);
        }

        /// <summary>
        /// Detokenises the provided query.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>detokenisation result</returns>
        public string Detokenize(List<int> tokens)
        {
            AssertStarted();
            return llmService.Detokenize(tokens);
        }

        /// <summary>
        /// Computes the embeddings of the provided query.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>embeddings result</returns>
        public List<float> Embeddings(string content)
        {
            AssertStarted();
            return llmService.Embeddings(content);
        }

        /// <summary>
        /// Gets a list of the lora adapters
        /// </summary>
        /// <returns>list of lara adapters</returns>
        public List<LoraIdScalePath> ListLoras()
        {
            AssertStarted();
            return llmService.LoraList();
        }

        /// <summary>
        /// Allows to save / restore the state of a slot
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>slot result</returns>
        public string SaveSlot(int idSlot, string filepath)
        {
            AssertStarted();
            return llmService.SaveSlot(idSlot, filepath);
        }

        /// <summary>
        /// Allows to save / restore the state of a slot
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <returns>slot result</returns>
        public string LoadSlot(int idSlot, string filepath)
        {
            AssertStarted();
            return llmService.LoadSlot(idSlot, filepath);
        }

        /// <summary>
        /// Allows to use the chat and completion functionality of the LLM.
        /// </summary>
        /// <param name="json">json request containing the query</param>
        /// <param name="streamCallback">callback function to call with intermediate responses</param>
        /// <returns>completion result</returns>
        public string Completion(string prompt, LlamaLibUnity.CharArrayCallback streamCallback = null, int id_slot = -1)
        {
            AssertStarted();
            return llmService.Completion(prompt, streamCallback, id_slot);
        }

        public async Task<string> CompletionAsync(string prompt, LlamaLibUnity.CharArrayCallback streamCallback = null, int id_slot = -1)
        {
            AssertStarted();
            return await llmService.CompletionAsync(prompt, streamCallback, id_slot);
        }

        /// <summary>
        /// Allows to cancel the requests in a specific slot of the LLM
        /// </summary>
        /// <param name="id_slot">slot of the LLM</param>
        public void CancelRequest(int id_slot)
        {
            AssertStarted();
            llmService.Cancel(id_slot);
        }

        /// <summary>
        /// Stops and destroys the LLM
        /// </summary>
        public void Destroy()
        {
            lock (staticLock)
            {
                try
                {
                    if (llmlib != null)
                    {
                        if (llmService != null)
                        {
                            Debug.Log("stopping");
                            llmService.Dispose();
                            Debug.Log("Dispose");
                        }
                        llmlib = null;
                    }
                    started = false;
                    failed = false;
                }
                catch (Exception e)
                {
                    LLMUnitySetup.LogError(e.Message);
                }
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


    public class LlamaLibUnity : UndreamAI.LlamaLib.LlamaLib
    {
        public LlamaLibUnity(bool gpu = false) : base(gpu) {}

        public override string FindLibrary(string libraryName)
        {
            string lookupDir = Path.Combine(LLMUnitySetup.libraryPath, GetPlatform(), "native");
            string libraryPath = Path.Combine(lookupDir, libraryName);
            if (File.Exists(libraryPath)) return libraryPath;

            throw new System.Exception($"Library {libraryName} not found!");
        }
    }
}
