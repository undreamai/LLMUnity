using UnityEngine;
using UnityEngine.UI;
using System;
using LLMUnity;

namespace LLMUnitySamples
{
    struct BubbleUI
    {
        public Sprite sprite;
        public Font font;
        public int fontSize;
        public Color fontColor;
        public Color bubbleColor;
        public float bottomPosition;
        public float leftPosition;
        public float textPadding;
        public float bubbleOffset;
        public float bubbleWidth;
        public float bubbleHeight;
    }

    public class RectTransformResizeHandler : MonoBehaviour
    {
        EmptyCallback callback;

        public void SetCallBack(EmptyCallback callback)
        {
            this.callback = callback;
        }

        void OnRectTransformDimensionsChange()
        {
            callback?.Invoke();
        }
    }

    class Bubble
    {
        protected GameObject bubbleObject;
        protected GameObject imageObject;
        public BubbleUI bubbleUI;

        public Bubble(Transform parent, BubbleUI ui, string name, string message)
        {
            bubbleUI = ui;
            bubbleObject = CreateTextObject(parent, name, message, bubbleUI.bubbleWidth == -1, bubbleUI.bubbleHeight == -1);
            imageObject = CreateImageObject(bubbleObject.transform, "Image");
            SetBubblePosition(bubbleObject.GetComponent<RectTransform>(), imageObject.GetComponent<RectTransform>(), bubbleUI);
            SetSortingOrder(bubbleObject, imageObject);
        }

        public void SyncParentRectTransform(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        protected GameObject CreateTextObject(Transform parent, string name, string message, bool horizontalStretch = true, bool verticalStretch = false)
        {
            // Create a child GameObject for the text
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Canvas));
            textObject.transform.SetParent(parent);
            Text textContent = textObject.GetComponent<Text>();

            if (verticalStretch || horizontalStretch)
            {
                ContentSizeFitter contentSizeFitter = textObject.AddComponent<ContentSizeFitter>();
                if (verticalStretch) contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                if (horizontalStretch) contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            // Add text and font
            textContent.text = message;
            if (bubbleUI.font != null)
                textContent.font = bubbleUI.font;
            textContent.fontSize = bubbleUI.fontSize;
            textContent.color = bubbleUI.fontColor;
            return textObject;
        }

        protected GameObject CreateImageObject(Transform parent, string name)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Canvas));
            imageObject.transform.SetParent(parent);
            RectTransform imageRectTransform = imageObject.GetComponent<RectTransform>();
            Image bubbleImage = imageObject.GetComponent<Image>();

            bubbleImage.type = Image.Type.Sliced;
            bubbleImage.sprite = bubbleUI.sprite;
            bubbleImage.color = bubbleUI.bubbleColor;
            return imageObject;
        }

        void SetBubblePosition(RectTransform bubbleRectTransform, RectTransform imageRectTransform, BubbleUI bubbleUI)
        {
            // Set the position of the new bubble at the bottom
            bubbleRectTransform.pivot = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
            bubbleRectTransform.anchorMin = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
            bubbleRectTransform.anchorMax = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
            bubbleRectTransform.localScale = Vector3.one;
            Vector2 anchoredPosition = new Vector2(bubbleUI.bubbleOffset + bubbleUI.textPadding, bubbleUI.bubbleOffset + bubbleUI.textPadding);
            if (bubbleUI.leftPosition == 1) anchoredPosition.x *= -1;
            if (bubbleUI.bottomPosition == 1) anchoredPosition.y *= -1;
            bubbleRectTransform.anchoredPosition = anchoredPosition;

            float width = bubbleUI.bubbleWidth == -1 ? bubbleRectTransform.sizeDelta.x : bubbleUI.bubbleWidth;
            float height = bubbleUI.bubbleHeight == -1 ? bubbleRectTransform.sizeDelta.y : bubbleUI.bubbleHeight;
            bubbleRectTransform.sizeDelta = new Vector2(width - 2 * bubbleUI.textPadding, height - 2 * bubbleUI.textPadding);
            SyncParentRectTransform(imageRectTransform);
            imageRectTransform.offsetMin = new Vector2(-bubbleUI.textPadding, -bubbleUI.textPadding);
            imageRectTransform.offsetMax = new Vector2(bubbleUI.textPadding, bubbleUI.textPadding);
        }

        void SetSortingOrder(GameObject bubbleObject, GameObject imageObject)
        {
            // Set the sorting order to make bubbleObject render behind textObject
            Canvas bubbleCanvas = bubbleObject.GetComponent<Canvas>();
            bubbleCanvas.overrideSorting = true;
            bubbleCanvas.sortingOrder = 2;
            Canvas imageCanvas = imageObject.GetComponent<Canvas>();
            imageCanvas.overrideSorting = true;
            imageCanvas.sortingOrder = 1;
        }

        public void OnResize(EmptyCallback callback)
        {
            RectTransformResizeHandler resizeHandler = bubbleObject.AddComponent<RectTransformResizeHandler>();
            resizeHandler.SetCallBack(callback);
        }

        public RectTransform GetRectTransform()
        {
            return bubbleObject.GetComponent<RectTransform>();
        }

        public RectTransform GetOuterRectTransform()
        {
            return imageObject.GetComponent<RectTransform>();
        }

        public Vector2 GetSize()
        {
            return bubbleObject.GetComponent<RectTransform>().sizeDelta + imageObject.GetComponent<RectTransform>().sizeDelta;
        }

        public string GetText()
        {
            return bubbleObject.GetComponent<Text>().text;
        }

        public void SetText(string text)
        {
            bubbleObject.GetComponent<Text>().text = text;
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(bubbleObject);
        }
    }

    class InputBubble : Bubble
    {
        protected GameObject inputFieldObject;
        protected InputField inputField;
        protected GameObject placeholderObject;

        public InputBubble(Transform parent, BubbleUI ui, string name, string message, int lineHeight = 4) :
        base(parent, ui, name, emptyLines(message, lineHeight))
        {
            Text textObjext = bubbleObject.GetComponent<Text>();
            RectTransform bubbleRectTransform = bubbleObject.GetComponent<RectTransform>();
            bubbleObject.GetComponent<ContentSizeFitter>().enabled = false;
            placeholderObject = CreatePlaceholderObject(bubbleObject.transform, bubbleRectTransform, textObjext.text);
            inputFieldObject = CreateInputFieldObject(bubbleObject.transform, textObjext, placeholderObject.GetComponent<Text>());
            inputField = inputFieldObject.GetComponent<InputField>();
        }

        static string emptyLines(string message, int lineHeight)
        {
            string messageLines = message;
            for (int i = 0; i < lineHeight - 1; i++)
                messageLines += "\n";
            return messageLines;
        }

        GameObject CreatePlaceholderObject(Transform parent, RectTransform textRectTransform, string message)
        {
            // Create a child GameObject for the placeholder text
            GameObject placeholderObject = CreateTextObject(parent, "Placeholder", message, false, false);
            RectTransform placeholderRectTransform = placeholderObject.GetComponent<RectTransform>();
            placeholderRectTransform.sizeDelta = textRectTransform.sizeDelta;
            placeholderRectTransform.anchoredPosition = textRectTransform.anchoredPosition;
            placeholderRectTransform.localScale = Vector3.one;
            SyncParentRectTransform(placeholderRectTransform);
            return placeholderObject;
        }

        GameObject CreateInputFieldObject(Transform parent, Text textObject, Text placeholderTextObject)
        {
            GameObject inputFieldObject = new GameObject("InputField", typeof(RectTransform), typeof(InputField), typeof(Canvas));
            inputFieldObject.transform.SetParent(parent);
            inputField = inputFieldObject.GetComponent<InputField>();
            inputField.textComponent = textObject;
            inputField.placeholder = placeholderTextObject;
            inputField.interactable = true;
            inputField.lineType = InputField.LineType.MultiLineSubmit;
            inputField.shouldHideMobileInput = false;
            inputField.shouldActivateOnSelect = true;
            RectTransform inputFieldRect = inputFieldObject.GetComponent<RectTransform>();
            inputFieldRect.localScale = Vector3.one;
            SyncParentRectTransform(inputFieldRect);
            return inputFieldObject;
        }

        public void FixCaretSorting()
        {
            GameObject caret = GameObject.Find($"{inputField.name} Input Caret");
            Canvas bubbleCanvas = caret.GetComponent<Canvas>();
            if (bubbleCanvas == null)
            {
                bubbleCanvas = caret.AddComponent<Canvas>();
                bubbleCanvas.overrideSorting = true;
                bubbleCanvas.sortingOrder = 3;
            }
        }

        public void AddSubmitListener(UnityEngine.Events.UnityAction<string> onInputFieldSubmit)
        {
            inputField.onSubmit.AddListener(onInputFieldSubmit);
        }

        public void AddValueChangedListener(UnityEngine.Events.UnityAction<string> onValueChanged)
        {
            inputField.onValueChanged.AddListener(onValueChanged);
        }

        public new string GetText()
        {
            return inputField.text;
        }

        public new void SetText(string text)
        {
            inputField.text = text;
            MoveTextEnd();
        }

        public void SetPlaceHolderText(string text)
        {
            placeholderObject.GetComponent<Text>().text = text;
        }

        public bool inputFocused()
        {
            return inputField.isFocused;
        }

        public void MoveTextEnd()
        {
            inputField.MoveTextEnd(true);
        }

        public void setInteractable(bool interactable)
        {
            inputField.interactable = interactable;
        }

        public void SetSelectionColorAlpha(float alpha)
        {
            Color color = inputField.selectionColor;
            color.a = alpha;
            inputField.selectionColor = color;
        }

        public void ActivateInputField()
        {
            inputField.ActivateInputField();
            FixCaretSorting();
        }

        public void ReActivateInputField()
        {
            inputField.DeactivateInputField();
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

}
