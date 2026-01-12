/// @file
/// @brief File implementing the LLM chat agent functionality for Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-1)]
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
        [Tooltip("Filename for saving chat history (saved in persistentDataPath)")]
        [LLM] public string save = "";

        /// <summary>Debug LLM prompts</summary>
        [Tooltip("Debug LLM prompts")]
        [LLM] public bool debugPrompt = false;

        /// <summary>Server slot to use for processing (affects caching behavior)</summary>
        [Tooltip("Server slot to use for processing (affects caching behavior)")]
        [ModelAdvanced, SerializeField] protected int _slot = -1;

        /// <summary>System prompt that defines the AI's personality and behavior</summary>
        [TextArea(5, 10), Chat, SerializeField]
        [Tooltip("System prompt that defines the AI's personality and behavior")]
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
            get
            {
                if (llmAgent == null) return new List<ChatMessage>();

                // convert each UndreamAI.LlamaLib.ChatMessage to LLMUnity.ChatMessage
                return llmAgent.GetHistory()
                    .Select(m => new ChatMessage(m))
                    .ToList();
            }
            set
            {
                if (llmAgent != null)
                {
                    // convert LLMUnity.ChatMessage back to UndreamAI.LlamaLib.ChatMessage
                    var history = value?.Select(m => (UndreamAI.LlamaLib.ChatMessage)m).ToList()
                        ?? new List<UndreamAI.LlamaLib.ChatMessage>();

                    llmAgent.SetHistory(history);
                }
            }
        }
        #endregion

        #region Unity Lifecycle and Initialization
        public override void Awake()
        {
            if (!remote) llm?.Register(this);
            base.Awake();
        }

        protected override async Task SetupCallerObject()
        {
            await base.SetupCallerObject();

            string exceptionMessage = "";
            try
            {
                llmAgent = new UndreamAI.LlamaLib.LLMAgent(llmClient, systemPrompt);
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }
            if (llmAgent == null || exceptionMessage != "")
            {
                string error = "LLMAgent not initialized";
                if (exceptionMessage != "") error += ", error: " + exceptionMessage;
                LLMUnitySetup.LogError(error, true);
            }
        }

        /// <summary>
        /// Initialisation after setting up the LLM client (local or remote).
        /// </summary>
        protected override async Task PostSetupCallerObject()
        {
            await base.PostSetupCallerObject();
            if (slot != -1) llmAgent.SlotId = slot;
            await InitHistory();
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

        protected override LLMLocal GetCaller()
        {
            return llmAgent;
        }

        /// <summary>
        /// Initializes conversation history by clearing current state and loading from file if available.
        /// </summary>
        protected virtual async Task InitHistory()
        {
            await ClearHistory();
            if (!string.IsNullOrEmpty(save) && File.Exists(GetSavePath()))
            {
                await LoadHistory();
            }
        }

        #endregion

        #region File Path Management
        /// <summary>
        /// Gets the full path for a file in the persistent data directory.
        /// </summary>
        /// <returns>Full file path in persistent data directory</returns>
        public virtual string GetSavePath()
        {
            if (string.IsNullOrEmpty(save))
            {
                LLMUnitySetup.LogError("No save path specified");
                return null;
            }

            return Path.Combine(Application.persistentDataPath, save).Replace('\\', '/');
        }

        #endregion

        #region Chat Management
        /// <summary>
        /// Clears the entire conversation history.
        /// </summary>
        public virtual async Task ClearHistory()
        {
            await CheckCaller(checkConnection: false);
            llmAgent.ClearHistory();
        }

        /// <summary>
        /// Adds a user message to the conversation history.
        /// </summary>
        /// <param name="content">User message content</param>
        public virtual async Task AddUserMessage(string content)
        {
            await CheckCaller();
            llmAgent.AddUserMessage(content);
        }

        /// <summary>
        /// Adds an AI assistant message to the conversation history.
        /// </summary>
        /// <param name="content">Assistant message content</param>
        public virtual async Task AddAssistantMessage(string content)
        {
            await CheckCaller();
            llmAgent.AddAssistantMessage(content);
        }

        #endregion

        #region Chat Functionality
        /// \cond HIDE
        [Serializable]
        public class CompletionResponseJson
        {
            public string prompt;
            public string content;
        }
        /// \endcond
        /// <summary>
        /// Processes a user query asynchronously and generates an AI response using conversation context.
        /// The query and response are automatically added to chat history if specified.
        /// </summary>
        /// <param name="query">User's message or question</param>
        /// <param name="callback">Optional streaming callback for partial responses</param>
        /// <param name="completionCallback">Optional callback when response is complete</param>
        /// <param name="addToHistory">Whether to add the exchange to conversation history</param>
        /// <returns>Task that returns the AI assistant's response</returns>
        public virtual async Task<string> Chat(string query, Action<string> callback = null,
            Action completionCallback = null, bool addToHistory = true)
        {
            await CheckCaller();
            string result = "";
            try
            {
                LlamaLib.CharArrayCallback wrappedCallback = null;
                if (callback != null)
                {
#if ENABLE_IL2CPP
                    // For IL2CPP: wrap to IntPtr callback, then wrap for main thread
                    Action<string> mainThreadCallback = Utils.WrapActionForMainThread(callback, this);
                    wrappedCallback = IL2CPP_Completion.CreateCallback(mainThreadCallback);
#else
                    // For Mono: direct callback wrapping
                    wrappedCallback = Utils.WrapCallbackForAsync(callback, this);
#endif
                }

                SetCompletionParameters();
                result = await llmAgent.ChatAsync(query, addToHistory, wrappedCallback, false, debugPrompt);
                if (this == null) return null;
                if (addToHistory && result != null && save != "") _ = SaveHistory();
                if (this != null) completionCallback?.Invoke();
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError(ex.Message, true);
            }
            return result;
        }

        /// <summary>
        /// Warms up the model by processing the system prompt without generating output.
        /// This caches the system prompt processing for faster subsequent responses.
        /// </summary>
        /// <param name="completionCallback">Optional callback when warmup completes</param>
        /// <returns>Task that completes when warmup finishes</returns>
        public virtual async Task Warmup(Action completionCallback = null)
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
        public virtual async Task Warmup(string query, Action completionCallback = null)
        {
            int originalNumPredict = numPredict;
            try
            {
                // Set to generate no tokens for warmup
                numPredict = 0;
                await Chat(query, null, completionCallback, false);
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
        public virtual async Task SaveHistory()
        {
            if (string.IsNullOrEmpty(save))
            {
                LLMUnitySetup.LogError("No save path specified");
                return;
            }
            await CheckCaller();

            // Save chat history
            string jsonPath = GetSavePath();
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
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to save chat history to '{jsonPath}': {ex.Message}", true);
            }
        }

        /// <summary>
        /// Loads conversation history and optionally the LLM cache from disk.
        /// </summary>
        public virtual async Task LoadHistory()
        {
            if (string.IsNullOrEmpty(save))
            {
                LLMUnitySetup.LogError("No save path specified");
                return;
            }
            await CheckCaller();

            // Load chat history
            string jsonPath = GetSavePath();
            if (!File.Exists(jsonPath))
            {
                LLMUnitySetup.LogError($"Chat history file not found: {jsonPath}");
            }

            try
            {
                llmAgent.LoadHistory(jsonPath);
                LLMUnitySetup.Log($"Loaded chat history from: {jsonPath}");
            }
            catch (Exception ex)
            {
                LLMUnitySetup.LogError($"Failed to load chat history from '{jsonPath}': {ex.Message}", true);
            }
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

    public class ChatMessage : UndreamAI.LlamaLib.ChatMessage
    {
        public ChatMessage(string role, string content) : base(role, content) {}
        public ChatMessage(UndreamAI.LlamaLib.ChatMessage other) : base(other.role, other.content) {}
    }
}
