using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class SimpleChat : MonoBehaviour
{
    public string apiUrl = "http://localhost:11434/api/generate";
    public string model = "llava-llama3-int4:latest";
    public TMP_InputField userInputField;
    public TextMeshProUGUI chatOutputText;
    public Button sendButton;
    public Main mainScript; // Referenz zum Main-Skript

    void Start()
    {
        if (userInputField == null || chatOutputText == null || sendButton == null || mainScript == null)
        {
            Debug.LogError("One or more components are not assigned in the Inspector");
            return;
        }

        sendButton.onClick.AddListener(() =>
        {
            string userInput = userInputField.text;
            if (!string.IsNullOrEmpty(userInput))
            {
                Texture2D imageToSend = mainScript.GetWebCamTextureAsTexture2D();
                if (imageToSend != null)
                {
                    StartCoroutine(SendChatRequestWithImage(userInput, imageToSend));
                }
                else
                {
                    StartCoroutine(SendChatRequest(userInput));
                }

                userInputField.text = ""; // Eingabefeld zurücksetzen
            }
        });
    }

    IEnumerator SendChatRequest(string userInput)
    {
        string json = $"{{\"model\":\"{model}\",\"prompt\":\"{userInput}\",\"stream\":false}}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Error: " + request.error);
            chatOutputText.text += $"\n[Error]: {request.error}";
        }
        else
        {
            ProcessResponse(request.downloadHandler.text);
        }
    }

    IEnumerator SendChatRequestWithImage(string userInput, Texture2D imageToSend)
    {
        string base64Image = ConvertImageToBase64(imageToSend);
        if (string.IsNullOrEmpty(base64Image))
        {
            chatOutputText.text += "\n[Error]: Unable to process image.";
            yield break;
        }

        string json = $"{{\"model\":\"{model}\",\"prompt\":\"{userInput}\",\"stream\":false,\"images\":[\"{base64Image}\"]}}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Error: " + request.error);
            chatOutputText.text += $"\n[Error]: {request.error}";
        }
        else
        {
            ProcessResponse(request.downloadHandler.text);
        }
    }

    string ConvertImageToBase64(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToJPG();
        return System.Convert.ToBase64String(imageBytes);
    }

    void ProcessResponse(string response)
    {
        try
        {
            chatOutputText.text += $"\n[AI]: {response}";
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error processing response: " + ex.Message);
            chatOutputText.text += $"\n[Error]: Unable to process AI response.";
        }
    }
}
