using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMUnity
{
    public sealed class FloatAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }

        public FloatAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
    public sealed class IntAttribute : PropertyAttribute
    {
        public int Min { get; private set; }
        public int Max { get; private set; }

        public IntAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    public class ClientAttribute : PropertyAttribute {}
    public class ServerAttribute : PropertyAttribute {}
    public class ModelAttribute : PropertyAttribute {}
    public class ModelAddonAttribute : PropertyAttribute {}
    public class ChatAttribute : PropertyAttribute {}
    public class ClientAdvancedAttribute : PropertyAttribute {}
    public class ServerAdvancedAttribute : PropertyAttribute {}
    public class ModelAdvancedAttribute : PropertyAttribute {}
    public class ModelAddonAdvancedAttribute : PropertyAttribute {}
    public class ModelExpertAttribute : PropertyAttribute {}

    [DefaultExecutionOrder(-1)]
    public class LLMClient : MonoBehaviour
    {
        [HideInInspector] public bool advancedOptions = false;
        [HideInInspector] public bool expertOptions = false;

        [ClientAdvanced] public string host = "localhost";
        [ServerAdvanced] public int port = 13333;
        [Server] public bool stream = true;

        [ModelAddonAdvanced] public string grammar = null;
        [ModelAdvanced] public int seed = 0;
        [ModelAdvanced] public int numPredict = 256;
        [ModelAdvanced] public bool cachePrompt = true;
        [ModelAdvanced, Float(0f, 2f)] public float temperature = 0.2f;
        [ModelAdvanced, Int(-1, 100)] public int topK = 40;
        [ModelAdvanced, Float(0f, 1f)] public float topP = 0.9f;
        [ModelAdvanced, Float(0f, 1f)] public float minP = 0.05f;
        [ModelAdvanced, Float(0f, 1f)] public float repeatPenalty = 1.1f;
        [ModelAdvanced, Float(0f, 1f)] public float presencePenalty = 0f;
        [ModelAdvanced, Float(0f, 1f)] public float frequencyPenalty = 0f;

        [ModelExpert, Float(0f, 1f)] public float tfsZ = 1f;
        [ModelExpert, Float(0f, 1f)] public float typicalP = 1f;
        [ModelExpert, Int(0, 2048)] public int repeatLastN = 64;
        [ModelExpert] public bool penalizeNl = true;
        [ModelExpert] public string penaltyPrompt;
        [ModelExpert, Int(0, 2)] public int mirostat = 0;
        [ModelExpert, Float(0f, 10f)] public float mirostatTau = 5f;
        [ModelExpert, Float(0f, 1f)] public float mirostatEta = 0.1f;
        [ModelExpert, Int(0, 10)] public int nProbs = 0;
        [ModelExpert] public bool ignoreEos = false;

        public int nKeep = -1;
        public List<string> stop = new List<string>();
        public Dictionary<int, string> logitBias = null;
        public string grammarString;

        [Chat] public string playerName = "user";
        [Chat] public string AIName = "assistant";
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";

        protected List<ChatMessage> chat;
        public string chatTemplate = ChatTemplate.DefaultTemplate;
        public ChatTemplate template;
        private List<(string, string)> requestHeaders = new List<(string, string)> { ("Content-Type", "application/json") };
        private string previousEndpoint;
        public bool setNKeepToPrompt = true;
        private List<UnityWebRequest> WIPRequests = new List<UnityWebRequest>();
        static object chatPromptLock = new object();
        static object chatAddLock = new object();

        public void Awake()
        {
            InitGrammar();
            InitPrompt();
            LoadTemplate();
            _ = InitNKeep();
        }

        public LLM GetServer()
        {
            foreach (LLM server in FindObjectsOfType<LLM>())
            {
                if (server.host == host && server.port == port)
                {
                    return server;
                }
            }
            return null;
        }

        public virtual void SetTemplate(string templateName)
        {
            chatTemplate = templateName;
            LoadTemplate();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            previousEndpoint = "";
            OnValidate();
        }

        private void OnValidate()
        {
            string newEndpoint = host + ":" + port;
            if (newEndpoint != previousEndpoint)
            {
                string templateToSet = chatTemplate;
                if (GetType() == typeof(LLMClient))
                {
                    LLM server = GetServer();
                    if (server != null) templateToSet = server.chatTemplate;
                }
                SetTemplate(templateToSet);
                previousEndpoint = newEndpoint;
            }
        }

#endif

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

        public void SetPrompt(string newPrompt, bool clearChat = true)
        {
            prompt = newPrompt;
            nKeep = -1;
            InitPrompt(clearChat);
            _ = InitNKeep();
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

        private void LoadTemplate()
        {
            template = ChatTemplate.GetTemplate(chatTemplate);
        }

#if UNITY_EDITOR
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

        public ChatRequest GenerateRequest(string prompt)
        {
            // setup the request struct
            ChatRequest chatRequest = new ChatRequest();
            chatRequest.prompt = prompt;
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

        public string ChatContent(ChatResult result)
        {
            // get content from a chat result received from the endpoint
            return result.content.Trim();
        }

        public string MultiChatContent(MultiChatResult result)
        {
            // get content from a chat result received from the endpoint
            string response = "";
            foreach (ChatResult resultPart in result.data)
            {
                response += resultPart.content;
            }
            return response.Trim();
        }

        public string ChatOpenAIContent(ChatOpenAIResult result)
        {
            // get content from a char result received from the endpoint in open AI format
            return result.choices[0].message.content;
        }

        public List<int> TokenizeContent(TokenizeResult result)
        {
            // get the tokens from a tokenize result received from the endpoint
            return result.tokens;
        }

        public async Task<string> Chat(string question, Callback<string> callback = null, EmptyCallback completionCallback = null, bool addToHistory = true)
        {
            // handle a chat message by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received
            await InitNKeep();

            string json;
            lock (chatPromptLock) {
                AddPlayerMessage(question);
                string prompt = template.ComputePrompt(chat, AIName);
                json = JsonUtility.ToJson(GenerateRequest(prompt));
                chat.RemoveAt(chat.Count - 1);
            }

            string result;
            if (stream)
            {
                result = await PostRequest<MultiChatResult, string>(json, "completion", MultiChatContent, callback);
            }
            else
            {
                result = await PostRequest<ChatResult, string>(json, "completion", ChatContent, callback);
            }

            if (addToHistory && result != null)
            {
                lock (chatAddLock) {
                    AddPlayerMessage(question);
                    AddAIMessage(result);
                }
            }

            completionCallback?.Invoke();
            return result;
        }

        public async Task<string> Complete(string prompt, Callback<string> callback = null, EmptyCallback completionCallback = null)
        {
            // handle a completion request by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received

            string json = JsonUtility.ToJson(GenerateRequest(prompt));
            string result;
            if (stream)
            {
                result = await PostRequest<MultiChatResult, string>(json, "completion", MultiChatContent, callback);
            }
            else
            {
                result = await PostRequest<ChatResult, string>(json, "completion", ChatContent, callback);
            }
            completionCallback?.Invoke();
            return result;
        }

        public async Task<string> Warmup(EmptyCallback completionCallback = null, string question = "hi")
        {
            return await Chat(question, null, completionCallback, false);
        }

        public async Task<List<int>> Tokenize(string question, Callback<List<int>> callback = null)
        {
            // handle the tokenization of a message by the user
            TokenizeRequest tokenizeRequest = new TokenizeRequest();
            tokenizeRequest.content = question;
            string json = JsonUtility.ToJson(tokenizeRequest);
            return await PostRequest<TokenizeResult, List<int>>(json, "tokenize", TokenizeContent, callback);
        }

        public Ret ConvertContent<Res, Ret>(string response, ContentCallback<Res, Ret> getContent = null)
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

        public string[] MultiResponse(string response)
        {
            return response.Trim().Replace("\n\n", "").Split("data: ");
        }

        public void CancelRequests()
        {
            foreach (UnityWebRequest request in WIPRequests)
            {
                request.Abort();
            }
            WIPRequests.Clear();
        }

        public bool IsServerReachable(int timeout = 5)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Head($"{host}:{port}/tokenize"))
            {
                webRequest.timeout = timeout;
                webRequest.SendWebRequest();
                while (!webRequest.isDone) {}
                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    return false;
                }
                return true;
            }
        }

        public async Task<bool> IsServerReachableAsync(int timeout = 5)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Head($"{host}:{port}/tokenize"))
            {
                webRequest.timeout = timeout;
                webRequest.SendWebRequest();
                while (!webRequest.isDone)
                {
                    await Task.Yield();
                }
                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    return false;
                }
                return true;
            }
        }

        public async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
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
    }
}
