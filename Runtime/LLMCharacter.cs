/// @file
/// @brief File implementing the LLM characters.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM characters.
    /// </summary>
    public class LLMCharacter : LLMCaller
    {
        /// <summary> file to save the chat history.
        /// The file will be saved within the persistentDataPath directory. </summary>
        [Tooltip("file to save the chat history. The file will be saved within the persistentDataPath directory.")]
        [LLM] public string save = "";
        /// <summary> save the LLM cache. Speeds up the prompt calculation when reloading from history but also requires ~100MB of space per character. </summary>
        [Tooltip("save the LLM cache. Speeds up the prompt calculation when reloading from history but also requires ~100MB of space per character.")]
        [LLM] public bool saveCache = false;
        /// <summary> log the constructed prompt the Unity Editor. </summary>
        [Tooltip("log the constructed prompt the Unity Editor.")]
        [LLM] public bool debugPrompt = false;
        /// <summary> maximum number of tokens that the LLM will predict (-1 = infinity, -2 = until context filled). </summary>
        [Tooltip("maximum number of tokens that the LLM will predict (-1 = infinity, -2 = until context filled).")]
        [Model] public int numPredict = 256;
        /// <summary> slot of the server to use for computation (affects caching) </summary>
        [Tooltip("slot of the server to use for computation (affects caching)")]
        [ModelAdvanced] public int slot = -1;
        /// <summary> grammar file used for the LLMCharacter (.gbnf format) </summary>
        [Tooltip("grammar file used for the LLMCharacter (.gbnf format)")]
        [ModelAdvanced] public string grammar = null;
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
        /// <summary> number of tokens to retain from the prompt when the model runs out of context (-1 = LLMCharacter prompt tokens if setNKeepToPrompt is set to true). </summary>
        [Tooltip("number of tokens to retain from the prompt when the model runs out of context (-1 = LLMCharacter prompt tokens if setNKeepToPrompt is set to true).")]
        public int nKeep = -1;
        /// <summary> stopwords to stop the LLM in addition to the default stopwords from the chat template. </summary>
        [Tooltip("stopwords to stop the LLM in addition to the default stopwords from the chat template.")]
        public List<string> stop = new List<string>();
        /// <summary> the logit bias option allows to manually adjust the likelihood of specific tokens appearing in the generated text.
        /// By providing a token ID and a positive or negative bias value, you can increase or decrease the probability of that token being generated. </summary>
        [Tooltip("the logit bias option allows to manually adjust the likelihood of specific tokens appearing in the generated text. By providing a token ID and a positive or negative bias value, you can increase or decrease the probability of that token being generated.")]
        public Dictionary<int, string> logitBias = null;
        /// <summary> Receive the reply from the model as it is produced (recommended!).
        /// If not selected, the full reply from the model is received in one go </summary>
        [Tooltip("Receive the reply from the model as it is produced (recommended!). If not selected, the full reply from the model is received in one go")]
        [Chat] public bool stream = true;
        /// <summary> the name of the player </summary>
        [Tooltip("the name of the player")]
        [Chat] public string playerName = "user";
        /// <summary> the name of the AI </summary>
        [Tooltip("the name of the AI")]
        [Chat] public string AIName = "assistant";
        /// <summary> a description of the AI role (system prompt) </summary>
        [Tooltip("a description of the AI role (system prompt)")]
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
        /// <summary> set the number of tokens to always retain from the prompt (nKeep) based on the LLMCharacter system prompt </summary>
        [Tooltip("set the number of tokens to always retain from the prompt (nKeep) based on the LLMCharacter system prompt")]
        public bool setNKeepToPrompt = true;
        /// <summary> the chat history as list of chat messages </summary>
        [Tooltip("the chat history as list of chat messages")]
        public List<ChatMessage> chat = new List<ChatMessage>();
        /// <summary> the grammar to use </summary>
        [Tooltip("the grammar to use")]
        public string grammarString;

        /// \cond HIDE
        protected SemaphoreSlim chatLock = new SemaphoreSlim(1, 1);
        protected string chatTemplate;
        protected ChatTemplate template = null;
        /// \endcond

        /// <summary>
        /// The Unity Awake function that initializes the state before the application starts.
        /// The following actions are executed:
        /// - the corresponding LLM server is defined (if ran locally)
        /// - the grammar is set based on the grammar file
        /// - the prompt and chat history are initialised
        /// - the chat template is constructed
        /// - the number of tokens to keep are based on the system prompt (if setNKeepToPrompt=true)
        /// </summary>
        public override void Awake()
        {
            if (!enabled) return;
            base.Awake();
            if (!remote)
            {
                int slotFromServer = llm.Register(this);
                if (slot == -1) slot = slotFromServer;
            }
            InitGrammar();
            InitHistory();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (llm != null && llm.parallelPrompts > -1 && (slot < -1 || slot >= llm.parallelPrompts)) LLMUnitySetup.LogError($"The slot needs to be between 0 and {llm.parallelPrompts-1}, or -1 to be automatically set");
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

        protected virtual void InitHistory()
        {
            ClearChat();
            _ = LoadHistory();
        }

        protected virtual async Task LoadHistory()
        {
            if (save == "" || !File.Exists(GetJsonSavePath(save))) return;
            await chatLock.WaitAsync(); // Acquire the lock
            try
            {
                await Load(save);
            }
            finally
            {
                chatLock.Release(); // Release the lock
            }
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
        /// Clear the chat of the LLMCharacter.
        /// </summary>
        public virtual void ClearChat()
        {
            chat.Clear();
            ChatMessage promptMessage = new ChatMessage { role = "system", content = prompt };
            chat.Add(promptMessage);
        }

        /// <summary>
        /// Set the system prompt for the LLMCharacter.
        /// </summary>
        /// <param name="newPrompt"> the system prompt </param>
        /// <param name="clearChat"> whether to clear (true) or keep (false) the current chat history on top of the system prompt. </param>
        public virtual void SetPrompt(string newPrompt, bool clearChat = true)
        {
            prompt = newPrompt;
            nKeep = -1;
            if (clearChat) ClearChat();
            else chat[0] = new ChatMessage { role = "system", content = prompt };
        }

        protected virtual bool CheckTemplate()
        {
            if (template == null)
            {
                LLMUnitySetup.LogError("Template not set!");
                return false;
            }
            return true;
        }

        protected virtual async Task<bool> InitNKeep()
        {
            if (setNKeepToPrompt && nKeep == -1)
            {
                if (!CheckTemplate()) return false;
                string systemPrompt = template.ComputePrompt(new List<ChatMessage>(){chat[0]}, playerName, "", false);
                List<int> tokens = await Tokenize(systemPrompt);
                if (tokens == null) return false;
                SetNKeep(tokens);
            }
            return true;
        }

        protected virtual void InitGrammar()
        {
            if (grammar != null && grammar != "")
            {
                grammarString = File.ReadAllText(LLMUnitySetup.GetAssetPath(grammar));
            }
        }

        protected virtual void SetNKeep(List<int> tokens)
        {
            // set the tokens to keep
            nKeep = tokens.Count;
        }

        /// <summary>
        /// Loads the chat template of the LLMCharacter.
        /// </summary>
        /// <returns></returns>
        public virtual async Task LoadTemplate()
        {
            string llmTemplate;
            if (remote)
            {
                llmTemplate = await AskTemplate();
            }
            else
            {
                llmTemplate = llm.GetTemplate();
            }
            if (llmTemplate != chatTemplate)
            {
                chatTemplate = llmTemplate;
                template = chatTemplate == null ? null : ChatTemplate.GetTemplate(chatTemplate);
                nKeep = -1;
            }
        }

        /// <summary>
        /// Sets the grammar file of the LLMCharacter
        /// </summary>
        /// <param name="path">path to the grammar file</param>
        public virtual async void SetGrammar(string path)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) path = LLMUnitySetup.AddAsset(path);
#endif
            await LLMUnitySetup.AndroidExtractAsset(path, true);
            grammar = path;
            InitGrammar();
        }

        protected virtual List<string> GetStopwords()
        {
            if (!CheckTemplate()) return null;
            List<string> stopAll = new List<string>(template.GetStop(playerName, AIName));
            if (stop != null) stopAll.AddRange(stop);
            return stopAll;
        }

        protected virtual ChatRequest GenerateRequest(string prompt)
        {
            // setup the request struct
            ChatRequest chatRequest = new ChatRequest();
            if (debugPrompt) LLMUnitySetup.Log(prompt);
            chatRequest.prompt = prompt;
            chatRequest.id_slot = slot;
            chatRequest.temperature = temperature;
            chatRequest.top_k = topK;
            chatRequest.top_p = topP;
            chatRequest.min_p = minP;
            chatRequest.n_predict = numPredict;
            chatRequest.n_keep = nKeep;
            chatRequest.stream = stream;
            chatRequest.stop = GetStopwords();
            chatRequest.typical_p = typicalP;
            chatRequest.repeat_penalty = repeatPenalty;
            chatRequest.repeat_last_n = repeatLastN;
            chatRequest.penalize_nl = penalizeNl;
            chatRequest.presence_penalty = presencePenalty;
            chatRequest.frequency_penalty = frequencyPenalty;
            chatRequest.penalty_prompt = (penaltyPrompt != null && penaltyPrompt != "") ? penaltyPrompt : null;
            chatRequest.mirostat = mirostat;
            chatRequest.mirostat_tau = mirostatTau;
            chatRequest.mirostat_eta = mirostatEta;
            chatRequest.grammar = grammarString;
            chatRequest.seed = seed;
            chatRequest.ignore_eos = ignoreEos;
            chatRequest.logit_bias = logitBias;
            chatRequest.n_probs = nProbs;
            chatRequest.cache_prompt = cachePrompt;
            return chatRequest;
        }

        /// <summary>
        /// Allows to add a message in the chat history.
        /// </summary>
        /// <param name="role">message role (e.g. playerName or AIName)</param>
        /// <param name="content">message content</param>
        public virtual void AddMessage(string role, string content)
        {
            // add the question / answer to the chat list, update prompt
            chat.Add(new ChatMessage { role = role, content = content });
        }

        /// <summary>
        /// Allows to add a player message in the chat history.
        /// </summary>
        /// <param name="content">message content</param>
        public virtual void AddPlayerMessage(string content)
        {
            AddMessage(playerName, content);
        }

        /// <summary>
        /// Allows to add a AI message in the chat history.
        /// </summary>
        /// <param name="content">message content</param>
        public virtual void AddAIMessage(string content)
        {
            AddMessage(AIName, content);
        }

        protected virtual string ChatContent(ChatResult result)
        {
            // get content from a chat result received from the endpoint
            return result.content.Trim();
        }

        protected virtual string MultiChatContent(MultiChatResult result)
        {
            // get content from a chat result received from the endpoint
            string response = "";
            foreach (ChatResult resultPart in result.data)
            {
                response += resultPart.content;
            }
            return response.Trim();
        }

        protected virtual string SlotContent(SlotResult result)
        {
            // get the tokens from a tokenize result received from the endpoint
            return result.filename;
        }

        protected virtual string TemplateContent(TemplateResult result)
        {
            // get content from a char result received from the endpoint in open AI format
            return result.template;
        }

        protected virtual async Task<string> CompletionRequest(string json, Callback<string> callback = null)
        {
            string result = "";
            if (stream)
            {
                result = await PostRequest<MultiChatResult, string>(json, "completion", MultiChatContent, callback);
            }
            else
            {
                result = await PostRequest<ChatResult, string>(json, "completion", ChatContent, callback);
            }
            return result;
        }

        protected async Task<ChatRequest> PromptWithQuery(string query)
        {
            ChatRequest result = default;
            await chatLock.WaitAsync();
            try
            {
                AddPlayerMessage(query);
                string prompt = template.ComputePrompt(chat, playerName, AIName);
                result = GenerateRequest(prompt);
                chat.RemoveAt(chat.Count - 1);
            }
            finally
            {
                chatLock.Release();
            }
            return result;
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
        public virtual async Task<string> Chat(string query, Callback<string> callback = null, EmptyCallback completionCallback = null, bool addToHistory = true)
        {
            // handle a chat message by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received
            await LoadTemplate();
            if (!CheckTemplate()) return null;
            if (!await InitNKeep()) return null;

            string json = JsonUtility.ToJson(await PromptWithQuery(query));
            string result = await CompletionRequest(json, callback);

            if (addToHistory && result != null)
            {
                await chatLock.WaitAsync();
                try
                {
                    AddPlayerMessage(query);
                    AddAIMessage(result);
                }
                finally
                {
                    chatLock.Release();
                }
                if (save != "") _ = Save(save);
            }

            completionCallback?.Invoke();
            return result;
        }

        /// <summary>
        /// Pure completion functionality of the LLM.
        /// It calls the LLM completion based solely on the provided prompt (no formatting by the chat template).
        /// The function allows callbacks when the response is partially or fully received.
        /// </summary>
        /// <param name="prompt">user query</param>
        /// <param name="callback">callback function that receives the response as string</param>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <returns>the LLM response</returns>
        public virtual async Task<string> Complete(string prompt, Callback<string> callback = null, EmptyCallback completionCallback = null)
        {
            // handle a completion request by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received
            await LoadTemplate();

            string json = JsonUtility.ToJson(GenerateRequest(prompt));
            string result = await CompletionRequest(json, callback);
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
            await LoadTemplate();
            if (!CheckTemplate()) return;
            if (!await InitNKeep()) return;

            ChatRequest request;
            if (String.IsNullOrEmpty(query))
            {
                string prompt = template.ComputePrompt(chat, playerName, AIName);
                request = GenerateRequest(prompt);
            }
            else
            {
                request = await PromptWithQuery(query);
            }

            request.n_predict = 0;
            string json = JsonUtility.ToJson(request);
            await CompletionRequest(json);
            completionCallback?.Invoke();
        }

        /// <summary>
        /// Asks the LLM for the chat template to use.
        /// </summary>
        /// <returns>the chat template of the LLM</returns>
        public virtual async Task<string> AskTemplate()
        {
            return await PostRequest<TemplateResult, string>("{}", "template", TemplateContent);
        }

        protected override void CancelRequestsLocal()
        {
            if (slot >= 0) llm.CancelRequest(slot);
        }

        protected virtual async Task<string> Slot(string filepath, string action)
        {
            SlotRequest slotRequest = new SlotRequest();
            slotRequest.id_slot = slot;
            slotRequest.filepath = filepath;
            slotRequest.action = action;
            string json = JsonUtility.ToJson(slotRequest);
            return await PostRequest<SlotResult, string>(json, "slots", SlotContent);
        }

        /// <summary>
        /// Saves the chat history and cache to the provided filename / relative path.
        /// </summary>
        /// <param name="filename">filename / relative path to save the chat history</param>
        /// <returns></returns>
        public virtual async Task<string> Save(string filename)
        {
            string filepath = GetJsonSavePath(filename);
            string dirname = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);
            string json = JsonUtility.ToJson(new ChatListWrapper { chat = chat.GetRange(1, chat.Count - 1) });
            File.WriteAllText(filepath, json);

            string cachepath = GetCacheSavePath(filename);
            if (remote || !saveCache) return null;
            string result = await Slot(cachepath, "save");
            return result;
        }

        /// <summary>
        /// Load the chat history and cache from the provided filename / relative path.
        /// </summary>
        /// <param name="filename">filename / relative path to load the chat history from</param>
        /// <returns></returns>
        public virtual async Task<string> Load(string filename)
        {
            string filepath = GetJsonSavePath(filename);
            if (!File.Exists(filepath))
            {
                LLMUnitySetup.LogError($"File {filepath} does not exist.");
                return null;
            }
            string json = File.ReadAllText(filepath);
            List<ChatMessage> chatHistory = JsonUtility.FromJson<ChatListWrapper>(json).chat;
            ClearChat();
            chat.AddRange(chatHistory);
            LLMUnitySetup.Log($"Loaded {filepath}");

            string cachepath = GetCacheSavePath(filename);
            if (remote || !saveCache || !File.Exists(GetSavePath(cachepath))) return null;
            string result = await Slot(cachepath, "restore");
            return result;
        }

        protected override async Task<Ret> PostRequestLocal<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            if (endpoint != "completion") return await base.PostRequestLocal(json, endpoint, getContent, callback);

            while (!llm.failed && !llm.started) await Task.Yield();

            string callResult = null;
            bool callbackCalled = false;
            if (llm.embeddingsOnly) LLMUnitySetup.LogError("The LLM can't be used for completion, only for embeddings");
            else
            {
                Callback<string> callbackString = null;
                if (stream && callback != null)
                {
                    if (typeof(Ret) == typeof(string))
                    {
                        callbackString = (strArg) =>
                        {
                            callback(ConvertContent(strArg, getContent));
                        };
                    }
                    else
                    {
                        LLMUnitySetup.LogError($"wrong callback type, should be string");
                    }
                    callbackCalled = true;
                }
                callResult = await llm.Completion(json, callbackString);
            }

            Ret result = ConvertContent(callResult, getContent);
            if (!callbackCalled) callback?.Invoke(result);
            return result;
        }
    }

    /// \cond HIDE
    [Serializable]
    public class ChatListWrapper
    {
        public List<ChatMessage> chat;
    }
    /// \endcond
}
