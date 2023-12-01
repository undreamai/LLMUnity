using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour
{
    public Transform chatContainer;
    public Color playerColor = new Color32(81, 164, 81, 255);
    public Color aiColor = new Color32(29, 29, 73, 255);
    public Color fontColor = Color.white;
    public TMP_FontAsset font;
    public int fontSize = 16;
    public LLMClient llmClient;
    public float padding = 10f;
    private float spacing = 10f;    
    private InputBubble inputBubble;
    private List<Bubble> chatBubbles = new List<Bubble>();
    private bool blockInput = false;

    void Start()
    {
        font = TMP_Settings.defaultFontAsset;
        inputBubble = new InputBubble(chatContainer, font, fontSize, fontColor, "InputBubble", playerColor, 0, 0, "Message me", 600, 4);
        inputBubble.AddSubmitListener(onInputFieldSubmit);
        inputBubble.AddValueChangedListener(onValueChanged);
        AllowInput();
    }

    void onInputFieldSubmit(string newText){
        inputBubble.ActivateInputField();
        if (blockInput || newText == "" || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)){
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)){
                inputBubble.SetText(inputBubble.GetText() + "\n");
            }
            return;
        }
        blockInput = true;
        // replace vertical_tab
        string message = inputBubble.GetText().Replace("\v", "\n");

        Bubble playerBubble = new Bubble(chatContainer, font, fontSize, fontColor, "PlayerBubble", playerColor, 0, 0, message);
        Bubble aiBubble = new Bubble(chatContainer, font, fontSize, fontColor, "AIBubble", aiColor, 0, 1, "...");
        chatBubbles.Add(playerBubble);
        chatBubbles.Add(aiBubble);
        UpdateBubblePositions();

        BubbleTextSetter aiBubbleTextSetter = new BubbleTextSetter(this, aiBubble);
        llmClient.Chat(message, aiBubbleTextSetter.SetText);

        inputBubble.SetText("");
    }
    
    public void AllowInput(){
        blockInput = false;
        inputBubble.ReActivateInputField();
    }

    void onValueChanged(string newText){
        // Get rid of newline character added when we press enter
        if (Input.GetKey(KeyCode.Return)){
            if(inputBubble.GetText() == "\n"){
                inputBubble.SetText("");
            }
        }
    }

    public void UpdateBubblePositions()
    {
        float y = inputBubble.GetRectTransform().sizeDelta.y + spacing;

        int lastBubbleOutsideFOV = -1;
        float containerHeight = chatContainer.GetComponent<RectTransform>().rect.height;
        for (int i = chatBubbles.Count - 1; i >= 0; i--) {
            Bubble bubble = chatBubbles[i];
            RectTransform childRect = bubble.GetRectTransform();
            childRect.position = new Vector2(childRect.position.x, y);
            y += childRect.sizeDelta.y + spacing;

            // last bubble outside the container
            if (y > containerHeight && lastBubbleOutsideFOV == -1){
                lastBubbleOutsideFOV = i;
            }
        }
        // destroy bubbles outside the container
        for (int i = 0; i <= lastBubbleOutsideFOV; i++) {
            chatBubbles[i].Destroy();
        }
        chatBubbles.RemoveRange(0, lastBubbleOutsideFOV+1);
    }
}