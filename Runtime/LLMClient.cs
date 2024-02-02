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
        public List<string> stop = null;
        public Dictionary<int, string> logitBias = null;
        public string grammarString;

        [Chat] public string playerName = "Human";
        [Chat] public string AIName = "Assistant";
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";

        private string currentPrompt;
        private List<ChatMessage> chat;
        private List<(string, string)> requestHeaders;
        public bool setNKeepToPrompt = true;

        public LLMClient()
        {
            // initialise headers and chat lists
            requestHeaders = new List<(string, string)> { ("Content-Type", "application/json") };
        }

        public async void Awake()
        {
            // initialise the prompt and set the keep tokens based on its length
            InitStop();
            InitGrammar();
            await InitPrompt();
        }

        private async Task InitPrompt(bool clearChat = true)
        {
            await InitNKeep();
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

            currentPrompt = prompt;
            for (int i = 1; i < chat.Count; i++)
            {
                currentPrompt += RoleMessageString(chat[i].role, chat[i].content);
            }
        }

        public async Task SetPrompt(string newPrompt, bool clearChat = true)
        {
            prompt = newPrompt;
            nKeep = -1;
            await InitPrompt(clearChat);
        }

        protected static string GetAssetPath(string relPath = "")
        {
            // Path to store llm server binaries and models
            return Path.Combine(Application.streamingAssetsPath, relPath).Replace('\\', '/');
        }

        private async Task InitNKeep()
        {
            if (setNKeepToPrompt && nKeep == -1)
            {
                await Tokenize(prompt, SetNKeep);
            }
        }

        private void InitStop()
        {
            // set stopwords
            if (stop == null || stop.Count == 0)
            {
                stop = new List<string> { RoleString(playerName), playerName + ":" };
            }
        }

        private void InitGrammar()
        {
            if (grammar != null && grammar != "")
            {
                grammarString = File.ReadAllText(GetAssetPath(grammar));
            }
        }

        private void SetNKeep(List<int> tokens)
        {
            // set the tokens to keep
            nKeep = tokens.Count;
        }

#if UNITY_EDITOR
        public async void SetGrammar(string path)
        {
            grammar = await LLMUnitySetup.AddAsset(path, GetAssetPath());
        }
#endif

        private string RoleString(string role)
        {
            // role as a delimited string for the model
            return "\n### " + role + ":";
        }

        private string RoleMessageString(string role, string message)
        {
            // role and the role message
            return RoleString(role) + " " + message;
        }

        public ChatRequest GenerateRequest(string message, bool openAIFormat = false)
        {
            // setup the request struct
            ChatRequest chatRequest = new ChatRequest();
            if (openAIFormat)
            {
                chatRequest.messages = chat;
            }
            else
            {
                chatRequest.prompt = currentPrompt + RoleMessageString(playerName, message) + RoleString(AIName);
            }
            chatRequest.temperature = temperature;
            chatRequest.top_k = topK;
            chatRequest.top_p = topP;
            chatRequest.min_p = minP;
            chatRequest.n_predict = numPredict;
            chatRequest.n_keep = nKeep;
            chatRequest.stream = stream;
            chatRequest.stop = stop;
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
            currentPrompt += RoleMessageString(role, content);
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
            return result.content;
        }

        public string MultiChatContent(MultiChatResult result)
        {
            // get content from a chat result received from the endpoint
            string response = "";
            foreach (ChatResult resultPart in result.data)
            {
                response += resultPart.content;
            }
            return response;
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
            string json = JsonUtility.ToJson(GenerateRequest(question));
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
            if (addToHistory)
            {
                AddPlayerMessage(question);
                AddAIMessage(result);
            }
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

        public async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            Ret result;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            using (var request = UnityWebRequest.Put($"{host}:{port}/{endpoint}", jsonToSend))
            {
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
                result = ConvertContent(request.downloadHandler.text, getContent);
                callback?.Invoke(result);
                if (request.result != UnityWebRequest.Result.Success) throw new System.Exception(request.error);
            }
            return result;
        }
    }
}
