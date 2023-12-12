using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

struct BubbleUI {
    public Sprite sprite;
    public TMP_FontAsset font;
    public int fontSize;
    public Color fontColor;
    public Color bubbleColor;
    public float bottomPosition;
    public float leftPosition;
    public float textPadding;
    public float bubbleSpacing;
    public float bubbleWidth;
    public float bubbleHeight;
}

class Bubble {
    protected GameObject bubbleObject;
    protected RectTransform bubbleRectTransform;
    protected Image bubbleImage;
    protected GameObject paddingObject;
    protected RectTransform paddingRectTransform;
    protected GameObject textObject;
    protected RectTransform textRectTransform;
    protected TextMeshProUGUI textContent;
    public BubbleUI bubbleUI;

    public Bubble(Transform parent, BubbleUI ui, string name, string message)
    {
        bubbleUI = ui;
        AddBubbleObject(parent, name);
        AddPaddingObject();
        AddTextObject(message);
        SetBubblePosition();
        UpdateSize();
    }

    void AddBubbleObject(Transform parent, String bubbleName){
        // Create a new GameObject for the chat bubble
        bubbleObject = new GameObject(bubbleName, typeof(RectTransform), typeof(Image));
        bubbleObject.transform.SetParent(parent);
        bubbleRectTransform = bubbleObject.GetComponent<RectTransform>();
        bubbleImage = bubbleObject.GetComponent<Image>();

        bubbleImage.type = Image.Type.Sliced;
        bubbleImage.sprite = bubbleUI.sprite;
        bubbleImage.color = bubbleUI.bubbleColor;
    }

    void AddPaddingObject(){
        // Add an object for alignment
        paddingObject = new GameObject("paddingObject");
        paddingRectTransform = paddingObject.AddComponent<RectTransform>();
        paddingObject.transform.SetParent(bubbleObject.transform);
    }

    protected GameObject CreateTextObject(Transform parent, string message){
        // Create a child GameObject for the text
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent);
        TextMeshProUGUI textContent = textObject.GetComponent<TextMeshProUGUI>();
        // Add text and font
        textContent.text = message;
        textContent.font = bubbleUI.font;
        textContent.fontSize = bubbleUI.fontSize;
        textContent.color = bubbleUI.fontColor;
        return textObject;
    }

    void AddTextObject(string message) {
        textObject = CreateTextObject(paddingObject.transform, message);
        textContent = textObject.GetComponent<TextMeshProUGUI>();
        textRectTransform = textObject.GetComponent<RectTransform>();
    }
 
    void SetBubblePosition()
    {
        // Set the position of the new bubble at the bottom
        bubbleRectTransform.pivot = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
        bubbleRectTransform.anchorMin = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
        bubbleRectTransform.anchorMax = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
        Vector2 anchoredPosition = new Vector2(bubbleUI.bubbleSpacing, bubbleUI.bubbleSpacing);
        if (bubbleUI.leftPosition == 1) anchoredPosition.x *= -1;
        if (bubbleUI.bottomPosition == 1) anchoredPosition.y *= -1;
        bubbleRectTransform.anchoredPosition = anchoredPosition;
    }

    public void UpdateSize(){
        // Set position and size of bubble
        textContent.ForceMeshUpdate();
        Vector2 messageSize = textContent.GetRenderedValues(false);
        float width = bubbleUI.bubbleWidth >= 0? bubbleUI.bubbleWidth: messageSize.x;
        float height = bubbleUI.bubbleHeight >= 0? bubbleUI.bubbleHeight: messageSize.y;
        bubbleRectTransform.sizeDelta = new Vector2(width + 2 * bubbleUI.textPadding, height + 2 * bubbleUI.textPadding);

        // Set position and size of the components
        paddingRectTransform.sizeDelta = new Vector2(width, height);
        textRectTransform.sizeDelta = new Vector2(width, height);
        textRectTransform.anchoredPosition = Vector2.zero;
    }

    public RectTransform GetRectTransform(){
        return bubbleRectTransform;
    }
    public string GetText(){
        return textContent.text;
    }
    public void SetText(string text){
        textContent.text = text;
    }

    public void Destroy(){
        UnityEngine.Object.Destroy(bubbleObject);
    }
}


class InputBubble : Bubble {
    protected TMP_InputField inputField;
    protected GameObject placeholderObject;
    protected TextMeshProUGUI placeholderContent;
    protected RectTransform placeholderRectTransform;

    public InputBubble(Transform parent, BubbleUI ui, string name, string message, int lineHeight=4) : 
    base(parent, ui, name, addNewLines(message, lineHeight))
    {
        AddInputField();
        AddPlaceholderObject(message);
        FixCaret();
    }

    static string addNewLines(string message, int lineHeight){
        string messageLines = message;
        for (int i = 0; i <= lineHeight-1; i++)
            messageLines += "\n";
        return messageLines;
    }

    void AddPlaceholderObject(string message){
        // Create a child GameObject for the placeholder text
        GameObject placeholderObject = CreateTextObject(paddingObject.transform, message);
        placeholderContent = placeholderObject.GetComponent<TextMeshProUGUI>();
        placeholderRectTransform = placeholderObject.GetComponent<RectTransform>();

        placeholderObject.transform.SetParent(paddingObject.transform);
        placeholderRectTransform.sizeDelta = inputField.textViewport.sizeDelta;
        placeholderRectTransform.anchoredPosition = inputField.textViewport.anchoredPosition;
        inputField.placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
    }

    void AddInputField(){
        // Create the input field GameObject
        inputField = bubbleObject.AddComponent<TMP_InputField>();
        inputField.interactable = true;
        inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
        inputField.onFocusSelectAll = false;
        inputField.shouldHideMobileInput = false;
        inputField.textComponent = textObject.GetComponent<TextMeshProUGUI>();
        inputField.textViewport = textObject.GetComponent<RectTransform>();
    }

    public void FixCaret(){
        // disable and re-enable the inputField because otherwise caret doesn't appear (unity bug)
        inputField.enabled = false;
        inputField.enabled = true;
        // inputField.ActivateInputField();
    }

    public void AddSubmitListener(UnityEngine.Events.UnityAction<string> onInputFieldSubmit){
        inputField.onSubmit.AddListener(onInputFieldSubmit);
    }
    public void AddValueChangedListener(UnityEngine.Events.UnityAction<string> onValueChanged){
        inputField.onValueChanged.AddListener(onValueChanged);
    }

    public new string GetText(){
        return inputField.text;
    }
    public new void SetText(string text){
        inputField.text = text;
        inputField.caretPosition = inputField.text.Length;
    }
    public void ActivateInputField(){
        inputField.ActivateInputField();
    } 
    public void ReActivateInputField(){
        inputField.DeactivateInputField();
        inputField.Select();
        inputField.ActivateInputField();
    }        
}

class BubbleTextSetter {
    ChatManager chatManager;
    Bubble bubble;
    public BubbleTextSetter(ChatManager chatManager, Bubble bubble){
        this.chatManager = chatManager;
        this.bubble = bubble;
    }

    public void SetText(string text){
        bubble.SetText(text);
        bubble.UpdateSize();
        chatManager.UpdateBubblePositions();
        chatManager.AllowInput();
    }
}
