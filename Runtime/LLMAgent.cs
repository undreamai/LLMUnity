/// @file
/// @brief File implementing the LLM characters.
using Newtonsoft.Json.Linq;
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
    [DefaultExecutionOrder(-2)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM characters.
    /// </summary>
    public class LLMAgent : LLMCaller
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
        /// <summary> maximum number of tokens that the LLM will predict (-1 = infinity). </summary>
        [Tooltip("maximum number of tokens that the LLM will predict (-1 = infinity).")]
        [Model] public int numPredict = -1;
        /// <summary> slot of the server to use for computation (affects caching) </summary>
        [Tooltip("slot of the server to use for computation (affects caching)")]
        [ModelAdvanced] public int slot = -1;
        /// <summary> grammar file used for the LLMAgent (.gbnf format) </summary>
        [Tooltip("grammar file used for the LLMAgent (.gbnf format)")]
        [ModelAdvanced] public string grammar = null;
        /// <summary> grammar file used for the LLMAgent (.json format) </summary>
        [Tooltip("grammar file used for the LLMAgent (.json format)")]
        [ModelAdvanced] public string grammarJSON = null;
        /// <summary> cache the processed prompt to avoid reprocessing the entire prompt every time (default: true, recommended!) </summary>
        [Tooltip("cache the processed prompt to avoid reprocessing the entire prompt every time (default: true, recommended!)")]
        [ModelAdvanced] public bool cachePrompt = true;
        /// <summary> seed for reproducibility (-1 = no reproducibility). </summary>
        [Tooltip("seed for reproducibility (-1 = no reproducibility).")]
        [ModelAdvanced] public int seed = 0;
        /// <summary> LLM temperature, lower values give more deterministic answers. </summary>
        [Tooltip("LLM temperature, lower values give more deterministic answers.")]
        [ModelAdvanced, Float(0f, 2f)] public float temperature = 0.2f;
        /// <summary> Top-k sampling selects the next token only from the top k most likely predicted tokens (0 = disabled).
        /// Higher values lead to more diverse text, while lower value will generate more focused and conservative text.
        /// </summary>
        [Tooltip("Top-k sampling selects the next token only from the top k most likely predicted tokens (0 = disabled). Higher values lead to more diverse text, while lower value will generate more focused and conservative text. ")]
        [ModelAdvanced, Int(-1, 100)] public int topK = 40;
        /// <summary> Top-p sampling selects the next token from a subset of tokens that together have a cumulative probability of at least p (1.0 = disabled).
        /// Higher values lead to more diverse text, while lower value will generate more focused and conservative text.
        /// </summary>
        [Tooltip("Top-p sampling selects the next token from a subset of tokens that together have a cumulative probability of at least p (1.0 = disabled). Higher values lead to more diverse text, while lower value will generate more focused and conservative text. ")]
        [ModelAdvanced, Float(0f, 1f)] public float topP = 0.9f;
        /// <summary> minimum probability for a token to be used. </summary>
        [Tooltip("minimum probability for a token to be used.")]
        [ModelAdvanced, Float(0f, 1f)] public float minP = 0.05f;
        /// <summary> Penalty based on repeated tokens to control the repetition of token sequences in the generated text. </summary>
        [Tooltip("Penalty based on repeated tokens to control the repetition of token sequences in the generated text.")]
        [ModelAdvanced, Float(0f, 2f)] public float repeatPenalty = 1.1f;
        /// <summary> Penalty based on token presence in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled). </summary>
        [Tooltip("Penalty based on token presence in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled).")]
        [ModelAdvanced, Float(0f, 1f)] public float presencePenalty = 0f;
        /// <summary> Penalty based on token frequency in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled). </summary>
        [Tooltip("Penalty based on token frequency in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled).")]
        [ModelAdvanced, Float(0f, 1f)] public float frequencyPenalty = 0f;
        /// <summary> enable locally typical sampling (1.0 = disabled). Higher values will promote more contextually coherent tokens, while  lower values will promote more diverse tokens. </summary>
        [Tooltip("enable locally typical sampling (1.0 = disabled). Higher values will promote more contextually coherent tokens, while  lower values will promote more diverse tokens.")]
        [ModelAdvanced, Float(0f, 1f)] public float typicalP = 1f;
        /// <summary> last n tokens to consider for penalizing repetition (0 = disabled, -1 = ctx-size). </summary>
        [Tooltip("last n tokens to consider for penalizing repetition (0 = disabled, -1 = ctx-size).")]
        [ModelAdvanced, Int(0, 2048)] public int repeatLastN = 64;
        /// <summary> penalize newline tokens when applying the repeat penalty. </summary>
        [Tooltip("penalize newline tokens when applying the repeat penalty.")]
        [ModelAdvanced] public bool penalizeNl = true;
        /// <summary> prompt for the purpose of the penalty evaluation. Can be either null, a string or an array of numbers representing tokens (null/'' = use original prompt) </summary>
        [Tooltip("prompt for the purpose of the penalty evaluation. Can be either null, a string or an array of numbers representing tokens (null/'' = use original prompt)")]
        [ModelAdvanced] public string penaltyPrompt;
        /// <summary> enable Mirostat sampling, controlling perplexity during text generation (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0). </summary>
        [Tooltip("enable Mirostat sampling, controlling perplexity during text generation (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0).")]
        [ModelAdvanced, Int(0, 2)] public int mirostat = 0;
        /// <summary> The Mirostat target entropy (tau) controls the balance between coherence and diversity in the generated text. </summary>
        [Tooltip("The Mirostat target entropy (tau) controls the balance between coherence and diversity in the generated text.")]
        [ModelAdvanced, Float(0f, 10f)] public float mirostatTau = 5f;
        /// <summary> The Mirostat learning rate (eta) controls how quickly the algorithm responds to feedback from the generated text. </summary>
        [Tooltip("The Mirostat learning rate (eta) controls how quickly the algorithm responds to feedback from the generated text.")]
        [ModelAdvanced, Float(0f, 1f)] public float mirostatEta = 0.1f;
        /// <summary> if greater than 0, the response also contains the probabilities of top N tokens for each generated token. </summary>
        [Tooltip("if greater than 0, the response also contains the probabilities of top N tokens for each generated token.")]
        [ModelAdvanced, Int(0, 10)] public int nProbs = 0;
        /// <summary> ignore end of stream token and continue generating. </summary>
        [Tooltip("ignore end of stream token and continue generating.")]
        [ModelAdvanced] public bool ignoreEos = false;
        /// <summary> stopwords to stop the LLM in addition to the default stopwords from the chat template. </summary>
        [Tooltip("stopwords to stop the LLM in addition to the default stopwords from the chat template.")]
        public List<string> stop = new List<string>();
        /// <summary> the logit bias option allows to manually adjust the likelihood of specific tokens appearing in the generated text.
        /// By providing a token ID and a positive or negative bias value, you can increase or decrease the probability of that token being generated. </summary>
        // [Tooltip("the logit bias option allows to manually adjust the likelihood of specific tokens appearing in the generated text. By providing a token ID and a positive or negative bias value, you can increase or decrease the probability of that token being generated.")]
        // public Dictionary<int, string> logitBias = null;
        /// <summary> Receive the reply from the model as it is produced (recommended!).
        /// If not selected, the full reply from the model is received in one go </summary>
        [Tooltip("Receive the reply from the model as it is produced (recommended!). If not selected, the full reply from the model is received in one go")]
        [Chat] public bool stream = true;
        /// <summary> the name of the player </summary>
        [Tooltip("the name of the player")]
        [Chat] public string userName = "user";
        /// <summary> the name of the AI </summary>
        [Tooltip("the name of the AI")]
        [Chat] public string AIName = "assistant";
        /// <summary> a description of the AI role (system prompt) </summary>
        [Tooltip("a description of the AI role (system prompt)")]
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
        /// <summary> the chat history as list of chat messages </summary>
        /// <summary> the grammar to use </summary>
        [Tooltip("the grammar to use")]
        public string grammarString;
        /// <summary> the grammar to use </summary>
        [Tooltip("the grammar to use")]
        public string grammarJSONString;

        string completionParametersPre = "";

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
            InitGrammar();
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
        /// Checks if a LLM is valid for the LLMCaller
        /// </summary>
        /// <param name="llmSet">LLM object</param>
        /// <returns>bool specifying whether the LLM is valid</returns>
        public override bool IsValidLLM(LLM llmSet)
        {
            return !llmSet.embeddingsOnly;
        }

        protected override void SetLLMClient(LLMClient llmClientSet)
        {
            base.SetLLMClient(llmClientSet);
            llmAgent = new UndreamAI.LlamaLib.LLMAgent(llmClient, prompt, userName, AIName);
        }

        protected virtual void InitHistory()
        {
            ClearChat();
            _ = LoadHistory();
        }

        protected virtual async Task LoadHistory()
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

        protected virtual void InitGrammar()
        {
            grammarString = "";
            if (!String.IsNullOrEmpty(grammar))
            {
                grammarString = File.ReadAllText(LLMUnitySetup.GetAssetPath(grammar));
            }
            llmAgent.SetGrammar(grammarString);
        }

        /// <summary>
        /// Sets the grammar file of the LLMAgent (GBNF or JSON schema)
        /// </summary>
        /// <param name="path">path to the grammar file</param>
        public virtual async Task SetGrammar(string path)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) path = LLMUnitySetup.AddAsset(path);
#endif
            await LLMUnitySetup.AndroidExtractAsset(path, true);
            grammar = path;
            InitGrammar();
        }

        protected virtual void SetCompletionParameters()
        {
            JObject json = new JObject
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
            string completionParameters = json.ToString();
            if (completionParameters != completionParametersPre)
            {
                llmAgent.SetCompletionParameters(json);
                completionParametersPre = completionParameters;
            }
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
            SetCompletionParameters();
            LlamaLib.CharArrayCallback wrappedCallback = null;
            if (callback != null)
            {
                var context = SynchronizationContext.Current;
                wrappedCallback = (string msg) => {
                    if (context != null) context.Post(_ => callback(msg), null);
                    else callback(msg);
                };
            }

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
