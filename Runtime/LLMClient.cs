/// @file
/// @brief File implementing the base LLM client functionality for Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace LLMUnity
{
    /// @ingroup llm
    /// <summary>
    /// Unity MonoBehaviour base class for LLM client functionality.
    /// Handles both local and remote LLM connections, completion parameters,
    /// and provides tokenization, completion, and embedding capabilities.
    /// </summary>
    public class LLMClient : MonoBehaviour
    {
        #region Inspector Fields
        /// <summary>Show/hide advanced options in the inspector</summary>
        [Tooltip("Show/hide advanced options in the inspector")]
        [HideInInspector] public bool advancedOptions = false;

        /// <summary>Use remote LLM server instead of local instance</summary>
        [Tooltip("Use remote LLM server instead of local instance")]
        [LocalRemote, SerializeField] protected bool _remote;

        /// <summary>Local LLM GameObject to connect to</summary>
        [Tooltip("Local LLM GameObject to connect to")]
        [Local, SerializeField] protected LLM _llm;

        /// <summary>API key for remote server authentication</summary>
        [Tooltip("API key for remote server authentication")]
        [Remote, SerializeField] protected string _APIKey;

        /// <summary>Hostname or IP address of remote LLM server</summary>
        [Tooltip("Hostname or IP address of remote LLM server")]
        [Remote, SerializeField] protected string _host = "localhost";

        /// <summary>Port number of remote LLM server</summary>
        [Tooltip("Port number of remote LLM server")]
        [Remote, SerializeField] protected int _port = 13333;

        /// <summary>Number of retries of remote LLM server</summary>
        [Tooltip("Number of retries of remote LLM server")]
        [Remote, SerializeField] protected int numRetries = 5;

        /// <summary>Grammar constraints for output formatting (GBNF or JSON schema format)</summary>
        [Tooltip("Grammar constraints for output formatting (GBNF or JSON schema format)")]
        [ModelAdvanced, TextArea(1, 10), SerializeField] protected string _grammar = "";

        // Completion Parameters
        /// <summary>Maximum tokens to generate (-1 = unlimited)</summary>
        [Tooltip("Maximum tokens to generate (-1 = unlimited)")]
        [Model] public int numPredict = -1;

        /// <summary>Cache processed prompts to speed up subsequent requests</summary>
        [Tooltip("Cache processed prompts to speed up subsequent requests")]
        [ModelAdvanced] public bool cachePrompt = true;

        /// <summary>Random seed for reproducible generation (0 = random)</summary>
        [Tooltip("Random seed for reproducible generation (0 = random)")]
        [ModelAdvanced] public int seed = 0;

        /// <summary>Sampling temperature (0.0 = deterministic, higher = more creative)</summary>
        [Tooltip("Sampling temperature (0.0 = deterministic, higher = more creative)")]
        [ModelAdvanced, Range(0f, 2f)] public float temperature = 0.2f;

        /// <summary>Top-k sampling: limit to k most likely tokens (0 = disabled)</summary>
        [Tooltip("Top-k sampling: limit to k most likely tokens (0 = disabled)")]
        [ModelAdvanced, Range(0, 100)] public int topK = 40;

        /// <summary>Top-p (nucleus) sampling: cumulative probability threshold (1.0 = disabled)</summary>
        [Tooltip("Top-p (nucleus) sampling: cumulative probability threshold (1.0 = disabled)")]
        [ModelAdvanced, Range(0f, 1f)] public float topP = 0.9f;

        /// <summary>Minimum probability threshold for token selection</summary>
        [Tooltip("Minimum probability threshold for token selection")]
        [ModelAdvanced, Range(0f, 1f)] public float minP = 0.05f;

        /// <summary>Penalty for repeated tokens (1.0 = no penalty)</summary>
        [Tooltip("Penalty for repeated tokens (1.0 = no penalty)")]
        [ModelAdvanced, Range(0f, 2f)] public float repeatPenalty = 1.1f;

        /// <summary>Presence penalty: reduce likelihood of any repeated token (0.0 = disabled)</summary>
        [Tooltip("Presence penalty: reduce likelihood of any repeated token (0.0 = disabled)")]
        [ModelAdvanced, Range(0f, 1f)] public float presencePenalty = 0f;

        /// <summary>Frequency penalty: reduce likelihood based on token frequency (0.0 = disabled)</summary>
        [Tooltip("Frequency penalty: reduce likelihood based on token frequency (0.0 = disabled)")]
        [ModelAdvanced, Range(0f, 1f)] public float frequencyPenalty = 0f;

        /// <summary>Locally typical sampling strength (1.0 = disabled)</summary>
        [Tooltip("Locally typical sampling strength (1.0 = disabled)")]
        [ModelAdvanced, Range(0f, 1f)] public float typicalP = 1f;

        /// <summary>Number of recent tokens to consider for repetition penalty (0 = disabled, -1 = context size)</summary>
        [Tooltip("Number of recent tokens to consider for repetition penalty (0 = disabled, -1 = context size)")]
        [ModelAdvanced, Range(0, 2048)] public int repeatLastN = 64;

        /// <summary>Mirostat sampling mode (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0)</summary>
        [Tooltip("Mirostat sampling mode (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0)")]
        [ModelAdvanced, Range(0, 2)] public int mirostat = 0;

        /// <summary>Mirostat target entropy (tau) - balance between coherence and diversity</summary>
        [Tooltip("Mirostat target entropy (tau) - balance between coherence and diversity")]
        [ModelAdvanced, Range(0f, 10f)] public float mirostatTau = 5f;

        /// <summary>Mirostat learning rate (eta) - adaptation speed</summary>
        [Tooltip("Mirostat learning rate (eta) - adaptation speed")]
        [ModelAdvanced, Range(0f, 1f)] public float mirostatEta = 0.1f;

        /// <summary>Include top N token probabilities in response (0 = disabled)</summary>
        [Tooltip("Include top N token probabilities in response (0 = disabled)")]
        [ModelAdvanced, Range(0, 10)] public int nProbs = 0;

        /// <summary>Ignore end-of-stream token and continue generating</summary>
        [Tooltip("Ignore end-of-stream token and continue generating")]
        [ModelAdvanced] public bool ignoreEos = false;
        #endregion

        #region Public Properties
        /// <summary>Whether this client uses a remote server connection</summary>
        public bool remote
        {
            get => _remote;
            set
            {
                if (_remote != value)
                {
                    _remote = value;
                    if (started) _ = SetupCaller();
                }
            }
        }

        /// <summary>The local LLM instance (null if using remote)</summary>
        public LLM llm
        {
            get => _llm;
            set => _ = SetLLM(value);
        }

        /// <summary>API key for remote server authentication</summary>
        public string APIKey
        {
            get => _APIKey;
            set
            {
                if (_APIKey != value)
                {
                    _APIKey = value;
                    if (started) _ = SetupCaller();
                }
            }
        }

        /// <summary>Remote server hostname or IP address</summary>
        public string host
        {
            get => _host;
            set
            {
                if (_host != value)
                {
                    _host = value;
                    if (started) _ = SetupCaller();
                }
            }
        }

        /// <summary>Remote server port number</summary>
        public int port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    if (started) _ = SetupCaller();
                }
            }
        }

        /// <summary>Current grammar constraints for output formatting</summary>
        public string grammar
        {
            get => _grammar;
            set => SetGrammar(value);
        }

        #endregion

        #region Private Fields
        protected UndreamAI.LlamaLib.LLMClient llmClient;
        private bool started = false;
        private string completionParametersCache = "";
        private readonly SemaphoreSlim startSemaphore = new SemaphoreSlim(1, 1);
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Unity Awake method that validates configuration and assigns local LLM if needed.
        /// </summary>
        public virtual void Awake()
        {
            if (!enabled) return;

            if (!remote)
            {
                AssignLLM();
                if (llm == null) LLMUnitySetup.LogError($"No LLM assigned or detected for {GetType().Name} '{name}'!", true);
            }
        }

        /// <summary>
        /// Unity Start method that initializes the LLM client connection.
        /// </summary>
        public virtual async void Start()
        {
            if (!enabled) return;
            await SetupCaller();
            started = true;
        }

        protected virtual void OnValidate()
        {
            AssignLLM();
        }

        protected virtual void Reset()
        {
            AssignLLM();
        }

        #endregion

        #region Initialization
        protected virtual async Task CheckCaller(bool checkConnection = true)
        {
            await startSemaphore.WaitAsync();
            startSemaphore.Release();
            if (GetCaller() == null) LLMUnitySetup.LogError("LLM caller not initialized", true);
            if (remote && checkConnection)
            {
                for (int attempt = 0; attempt <= numRetries; attempt++)
                {
                    if (llmClient.IsServerAlive()) break;
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// Sets up the underlying LLM client connection (local or remote).
        /// </summary>
        protected virtual async Task SetupCaller()
        {
            await SetupCallerObject();
            await PostSetupCallerObject();
        }

        /// <summary>
        /// Sets up the underlying LLM client connection (local or remote).
        /// </summary>
        protected virtual async Task SetupCallerObject()
        {
            await startSemaphore.WaitAsync();

            string exceptionMessage = "";
            try
            {
                if (!remote)
                {
                    if (llm != null) await llm.WaitUntilReady();
                    if (llm?.llmService == null) LLMUnitySetup.LogError("Local LLM service is not available", true);
                    llmClient = new UndreamAI.LlamaLib.LLMClient(llm.llmService);
                }
                else
                {
                    llmClient = new UndreamAI.LlamaLib.LLMClient(host, port, APIKey, numRetries);
                }
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError(ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                startSemaphore.Release();
            }

            if (llmClient == null || exceptionMessage != "")
            {
                string error = "llmClient not initialized";
                if (exceptionMessage != "") error += ", error: " + exceptionMessage;
                LLMUnitySetup.LogError(error, true);
            }
        }

        /// <summary>
        /// Initialisation after setting up the LLM client (local or remote).
        /// </summary>
        protected virtual async Task PostSetupCallerObject()
        {
            SetGrammar(grammar);
            completionParametersCache = "";
            await Task.Yield();
        }

        /// <summary>
        /// Gets the underlying LLMLocal instance for operations requiring local access.
        /// </summary>
        protected virtual LLMLocal GetCaller()
        {
            return llmClient;
        }

        /// <summary>
        /// Sets the local LLM instance for this client.
        /// </summary>
        /// <param name="llmInstance">LLM instance to connect to</param>
        protected virtual async Task SetLLM(LLM llmInstance)
        {
            if (llmInstance == _llm) return;

            if (remote)
            {
                LLMUnitySetup.LogError("Cannot set LLM when client is in remote mode");
                return;
            }

            _llm = llmInstance;
            if (started) await SetupCaller();
        }

        #endregion

        #region LLM Assignment
        /// <summary>
        /// Determines if an LLM instance can be auto-assigned to this client.
        /// Override in derived classes to implement specific assignment logic.
        /// </summary>
        /// <param name="llmInstance">LLM instance to evaluate</param>
        /// <returns>True if the LLM can be auto-assigned</returns>
        public virtual bool IsAutoAssignableLLM(LLM llmInstance)
        {
            return true;
        }

        /// <summary>
        /// Automatically assigns a suitable LLM instance if none is set.
        /// </summary>
        protected virtual void AssignLLM()
        {
            if (remote || llm != null) return;

            var validLLMs = new List<LLM>();

#if UNITY_6000_0_OR_NEWER
            foreach (LLM foundLlm in FindObjectsByType<LLM>(FindObjectsSortMode.None))
#else
            foreach (LLM foundLlm in FindObjectsOfType<LLM>())
#endif
            {
                if (IsAutoAssignableLLM(foundLlm))
                {
                    validLLMs.Add(foundLlm);
                }
            }

            if (validLLMs.Count == 0) return;

            llm = SortLLMsByBestMatch(validLLMs.ToArray())[0];

            string message = $"Auto-assigned LLM '{llm.name}' to {GetType().Name} '{name}'";
            if (llm.gameObject.scene != gameObject.scene)
            {
                message += $" (from scene '{llm.gameObject.scene.name}')";
            }
            LLMUnitySetup.Log(message);
        }

        /// <summary>
        /// Sorts LLM instances by compatibility, preferring same-scene objects and hierarchy order.
        /// </summary>
        protected virtual LLM[] SortLLMsByBestMatch(LLM[] llmArray)
        {
            LLM[] array = (LLM[])llmArray.Clone();
            for (int i = 0; i < array.Length - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < array.Length - i - 1; j++)
                {
                    bool sameScene = array[j].gameObject.scene == array[j + 1].gameObject.scene;
                    bool swap = (
                        (!sameScene && array[j + 1].gameObject.scene == gameObject.scene) ||
                        (sameScene && array[j].transform.GetSiblingIndex() > array[j + 1].transform.GetSiblingIndex())
                    );
                    if (swap)
                    {
                        LLM temp = array[j];
                        array[j] = array[j + 1];
                        array[j + 1] = temp;
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
            return array;
        }

        #endregion

        #region Grammar Management
        /// <summary>
        /// Sets grammar constraints for structured output generation.
        /// </summary>
        /// <param name="grammarString">Grammar in GBNF or JSON schema format</param>
        public virtual void SetGrammar(string grammarString)
        {
            _grammar = grammarString ?? "";
            GetCaller()?.SetGrammar(_grammar);
        }

        /// <summary>
        /// Loads grammar constraints from a file.
        /// </summary>
        /// <param name="path">Path to grammar file</param>
        public virtual void LoadGrammar(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            if (!File.Exists(path))
            {
                LLMUnitySetup.LogError($"Grammar file not found: {path}");
                return;
            }

            try
            {
                string grammarContent = File.ReadAllText(path);
                SetGrammar(grammarContent);
                LLMUnitySetup.Log($"Loaded grammar from: {path}");
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to load grammar file '{path}': {ex.Message}");
            }
        }

        #endregion

        #region Completion Parameters
        /// <summary>
        /// Applies current completion parameters to the LLM client.
        /// Only updates if parameters have changed since last call.
        /// </summary>
        protected virtual void SetCompletionParameters()
        {
            if (llm != null && llm.embeddingsOnly)
            {
                string error = "LLM can't be used for completion, it is an embeddings only model!";
                LLMUnitySetup.LogError(error, true);
            }

            var parameters = new JObject
            {
                ["temperature"] = temperature,
                ["top_k"] = topK,
                ["top_p"] = topP,
                ["min_p"] = minP,
                ["n_predict"] = numPredict,
                ["typical_p"] = typicalP,
                ["repeat_penalty"] = repeatPenalty,
                ["repeat_last_n"] = repeatLastN,
                ["presence_penalty"] = presencePenalty,
                ["frequency_penalty"] = frequencyPenalty,
                ["mirostat"] = mirostat,
                ["mirostat_tau"] = mirostatTau,
                ["mirostat_eta"] = mirostatEta,
                ["seed"] = seed,
                ["ignore_eos"] = ignoreEos,
                ["n_probs"] = nProbs,
                ["cache_prompt"] = cachePrompt
            };

            string parametersJson = parameters.ToString();
            if (parametersJson != completionParametersCache)
            {
                GetCaller()?.SetCompletionParameters(parameters);
                completionParametersCache = parametersJson;
            }
        }

        #endregion

        #region Core LLM Operations
        /// <summary>
        /// Converts text into a list of token IDs.
        /// </summary>
        /// <param name="query">Text to tokenize</param>
        /// <param name="callback">Optional callback to receive the result</param>
        /// <returns>List of token IDs</returns>
        public virtual async Task<List<int>> Tokenize(string query, Action<List<int>> callback = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                LLMUnitySetup.LogError("query is null", true);
            }
            await CheckCaller();

            List<int> tokens = llmClient.Tokenize(query);
            callback?.Invoke(tokens);
            return tokens;
        }

        /// <summary>
        /// Converts token IDs back to text.
        /// </summary>
        /// <param name="tokens">Token IDs to decode</param>
        /// <param name="callback">Optional callback to receive the result</param>
        /// <returns>Decoded text</returns>
        public virtual async Task<string> Detokenize(List<int> tokens, Action<string> callback = null)
        {
            if (tokens == null)
            {
                LLMUnitySetup.LogError("tokens is null", true);
            }
            await CheckCaller();

            string text = llmClient.Detokenize(tokens);
            callback?.Invoke(text);
            return text;
        }

        /// <summary>
        /// Generates embedding vectors for the input text.
        /// </summary>
        /// <param name="query">Text to embed</param>
        /// <param name="callback">Optional callback to receive the result</param>
        /// <returns>Embedding vector</returns>
        public virtual async Task<List<float>> Embeddings(string query, Action<List<float>> callback = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                LLMUnitySetup.LogError("query is null", true);
            }
            await CheckCaller();

            List<float> embeddings;
            if (!llm.embeddingsOnly)
            {
                LLMUnitySetup.LogError("You need to use an embedding model for embeddings (see \"RAG models\" in \"Download model\")");
                embeddings = new List<float>();
            }
            else
            {
                embeddings = llmClient.Embeddings(query);
            }
            if (embeddings.Count == 0) LLMUnitySetup.LogError("embeddings are empty!");
            callback?.Invoke(embeddings);
            return embeddings;
        }

        /// <summary>
        /// Generates text completion.
        /// </summary>
        /// <param name="prompt">Input prompt text</param>
        /// <param name="callback">Optional streaming callback for partial responses</param>
        /// <param name="completionCallback">Optional callback when completion finishes</param>
        /// <param name="id_slot">Slot ID for the request (-1 for auto-assignment)</param>
        /// <returns>Task that returns the generated completion text</returns>
        public virtual async Task<string> Completion(string prompt, Action<string> callback = null,
            Action completionCallback = null, int id_slot = -1)
        {
            await CheckCaller();

            LlamaLib.CharArrayCallback wrappedCallback = null;
            if (callback != null)
            {
#if ENABLE_IL2CPP
                Action<string> mainThreadCallback = Utils.WrapActionForMainThread(callback, this);
                wrappedCallback = IL2CPP_Completion.CreateCallback(mainThreadCallback);
#else
                wrappedCallback = Utils.WrapCallbackForAsync(callback, this);
#endif
            }

            SetCompletionParameters();
            string result = await llmClient.CompletionAsync(prompt, wrappedCallback, id_slot);
            completionCallback?.Invoke();
            return result;
        }

        /// <summary>
        /// Cancels an active request in the specified slot.
        /// </summary>
        /// <param name="id_slot">Slot ID of the request to cancel</param>
        public void CancelRequest(int id_slot)
        {
            llmClient?.Cancel(id_slot);
        }

        #endregion
    }
}
