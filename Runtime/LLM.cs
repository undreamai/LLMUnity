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
        [Tooltip("Size of the prompt context in tokens (0 = use model's default context size)")]
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

        /// <summary>Enable LLM reasoning ('thinking' mode)</summary>
        [Tooltip("Enable LLM reasoning ('thinking' mode)")]
        [ModelAdvanced, SerializeField] private bool _reasoning = false;

        /// <summary>LORA adapter model paths (.gguf format), separated by commas</summary>
        [Tooltip("LORA adapter model paths (.gguf format), separated by commas")]
        [ModelAdvanced, SerializeField] private string _lora = "";

        /// <summary>Weights for LORA adapters, separated by commas (default: 1.0 for each)</summary>
        [Tooltip("Weights for LORA adapters, separated by commas (default: 1.0 for each)")]
        [ModelAdvanced, SerializeField] private string _loraWeights = "";

        /// <summary>Persist this LLM GameObject across scene transitions</summary>
        [Tooltip("Persist this LLM GameObject across scene transitions")]
        [LLM] public bool dontDestroyOnLoad = true;

        /// <summary>True if this model only supports embeddings (no text generation)</summary>
        [SerializeField]
        [Tooltip("True if this model only supports embeddings (no text generation)")]
        private bool _embeddingsOnly = false;

        /// <summary>Number of dimensions in embedding vectors (0 if not an embedding model)</summary>
        [SerializeField]
        [Tooltip("Number of dimensions in embedding vectors (0 if not an embedding model)")]
        private int _embeddingLength = 0;
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
                    LLMUnitySetup.LogError("numThreads must be >= -1", true);
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
                    LLMUnitySetup.LogError("numGPULayers must be >= 0", true);
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
                    LLMUnitySetup.LogError("parallelPrompts must be >= -1", true);
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
                    LLMUnitySetup.LogError("contextSize must be >= 0", true);
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
                    LLMUnitySetup.LogError("batchSize must be > 0", true);
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

        /// <summary>Enable LLM reasoning ('thinking' mode)</summary>
        public bool reasoning
        {
            get => _reasoning;
            set => SetReasoning(value);
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
                    LLMUnitySetup.LogError("port must be between 0 and 65535", true);
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

        /// <summary>Model architecture name (e.g., llama, mistral)</summary>
        [Tooltip("Model architecture name (e.g., llama, mistral)")]
        public string architecture => llmlib?.architecture;

        /// <summary>True if this model only supports embeddings (no text generation)</summary>
        [Tooltip("True if this model only supports embeddings (no text generation)")]
        public bool embeddingsOnly => _embeddingsOnly;

        /// <summary>Number of dimensions in embedding vectors (0 if not an embedding model)</summary>
        [Tooltip("Number of dimensions in embedding vectors (0 if not an embedding model)")]
        public int embeddingLength => _embeddingLength;
        #endregion

        #region Private Fields
        /// \cond HIDE
        public int minContextLength = 0;
        public int maxContextLength = 0;

        private LlamaLib llmlib = null;
        // [Local, SerializeField]
        protected LLMService _llmService;
        private readonly List<LLMClient> clients = new List<LLMClient>();
        public LLMManager llmManager = new LLMManager();
        private static readonly object staticLock = new object();
        public LoraManager loraManager = new LoraManager();
        string loraPre = "";
        string loraWeightsPre = "";
        /// \endcond
        #endregion

        #region Unity Lifecycle
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
                LLMUnitySetup.LogError("Both SSL certificate and key must be provided together!", true);
            }
        }

        private string GetValidatedModelPath()
        {
            if (string.IsNullOrEmpty(model))
            {
                LLMUnitySetup.LogError("No model file provided!", true);
            }

            string modelPath = GetLLMManagerAssetRuntime(model);
            if (!File.Exists(modelPath))
            {
                LLMUnitySetup.LogError($"Model file not found: {modelPath}", true);
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
                    LLMUnitySetup.LogError($"LORA file not found: {resolvedPath}", true);
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
            catch (LLMUnityException ex)
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
            if (LLMUnitySetup.DebugMode <= LLMUnitySetup.DebugModeType.All)
            {
                LlamaLib.Debug(LLMUnitySetup.DebugModeType.All - LLMUnitySetup.DebugMode + 1);
#if ENABLE_IL2CPP
                IL2CPP_Logging.LoggingCallback(LLMUnitySetup.Log);
#else
                LlamaLib.LoggingCallback(LLMUnitySetup.Log);
#endif
            }
            bool useGPU = numGPULayers > 0;
            llmlib = new LlamaLib(useGPU);
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

            string processorType = SystemInfo.processorType;
            await Task.Run(() =>
            {
                lock (staticLock)
                {
                    IntPtr llmPtr = LLMService.CreateLLM(
                        llmlib, modelPath, numSlots, effectiveThreads, numGPULayers,
                        flashAttention, contextSize, batchSize, embeddingsOnly, loraPaths.ToArray());

                    llmService = new LLMService(llmlib, llmPtr);

                    string serverString = "llamalib_**architecture**_server";
                    if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
                        serverString = "llamalib_win-x64_server.exe";
                    else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
                        serverString = processorType.Contains("Intel") ? "llamalib_osx-x64_server" : "llamalib_osx-arm64_server";
                    else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
                        serverString = "llamalib_linux-x64_server";
                    LLMUnitySetup.Log($"Deploy server command: {serverString} {llmService.Command}");
                    SetupServer();
                    llmService.Start();

                    started = llmService.Started();
                    if (started)
                    {
                        ApplyLoras();
                        SetReasoning(reasoning);
                    }
                }
            });
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
                LLMUnitySetup.LogError("LLM failed to start", true);
            }
        }

        /// <summary>
        /// Waits asynchronously until model setup is complete.
        /// </summary>
        /// <param name="downloadProgressCallback">Optional callback for download progress updates</param>
        /// <returns>True if setup succeeded, false if it failed</returns>
        public static async Task<bool> WaitUntilModelSetup(Action<float> downloadProgressCallback = null)
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
        /// Enable LLM reasoning ("thinking" mode)
        /// </summary>
        /// <param name="reasoning"Use LLM reasoning</param>
        public void SetReasoning(bool reasoning)
        {
            llmService.EnableReasoning(reasoning);
        }

        /// <summary>
        /// Configure the LLM for embedding generation.
        /// </summary>
        /// <param name="embeddingLength">Number of embedding dimensions</param>
        /// <param name="embeddingsOnly">True if model only supports embeddings</param>
        public void SetEmbeddings(int embeddingLength, bool embeddingsOnly)
        {
            _embeddingsOnly = embeddingsOnly;
            _embeddingLength = embeddingLength;

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
                LLMUnitySetup.LogError("llmClient is null", true);
            }

            clients.Add(llmClient);
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
                LLMUnitySetup.LogError("loraToWeight is null", true);
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
            if (error != null) LLMUnitySetup.LogError(error, true);
        }

        private void AssertNotStarted()
        {
            if (started) LLMUnitySetup.LogError("This method can't be called when the LLM has started", true);
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
}
