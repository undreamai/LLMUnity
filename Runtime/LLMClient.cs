using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMUnity
{
    public class ClientAttribute : PropertyAttribute {}
    public class ServerAttribute : PropertyAttribute {}
    public class ModelAttribute : PropertyAttribute {}
    public class ChatAttribute : PropertyAttribute {}
    public class ClientAdvancedAttribute : PropertyAttribute {}
    public class ServerAdvancedAttribute : PropertyAttribute {}
    public class ModelAdvancedAttribute : PropertyAttribute {}

    [DefaultExecutionOrder(-1)]
    public class LLMClient : MonoBehaviour
    {
        [HideInInspector] public bool advancedOptions = false;

        [ClientAdvanced] public string host = "localhost";
        [ServerAdvanced] public int port = 13333;
        [Server] public bool stream = true;

        [ModelAdvanced] public int seed = 0;
        [ModelAdvanced] public float temperature = 0.2f;
        [ModelAdvanced] public int topK = 40;
        [ModelAdvanced] public float topP = 0.9f;
        [ModelAdvanced] public int nPredict = 256;

        [Chat] public string playerName = "Human";
        [Chat] public string AIName = "Assistant";
        [TextArea(5, 10), Chat] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
        
        private int nKeep = -1;

        private string currentPrompt;
        private List<ChatMessage> chat;
        
        private List<(string, string)> requestHeaders;

        public LLMClient()
        {
            // initialise headers and chat lists
            requestHeaders = new List<(string, string)>{("Content-Type", "application/json")};
            chat = new List<ChatMessage>();
            chat.Add(new ChatMessage{role="system", content=prompt});
        }

        public async void Awake(){
            // initialise the prompt and set the keep tokens based on its length
            currentPrompt = prompt;
            await Tokenize(prompt, SetNKeep);
        }

        private string RoleString(string role){
            // role as a delimited string for the model
            return "\n### "+role+":";
        }

        private string RoleMessageString(string role, string message){
            // role and the role message
            return RoleString(role) + " " + message;
        }

        public ChatRequest GenerateRequest(string message, bool openAIFormat=false){
            // setup the request struct
            ChatRequest chatRequest = new ChatRequest();
            if (openAIFormat){
                chatRequest.messages = chat;
            }
            else{
                chatRequest.prompt = currentPrompt + RoleMessageString(playerName, message) + RoleString(AIName);
            }
            chatRequest.temperature = temperature;
            chatRequest.top_k = topK;
            chatRequest.top_p = topP;
            chatRequest.n_predict = nPredict;
            chatRequest.n_keep = nKeep;
            chatRequest.stream = stream;
            chatRequest.cache_prompt = true;
            if (seed != -1){
                chatRequest.seed = seed;
            }
            chatRequest.stop = new List<string>{RoleString(playerName), playerName + ":"};
            return chatRequest;
        }

        private void AddMessage(string role, string content){
            // add the question / answer to the chat list, update prompt
            chat.Add(new ChatMessage{role=role, content=content});
            currentPrompt += RoleMessageString(role, content);
        }

        private void AddPlayerMessage(string content){
            AddMessage(playerName, content);
        }

        private void AddAIMessage(string content){
            AddMessage(AIName, content);
        }

        public string ChatContent(ChatResult result){
            // get content from a chat result received from the endpoint
            return result.content;
        }

        public string MultiChatContent(MultiChatResult result){
            // get content from a chat result received from the endpoint
            string response = "";
            foreach (ChatResult resultPart in result.data){
                response += resultPart.content;
            }
            return response;
        }

        public string ChatOpenAIContent(ChatOpenAIResult result){
            // get content from a char result received from the endpoint in open AI format
            return result.choices[0].message.content;
        }

        public List<int> TokenizeContent(TokenizeResult result){
            // get the tokens from a tokenize result received from the endpoint
            return result.tokens;
        }

        public async Task Chat(string question, Callback<string> callback=null, EmptyCallback completionCallback=null, bool addToHistory=true)
        {
            // handle a chat message by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received
            string json = JsonUtility.ToJson(GenerateRequest(question));
            string result;
            if (stream) {
                result = await PostRequest<MultiChatResult, string>(json, "completion", MultiChatContent, callback);
            } else {
                result = await PostRequest<ChatResult, string>(json, "completion", ChatContent, callback);
            }
            completionCallback?.Invoke();
            if (addToHistory) {
                AddPlayerMessage(question);
                AddAIMessage(result);
            }
        }

        public async Task Warmup(EmptyCallback completionCallback=null, string question="hi")
        {
            await Chat(question, null, completionCallback, false);
        }

        public async Task Tokenize(string question, Callback<List<int>> callback=null)
        {
            // handle the tokenization of a message by the user
            TokenizeRequest tokenizeRequest = new TokenizeRequest();
            tokenizeRequest.content = question;
            string json = JsonUtility.ToJson(tokenizeRequest);
            await PostRequest<TokenizeResult, List<int>>(json, "tokenize", TokenizeContent, callback);
        }

        private void SetNKeep(List<int> tokens){
            // set the tokens to keep
            nKeep = tokens.Count;
        }

        public Ret ConvertContent<Res, Ret>(string response, ContentCallback<Res, Ret> getContent=null){
            // template function to convert the json received and get the content
            response = response.Trim();
            if (response.StartsWith("data: ")){
                string responseArray = "";
                foreach (string responsePart in response.Replace("\n\n", "").Split("data: ")){
                    if (responsePart == "") continue;
                    if (responseArray != "") responseArray += ",\n";
                    responseArray += responsePart;
                }
                response = $"{{\"data\": [{responseArray}]}}";
            }
            return getContent(JsonUtility.FromJson<Res>(response));
        }

        public string[] MultiResponse(string response){
            return response.Trim().Replace("\n\n", "").Split("data: ");
        }

        public async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback=null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            Ret result;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            using (var request = UnityWebRequest.Put($"{host}:{port}/{endpoint}", jsonToSend))
            {
                request.method = "POST";
                if (requestHeaders != null){
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