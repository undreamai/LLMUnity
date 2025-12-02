using UnityEngine;
using LLMUnity;
using UnityEngine.UI;

namespace LLMUnitySamples
{
    public class SimpleInteraction : MonoBehaviour
    {
        public LLMAgent llmAgent;
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
            _ = llmAgent.Chat(message, SetAIText, AIReplyComplete);
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

        public void CancelRequests()
        {
            llmAgent.CancelRequests();
            AIReplyComplete();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning && !llmAgent.remote && llmAgent.llm != null && llmAgent.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmAgent.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
        }
    }
}
