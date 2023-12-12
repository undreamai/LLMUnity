using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

class Bubble {
    protected GameObject bubbleObject;
    protected RectTransform bubbleRectTransform;
    protected Image bubbleImage;
    protected GameObject paddingObject;
    protected RectTransform paddingRectTransform;
    protected GameObject textObject;
    protected RectTransform textRectTransform;
    protected TextMeshProUGUI textContent;
    protected float bubbleWidth;
    protected float bubbleHeight;
    protected float textPadding;
    protected float bubbleSpacing;

    public Bubble(Transform parent, Sprite sprite, TMP_FontAsset font, int fontSize, Color fontColor, string bubbleName, Color bubbleColor, float bottomPosition, float leftPosition, string message, float padding=10f, float spacing=10f, float width=-1f, float height=-1f)
    {
        bubbleWidth = width;
        bubbleHeight = height;
        textPadding = padding;
        bubbleSpacing = spacing;
        AddBubbleObject(parent, sprite, bubbleName, bubbleColor);
        AddPaddingObject();
        AddTextObject(font, fontSize, fontColor, message);
        SetBubblePosition(bottomPosition, leftPosition);
        UpdateSize();
    }

    void AddBubbleObject(Transform parent, Sprite sprite, String bubbleName, Color bubbleColor){
        // Create a new GameObject for the chat bubble
        bubbleObject = new GameObject(bubbleName, typeof(RectTransform), typeof(Image));
        bubbleObject.transform.SetParent(parent);
        bubbleRectTransform = bubbleObject.GetComponent<RectTransform>();
        bubbleImage = bubbleObject.GetComponent<Image>();

        bubbleImage.type = Image.Type.Sliced;
        bubbleImage.sprite = sprite;
        bubbleImage.color = bubbleColor;
    }

    void AddPaddingObject(){
        // Add an object for alignment
        paddingObject = new GameObject("paddingObject");
        paddingRectTransform = paddingObject.AddComponent<RectTransform>();
        paddingObject.transform.SetParent(bubbleObject.transform);
    }

    protected GameObject CreateTextObject(Transform parent, TMP_FontAsset font, int fontSize, Color fontColor, string message){
        // Create a child GameObject for the text
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent);
        TextMeshProUGUI textContent = textObject.GetComponent<TextMeshProUGUI>();
        // Add text and font
        textContent.text = message;
        textContent.font = font;
        textContent.fontSize = fontSize;
        textContent.color = fontColor;
        return textObject;
    }

    void AddTextObject(TMP_FontAsset font, int fontSize, Color fontColor, string message) {
        textObject = CreateTextObject(paddingObject.transform, font, fontSize, fontColor, message);
        textContent = textObject.GetComponent<TextMeshProUGUI>();
        textRectTransform = textObject.GetComponent<RectTransform>();
    }
 
    void SetBubblePosition(float bottomPosition, float leftPosition)
    {
        // Set the position of the new bubble at the bottom
        bubbleRectTransform.pivot = new Vector2(leftPosition, bottomPosition);
        bubbleRectTransform.anchorMin = new Vector2(leftPosition, bottomPosition);
        bubbleRectTransform.anchorMax = new Vector2(leftPosition, bottomPosition);
        Vector2 anchoredPosition = new Vector2(bubbleSpacing, bubbleSpacing);
        if (leftPosition == 1) anchoredPosition.x *= -1;
        if (bottomPosition == 1) anchoredPosition.y *= -1;
        bubbleRectTransform.anchoredPosition = anchoredPosition;
    }

    public void UpdateSize(){
        // Set position and size of bubble
        textContent.ForceMeshUpdate();
        Vector2 messageSize = textContent.GetRenderedValues(false);
        float width = bubbleWidth >= 0? bubbleWidth: messageSize.x;
        float height = bubbleHeight >= 0? bubbleHeight: messageSize.y;
        bubbleRectTransform.sizeDelta = new Vector2(width + 2 * textPadding, height + 2 * textPadding);

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
    protected TMP_InputField inputField ;
    protected GameObject placeholderObject;
    protected TextMeshProUGUI placeholderContent;
    protected RectTransform placeholderRectTransform;

    public InputBubble(Transform parent, Sprite sprite, TMP_FontAsset font, int fontSize, Color fontColor, string bubbleName, Color bubbleColor, float bottomPosition, float leftPosition, string message, float padding=10f, float spacing=10f, float width=-1f, int lineHeight=4) : 
    base(parent, sprite, font, fontSize, fontColor, bubbleName, bubbleColor, bottomPosition, leftPosition, addNewLines(message, lineHeight), padding, spacing, width, -1f)
    {
        AddInputField();
        AddPlaceholderObject(font, fontSize, fontColor, message);
        FixCaret();
    }

    static string addNewLines(string message, int lineHeight){
        string messageLines = message;
        for (int i = 0; i <= lineHeight-1; i++)
            messageLines += "\n";
        return messageLines;
    }

    void AddPlaceholderObject(TMP_FontAsset font, int fontSize, Color fontColor, string message){
        // Create a child GameObject for the placeholder text
        GameObject placeholderObject = CreateTextObject(paddingObject.transform, font, fontSize, fontColor, message);
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
