using UnityEngine;
using LLMUnity;
using UnityEngine.UI;

public class SimpleInteraction : MonoBehaviour
{
    public LLM llm;
    public InputField playerText;
    public Text AIText;

    void Start()
    {
        playerText.onSubmit.AddListener(onInputFieldSubmit);
        playerText.Select();
    }

    void onInputFieldSubmit(string message)
    {
        playerText.interactable = false;
        AIText.text = "...";
        // ask the LLM to reply for the message
        _ = llm.Chat(message, SetAIText, AIReplyComplete);
    }

    public void SetAIText(string text)
    {
        // write the reply in a streaming fashion
        AIText.text = text;
    }

    public void AIReplyComplete()
    {
        // the reply is complete, allow the player to provide the next input
        playerText.interactable = true;
        playerText.Select();
        playerText.text = "";
    }
}
