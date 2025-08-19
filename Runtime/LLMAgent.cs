/// @file
/// @brief File implementing the LLM chat agent functionality for Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    /// @ingroup llm
    /// <summary>
    /// Unity MonoBehaviour that implements a conversational AI agent with persistent chat history.
    /// Extends LLMClient to provide chat-specific functionality including role management,
    /// conversation history persistence, and specialized chat completion methods.
    /// </summary>
    public class LLMAgent : LLMClient
    {
        #region Inspector Fields
        /// <summary>Filename for saving chat history (saved in persistentDataPath)</summary>
        [Tooltip("Filename for saving chat history (saved in Application.persistentDataPath)")]
        [LLM] public string save = "";

        /// <summary>Save LLM processing cache for faster reload (~100MB per agent)</summary>
        [Tooltip("Save LLM processing cache for faster reload (~100MB per agent)")]
        [LLM] public bool saveCache = false;

        /// <summary>Server slot to use for processing (affects caching behavior)</summary>
        [Tooltip("Server slot to use for processing (affects caching behavior)")]
        [ModelAdvanced, SerializeField] protected int _slot = -1;

        /// <summary>Role name for user messages in conversation</summary>
        [Tooltip("Role name for user messages in conversation")]
        [Chat, SerializeField] protected string _userRole = "user";

        /// <summary>Role name for AI assistant messages in conversation</summary>
        [Tooltip("Role name for AI assistant messages in conversation")]
        [Chat, SerializeField] protected string _assistantRole = "assistant";

        /// <summary>System prompt that defines the AI's personality and behavior</summary>
        [Tooltip("System prompt that defines the AI's personality and behavior")]
        [TextArea(5, 10), Chat, SerializeField]
        protected string _systemPrompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
        #endregion

        #region Public Properties
        /// <summary>Server slot ID for this agent's requests</summary>
        public int slot
        {
            get => _slot;
            set
            {
                if (_slot != value)
                {
                    _slot = value;
                    if (llmAgent != null) llmAgent.SlotId = _slot;
                }
            }
        }

        /// <summary>Role identifier for user messages</summary>
        public string userRole
        {
            get => _userRole;
            set
            {
                if (_userRole != value)
                {
                    _userRole = value;
                    if (llmAgent != null) llmAgent.UserRole = _userRole;
                }
            }
        }

        /// <summary>Role identifier for assistant messages</summary>
        public string assistantRole
        {
            get => _assistantRole;
            set
            {
                if (_assistantRole != value)
                {
                    _assistantRole = value;
                    if (llmAgent != null) llmAgent.AssistantRole = _assistantRole;
                }
            }
        }

        /// <summary>System prompt defining the agent's behavior and personality</summary>
        public string systemPrompt
        {
            get => _systemPrompt;
            set
            {
                if (_systemPrompt != value)
                {
                    _systemPrompt = value;
                    if (llmAgent != null) llmAgent.SystemPrompt = _systemPrompt;
                }
            }
        }

        /// <summary>The underlying LLMAgent instance from LlamaLib</summary>
        public UndreamAI.LlamaLib.LLMAgent llmAgent { get; protected set; }

        /// <summary>Current conversation history as a list of chat messages</summary>
        public List<ChatMessage> chat
        {
            get => llmAgent?.GetHistory() ?? new List<ChatMessage>();
            set
            {
                CheckLLMAgent();
                llmAgent.SetHistory(value ?? new List<ChatMessage>());
            }
        }
        #endregion

        #region Unity Lifecycle and Initialization
        private void CheckLLMAgent()
        {
            if (llmAgent == null)
            {
                string error = "LLMAgent not initialized";
                LLMUnitySetup.LogError(error);
                throw new System.InvalidOperationException(error);
            }
        }

        protected virtual void SetupLLMAgent()
        {
            string exceptionMessage = "";
            try
            {
                llmAgent = new UndreamAI.LlamaLib.LLMAgent(llmClient, systemPrompt, userRole, assistantRole);
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }
            if (llmAgent == null || exceptionMessage != "")
            {
                string error = "LLMAgent not initialized";
                if (exceptionMessage != "") error += ", error: " + exceptionMessage;
                LLMUnitySetup.LogError(error);
                throw new InvalidOperationException(error);
            }

            if (slot != -1) llmAgent.SlotId = slot;
        }

        /// <summary>
        /// Unity Start method that initializes the agent with chat functionality.
        /// Sets up the underlying LLMAgent with role configuration and loads saved history.
        /// </summary>
        public override void Start()
        {
            base.Start();
            SetupLLMAgent();
            InitHistory();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Validate slot configuration
            if (llm != null && llm.parallelPrompts > -1 && (slot < -1 || slot >= llm.parallelPrompts))
            {
                LLMUnitySetup.LogError($"Slot must be between 0 and {llm.parallelPrompts - 1}, or -1 for auto-assignment");
            }
        }

        protected override void SetLLMClient(UndreamAI.LlamaLib.LLMClient llmClientSet)
        {
            base.SetLLMClient(llmClientSet);
            SetupLLMAgent();
        }

        protected override LLMLocal GetCaller()
        {
            return llmAgent;
        }

        /// <summary>
        /// Initializes conversation history by clearing current state and loading from file if available.
        /// </summary>
        protected virtual void InitHistory()
        {
            ClearChat();
            LoadHistory();
        }

        /// <summary>
        /// Loads conversation history from the saved file if it exists.
        /// </summary>
        protected virtual void LoadHistory()
        {
            if (string.IsNullOrEmpty(save) || !File.Exists(GetJsonSavePath(save)))
            {
                return;
            }

            try
            {
                Load(save);
            }
            catch (System.Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to load chat history from '{save}': {ex.Message}");
            }
        }

        #endregion

        #region File Path Management
        /// <summary>
        /// Gets the full path for a file in the persistent data directory.
        /// </summary>
        /// <param name="filename">Filename or relative path</param>
        /// <returns>Full file path in persistent data directory</returns>
        protected virtual string GetSavePath(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new System.ArgumentNullException(nameof(filename));
            }

            return Path.Combine(Application.persistentDataPath, filename).Replace('\\', '/');
        }

        /// <summary>
        /// Gets the save path for chat history JSON file.
        /// </summary>
        /// <param name="filename">Base filename (without extension)</param>
        /// <returns>Full path to JSON file</returns>
        public virtual string GetJsonSavePath(string filename)
        {
            return GetSavePath(filename + ".json");
        }

        /// <summary>
        /// Gets the save path for LLM cache file.
        /// </summary>
        /// <param name="filename">Base filename (without extension)</param>
        /// <returns>Full path to cache file</returns>
        public virtual string GetCacheSavePath(string filename)
        {
            return GetSavePath(filename + ".cache");
        }

        #endregion

        #region Chat Management
        /// <summary>
        /// Clears the entire conversation history.
        /// </summary>
        public virtual void ClearChat()
        {
            CheckLLMAgent();
            llmAgent.ClearHistory();
        }

        /// <summary>
        /// Adds a message with a specific role to the conversation history.
        /// </summary>
        /// <param name="role">Message role (e.g., userRole, assistantRole, or custom role)</param>
        /// <param name="content">Message content</param>
        public virtual void AddMessage(string role, string content)
        {
            CheckLLMAgent();
            llmAgent.AddMessage(role, content);
        }

        /// <summary>
        /// Adds a user message to the conversation history.
        /// </summary>
        /// <param name="content">User message content</param>
        public virtual void AddUserMessage(string content)
        {
            CheckLLMAgent();
            llmAgent.AddUserMessage(content);
        }

        /// <summary>
        /// Adds an AI assistant message to the conversation history.
        /// </summary>
        /// <param name="content">Assistant message content</param>
        public virtual void AddAssistantMessage(string content)
        {
            CheckLLMAgent();
            llmAgent.AddAssistantMessage(content);
        }

        #endregion

        #region Chat Functionality
        /// <summary>
        /// Processes a user query and generates an AI response using conversation context.
        /// The query and response are automatically added to chat history if specified.
        /// </summary>
        /// <param name="query">User's message or question</param>
        /// <param name="callback">Optional streaming callback for partial responses</param>
        /// <param name="addToHistory">Whether to add the exchange to conversation history</param>
        /// <returns>AI assistant's response</returns>
        public virtual string Chat(string query, LlamaLib.CharArrayCallback callback = null, bool addToHistory = true)
        {
            CheckLLMAgent();
            SetCompletionParameters();
            return llmAgent.Chat(query, addToHistory, callback);
        }

        /// <summary>
        /// Processes a user query asynchronously and generates an AI response using conversation context.
        /// The query and response are automatically added to chat history if specified.
        /// </summary>
        /// <param name="query">User's message or question</param>
        /// <param name="callback">Optional streaming callback for partial responses</param>
        /// <param name="completionCallback">Optional callback when response is complete</param>
        /// <param name="addToHistory">Whether to add the exchange to conversation history</param>
        /// <returns>Task that returns the AI assistant's response</returns>
        public virtual async Task<string> ChatAsync(string query, LlamaLib.CharArrayCallback callback = null,
            EmptyCallback completionCallback = null, bool addToHistory = true)
        {
            CheckLLMAgent();

            // Wrap callback to ensure it runs on the main thread
            LlamaLib.CharArrayCallback wrappedCallback = null;
            if (callback != null)
            {
                var context = SynchronizationContext.Current;
                wrappedCallback = (string msg) =>
                {
                    if (context != null)
                        context.Post(_ => callback(msg), null);
                    else
                        callback(msg);
                };
            }

            SetCompletionParameters();
            string result = await llmAgent.ChatAsync(query, addToHistory, wrappedCallback);
            completionCallback?.Invoke();
            return result;
        }

        #endregion

        #region Model Warmup
        /// <summary>
        /// Warms up the model by processing the system prompt without generating output.
        /// This caches the system prompt processing for faster subsequent responses.
        /// </summary>
        /// <param name="completionCallback">Optional callback when warmup completes</param>
        /// <returns>Task that completes when warmup finishes</returns>
        public virtual async Task Warmup(EmptyCallback completionCallback = null)
        {
            await Warmup(null, completionCallback);
        }

        /// <summary>
        /// Warms up the model with a specific prompt without adding it to history.
        /// This pre-processes prompts for faster response times in subsequent interactions.
        /// </summary>
        /// <param name="query">Warmup prompt (not added to history)</param>
        /// <param name="completionCallback">Optional callback when warmup completes</param>
        /// <returns>Task that completes when warmup finishes</returns>
        public virtual async Task Warmup(string query, EmptyCallback completionCallback = null)
        {
            int originalNumPredict = numPredict;
            try
            {
                // Set to generate no tokens for warmup
                numPredict = 0;
                await ChatAsync(query, null, completionCallback, false);
            }
            finally
            {
                // Restore original setting
                numPredict = originalNumPredict;
                SetCompletionParameters();
            }
        }

        #endregion

        #region Persistence
        /// <summary>
        /// Saves the conversation history and optionally the LLM cache to disk.
        /// </summary>
        /// <param name="filename">Base filename (without extension) for saving</param>
        /// <returns>Result message from cache save operation, or null if cache not saved</returns>
        public virtual string Save(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new System.ArgumentNullException(nameof(filename));
            }
            CheckLLMAgent();

            // Save chat history
            string jsonPath = GetJsonSavePath(filename);
            string directory = Path.GetDirectoryName(jsonPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                llmAgent.SaveHistory(jsonPath);
                LLMUnitySetup.Log($"Saved chat history to: {jsonPath}");
            }
            catch (System.Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to save chat history to '{jsonPath}': {ex.Message}");
                throw;
            }

            // Save cache if enabled and not remote
            if (!remote && saveCache)
            {
                try
                {
                    string cachePath = GetCacheSavePath(filename);
                    string result = llmAgent.SaveSlot(cachePath);
                    LLMUnitySetup.Log($"Saved LLM cache to: {cachePath}");
                    return result;
                }
                catch (System.Exception ex)
                {
                    LLMUnitySetup.LogWarning($"Failed to save LLM cache: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Loads conversation history and optionally the LLM cache from disk.
        /// </summary>
        /// <param name="filename">Base filename (without extension) to load from</param>
        /// <returns>Result message from cache load operation, or null if cache not loaded</returns>
        public virtual string Load(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new System.ArgumentNullException(nameof(filename));
            }
            CheckLLMAgent();

            // Load chat history
            string jsonPath = GetJsonSavePath(filename);
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Chat history file not found: {jsonPath}");
            }

            try
            {
                llmAgent.LoadHistory(jsonPath);
                LLMUnitySetup.Log($"Loaded chat history from: {jsonPath}");
            }
            catch (System.Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to load chat history from '{jsonPath}': {ex.Message}");
                throw;
            }

            // Load cache if enabled and not remote
            if (!remote && saveCache)
            {
                string cachePath = GetCacheSavePath(filename);
                if (File.Exists(cachePath))
                {
                    try
                    {
                        string result = llmAgent.LoadSlot(cachePath);
                        LLMUnitySetup.Log($"Loaded LLM cache from: {cachePath}");
                        return result;
                    }
                    catch (System.Exception ex)
                    {
                        LLMUnitySetup.LogWarning($"Failed to load LLM cache from '{cachePath}': {ex.Message}");
                    }
                }
            }

            return null;
        }

        #endregion

        #region Request Management
        /// <summary>
        /// Cancels any active requests for this agent.
        /// </summary>
        public void CancelRequests()
        {
            llmAgent?.Cancel();
        }

        #endregion
    }
}
