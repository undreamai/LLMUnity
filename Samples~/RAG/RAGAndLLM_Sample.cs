using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;

namespace LLMUnitySamples
{
    public class RAGAndLLMSample : RAGSample
    {
        public LLMAgent llmAgent;
        public Toggle ParaphraseWithLLM;

        protected override async void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            AIText.text = "...";
            (string[] similarPhrases, float[] distances) = await rag.Search(message, 1);
            string similarPhrase = similarPhrases[0];
            if (!ParaphraseWithLLM.isOn)
            {
                AIText.text = similarPhrase;
                await Task.Yield();
                AIReplyComplete();
            }
            else
            {
                _ = llmAgent.Chat("Paraphrase the following phrase: " + similarPhrase, SetAIText, AIReplyComplete);
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
