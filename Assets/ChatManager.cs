using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ChatManager : MonoBehaviour
{
    public Transform chatContainer;
    public Color playerColor = new Color32(81, 164, 81, 255);
    public Color aiColor = new Color32(29, 29, 73, 255);
    public Color fontColor = Color.white;
    public TMP_FontAsset font;
    public int fontSize = 16;
    public int bubbleWidth = 600;
    public LLMClient llmClient;
    public float textPadding = 10f;
    public float bubbleSpacing = 10f;
    public Sprite sprite;

    private InputBubble inputBubble;
    private List<Bubble> chatBubbles = new List<Bubble>();
    private bool blockInput = false;

    void Start()
    {
        font = TMP_Settings.defaultFontAsset;
        inputBubble = new InputBubble(chatContainer, sprite, font, fontSize, fontColor, "InputBubble", playerColor, 0, 0, "Message me", textPadding, bubbleSpacing, bubbleWidth, 4);
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

        Bubble playerBubble = new Bubble(chatContainer, sprite, font, fontSize, fontColor, "PlayerBubble", playerColor, 0, 0, message, textPadding, bubbleSpacing, bubbleWidth);
        Bubble aiBubble = new Bubble(chatContainer, sprite, font, fontSize, fontColor, "AIBubble", aiColor, 0, 1, "...", textPadding, bubbleSpacing, bubbleWidth);
        chatBubbles.Add(playerBubble);
        chatBubbles.Add(aiBubble);
        UpdateBubblePositions();

        BubbleTextSetter aiBubbleTextSetter = new BubbleTextSetter(this, aiBubble);
        Task chatTask = llmClient.Chat(message, aiBubbleTextSetter.SetText);

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
        float y = inputBubble.GetRectTransform().sizeDelta.y + 2 * bubbleSpacing;
        int lastBubbleOutsideFOV = -1;
        float containerHeight = chatContainer.GetComponent<RectTransform>().rect.height;
        for (int i = chatBubbles.Count - 1; i >= 0; i--) {
            Bubble bubble = chatBubbles[i];
            RectTransform childRect = bubble.GetRectTransform();
            childRect.position = new Vector2(childRect.position.x, y);

            // last bubble outside the container
            if (y > containerHeight && lastBubbleOutsideFOV == -1){
                lastBubbleOutsideFOV = i;
            }
            y += childRect.sizeDelta.y + bubbleSpacing;
        }
        // destroy bubbles outside the container
        for (int i = 0; i <= lastBubbleOutsideFOV; i++) {
            chatBubbles[i].Destroy();
        }
        chatBubbles.RemoveRange(0, lastBubbleOutsideFOV+1);
    }
}