/// @file
/// @brief File implementing the LLMCharacter.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMUnity
{
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM characters.
    /// </summary>
    public class LLMCharacter : MonoBehaviour
    {
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> toggle to use remote LLM server or local LLM </summary>
        [LocalRemote] public bool remote = false;
        /// <summary> the LLM object to use </summary>
        [Local] public LLM llm;
        /// <summary> host to use for the LLM server </summary>
        [Remote] public string host = "localhost";
        /// <summary> port to use for the LLM server </summary>
        [Remote] public int port = 13333;
        /// <summary> file to save the chat history.
        /// The file is saved only for Chat calls with addToHistory set to true.
        /// The file will be saved within the persistentDataPath directory (see https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html). </summary>
        [LLM] public string save = "";
        /// <summary> toggle to save the LLM cache. This speeds up the prompt calculation but also requires ~100MB of space per character. </summary>
        [LLM] public bool saveCache = false;
        /// <summary> select to log the constructed prompt the Unity Editor. </summary>
        [LLM] public bool debugPrompt = false;
        /// <summary> option to receive the reply from the model as it is produced (recommended!).
        /// If it is not selected, the full reply from the model is received in one go </summary>
        [Model] public bool stream = true;
        /// <summary> grammar file used for the LLM in .cbnf format (relative to the Assets/StreamingAssets folder) </summary>
        [ModelAdvanced] public string grammar = null;
        /// <summary> option to cache the prompt as it is being created by the chat to avoid reprocessing the entire prompt every time (default: true) </summary>
        [ModelAdvanced] public bool cachePrompt = true;
        /// <summary> seed for reproducibility. For random results every time set to -1. </summary>
        [ModelAdvanced] public int seed = 0;
        /// <summary> number of tokens to predict (-1 = infinity, -2 = until context filled).
        /// This is the amount of tokens the model will maximum predict.
        /// When N predict is reached the model will stop generating.
        /// This means words / sentences might not get finished if this is too low. </summary>
        [ModelAdvanced] public int numPredict = 256;
        /// <summary> LLM temperature, lower values give more deterministic answers.
        /// The temperature setting adjusts how random the generated responses are.
        /// Turning it up makes the generated choices more varied and unpredictable.
        /// Turning it down makes the generated responses more predictable and focused on the most likely options. </summary>
        [ModelAdvanced, Float(0f, 2f)] public float temperature = 0.2f;
        /// <summary> top-k sampling (0 = disabled).
        /// The top k value controls the top k most probable tokens at each step of generation. This value can help fine tune the output and make this adhere to specific patterns or constraints. </summary>
        [ModelAdvanced, Int(-1, 100)] public int topK = 40;
        /// <summary> top-p sampling (1.0 = disabled).
        /// The top p value controls the cumulative probability of generated tokens.
        /// The model will generate tokens until this theshold (p) is reached.
        /// By lowering this value you can shorten output & encourage / discourage more diverse output. </summary>
        [ModelAdvanced, Float(0f, 1f)] public float topP = 0.9f;
        /// <summary> minimum probability for a token to be used.
        /// The probability is defined relative to the probability of the most likely token. </summary>
        [ModelAdvanced, Float(0f, 1f)] public float minP = 0.05f;
        /// <summary> control the repetition of token sequences in the generated text.
        /// The penalty is applied to repeated tokens. </summary>
        [ModelAdvanced, Float(0f, 2f)] public float repeatPenalty = 1.1f;
        /// <summary> repeated token presence penalty (0.0 = disabled).
        /// Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics. </summary>
        [ModelAdvanced, Float(0f, 1f)] public float presencePenalty = 0f;
        /// <summary> repeated token frequency penalty (0.0 = disabled).
        /// Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim. </summary>
        [ModelAdvanced, Float(0f, 1f)] public float frequencyPenalty = 0f;

        /// <summary> enable tail free sampling with parameter z (1.0 = disabled). </summary>
        [ModelAdvanced, Float(0f, 1f)] public float tfsZ = 1f;
        /// <summary> enable locally typical sampling with parameter p (1.0 = disabled). </summary>
        [ModelAdvanced, Float(0f, 1f)] public float typicalP = 1f;
        /// <summary> last n tokens to consider for penalizing repetition (0 = disabled, -1 = ctx-size). </summary>
        [ModelAdvanced, Int(0, 2048)] public int repeatLastN = 64;
        /// <summary> penalize newline tokens when applying the repeat penalty. </summary>
        [ModelAdvanced] public bool penalizeNl = true;
        /// <summary> prompt for the purpose of the penalty evaluation.
        /// Can be either null, a string or an array of numbers representing tokens (null/"" = use original prompt) </summary>
        [ModelAdvanced] public string penaltyPrompt;
        /// <summary> enable Mirostat sampling, controlling perplexity during text generation (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0). </summary>
        [ModelAdvanced, Int(0, 2)] public int mirostat = 0;
        /// <summary> set the Mirostat target entropy, parameter tau. </summary>
        [ModelAdvanced, Float(0f, 10f)] public float mirostatTau = 5f;
        /// <summary> set the Mirostat learning rate, parameter eta. </summary>
        [ModelAdvanced, Float(0f, 1f)] public float mirostatEta = 0.1f;
        /// <summary> if greater than 0, the response also contains the probabilities of top N tokens for each generated token. </summary>
        [ModelAdvanced, Int(0, 10)] public int nProbs = 0;
        /// <summary> ignore end of stream token and continue generating. </summary>
        [ModelAdvanced] public bool ignoreEos = false;

        /// <summary> number of tokens to retain from the prompt when the model runs out of context (-1 = LLMCharacter prompt tokens if setNKeepToPrompt is set to true). </summary>
        public int nKeep = -1;
        /// <summary> stopwords to stop the LLM in addition to the default stopwords from the chat template. </summary>
        public List<string> stop = new List<string>();
        /// <summary> the logit bias option allows to manually adjust the likelihood of specific tokens appearing in the generated text.
        /// By providing a token ID and a positive or negative bias value, you can increase or decrease the probability of that token being generated. </summary>
        public Dictionary<int, string> logitBias = null;

        /// <summary> the name of the player </summary>
        [Chat] public string playerName = "user";
        /// <summary> the name of the AI </summary>
        [Chat] public string AIName = "assistant";
        /// <summary> a description of the AI role. This defines the LLMCharacter system prompt </summary>
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
        /// <summary> option to set the number of tokens to retain from the prompt (nKeep) based on the LLMCharacter system prompt </summary>
        public bool setNKeepToPrompt = true;

        /// \cond HIDE
        public List<ChatMessage> chat;
        private SemaphoreSlim chatLock = new SemaphoreSlim(1, 1);
        private string chatTemplate;
        private ChatTemplate template;
        public string grammarString;
        protected int id_slot = -1;
        private List<(string, string)> requestHeaders = new List<(string, string)> { ("Content-Type", "application/json") };
        private List<UnityWebRequest> WIPRequests = new List<UnityWebRequest>();
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
        public void Awake()
        {
            // Start the LLM server in a cross-platform way
            if (!enabled) return;
            if (!remote)
            {
                if (llm == null)
                {
                    Debug.LogError("No llm provided!");
                    return;
                }
                id_slot = llm.Register(this);
            }

            InitGrammar();
            InitHistory();
        }

        protected void InitHistory()
        {
            InitPrompt();
            _ = LoadHistory();
        }

        protected async Task LoadHistory()
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

        string GetSavePath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, filename);
        }

        string GetJsonSavePath(string filename)
        {
            return GetSavePath(filename + ".json");
        }

        string GetCacheName(string filename)
        {
            // this is saved already in the Application.persistentDataPath folder
            return filename + ".cache";
        }

        private void InitPrompt(bool clearChat = true)
        {
            if (chat != null)
            {
                if (clearChat) chat.Clear();
            }
            else
            {
                chat = new List<ChatMessage>();
            }
            ChatMessage promptMessage = new ChatMessage { role = "system", content = prompt };
            if (chat.Count == 0)
            {
                chat.Add(promptMessage);
            }
            else
            {
                chat[0] = promptMessage;
            }
        }

        /// <summary>
        /// Set the system prompt for the LLMCharacter.
        /// </summary>
        /// <param name="newPrompt"> the system prompt </param>
        /// <param name="clearChat"> whether to clear (true) or keep (false) the current chat history on top of the system prompt. </param>
        public void SetPrompt(string newPrompt, bool clearChat = true)
        {
            prompt = newPrompt;
            nKeep = -1;
            InitPrompt(clearChat);
        }

        private async Task InitNKeep()
        {
            if (setNKeepToPrompt && nKeep == -1)
            {
                string systemPrompt = template.ComputePrompt(new List<ChatMessage>(){chat[0]}, "", false);
                await Tokenize(systemPrompt, SetNKeep);
            }
        }

        private void InitGrammar()
        {
            if (grammar != null && grammar != "")
            {
                grammarString = File.ReadAllText(LLMUnitySetup.GetAssetPath(grammar));
            }
        }

        private void SetNKeep(List<int> tokens)
        {
            // set the tokens to keep
            nKeep = tokens.Count;
        }

        /// <summary>
        /// Load the chat template of the LLMCharacter.
        /// </summary>
        /// <returns></returns>
        public async Task LoadTemplate()
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
                template = ChatTemplate.GetTemplate(chatTemplate);
            }
        }

        /// <summary>
        /// Set the grammar file of the LLMCharacter
        /// </summary>
        /// <param name="path">path to the grammar file</param>
        public async void SetGrammar(string path)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) path = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
#endif
            grammar = path;
            InitGrammar();
        }

        List<string> GetStopwords()
        {
            List<string> stopAll = new List<string>(template.GetStop(playerName, AIName));
            if (stop != null) stopAll.AddRange(stop);
            return stopAll;
        }

        ChatRequest GenerateRequest(string prompt)
        {
            // setup the request struct
            ChatRequest chatRequest = new ChatRequest();
            if (debugPrompt) Debug.Log(prompt);
            chatRequest.prompt = prompt;
            chatRequest.id_slot = id_slot;
            chatRequest.temperature = temperature;
            chatRequest.top_k = topK;
            chatRequest.top_p = topP;
            chatRequest.min_p = minP;
            chatRequest.n_predict = numPredict;
            chatRequest.n_keep = nKeep;
            chatRequest.stream = stream;
            chatRequest.stop = GetStopwords();
            chatRequest.tfs_z = tfsZ;
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

        private void AddMessage(string role, string content)
        {
            // add the question / answer to the chat list, update prompt
            chat.Add(new ChatMessage { role = role, content = content });
        }

        private void AddPlayerMessage(string content)
        {
            AddMessage(playerName, content);
        }

        private void AddAIMessage(string content)
        {
            AddMessage(AIName, content);
        }

        protected string ChatContent(ChatResult result)
        {
            // get content from a chat result received from the endpoint
            return result.content.Trim();
        }

        protected string MultiChatContent(MultiChatResult result)
        {
            // get content from a chat result received from the endpoint
            string response = "";
            foreach (ChatResult resultPart in result.data)
            {
                response += resultPart.content;
            }
            return response.Trim();
        }

        async Task<string> CompletionRequest(string json, Callback<string> callback = null)
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

        protected string TemplateContent(TemplateResult result)
        {
            // get content from a char result received from the endpoint in open AI format
            return result.template;
        }

        protected List<int> TokenizeContent(TokenizeResult result)
        {
            // get the tokens from a tokenize result received from the endpoint
            return result.tokens;
        }

        protected string SlotContent(SlotResult result)
        {
            // get the tokens from a tokenize result received from the endpoint
            return result.filename;
        }

        protected string DetokenizeContent(TokenizeRequest result)
        {
            // get content from a chat result received from the endpoint
            return result.content;
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
        public async Task<string> Chat(string query, Callback<string> callback = null, EmptyCallback completionCallback = null, bool addToHistory = true)
        {
            // handle a chat message by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received
            await LoadTemplate();
            await InitNKeep();

            string json;
            await chatLock.WaitAsync();
            try
            {
                AddPlayerMessage(query);
                string prompt = template.ComputePrompt(chat, AIName);
                json = JsonUtility.ToJson(GenerateRequest(prompt));
                chat.RemoveAt(chat.Count - 1);
            }
            finally
            {
                chatLock.Release();
            }

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
        public async Task<string> Complete(string prompt, Callback<string> callback = null, EmptyCallback completionCallback = null)
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
        /// Allow to warm-up a model by processing the prompt.
        /// The prompt processing will be cached (if cachePrompt=true) allowing for faster initialisation.
        /// The function allows callback for when the prompt is processed and the response received.
        ///
        /// The function calls the Chat function with a predefined query without adding it to history.
        /// </summary>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <param name="query">user prompt used during the initialisation (not added to history)</param>
        /// <returns>the LLM response</returns>
        public async Task<string> Warmup(EmptyCallback completionCallback = null, string query = "hi")
        {
            return await Chat(query, null, completionCallback, false);
        }

        /// <summary>
        /// Asks the LLM for the chat template to use.
        /// </summary>
        /// <returns>the chat template of the LLM</returns>
        public async Task<string> AskTemplate()
        {
            return await PostRequest<TemplateResult, string>("{}", "template", TemplateContent);
        }

        /// <summary>
        /// Tokenises the provided query.
        /// </summary>
        /// <param name="query">query to tokenise</param>
        /// <param name="callback">callback function called with the result tokens</param>
        /// <returns>list of the tokens</returns>
        public async Task<List<int>> Tokenize(string query, Callback<List<int>> callback = null)
        {
            // handle the tokenization of a message by the user
            TokenizeRequest tokenizeRequest = new TokenizeRequest();
            tokenizeRequest.content = query;
            string json = JsonUtility.ToJson(tokenizeRequest);
            return await PostRequest<TokenizeResult, List<int>>(json, "tokenize", TokenizeContent, callback);
        }

        /// <summary>
        /// Detokenises the provided tokens to a string.
        /// </summary>
        /// <param name="tokens">tokens to detokenise</param>
        /// <param name="callback">callback function called with the result string</param>
        /// <returns>the detokenised string</returns>
        public async Task<string> Detokenize(List<int> tokens, Callback<string> callback = null)
        {
            // handle the detokenization of a message by the user
            TokenizeResult tokenizeRequest = new TokenizeResult();
            tokenizeRequest.tokens = tokens;
            string json = JsonUtility.ToJson(tokenizeRequest);
            return await PostRequest<TokenizeRequest, string>(json, "detokenize", DetokenizeContent, callback);
        }

        private async Task<string> Slot(string filename, string action)
        {
            SlotRequest slotRequest = new SlotRequest();
            slotRequest.id_slot = id_slot;
            slotRequest.filename = filename;
            slotRequest.action = action;
            string json = JsonUtility.ToJson(slotRequest);
            return await PostRequest<SlotResult, string>(json, "slots", SlotContent);
        }

        /// <summary>
        /// Saves the chat history and cache to the provided filename / relative path.
        /// </summary>
        /// <param name="filename">filename / relative path to save the chat history</param>
        /// <returns></returns>
        public async Task<string> Save(string filename)
        {
            string filepath = GetJsonSavePath(filename);
            string json = JsonUtility.ToJson(new ChatListWrapper { chat = chat });
            File.WriteAllText(filepath, json);

            string cachename = GetCacheName(filename);
            if (remote || !saveCache) return null;
            string result = await Slot(cachename, "save");
            return result;
        }

        /// <summary>
        /// Load the chat history and cache from the provided filename / relative path.
        /// </summary>
        /// <param name="filename">filename / relative path to load the chat history from</param>
        /// <returns></returns>
        public async Task<string> Load(string filename)
        {
            string filepath = GetJsonSavePath(filename);
            if (!File.Exists(filepath))
            {
                Debug.LogError($"File {filepath} does not exist.");
                return null;
            }
            string json = File.ReadAllText(filepath);
            chat = JsonUtility.FromJson<ChatListWrapper>(json).chat;
            Debug.Log($"Loaded {filepath}");

            string cachename = GetCacheName(filename);
            if (remote || !saveCache || !File.Exists(GetSavePath(cachename))) return null;
            string result = await Slot(cachename, "restore");
            return result;
        }

        protected Ret ConvertContent<Res, Ret>(string response, ContentCallback<Res, Ret> getContent = null)
        {
            // template function to convert the json received and get the content
            if (response == null) return default;
            response = response.Trim();
            if (response.StartsWith("data: "))
            {
                string responseArray = "";
                foreach (string responsePart in response.Replace("\n\n", "").Split("data: "))
                {
                    if (responsePart == "") continue;
                    if (responseArray != "") responseArray += ",\n";
                    responseArray += responsePart;
                }
                response = $"{{\"data\": [{responseArray}]}}";
            }
            return getContent(JsonUtility.FromJson<Res>(response));
        }

        protected void CancelRequestsLocal()
        {
            if (id_slot >= 0) llm.CancelRequest(id_slot);
        }

        protected void CancelRequestsRemote()
        {
            foreach (UnityWebRequest request in WIPRequests)
            {
                request.Abort();
            }
            WIPRequests.Clear();
        }

        /// <summary>
        /// Cancel the ongoing requests e.g. Chat, Complete.
        /// </summary>
        // <summary>
        public void CancelRequests()
        {
            if (remote) CancelRequestsRemote();
            else CancelRequestsLocal();
        }

        protected async Task<Ret> PostRequestLocal<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            string callResult = null;
            bool callbackCalled = false;
            while (!llm.failed && !llm.started) await Task.Yield();
            switch (endpoint)
            {
                case "tokenize":
                    callResult = await llm.Tokenize(json);
                    break;
                case "detokenize":
                    callResult = await llm.Detokenize(json);
                    break;
                case "slots":
                    callResult = await llm.Slot(json);
                    break;
                case "completion":
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
                            Debug.LogError($"wrong callback type, should be string");
                        }
                        callbackCalled = true;
                    }
                    callResult = await llm.Completion(json, callbackString);
                    break;
                default:
                    Debug.LogError($"Unknown endpoint {endpoint}");
                    break;
            }

            Ret result = ConvertContent(callResult, getContent);
            if (!callbackCalled) callback?.Invoke(result);
            return result;
        }

        protected async Task<Ret> PostRequestRemote<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            if (endpoint == "slots")
            {
                Debug.LogError("Saving and loading is not currently supported in remote setting");
                return default;
            }

            Ret result = default;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            using (var request = UnityWebRequest.Put($"{host}:{port}/{endpoint}", jsonToSend))
            {
                WIPRequests.Add(request);

                request.method = "POST";
                if (requestHeaders != null)
                {
                    for (int i = 0; i < requestHeaders.Count; i++)
                        request.SetRequestHeader(requestHeaders[i].Item1, requestHeaders[i].Item2);
                }

                // Start the request asynchronously
                var asyncOperation = request.SendWebRequest();
                float lastProgress = 0f;
                // Continue updating progress until the request is completed
                while (!asyncOperation.isDone)
                {
                    float currentProgress = request.downloadProgress;
                    // Check if progress has changed
                    if (currentProgress != lastProgress && callback != null)
                    {
                        callback?.Invoke(ConvertContent(request.downloadHandler.text, getContent));
                        lastProgress = currentProgress;
                    }
                    // Wait for the next frame
                    await Task.Yield();
                }
                WIPRequests.Remove(request);
                if (request.result != UnityWebRequest.Result.Success) Debug.LogError(request.error);
                else result = ConvertContent(request.downloadHandler.text, getContent);
                callback?.Invoke(result);
            }
            return result;
        }

        protected async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            if (remote) return await PostRequestRemote(json, endpoint, getContent, callback);
            return await PostRequestLocal(json, endpoint, getContent, callback);
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
