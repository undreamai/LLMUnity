using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;

public class ChatManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text displayText;
    public Transform chatContainer; // Parent transform for instantiated chat bubbles
    public Color bubbleColor = Color.blue; // Color for the chat bubble background
    private List<GameObject> chatBubbles = new List<GameObject>(); // List to store chat bubble GameObjects

    void Start()
    {
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
        // displayText.text += "\n" + inputField.text;
        SubmitMessage(inputField.text);
        inputField.text = "";
    }

    void SubmitMessage(string message)
    {
        // Create a new GameObject for the chat bubble
        GameObject newBubble = new GameObject("ChatBubble", typeof(RectTransform));

        // Set the parent to the chat container
        newBubble.transform.SetParent(chatContainer);

        // Add an Image component for the background
        Image bubbleImage = newBubble.AddComponent<Image>();
        bubbleImage.color = bubbleColor;

        // Set the type to Sliced for rounded corners
        bubbleImage.type = Image.Type.Sliced;

        bubbleImage.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");;


        // Create a child GameObject for the text
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(newBubble.transform);

        // Get references to the UI components in the instantiated bubble
        Text textContent = textObject.GetComponent<Text>();

        // Set the text content
        textContent.text = message;

        // Set the text properties (customize as needed)
        textContent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textContent.fontSize = 20;
        textContent.color = Color.white;

        // Example: Update the bubble size based on text content
        UpdateBubbleSize(newBubble.GetComponent<RectTransform>(), textObject.GetComponent<RectTransform>(), textContent);

        // Set the position of the new bubble at the bottom
        SetBubblePosition(newBubble.transform);

        // Add the new bubble to the list
        chatBubbles.Add(newBubble);
    }

    void UpdateBubbleSize(RectTransform bubbleTransform, RectTransform textRect, Text textContent, float padding = 10f)
    {
        // Adjust the size of the bubble based on text content
        float preferredWidth = textContent.preferredWidth;
        float preferredHeight = textContent.fontSize * textContent.text.Split('\n').Length;

        // Set the size of the bubble image
        bubbleTransform.sizeDelta = new Vector2(preferredWidth + 2 * padding, preferredHeight + 2 * padding);

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(padding, -padding);
    }

    void SetBubblePosition(Transform bubbleTransform)
    {
        // Set the position of the new bubble at the bottom
        RectTransform bubbleRect = bubbleTransform.GetComponent<RectTransform>();
        bubbleRect.pivot = new Vector2(1f, 0f); // Set pivot to the bottom center
        bubbleRect.anchorMin = new Vector2(1f, 0f); // Set anchor to the bottom center
        bubbleRect.anchorMax = new Vector2(1f, 0f); // Set anchor to the bottom center
        
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