/// @file
/// @brief File implementing the LLM server component for Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    /// @ingroup llm
    /// <summary>
    /// Unity MonoBehaviour component that manages a local LLM server instance.
    /// Handles model loading, GPU acceleration, LORA adapters, and provides
    /// completion, tokenization, and embedding functionality.
    /// </summary>
    public class LLM : MonoBehaviour
    {
        #region Inspector Fields
        /// <summary>Show/hide advanced options in the inspector</summary>
        [Tooltip("Show/hide advanced options in the inspector")]
        [HideInInspector] public bool advancedOptions = false;

        /// <summary>Enable remote server functionality to allow external connections</summary>
        [Tooltip("Enable remote server functionality to allow external connections")]
        [LocalRemote, SerializeField] private bool _remote = false;

        /// <summary>Port to use for the remote LLM server</summary>
        [Tooltip("Port to use for the remote LLM server")]
        [Remote, SerializeField] private int _port = 13333;

        /// <summary>API key required for server access (leave empty to disable authentication)</summary>
        [Tooltip("API key required for server access (leave empty to disable authentication)")]
        [SerializeField] private string _APIKey = "";

        /// <summary>SSL certificate for the remote LLM server</summary>
        [Tooltip("SSL certificate for the remote LLM server")]
        [SerializeField] private string _SSLCert = "";

        /// <summary>SSL key for the remote LLM server</summary>
        [Tooltip("SSL key for the remote LLM server")]
        [SerializeField] private string _SSLKey = "";

        /// <summary>Number of threads to use for processing (-1 = use all available threads)</summary>
        [Tooltip("Number of threads to use for processing (-1 = use all available threads)")]
        [LLM, SerializeField] private int _numThreads = -1;

        /// <summary>Number of model layers to offload to GPU (0 = CPU only). Falls back to CPU if GPU unsupported</summary>
        [Tooltip("Number of model layers to offload to GPU (0 = CPU only). Falls back to CPU if GPU unsupported")]
        [LLM, SerializeField] private int _numGPULayers = 0;

        /// <summary>Number of prompts that can be processed in parallel (-1 = auto-detect from clients)</summary>
        [Tooltip("Number of prompts that can be processed in parallel (-1 = auto-detect from clients)")]
        [LLM, SerializeField] private int _parallelPrompts = -1;

        /// <summary>Size of the prompt context in tokens (0 = use model's default context size)</summary>
        [Tooltip("Size of the prompt context in tokens (0 = use model's default context size). This determines how much conversation history the model can remember.")]
        [DynamicRange("minContextLength", "maxContextLength", false), Model, SerializeField] private int _contextSize = 8192;

        /// <summary>Batch size for prompt processing (larger = more memory, potentially faster)</summary>
        [Tooltip("Batch size for prompt processing (larger = more memory, potentially faster)")]
        [ModelAdvanced, SerializeField] private int _batchSize = 512;

        /// <summary>LLM model file path (.gguf format)</summary>
        [Tooltip("LLM model file path (.gguf format)")]
        [ModelAdvanced, SerializeField] private string _model = "";

        /// <summary>Enable flash attention optimization (requires compatible model)</summary>
        [Tooltip("Enable flash attention optimization (requires compatible model)")]
        [ModelExtras, SerializeField] private bool _flashAttention = false;

        /// <summary>Chat template for conversation formatting ("auto" = detect from model)</summary>
        [Tooltip("Chat template for conversation formatting (\"auto\" = detect from model)")]
        [ModelAdvanced, SerializeField] private string _chatTemplate = "auto";

        /// <summary>LORA adapter model paths (.gguf format), separated by commas</summary>
        [Tooltip("LORA adapter model paths (.gguf format), separated by commas")]
        [ModelAdvanced, SerializeField] private string _lora = "";

        /// <summary>Weights for LORA adapters, separated by commas (default: 1.0 for each)</summary>
        [Tooltip("Weights for LORA adapters, separated by commas (default: 1.0 for each)")]
        [ModelAdvanced, SerializeField] private string _loraWeights = "";

        /// <summary>Persist this LLM GameObject across scene transitions</summary>
        [Tooltip("Persist this LLM GameObject across scene transitions")]
        [LLM] public bool dontDestroyOnLoad = true;
        #endregion

        #region Public Properties with Validation

        /// <summary>Number of threads to use for processing (-1 = use all available threads)</summary>
        public int numThreads
        {
            get => _numThreads;
            set
            {
                AssertNotStarted();
                if (value < -1)
                    throw new ArgumentException("numThreads must be >= -1");
                _numThreads = value;
            }
        }

        /// <summary>Number of model layers to offload to GPU (0 = CPU only)</summary>
        public int numGPULayers
        {
            get => _numGPULayers;
            set
            {
                AssertNotStarted();
                if (value < 0)
                    throw new ArgumentException("numGPULayers must be >= 0");
                _numGPULayers = value;
            }
        }

        /// <summary>Number of prompts that can be processed in parallel (-1 = auto-detect from clients)</summary>
        public int parallelPrompts
        {
            get => _parallelPrompts;
            set
            {
                AssertNotStarted();
                if (value < -1)
                    throw new ArgumentException("parallelPrompts must be >= -1");
                _parallelPrompts = value;
            }
        }

        /// <summary>Size of the prompt context in tokens (0 = use model's default context size)</summary>
        public int contextSize
        {
            get => _contextSize;
            set
            {
                AssertNotStarted();
                if (value < 0)
                    throw new ArgumentException("contextSize must be >= 0");
                _contextSize = value;
            }
        }

        /// <summary>Batch size for prompt processing (larger = more memory, potentially faster)</summary>
        public int batchSize
        {
            get => _batchSize;
            set
            {
                AssertNotStarted();
                if (value <= 0)
                    throw new ArgumentException("batchSize must be > 0");
                _batchSize = value;
            }
        }

        /// <summary>Enable flash attention optimization (requires compatible model)</summary>
        public bool flashAttention
        {
            get => _flashAttention;
            set
            {
                AssertNotStarted();
                _flashAttention = value;
            }
        }

        /// <summary>LLM model file path (.gguf format)</summary>
        public string model
        {
            get => _model;
            set => SetModel(value);
        }

        /// <summary>Chat template for conversation formatting ("auto" = detect from model)</summary>
        public string chatTemplate
        {
            get => _chatTemplate;
            set => SetTemplate(value);
        }

        /// <summary>LORA adapter model paths (.gguf format), separated by commas</summary>
        public string lora
        {
            get => _lora;
            set
            {
                if (value == _lora) return;
                AssertNotStarted();
                _lora = value;
                UpdateLoraManagerFromStrings();
            }
        }

        /// <summary>Weights for LORA adapters, separated by commas (default: 1.0 for each)</summary>
        public string loraWeights
        {
            get => _loraWeights;
            set
            {
                if (value == _loraWeights) return;
                _loraWeights = value;
                UpdateLoraManagerFromStrings();
                ApplyLoras();
            }
        }

        /// <summary>Enable remote server functionality to allow external connections</summary>
        public bool remote
        {
            get => _remote;
            set
            {
                if (value == _remote) return;
                _remote = value;
                RestartServer();
            }
        }

        /// <summary>Port to use for the remote LLM server</summary>
        public int port
        {
            get => _port;
            set
            {
                if (value == _port) return;
                if (value < 0 || value > 65535)
                    throw new ArgumentException("port must be between 0 and 65535");
                _port = value;
                RestartServer();
            }
        }

        /// <summary>API key required for server access (leave empty to disable authentication)</summary>
        public string APIKey
        {
            get => _APIKey;
            set
            {
                if (value == _APIKey) return;
                _APIKey = value;
                RestartServer();
            }
        }

        /// <summary>SSL certificate for the remote LLM server</summary>
        public string SSLCert
        {
            get => _SSLCert;
            set
            {
                AssertNotStarted();
                if (value == _SSLCert) return;
                _SSLCert = value;
            }
        }

        /// <summary>SSL key for the remote LLM server</summary>
        public string SSLKey
        {
            get => _SSLKey;
            set
            {
                AssertNotStarted();
                if (value == _SSLKey) return;
                _SSLKey = value;
            }
        }

        #endregion

        #region Other Public Properties
        /// <summary>True if the LLM server has started and is ready to receive requests</summary>
        public bool started { get; private set; } = false;

        /// <summary>True if the LLM server failed to start</summary>
        public bool failed { get; private set; } = false;

        /// <summary>True if model setup failed during initialization</summary>
        public static bool modelSetupFailed { get; private set; } = false;

        /// <summary>True if model setup completed (successfully or not)</summary>
        public static bool modelSetupComplete { get; private set; } = false;

        /// <summary>The underlying LLM service instance</summary>
        public LLMService llmService { get; private set; }

        /// <summary>Model architecture name (e.g., "llama", "mistral")</summary>
        public string architecture => llmlib?.architecture;

        /// <summary>True if this model only supports embeddings (no text generation)</summary>
        public bool embeddingsOnly { get; private set; } = false;

        /// <summary>Number of dimensions in embedding vectors (0 if not an embedding model)</summary>
        public int embeddingLength { get; private set; } = 0;
        #endregion

        #region Private Fields
        /// \cond HIDE
        public int minContextLength = 0;
        public int maxContextLength = 0;

        public static readonly string[] ChatTemplates = new string[]
        {
            "auto", "chatml", "llama2", "llama2-sys", "llama2-sys-bos", "llama2-sys-strip",
            "mistral-v1", "mistral-v3", "mistral-v3-tekken", "mistral-v7", "mistral-v7-tekken",
            "phi3", "phi4", "falcon3", "zephyr", "monarch", "gemma", "orion", "openchat",
            "vicuna", "vicuna-orca", "deepseek", "deepseek2", "deepseek3", "command-r",
            "llama3", "chatglm3", "chatglm4", "glmedge", "minicpm", "exaone3", "exaone4",
            "rwkv-world", "granite", "gigachat", "megrez", "yandex", "bailing", "llama4",
            "smolvlm", "hunyuan-moe", "gpt-oss", "hunyuan-dense", "kimi-k2"
        };

        private LlamaLib llmlib = null;
        // [Local, SerializeField]
        protected LLMService _llmService;
        private readonly List<LLMClient> clients = new List<LLMClient>();
        public LLMManager llmManager = new LLMManager();
        private static readonly object staticLock = new object();
        public LoraManager loraManager = new LoraManager();
        /// \endcond
        #endregion

        #region Unity Lifecycle
        public LLM()
        {
            LLMManager.Register(this);
        }

        /// <summary>
        /// Unity Awake method that initializes the LLM server.
        /// Sets up the model, starts the service, and handles GPU fallback if needed.
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

            await StartServiceAsync();
            if (!started) return;
            if (dontDestroyOnLoad) DontDestroyOnLoad(transform.root.gameObject);
        }

        public void OnDestroy()
        {
            Destroy();
            LLMManager.Unregister(this);
        }

        #endregion

        #region Initialization
        private void ValidateParameters()
        {
            if ((SSLCert != "" && SSLKey == "") || (SSLCert == "" && SSLKey != ""))
            {
                throw new ArgumentException("Both SSL certificate and key must be provided together!");
            }
        }

        private string GetValidatedModelPath()
        {
            if (string.IsNullOrEmpty(model))
            {
                throw new ArgumentException("No model file provided!");
            }

            string modelPath = GetLLMManagerAssetRuntime(model);
            if (!File.Exists(modelPath))
            {
                throw new ArgumentException($"Model file not found: {modelPath}");
            }
            return modelPath;
        }

        private List<string> GetValidatedLoraPaths()
        {
            loraManager.FromStrings(lora, loraWeights);
            List<string> loraPaths = new List<string>();

            foreach (string loraPath in loraManager.GetLoras())
            {
                string resolvedPath = GetLLMManagerAssetRuntime(loraPath);
                if (!File.Exists(resolvedPath))
                {
                    throw new ArgumentException($"LORA file not found: {resolvedPath}");
                }
                loraPaths.Add(resolvedPath);
            }
            return loraPaths;
        }

        private async Task StartServiceAsync()
        {
            started = false;
            failed = false;

            try
            {
                ValidateParameters();
                string modelPath = GetValidatedModelPath();
                List<string> loraPaths = GetValidatedLoraPaths();

                CreateLib();
                await CreateServiceAsync(modelPath, loraPaths);
            }
            catch (ArgumentException ex)
            {
                LLMUnitySetup.LogError(ex.Message);
                failed = true;
                return;
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to create LLM service: {ex.Message}");
                Destroy();
                failed = true;
                return;
            }

            if (started)
            {
                LLMUnitySetup.Log($"LLM service created successfully, using {architecture}");
            }
        }

        private void CreateLib()
        {
            bool useGPU = numGPULayers > 0;
            llmlib = new LlamaLibUnity(useGPU);

            if (LLMUnitySetup.DebugMode <= LLMUnitySetup.DebugModeType.All)
            {
                LlamaLibUnity.Debug(LLMUnitySetup.DebugModeType.All - LLMUnitySetup.DebugMode + 1);
                LlamaLibUnity.LoggingCallback(LLMUnitySetup.Log);
            }
        }

        /// <summary>
        /// Setup the remote LLM server
        /// </summary>
        private void SetupServer()
        {
            if (!remote) return;

            if (!string.IsNullOrEmpty(SSLCert) && !string.IsNullOrEmpty(SSLKey))
            {
                LLMUnitySetup.Log("Enabling SSL for server");
                llmService.SetSSL(SSLCert, SSLKey);
            }
            llmService.StartServer("", port, APIKey);
        }

        /// <summary>
        /// Restart the remote LLM server (on parameter change)
        /// </summary>
        private void RestartServer()
        {
            if (!started) return;
            llmService.StopServer();
            SetupServer();
        }

        private async Task CreateServiceAsync(string modelPath, List<string> loraPaths)
        {
            int numSlots = GetNumClients();
            int effectiveThreads = numThreads;

            if (Application.platform == RuntimePlatform.Android && numThreads <= 0)
            {
                effectiveThreads = LLMUnitySetup.AndroidGetNumBigCores();
            }

            await Task.Run(() =>
            {
                lock (staticLock)
                {
                    IntPtr llmPtr = LLMService.CreateLLM(
                        llmlib, modelPath, numSlots, effectiveThreads, numGPULayers,
                        flashAttention, contextSize, batchSize, embeddingsOnly, loraPaths.ToArray());

                    llmService = new LLMService(llmlib, llmPtr);
                    SetupServer();
                    llmService.Start();
                }
            });

            started = llmService.Started();
            if (!started) return;

            ApplyLoras();
            SetTemplate(chatTemplate);
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Waits asynchronously until the LLM is ready to accept requests.
        /// </summary>
        /// <returns>Task that completes when LLM is ready</returns>
        public async Task WaitUntilReady()
        {
            while (!started && !failed)
            {
                await Task.Yield();
            }

            if (failed)
            {
                throw new InvalidOperationException("LLM failed to start");
            }
        }

        /// <summary>
        /// Waits asynchronously until model setup is complete.
        /// </summary>
        /// <param name="downloadProgressCallback">Optional callback for download progress updates</param>
        /// <returns>True if setup succeeded, false if it failed</returns>
        public static async Task<bool> WaitUntilModelSetup(Callback<float> downloadProgressCallback = null)
        {
            if (downloadProgressCallback != null)
            {
                LLMManager.downloadProgressCallbacks.Add(downloadProgressCallback);
            }

            while (!modelSetupComplete)
            {
                await Task.Yield();
            }

            return !modelSetupFailed;
        }

        /// <summary>
        /// Sets the model file to use. Automatically configures context size and embedding settings.
        /// </summary>
        /// <param name="path">Path to the model file (.gguf format)</param>
        public void SetModel(string path)
        {
            if (model == path) return;
            AssertNotStarted();

            _model = GetLLMManagerAsset(path);
            if (string.IsNullOrEmpty(model)) return;

            ModelEntry modelEntry = LLMManager.Get(model) ?? new ModelEntry(GetLLMManagerAssetRuntime(model));

            maxContextLength = modelEntry.contextLength;
            if (contextSize > maxContextLength)
            {
                contextSize = maxContextLength;
            }

            SetEmbeddings(modelEntry.embeddingLength, modelEntry.embeddingOnly);

            if (contextSize == 0 && modelEntry.contextLength > 32768)
            {
                LLMUnitySetup.LogWarning($"Model {path} has large context size ({modelEntry.contextLength}). Consider setting contextSize to ≤32768 to avoid excessive memory usage.");
            }

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Sets the chat template for message formatting.
        /// </summary>
        /// <param name="templateName">Template name (see ChatTemplates array for options)</param>
        /// <param name="setDirty">Mark object dirty in editor</param>
        public void SetTemplate(string templateName, bool setDirty = true)
        {
            if (_chatTemplate == templateName) return;
            if (!ChatTemplates.Contains(templateName))
            {
                LLMUnitySetup.LogError($"Unsupported chat template: {templateName}");
                return;
            }

            _chatTemplate = templateName;
            if (started)
            {
                llmService.SetTemplate(_chatTemplate == "auto" ? "" : _chatTemplate);
            }

#if UNITY_EDITOR
            if (setDirty && !EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Configure the LLM for embedding generation.
        /// </summary>
        /// <param name="embeddingLength">Number of embedding dimensions</param>
        /// <param name="embeddingsOnly">True if model only supports embeddings</param>
        public void SetEmbeddings(int embeddingLength, bool embeddingsOnly)
        {
            this.embeddingsOnly = embeddingsOnly;
            this.embeddingLength = embeddingLength;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Registers an LLMClient for slot management.
        /// </summary>
        /// <param name="llmClient">Client to register</param>
        /// <returns>Assigned slot ID</returns>
        public void Register(LLMClient llmClient)
        {
            if (llmClient == null)
            {
                throw new ArgumentNullException(nameof(llmClient));
            }

            clients.Add(llmClient);
        }

        /// <summary>
        /// Tokenizes the provided text into a list of token IDs.
        /// </summary>
        /// <param name="content">Text to tokenize</param>
        /// <returns>List of token IDs</returns>
        public List<int> Tokenize(string content)
        {
            AssertStarted();
            return llmService.Tokenize(content);
        }

        /// <summary>
        /// Converts token IDs back to text.
        /// </summary>
        /// <param name="tokens">List of token IDs</param>
        /// <returns>Detokenised text</returns>
        public string Detokenize(List<int> tokens)
        {
            AssertStarted();
            return llmService.Detokenize(tokens);
        }

        /// <summary>
        /// Generates embedding vectors for the provided text.
        /// </summary>
        /// <param name="content">Text to embed</param>
        /// <returns>Embedding vector</returns>
        public List<float> Embeddings(string content)
        {
            AssertStarted();
            return llmService.Embeddings(content);
        }

        /// <summary>
        /// Generates text completion for the given prompt.
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="streamCallback">Optional callback for streaming responses</param>
        /// <param name="id_slot">Slot ID (-1 for automatic assignment)</param>
        /// <returns>Generated text</returns>
        public string Completion(string prompt, LlamaLibUnity.CharArrayCallback streamCallback = null, int id_slot = -1)
        {
            AssertStarted();
            return llmService.Completion(prompt, streamCallback, id_slot);
        }

        /// <summary>
        /// Generates text completion asynchronously.
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="streamCallback">Optional callback for streaming responses</param>
        /// <param name="id_slot">Slot ID (-1 for automatic assignment)</param>
        /// <returns>Task that returns generated text</returns>
        public async Task<string> CompletionAsync(string prompt, LlamaLibUnity.CharArrayCallback streamCallback = null, int id_slot = -1)
        {
            AssertStarted();
            // Wrap callback to ensure it runs on the main thread
            LlamaLib.CharArrayCallback wrappedCallback = Utils.WrapCallbackForAsync(streamCallback, this);
            return await llmService.CompletionAsync(prompt, wrappedCallback, id_slot);
        }

        /// <summary>
        /// Cancels the request in the specified slot.
        /// </summary>
        /// <param name="id_slot">Slot ID</param>
        public void CancelRequest(int id_slot)
        {
            AssertStarted();
            llmService.Cancel(id_slot);
        }

        /// <summary>
        /// Cancels all active requests.
        /// </summary>
        public void CancelRequests()
        {
            for (int i = 0; i < parallelPrompts; i++)
            {
                CancelRequest(i);
            }
        }

        /// <summary>
        /// Saves the state of a specific slot to disk.
        /// </summary>
        /// <param name="idSlot">Slot ID</param>
        /// <param name="filepath">File path to save to</param>
        /// <returns>Result message</returns>
        public string SaveSlot(int idSlot, string filepath)
        {
            AssertStarted();
            return llmService.SaveSlot(idSlot, filepath);
        }

        /// <summary>
        /// Loads the state of a specific slot from disk.
        /// </summary>
        /// <param name="idSlot">Slot ID</param>
        /// <param name="filepath">File path to load from</param>
        /// <returns>Result message</returns>
        public string LoadSlot(int idSlot, string filepath)
        {
            AssertStarted();
            return llmService.LoadSlot(idSlot, filepath);
        }

        /// <summary>
        /// Gets a list of loaded LORA adapters.
        /// </summary>
        /// <returns>List of LORA adapter information</returns>
        public List<LoraIdScalePath> ListLoras()
        {
            AssertStarted();
            return llmService.LoraList();
        }

        #endregion

        #region LORA Management
        /// <summary>
        /// Sets a single LORA adapter, replacing any existing ones.
        /// </summary>
        /// <param name="path">Path to LORA file (.gguf format)</param>
        /// <param name="weight">Adapter weight (default: 1.0)</param>
        public void SetLora(string path, float weight = 1f)
        {
            AssertNotStarted();
            loraManager.Clear();
            AddLora(path, weight);
        }

        /// <summary>
        /// Adds a LORA adapter to the existing set.
        /// </summary>
        /// <param name="path">Path to LORA file (.gguf format)</param>
        /// <param name="weight">Adapter weight (default: 1.0)</param>
        public void AddLora(string path, float weight = 1f)
        {
            AssertNotStarted();
            loraManager.Add(path, weight);
            UpdateLoras();
        }

        /// <summary>
        /// Removes a specific LORA adapter.
        /// </summary>
        /// <param name="path">Path to LORA file to remove</param>
        public void RemoveLora(string path)
        {
            AssertNotStarted();
            loraManager.Remove(path);
            UpdateLoras();
        }

        /// <summary>
        /// Removes all LORA adapters.
        /// </summary>
        public void RemoveLoras()
        {
            AssertNotStarted();
            loraManager.Clear();
            UpdateLoras();
        }

        /// <summary>
        /// Changes the weight of a specific LORA adapter.
        /// </summary>
        /// <param name="path">Path to LORA file</param>
        /// <param name="weight">New weight value</param>
        public void SetLoraWeight(string path, float weight)
        {
            loraManager.SetWeight(path, weight);
            UpdateLoras();
            if (started) ApplyLoras();
        }

        /// <summary>
        /// Changes the weights of multiple LORA adapters.
        /// </summary>
        /// <param name="loraToWeight">Dictionary mapping LORA paths to weights</param>
        public void SetLoraWeights(Dictionary<string, float> loraToWeight)
        {
            if (loraToWeight == null)
            {
                throw new ArgumentNullException(nameof(loraToWeight));
            }

            foreach (var entry in loraToWeight)
            {
                loraManager.SetWeight(entry.Key, entry.Value);
            }
            UpdateLoras();
            if (started) ApplyLoras();
        }

        private void UpdateLoras()
        {
            (_lora, _loraWeights) = loraManager.ToStrings();
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        private void UpdateLoraManagerFromStrings()
        {
            loraManager.FromStrings(_lora, _loraWeights);
        }

        private void ApplyLoras()
        {
            if (!started) return;
            var loras = new List<LoraIdScale>();
            float[] weights = loraManager.GetWeights();

            for (int i = 0; i < weights.Length; i++)
            {
                loras.Add(new LoraIdScale(i, weights[i]));
            }

            if (loras.Count > 0)
            {
                llmService.LoraWeight(loras);
            }
        }

        #endregion

        #region SSL Configuration
        /// <summary>
        /// Sets the SSL certificate for secure server connections.
        /// </summary>
        /// <param name="path">Path to SSL certificate file</param>
        public void SetSSLCertFromFile(string path)
        {
            SSLCert = ReadFileContents(path);
        }

        /// <summary>
        /// Sets the SSL private key for secure server connections.
        /// </summary>
        /// <param name="path">Path to SSL private key file</param>
        public void SetSSLKeyFromFile(string path)
        {
            SSLKey = ReadFileContents(path);
        }

        private string ReadFileContents(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            if (!File.Exists(path))
            {
                LLMUnitySetup.LogError($"File not found: {path}");
                return "";
            }

            return File.ReadAllText(path);
        }

        #endregion

        #region Helper Methods
        private int GetNumClients()
        {
            return Math.Max(parallelPrompts == -1 ? clients.Count : parallelPrompts, 1);
        }

        private void AssertStarted()
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

        private void AssertNotStarted()
        {
            if (started)
            {
                string error = "This method can't be called when the LLM has started";
                LLMUnitySetup.LogError(error);
                throw new Exception(error);
            }
        }

        /// <summary>
        /// Stops and cleans up the LLM service.
        /// </summary>
        public void Destroy()
        {
            lock (staticLock)
            {
                try
                {
                    llmService?.Dispose();
                    llmlib = null;
                    started = false;
                    failed = false;
                }
                catch (Exception ex)
                {
                    LLMUnitySetup.LogError($"Error during LLM cleanup: {ex.Message}");
                }
            }
        }

        #endregion

        #region Static Asset Management
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
            if (string.IsNullOrEmpty(path)) return path;

            // Check LLMManager first
            ModelEntry modelEntry = LLMManager.Get(path);
            if (modelEntry != null) return modelEntry.filename;

            // Check StreamingAssets
            string assetPath = LLMUnitySetup.GetAssetPath(path);
            string basePath = LLMUnitySetup.GetAssetPath();

            if (File.Exists(assetPath) && LLMUnitySetup.IsSubPath(assetPath, basePath))
            {
                return LLMUnitySetup.RelativePath(assetPath, basePath);
            }

            // Warn about local files not in build
            if (File.Exists(assetPath))
            {
                string errorMessage = $"The model {path} was loaded locally. You can include it in the build in one of these ways:";
                errorMessage += $"\n-Copy the model inside the StreamingAssets folder and use its StreamingAssets path";
                errorMessage += $"\n-Load the model with the model manager inside the LLM GameObject and use its filename";
                LLMUnitySetup.LogWarning(errorMessage);
            }
            else
            {
                LLMUnitySetup.LogError($"Model file not found: {path}");
            }

            return path;
        }

        public static string GetLLMManagerAssetRuntime(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Try LLMManager path
            string managerPath = LLMManager.GetAssetPath(path);
            if (!string.IsNullOrEmpty(managerPath) && File.Exists(managerPath))
            {
                return managerPath;
            }

            // Try StreamingAssets
            string assetPath = LLMUnitySetup.GetAssetPath(path);
            if (File.Exists(assetPath)) return assetPath;

            // Try download path
            string downloadPath = LLMUnitySetup.GetDownloadAssetPath(path);
            if (File.Exists(downloadPath)) return downloadPath;

            return path;
        }

        /// \endcond
        #endregion
    }

    /// <summary>
    /// Unity-specific implementation of LlamaLib for handling native library loading.
    /// </summary>
    public class LlamaLibUnity : UndreamAI.LlamaLib.LlamaLib
    {
        public LlamaLibUnity(bool gpu = false) : base(gpu) {}

        public override string FindLibrary(string libraryName)
        {
            string lookupDir = Path.Combine(LLMUnitySetup.libraryPath, GetPlatform(), "native");
            string libraryPath = Path.Combine(lookupDir, libraryName);

            if (File.Exists(libraryPath))
            {
                return libraryPath;
            }

            throw new FileNotFoundException($"Native library not found: {libraryName} in {lookupDir}");
        }
    }
}
