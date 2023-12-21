using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ClientAttribute : PropertyAttribute {}
public class ServerAttribute : PropertyAttribute {}
public class ModelAttribute : PropertyAttribute {}
public class ChatAttribute : PropertyAttribute {}

public class LLMClient : MonoBehaviour
{   
    [ClientAttribute] public string host = "localhost";
    [ServerAttribute] public int port = 13333;
    [ServerAttribute] public bool stream = true;

    [ChatAttribute] public string playerName = "Human";
    [ChatAttribute] public string AIName = "Assistant";
    [TextArea(5, 10), ChatAttribute] public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
    
    [ModelAttribute] public int seed = 0;
    [ModelAttribute] public float temperature = 0.2f;
    [ModelAttribute] public int topK = 40;
    [ModelAttribute] public float topP = 0.9f;
    [ModelAttribute] public int nPredict = 256;
    private int nKeep = -1;

    private string currentPrompt;
    private List<ChatMessage> chat;
    
    private List<(string, string)> requestHeaders;
    public delegate void EmptyCallback();
    public delegate void Callback<T>(T message);
    public delegate T2 ContentCallback<T, T2>(T message);

    public LLMClient()
    {
        // initialise headers and chat lists
        requestHeaders = new List<(string, string)>{("Content-Type", "application/json")};
        chat = new List<ChatMessage>();
        chat.Add(new ChatMessage{role="system", content=prompt});
    }

    public async void OnEnable(){
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

    private void AddQA(string question, string answer){
        // add the question and answer to the chat list, update prompt
        foreach ((string role, string content) in new[] { (playerName, question), (AIName, answer) })
        {
            chat.Add(new ChatMessage{role=role, content=content});
            currentPrompt += RoleMessageString(role, content);
        }
    }

    public string ChatContent(ChatResult result){
        // get content from a chat result received from the endpoint
        return result.content;
    }

    public string ChatContentTrim(ChatResult result){
        // get content from a chat result received from the endpoint and trim
        return ChatContent(result).Trim();
    }

    public string ChatOpenAIContent(ChatOpenAIResult result){
        // get content from a char result received from the endpoint in open AI format
        return result.choices[0].message.content;
    }

    public List<int> TokenizeContent(TokenizeResult result){
        // get the tokens from a tokenize result received from the endpoint
        return result.tokens;
    }

    public async Task<string> Chat(string question, Callback<string> callback=null, EmptyCallback completionCallback=null)
    {
        // handle a chat message by the user
        // call the callback function while the answer is received
        // call the completionCallback function when the answer is fully received
        string json = JsonUtility.ToJson(GenerateRequest(question));
        string result;
        if (stream) result = await PostRequestStream<ChatResult>(json, "completion", ChatContent, callback);
        else result = await PostRequest<ChatResult, string>(json, "completion", ChatContentTrim, callback);
        if (completionCallback != null) completionCallback();
        AddQA(question, result);
        return result;
    }

    public async Task<string> ChatOpenAI(string question, Callback<string> callback=null, EmptyCallback completionCallback=null)
    {
        // handle a chat message by the user in open AI style
        // call the callback function while the answer is received
        // call the completionCallback function when the answer is fully received
        chat.Add(new ChatMessage{role="user", content=question});
        string json = JsonUtility.ToJson(GenerateRequest(question, true));
        string result;
        if (stream) result = await PostRequestStream<ChatOpenAIResult>(json, "v1/chat/completions", ChatOpenAIContent, callback);
        else result = await PostRequest<ChatOpenAIResult, string>(json, "v1/chat/completions", ChatOpenAIContent, callback);
        if (completionCallback != null) completionCallback();
        chat.Add(new ChatMessage{role="assistant", content=result});
        return result;
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
        if (getContent == null){
            if (typeof(Res) != typeof(Ret)){
                throw new System.Exception("Res and Ret must be the same type without a getContent callback.");
            } else {
                return JsonUtility.FromJson<Ret>(response);
            }
        } else {
            return getContent(JsonUtility.FromJson<Res>(response));
        }
    }

    public async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent=null, Callback<Ret> callback=null)
    {
        // send a post request to the server and call the relevant callbacks to convert the received content and handle it
        // this function doesn't have streaming functionality i.e. handles the answer when it is fully received
        UnityWebRequest request = new UnityWebRequest($"{host}:{port}/{endpoint}", "POST");
        if (requestHeaders != null){
            for (int i = 0; i < requestHeaders.Count; i++){
                request.SetRequestHeader(requestHeaders[i].Item1, requestHeaders[i].Item2);
            }
        }
        byte[] payload = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(payload);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.disposeDownloadHandlerOnDispose = true;
        request.disposeUploadHandlerOnDispose = true;

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success) throw new System.Exception(request.error);
        Ret result = ConvertContent(request.downloadHandler.text, getContent);
        if (callback!=null) callback(result);
        return result;
    }

    public async Task<string> PostRequestStream<Res>(string json, string endpoint, ContentCallback<Res, string> getContent, Callback<string> callback)
    {
        // send a post request to the server and call the relevant callbacks to convert the received content and handle it
        // this function has streaming functionality i.e. handles the answer while it is being received
        string answer = "";
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
            int seenLines = 0;
            // Continue updating progress until the request is completed
            while (!asyncOperation.isDone)
            {
                float currentProgress = request.downloadProgress;
                // Check if progress has changed
                if (currentProgress != lastProgress)
                {
                    string[] responses = request.downloadHandler.text.Trim().Replace("\n\n", "").Split("data: ");
                    for (int i =seenLines+1; i<responses.Length; i++){
                        string answer_part = ConvertContent(responses[i], getContent);
                        if (answer_part!= null){
                            if (answer == "") answer_part = answer_part.TrimStart();
                            answer += answer_part;
                            if (callback != null) callback(answer);
                        }
                    }
                    seenLines = responses.Length -1;
                    lastProgress = currentProgress;
                }
                // Wait for the next frame
                await Task.Yield();
            }
            if (request.result != UnityWebRequest.Result.Success) throw new System.Exception(request.error);
        }
        return answer;
    }
}