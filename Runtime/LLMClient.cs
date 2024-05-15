/// @file
/// @brief File implementing the LLM client.
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMUnity
{
    public class LLMClient : MonoBehaviour
    {
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;

        [Client] public bool remote = false;

        [Client] public LLM llm;
        /// <summary> host to use for the LLMClient object </summary>
        [Client] public string host = "localhost";
        /// <summary> port to use for the server (LLM) or client (LLMClient) </summary>
        [Client] public int port = 13333;


        /// <summary> option to receive the reply from the model as it is produced (recommended!).
        /// If it is not selected, the full reply from the model is received in one go </summary>
        [Model] public bool stream = true;

        /// <summary> grammar file used for the LLM in .cbnf format (relative to the Assets/StreamingAssets folder) </summary>
        [ModelAdvanced] public string grammar = null;
        /// <summary> seed for reproducibility. For random results every time set to -1. </summary>
        [ModelAdvanced] public int seed = 0;
        /// <summary> number of tokens to predict (-1 = infinity, -2 = until context filled).
        /// This is the amount of tokens the model will maximum predict.
        /// When N predict is reached the model will stop generating.
        /// This means words / sentences might not get finished if this is too low. </summary>
        [ModelAdvanced] public int numPredict = 256;
        /// <summary> option to cache the prompt as it is being created by the chat to avoid reprocessing the entire prompt every time (default: true) </summary>
        [ModelAdvanced] public bool cachePrompt = true;
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

        /// <summary> number of tokens to retain from the prompt when the model runs out of context (-1 = LLM/LLMClient prompt tokens if setNKeepToPrompt is set to true). </summary>
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
        /// <summary> a description of the AI role. This defines the LLM/LLMClient system prompt </summary>
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
        /// <summary> option to set the number of tokens to retain from the prompt (nKeep) based on the LLM/LLMClient system prompt </summary>
        public bool setNKeepToPrompt = true;

        /// \cond HIDE
        protected List<ChatMessage> chat;
        static object chatPromptLock = new object();
        static object chatAddLock = new object();
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
            InitPrompt();
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
        /// Set the system prompt for the LLM/LLMClient.
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
                await Tokenize(prompt, SetNKeep);
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

#if UNITY_EDITOR
        /// <summary>
        /// Set the grammar file of the LLM/LLMClient
        /// </summary>
        /// <param name="path">path to the grammar file</param>
        public async void SetGrammar(string path)
        {
            grammar = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
        }

#endif
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
            Debug.Log(prompt);
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

        protected string ChatOpenAIContent(ChatOpenAIResult result)
        {
            // get content from a char result received from the endpoint in open AI format
            return result.choices[0].message.content;
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
        ///
        /// It can be used as follows:
        /// \code
        /// public class MyScript {
        ///     public LLM llm;
        ///     void HandleReply(string reply){
        ///         // do something with the reply from the model
        ///         Debug.Log(reply);
        ///     }
        ///
        ///     void ReplyCompleted(){
        ///         // do something when the reply from the model is complete
        ///         Debug.Log("The AI replied");
        ///     }
        ///
        ///     void Game(){
        ///         // your game function
        ///         ...
        ///         string message = "Hello bot!";
        ///         _ = llm.Chat(message, HandleReply);
        ///         ...
        ///     }
        /// }
        /// \endcode
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
            await InitNKeep();
            await LoadTemplate();

            string json;
            lock (chatPromptLock) {
                AddPlayerMessage(query);
                string prompt = template.ComputePrompt(chat, AIName);
                json = JsonUtility.ToJson(GenerateRequest(prompt));
                chat.RemoveAt(chat.Count - 1);
            }

            string result = await CompletionRequest(json, callback);

            if (addToHistory && result != null)
            {
                lock (chatAddLock) {
                    AddPlayerMessage(query);
                    AddAIMessage(result);
                }
            }

            completionCallback?.Invoke();
            return result;
        }

        /// <summary>
        /// Pure completion functionality of the LLM.
        /// It calls the LLM completion based solely on the provided prompt (no formatting by the chat template).
        /// The function allows callbacks when the response is partially or fully received.
        ///
        /// It can be used as follows:
        /// \code
        /// public class MyScript {
        ///     public LLM llm;
        ///     void HandleReply(string reply){
        ///         // do something with the reply from the model
        ///         Debug.Log(reply);
        ///     }
        ///
        ///     void ReplyCompleted(){
        ///         // do something when the reply from the model is complete
        ///         Debug.Log("The AI replied");
        ///     }
        ///
        ///     void Game(){
        ///         // your game function
        ///         ...
        ///         string message = "Hello bot!";
        ///         _ = llm.Complete(message, HandleReply);
        ///         ...
        ///     }
        /// }
        /// \endcode
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

        protected Ret ConvertContent<Res, Ret>(string response, ContentCallback<Res, Ret> getContent = null)
        {
            // template function to convert the json received and get the content
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

        /// <summary>
        /// Cancel the ongoing requests e.g. Chat, Complete.
        /// </summary>
        // <summary>
        public void CancelRequestsLocal()
        {
            if (id_slot >= 0) llm.CancelRequest(id_slot);
        }

        public void CancelRequestsRemote()
        {
            foreach (UnityWebRequest request in WIPRequests)
            {
                request.Abort();
            }
            WIPRequests.Clear();
        }

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
            switch (endpoint)
            {
                case "tokenize":
                    callResult = await llm.Tokenize(json);
                    break;
                case "detokenize":
                    callResult = await llm.Detokenize(json);
                    break;
                case "completion":
                    Callback<string> callbackString = null;
                    if (callback != null)
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
                    }
                    callResult = await llm.Completion(json, callbackString);
                    break;
                default:
                    Debug.LogError($"Unknown endpoint {endpoint}");
                    break;
            }

            Ret result = ConvertContent(callResult, getContent);
            callback?.Invoke(result);
            return result;
        }

        protected async Task<Ret> PostRequestRemote<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
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
}
