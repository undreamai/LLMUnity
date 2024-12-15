using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OllamaTest : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(TestOllamaAPI());
        StartCoroutine(TestOllamaAPIWithPost());
    }

    IEnumerator TestOllamaAPI()
    {
        UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:11434/api/tags");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Error: " + request.error);
        }
        else
        {
            Debug.Log("API Response: " + request.downloadHandler.text);
        }
    }

    IEnumerator TestOllamaAPIWithPost()
    {
        string json = "{\"model\":\"llava-llama3-int4:latest\",\"prompt\":\"Hello, Unity!\"}";
        UnityWebRequest request = UnityWebRequest.PostWwwForm("http://127.0.0.1:11434/api/generate", "application/json");
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Error: " + request.error);
        }
        else
        {
            Debug.Log("API Response: " + request.downloadHandler.text);
        }
    }
}
