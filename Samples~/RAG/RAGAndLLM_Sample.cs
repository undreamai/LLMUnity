using UnityEngine.UI;
using LLMUnity;

namespace LLMUnitySamples
{
    public class RAGAndLLMSample : RAGSample
    {
        public LLMCharacter llmCharacter;
        public Toggle ParaphraseWithLLM;

        protected override void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            AIText.text = "...";
            (string[] similarPhrases, float[] distances) = rag.Search(message, 1);
            string similarPhrase = similarPhrases[0];
            if (!ParaphraseWithLLM.isOn)
            {
                AIText.text = similarPhrase;
                AIReplyComplete();
            }
            else
            {
                _ = llmCharacter.ChatAsync("Paraphrase the following phrase: " + similarPhrase, SetAIText, AIReplyComplete);
            }
        }

        public void CancelRequests()
        {
            llmCharacter.CancelRequests();
            AIReplyComplete();
        }

        protected override void CheckLLMs(bool debug)
        {
            base.CheckLLMs(debug);
            CheckLLM(llmCharacter, debug);
        }
    }
}
