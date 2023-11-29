using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;
using System.Text;
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
            return;
        }
        // replace vertical_tab
        string message = inputField.text.Replace("\v", "\n");
        CreateBubble(message, true);
        CreateBubble("...", false);
        inputField.text = "";
    }
    void onValueChanged(string newText){
        // Get rid of newline character added when we press enter
        if (Input.GetKey(KeyCode.Return)){
            if(inputField.text == "\n")
                inputField.text = "";
        }
    }

    GameObject CreateBubble(string message, bool player, bool addToList=true, float width=-1f, float height=-1f)
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

        // Add an object for alignment
        GameObject paddingObject = new GameObject("paddingObject");
        RectTransform paddingRectTransform = paddingObject.AddComponent<RectTransform>();
        paddingObject.transform.SetParent(newBubble.transform);

        // Create a child GameObject for the text
        GameObject textObject = CreateTextObject(message);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textObject.transform.SetParent(paddingObject.transform);

        // Set position and size of bubble
        float padding = 10f;
        float bubbleWidth = width >= 0? width: textObject.GetComponent<TextMeshProUGUI>().preferredWidth;
        float bubbleHeight = height >= 0? height: fontSize * message.Split('\n').Length;
        bubbleRectTransform.sizeDelta = new Vector2(bubbleWidth + 2 * padding, bubbleHeight + 2 * padding);
        SetBubblePosition(bubbleRectTransform, leftPosition);

        // Set position and size of the components
        paddingRectTransform.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
        textRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
        textRect.anchoredPosition = Vector2.zero;

        if (addToList) chatBubbles.Add(newBubble);
        return newBubble;
    }

    TMP_InputField CreateInputField(string message, float width=600, float lineHeight=4){
        GameObject newBubble = CreateBubble(message, true, false, width, fontSize*lineHeight);
        TMP_InputField inputField = newBubble.AddComponent<TMP_InputField>();
        Transform paddingObject = newBubble.transform.Find("paddingObject");
        Transform textObject = paddingObject.transform.Find("Text");

        // Set up the input field parameters
        inputField.interactable = true;
        inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
        inputField.onSubmit.AddListener(onInputFieldSubmit);
        inputField.onValueChanged.AddListener(onValueChanged);
        inputField.onFocusSelectAll = false;
        inputField.shouldHideMobileInput = false;
        inputField.textComponent = textObject.GetComponent<TextMeshProUGUI>();
        inputField.textViewport = textObject.GetComponent<RectTransform>();

        // Create a child GameObject for the placeholder text
        GameObject placeholderObject = CreateTextObject(message);
        placeholderObject.transform.SetParent(paddingObject.transform);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.sizeDelta = inputField.textViewport.sizeDelta;
        placeholderRect.anchoredPosition = inputField.textViewport.anchoredPosition;
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