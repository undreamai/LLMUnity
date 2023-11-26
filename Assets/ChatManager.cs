using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;
using System.Net.WebSockets;

public class ChatManager : MonoBehaviour
{
    public Transform chatContainer;
    public Color playerColor = new Color32(81, 164, 81, 255);
    public Color botColor = new Color32(29, 29, 73, 255);
    public Color fontColor = Color.white;
    public Font font;
    public int fontSize = 16;
    
    private TMP_InputField inputField;
    private GameObject inputBubble;
    private List<GameObject> chatBubbles = new List<GameObject>();

    void Start()
    {
        if (font == null) {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        inputBubble = CreateBubble("Message me", true, false, 600f, fontSize * 4);
        inputField = createInputField(inputBubble);
    }

    TMP_InputField createInputField(GameObject inputBubble){
        Transform textObject = inputBubble.transform.Find("Text");
        TextMeshProUGUI textArea = textObject.GetComponent<TextMeshProUGUI>();
        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();

        inputField = inputBubble.AddComponent<TMP_InputField>();
        inputField.interactable = true;
        inputField.textComponent = textArea;
        inputField.textViewport = textRectTransform;
        inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
        inputField.onSubmit.AddListener(onInputFieldSubmit);
        inputField.onFocusSelectAll = false;
        inputField.shouldHideMobileInput = false;
        inputField.enabled = false;
        inputField.enabled = true;
        return inputField;
    }

    void onInputFieldSubmit(string newText){
        inputField.ActivateInputField();
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)){
            inputField.text += "\n";
            inputField.caretPosition = inputField.text.Length;
            return;
        }
        CreateBubble(inputField.text, true);
        CreateBubble("...", false);
        inputField.text = "";
    }

    GameObject CreateBubble(string message, bool player, bool addToList=true, float width=-1f, float height=-1f, float padding = 10f)
    {
        Color bubbleColor;
        string bubbleName;
        float leftPosition;
        if (player){
            bubbleName = "PlayerBubble";
            bubbleColor = playerColor;
            leftPosition = 0f;
        }else{
            bubbleName = "BotBubble";
            bubbleColor = botColor;
            leftPosition = 1f;
        }
        // Create a new GameObject for the chat bubble
        GameObject newBubble = new GameObject(bubbleName, typeof(RectTransform), typeof(Image));
        newBubble.transform.SetParent(chatContainer);
        RectTransform bubbleRectTransform = newBubble.GetComponent<RectTransform>();
        Image bubbleImage = newBubble.GetComponent<Image>();

        bubbleImage.type = Image.Type.Sliced;
        bubbleImage.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bubbleImage.color = bubbleColor;

        // Create a child GameObject for the text
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(newBubble.transform);
        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
        TextMeshProUGUI textContent = textObject.GetComponent<TextMeshProUGUI>();
        // Add text and font
        textContent.text = message;
        // textContent.font = font;
        textContent.fontSize = fontSize;
        textContent.color = fontColor;

        // Set position and size and add to list
        float bubbleWidth = width >= 0? width: textContent.preferredWidth;
        float bubbleHeight = height >= 0? height: fontSize * message.Split('\n').Length;
        bubbleRectTransform.sizeDelta = new Vector2(bubbleWidth + 2 * padding, bubbleHeight + 2 * padding);
        SetTextPosition(textRectTransform, padding);
        SetBubblePosition(bubbleRectTransform, leftPosition);
        if (addToList) chatBubbles.Add(newBubble);
        return newBubble;
    }
    void SetTextPosition(RectTransform textRect, float padding = 10f)
    {
        textRect.pivot = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(padding, -padding);
    }
    void SetBubblePosition(RectTransform bubbleRect, float leftPosition)
    {
        // Set the position of the new bubble at the bottom
        bubbleRect.pivot = new Vector2(leftPosition, 0f);
        bubbleRect.anchorMin = new Vector2(leftPosition, 0f);
        bubbleRect.anchorMax = new Vector2(leftPosition, 0f);
        
        foreach (GameObject bubble in chatBubbles)
        {
            // RectTransform childRect = chatContainer.GetChild(i).GetComponent<RectTransform>();
            RectTransform childRect = bubble.GetComponent<RectTransform>();
            Vector2 currentPosition = childRect.localPosition;
            currentPosition.y += bubbleRect.sizeDelta.y + 10f;
            childRect.localPosition = currentPosition;
        }
        float y = 0f;
        if (inputField != null)
            y += inputField.GetComponent<RectTransform>().sizeDelta.y + 10f;
        bubbleRect.anchoredPosition = new Vector2(0f, y);
        
    }
}