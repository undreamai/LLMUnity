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
    private List<GameObject> chatBubbles = new List<GameObject>();

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputField = CreateInputField("Message me", 600, 4);
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
        GameObject textObject = CreateTextObject(message);
        textObject.transform.SetParent(newBubble.transform);

        // Set position and size of bubble
        float bubbleWidth = width >= 0? width: textObject.GetComponent<TextMeshProUGUI>().preferredWidth;
        float bubbleHeight = height >= 0? height: fontSize * message.Split('\n').Length;
        bubbleRectTransform.sizeDelta = new Vector2(bubbleWidth + 2 * padding, bubbleHeight + 2 * padding);
        SetBubblePosition(bubbleRectTransform, leftPosition);

        // Set position and size of text object
        SetTextPosition(textObject.GetComponent<RectTransform>(), padding);
        if (addToList) chatBubbles.Add(newBubble);
        return newBubble;
    }

    TMP_InputField CreateInputField(string message, float width=600, float lineHeight=4, float padding = 10f){
        GameObject newBubble = CreateBubble(message, true, false, width, fontSize*lineHeight, padding);
        TMP_InputField inputField = newBubble.AddComponent<TMP_InputField>();
        Transform textObject = newBubble.transform.Find("Text");

        // Create a child GameObject for the placeholder text
        GameObject placeholderObject = CreateTextObject(message);
        placeholderObject.transform.SetParent(newBubble.transform);
        SetTextPosition(placeholderObject.GetComponent<RectTransform>(), padding);

        // Set up the input field parameters
        inputField.interactable = true;
        inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
        inputField.onSubmit.AddListener(onInputFieldSubmit);
        inputField.onFocusSelectAll = false;
        inputField.shouldHideMobileInput = false;
        inputField.textComponent = textObject.GetComponent<TextMeshProUGUI>();
        inputField.textViewport = textObject.GetComponent<RectTransform>();
        inputField.placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();

        // disable and re-enable the inputField because otherwise caret doesn't appear (unity bug)
        inputField.enabled = false;
        inputField.enabled = true;
        inputField.ActivateInputField();
        return inputField;
    }

    GameObject CreateTextObject(string message){
        // Create a child GameObject for the text
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
        TextMeshProUGUI textContent = textObject.GetComponent<TextMeshProUGUI>();
        // Add text and font
        textContent.text = message;
        // textContent.font = font;
        textContent.fontSize = fontSize;
        textContent.color = fontColor;
        return textObject;
    }

    void SetTextPosition(RectTransform textRect, float padding = 10f)
    {
        textRect.pivot = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        textRect.sizeDelta = new Vector2(-2 * padding, -padding);
        textRect.anchoredPosition = new Vector2(padding, 0);
        
        // textRect.sizeDelta = new Vector2(-2 * padding, -2 * padding);
        // textRect.anchoredPosition = new Vector2(padding, 2*padding);
        // textRect.sizeDelta = new Vector2(-2 * padding, -padding);
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