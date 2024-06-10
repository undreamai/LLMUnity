using UnityEngine;
using LLMUnity;
using UnityEngine.UI;


namespace LLMUnitySamples
{
    public class ServerClientInteraction
    {
        InputField playerText;
        Text AIText;
        LLMCharacter llmCharacter;

        public ServerClientInteraction(InputField playerText, Text AIText, LLMCharacter llmCharacter)
        {
            this.playerText = playerText;
            this.AIText = AIText;
            this.llmCharacter = llmCharacter;
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
            _ = llmCharacter.Chat(message, SetAIText, AIReplyComplete);
        }

        public void SetAIText(string text)
        {
            AIText.text = text;
        }

        public void AIReplyComplete()
        {
            playerText.interactable = true;
            playerText.Select();
            playerText.text = "";
        }
    }

    public class ServerClient : MonoBehaviour
    {
        public LLM llmCharacter;

        public LLMCharacter llmCharacter1;
        public InputField playerText1;
        public Text AIText1;
        ServerClientInteraction interaction1;

        public LLMCharacter llmCharacter2;
        public InputField playerText2;
        public Text AIText2;
        ServerClientInteraction interaction2;

        void Start()
        {
            interaction1 = new ServerClientInteraction(playerText1, AIText1, llmCharacter1);
            interaction2 = new ServerClientInteraction(playerText2, AIText2, llmCharacter2);
            interaction1.Start();
            interaction2.Start();
        }

        public void CancelRequests()
        {
            llmCharacter1.CancelRequests();
            llmCharacter2.CancelRequests();
            interaction1.AIReplyComplete();
            interaction2.AIReplyComplete();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }
    }
}
