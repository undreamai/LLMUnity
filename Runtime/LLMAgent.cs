/// @file
/// @brief File implementing the LLM characters.
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
    /// Class implementing the LLM characters.
    /// </summary>
    public class LLMAgent : LLMClient
    {
        /// <summary> file to save the chat history.
        /// The file will be saved within the persistentDataPath directory. </summary>
        [Tooltip("file to save the chat history. The file will be saved within the persistentDataPath directory.")]
        [LLM] public string save = "";
        /// <summary> save the LLM cache. Speeds up the prompt calculation when reloading from history but also requires ~100MB of space per character. </summary>
        [Tooltip("save the LLM cache. Speeds up the prompt calculation when reloading from history but also requires ~100MB of space per character.")]
        [LLM] public bool saveCache = false;
        /// <summary> log the constructed prompt the Unity Editor. </summary>
        // [Tooltip("log the constructed prompt the Unity Editor.")]
        // [LLM] public bool debugPrompt = false;
        /// <summary> slot of the server to use for computation (affects caching) </summary>
        [Tooltip("slot of the server to use for computation (affects caching)")]
        [ModelAdvanced] public int slot = -1;
        /// <summary> the name of the player </summary>
        [Tooltip("the name of the player")]
        [Chat] public string userName = "user";
        /// <summary> the name of the AI </summary>
        [Tooltip("the name of the AI")]
        [Chat] public string AIName = "assistant";
        /// <summary> a description of the AI role (system prompt) </summary>
        [Tooltip("a description of the AI role (system prompt)")]
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";

        [Local, SerializeField] protected UndreamAI.LlamaLib.LLMAgent _llmAgent;
        public UndreamAI.LlamaLib.LLMAgent llmAgent
        {
            get => _llmAgent;
            protected set => _llmAgent = value;
        }

        public List<ChatMessage> chat
        {
            get => llmAgent.GetHistory();
            set => llmAgent.SetHistory(value);
        }

        /// <summary>
        /// The Unity Awake function that initializes the state before the application starts.
        /// The following actions are executed:
        /// - the corresponding LLM server is defined (if ran locally)
        /// - the grammar is set based on the grammar file
        /// - the prompt and chat history are initialised
        /// - the chat template is constructed
        /// - the number of tokens to keep are based on the system prompt (if setNKeepToPrompt=true)
        /// </summary>
        public override void Start()
        {
            base.Start();
            llmAgent = new UndreamAI.LlamaLib.LLMAgent(llmClient, prompt, userName, AIName);
            InitHistory();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (llm != null && llm.parallelPrompts > -1 && (slot < -1 || slot >= llm.parallelPrompts)) LLMUnitySetup.LogError($"The slot needs to be between 0 and {llm.parallelPrompts - 1}, or -1 to be automatically set");
        }

        protected override string NotValidLLMError()
        {
            return base.NotValidLLMError() + $", it is an embedding only model";
        }

        /// <summary>
        /// Checks if a LLM is valid for the LLMClient
        /// </summary>
        /// <param name="llmSet">LLM object</param>
        /// <returns>bool specifying whether the LLM is valid</returns>
        public override bool IsValidLLM(LLM llmSet)
        {
            return !llmSet.embeddingsOnly;
        }

        protected override void SetLLMClient(UndreamAI.LlamaLib.LLMClient llmClientSet)
        {
            base.SetLLMClient(llmClientSet);
            llmAgent = new UndreamAI.LlamaLib.LLMAgent(llmClient, prompt, userName, AIName);
        }

        protected override LLMLocal GetCaller()
        {
            return _llmAgent;
        }

        protected virtual void InitHistory()
        {
            ClearChat();
            LoadHistory();
        }

        protected virtual void LoadHistory()
        {
            if (save == "" || !File.Exists(GetJsonSavePath(save))) return;
            Load(save);
        }

        protected virtual string GetSavePath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, filename).Replace('\\', '/');
        }

        /// <summary>
        /// Allows to get the save path of the chat history based on the provided filename or relative path.
        /// </summary>
        /// <param name="filename">filename or relative path used for the save</param>
        /// <returns>save path</returns>
        public virtual string GetJsonSavePath(string filename)
        {
            return GetSavePath(filename + ".json");
        }

        /// <summary>
        /// Allows to get the save path of the LLM cache based on the provided filename or relative path.
        /// </summary>
        /// <param name="filename">filename or relative path used for the save</param>
        /// <returns>save path</returns>
        public virtual string GetCacheSavePath(string filename)
        {
            return GetSavePath(filename + ".cache");
        }

        /// <summary>
        /// Clear the chat of the LLMAgent.
        /// </summary>
        public virtual void ClearChat()
        {
            llmAgent.ClearHistory();
        }

        /// <summary>
        /// Set the system prompt for the LLMAgent.
        /// </summary>
        /// <param name="newPrompt"> the system prompt </param>
        /// <param name="clearChat"> whether to clear (true) or keep (false) the current chat history on top of the system prompt. </param>
        public virtual void SetPrompt(string newPrompt, bool clearChat = true)
        {
            //TODO
            llmAgent.SystemPrompt = newPrompt;
        }

        /// <summary>
        /// Allows to add a message in the chat history.
        /// </summary>
        /// <param name="role">message role (e.g. userName or AIName)</param>
        /// <param name="content">message content</param>
        public virtual void AddMessage(string role, string content)
        {
            // add the question / answer to the chat list, update prompt
            llmAgent.AddMessage(role, content);
        }

        /// <summary>
        /// Allows to add a player message in the chat history.
        /// </summary>
        /// <param name="content">message content</param>
        public virtual void AddUserMessage(string content)
        {
            llmAgent.AddUserMessage(content);
        }

        /// <summary>
        /// Allows to add a AI message in the chat history.
        /// </summary>
        /// <param name="content">message content</param>
        public virtual void AddAIMessage(string content)
        {
            llmAgent.AddAssistantMessage(content);
        }

        /// <summary>
        /// Chat functionality of the LLM.
        /// It calls the LLM completion based on the provided query including the previous chat history.
        /// The function allows callbacks when the response is partially or fully received.
        /// The question is added to the history if specified.
        /// </summary>
        /// <param name="query">user query</param>
        /// <param name="callback">callback function that receives the response as string</param>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <param name="addToHistory">whether to add the user query to the chat history</param>
        /// <returns>the LLM response</returns>
        public virtual string Chat(string query, LlamaLib.CharArrayCallback callback = null, bool addToHistory = true)
        {
            // handle a chat message by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received
            SetCompletionParameters();
            return llmAgent.Chat(query, addToHistory, callback);
        }

        /// <summary>
        /// Chat functionality of the LLM (async).
        /// It calls the LLM completion based on the provided query including the previous chat history.
        /// The function allows callbacks when the response is partially or fully received.
        /// The question is added to the history if specified.
        /// </summary>
        /// <param name="query">user query</param>
        /// <param name="callback">callback function that receives the response as string</param>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <param name="addToHistory">whether to add the user query to the chat history</param>
        /// <returns>the LLM response</returns>
        public virtual async Task<string> ChatAsync(string query, LlamaLib.CharArrayCallback callback = null, EmptyCallback completionCallback = null, bool addToHistory = true)
        {
            LlamaLib.CharArrayCallback wrappedCallback = null;
            if (callback != null)
            {
                var context = SynchronizationContext.Current;
                wrappedCallback = (string msg) => {
                    if (context != null) context.Post(_ => callback(msg), null);
                    else callback(msg);
                };
            }

            SetCompletionParameters();
            string result = await llmAgent.ChatAsync(query, addToHistory, wrappedCallback);
            completionCallback?.Invoke();
            return result;
        }

        /// <summary>
        /// Allow to warm-up a model by processing the system prompt.
        /// The prompt processing will be cached (if cachePrompt=true) allowing for faster initialisation.
        /// The function allows a callback function for when the prompt is processed and the response received.
        /// </summary>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <returns>the LLM response</returns>
        public virtual async Task Warmup(EmptyCallback completionCallback = null)
        {
            await Warmup(null, completionCallback);
        }

        /// <summary>
        /// Allow to warm-up a model by processing the provided prompt without adding it to history.
        /// The prompt processing will be cached (if cachePrompt=true) allowing for faster initialisation.
        /// The function allows a callback function for when the prompt is processed and the response received.
        ///
        /// </summary>
        /// <param name="query">user prompt used during the initialisation (not added to history)</param>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <returns>the LLM response</returns>
        public virtual async Task Warmup(string query, EmptyCallback completionCallback = null)
        {
            int currNumPredict = numPredict;
            numPredict = 0;
            await ChatAsync(query, null, completionCallback, false);
            numPredict = currNumPredict;
            SetCompletionParameters();
        }

        /// <summary>
        /// Saves the chat history and cache to the provided filename / relative path.
        /// </summary>
        /// <param name="filename">filename / relative path to save the chat history</param>
        /// <returns></returns>
        public virtual string Save(string filename)
        {
            string filepath = GetJsonSavePath(filename);
            string dirname = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);
            llmAgent.SaveHistory(filepath);

            string cachepath = GetCacheSavePath(filename);
            if (remote || !saveCache) return null;
            string result = llmAgent.SaveSlot(cachepath);
            return result;
        }

        /// <summary>
        /// Load the chat history and cache from the provided filename / relative path.
        /// </summary>
        /// <param name="filename">filename / relative path to load the chat history from</param>
        /// <returns></returns>
        public virtual string Load(string filename)
        {
            string filepath = GetJsonSavePath(filename);
            if (!File.Exists(filepath))
            {
                LLMUnitySetup.LogError($"File {filepath} does not exist.");
                return null;
            }
            llmAgent.LoadHistory(filepath);
            LLMUnitySetup.Log($"Loaded {filepath}");

            string cachepath = GetCacheSavePath(filename);
            if (remote || !saveCache || !File.Exists(GetSavePath(cachepath))) return null;
            string result = llmAgent.LoadSlot(cachepath);
            return result;
        }

        /// <summary>
        /// Allows to cancel the requests of the LLMAgent
        /// </summary>
        public void CancelRequests()
        {
            llmAgent.Cancel();
        }
    }
}
