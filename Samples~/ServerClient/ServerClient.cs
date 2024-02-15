using UnityEngine;
using LLMUnity;
using UnityEngine.UI;


public class ServerClientInteraction
{
    InputField playerText;
    Text AIText;
    LLMClient llm;

    public ServerClientInteraction(InputField playerText, Text AIText, LLMClient llm)
    {
        this.playerText = playerText;
        this.AIText = AIText;
        this.llm = llm;
    }

    public void Start()
    {
        playerText.onSubmit.AddListener(onInputFieldSubmit);
        playerText.Select();
    }

    public void onInputFieldSubmit(string message)
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

public class ServerClient : MonoBehaviour
{
    // LLM object for the first character
    public LLM llm;
    public InputField playerText1;
    public Text AIText1;
    ServerClientInteraction interaction1;

    // LLMClient object for the second character
    public LLMClient llmClient;
    public InputField playerText2;
    public Text AIText2;
    ServerClientInteraction interaction2;

    void Start()
    {
        interaction1 = new ServerClientInteraction(playerText1, AIText1, llm);
        interaction2 = new ServerClientInteraction(playerText2, AIText2, llmClient);
        interaction1.Start();
        interaction2.Start();
    }
}
