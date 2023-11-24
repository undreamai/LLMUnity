using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;

public class ChatManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public Transform chatContainer;
    public Color playerColor = new Color32(81, 164, 81, 255);
    public Color botColor = new Color32(29, 29, 73, 255);
    public Color fontColor = Color.white;
    public Font font;
    public int fontSize = 16;
    
    private List<GameObject> chatBubbles = new List<GameObject>();

    void Start()
    {
        if (font == null) {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(onInputFieldSubmit);
        }
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

    void CreateBubble(string message, bool player)
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
        GameObject newBubble = new GameObject(bubbleName, typeof(RectTransform));

        // Set the parent to the chat container
        newBubble.transform.SetParent(chatContainer);

        // Add an Image component for the background
        Image bubbleImage = newBubble.AddComponent<Image>();
        bubbleImage.type = Image.Type.Sliced;
        bubbleImage.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bubbleImage.color = bubbleColor;

        // Create a child GameObject for the text
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(newBubble.transform);
        Text textContent = textObject.GetComponent<Text>();
        // Add text and font
        textContent.text = message;
        textContent.font = font;
        textContent.fontSize = fontSize;
        textContent.color = fontColor;

        // Set position and size and add to list
        UpdateBubbleSize(newBubble.GetComponent<RectTransform>(), textObject.GetComponent<RectTransform>(), textContent);
        SetBubblePosition(newBubble.transform, leftPosition);
        chatBubbles.Add(newBubble);
    }

    void UpdateBubbleSize(RectTransform bubbleTransform, RectTransform textRect, Text textContent, float padding = 10f)
    {
        // Adjust the size of the bubble based on text content
        float preferredWidth = textContent.preferredWidth;
        float preferredHeight = textContent.fontSize * textContent.text.Split('\n').Length;
        bubbleTransform.sizeDelta = new Vector2(preferredWidth + 2 * padding, preferredHeight + 2 * padding);

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(padding, -padding);
    }

    void SetBubblePosition(Transform bubbleTransform, float leftPosition)
    {
        // Set the position of the new bubble at the bottom
        RectTransform bubbleRect = bubbleTransform.GetComponent<RectTransform>();
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
        bubbleRect.anchoredPosition = new Vector2(0f, inputField.GetComponent<RectTransform>().sizeDelta.y + 10f);
        
    }
}