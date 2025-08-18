using UnityEngine.UI;
using LLMUnity;

namespace LLMUnitySamples
{
    public class RAGAndLLMSample : RAGSample
    {
        public LLMAgent llmAgent;
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
                _ = llmAgent.ChatAsync("Paraphrase the following phrase: " + similarPhrase, SetAIText, AIReplyComplete);
            }
        }

        public void CancelRequests()
        {
            llmAgent.CancelRequests();
            AIReplyComplete();
        }

        protected override void CheckLLMs(bool debug)
        {
            base.CheckLLMs(debug);
            CheckLLM(llmAgent, debug);
        }
    }
}
