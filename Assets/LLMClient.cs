using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LLMClient : MonoBehaviour
{
    public string host = "localhost";
    public int port = 13333;
    public string player_name = "Human";
    public string ai_name = "Assistant";
    public string prompt = "A chat between a curious human and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the human's questions.";
    public int seed = -1;
    public float temperature = 0.2f;
    public int top_k = 40;
    public float top_p = 0.9f;
    public int n_predict = 256;
    public int n_keep = 30;

    private string currentPrompt;
    private List<(string, string)> chat;
    
    private List<(string, string)> requestHeaders;
    public delegate void CallbackDelegate(string message);

    public LLMClient()
    {
        requestHeaders = new List<(string, string)>{("Content-Type", "application/json")};
        chat = new List<(string, string)>();
    }

    public void OnEnable(){
        currentPrompt = prompt;
    }

    private string RoleString(string role){
        return "\n### "+role+":";
    }
    private string RoleMessageString(string role, string message){
        return RoleString(role) + " " + message;
    }

    public ChatRequest GenerateRequest(string message){        
        ChatRequest chatRequest = new ChatRequest();
        chatRequest.prompt = currentPrompt + RoleMessageString(player_name, message) + RoleString(ai_name);
        chatRequest.temperature = temperature;
        chatRequest.top_k = top_k;
        chatRequest.top_p = top_p;
        chatRequest.n_predict = n_predict;
        chatRequest.n_keep = n_keep;
        chatRequest.stream = false;
        if (seed != -1)
            chatRequest.seed = seed;
        chatRequest.stop = new List<string>{RoleString(player_name)};
        return chatRequest;
    }

    private void AddQA(string question, string answer){
        foreach ((string role, string message) in new[] { (player_name, question), (ai_name, answer) })
        {
            chat.Add((role, message));
            currentPrompt += RoleMessageString(role, message);
        }
    }
    public async void Chat(string question, CallbackDelegate callback)
    {
        string requestJson = JsonUtility.ToJson(GenerateRequest(question));
        string response = await PostRequest(requestJson);
        if (response == null) return;
        var responseJson = JsonUtility.FromJson<ChatResult>(response);
        string answer = responseJson.content.Trim();
        callback.Invoke(answer);
        AddQA(question, answer);
    }

    public async Task<string> PostRequest(string json)
    {
        UnityWebRequest webRequest = new UnityWebRequest($"{host}:{port}/completion", "POST");
        if (requestHeaders != null){
            for (int i = 0; i < requestHeaders.Count; i++){
                webRequest.SetRequestHeader(requestHeaders[i].Item1, requestHeaders[i].Item2);
            }
        }
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        webRequest.disposeDownloadHandlerOnDispose = true;
        webRequest.disposeUploadHandlerOnDispose = true;

        await webRequest.SendWebRequest();

        switch (webRequest.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("Error: " + webRequest.error);
                break;
            case UnityWebRequest.Result.Success:
                string responseString = webRequest.downloadHandler.text;
                webRequest.Dispose();
                return responseString;
        }
        webRequest.Dispose();
        return null;
    }
}