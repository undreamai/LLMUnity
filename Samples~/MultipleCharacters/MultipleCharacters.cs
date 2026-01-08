using UnityEngine;
using UnityEngine.UI;
using LLMUnity;

namespace LLMUnitySamples
{
    public class MultipleAgents : MonoBehaviour
    {
        [Header("Shared UI")]
        public InputField playerText;
        public Dropdown agentDropdown;

        [Header("AI 1")]
        public LLMAgent llmAgent1;
        public Text AIText1;

        [Header("AI 2")]
        public LLMAgent llmAgent2;
        public Text AIText2;

        bool onValidateWarning = true;

        void Start()
        {
            playerText.onSubmit.AddListener(OnInputFieldSubmit);
            playerText.Select();
        }

        void OnInputFieldSubmit(string message)
        {
            playerText.interactable = false;

            var agent = agentDropdown.value == 0 ? llmAgent1 : llmAgent2;
            var aiText = agentDropdown.value == 0 ? AIText1 : AIText2;

            aiText.text = "...";
            _ = agent.Chat(message, (reply) => aiText.text = reply, AIReplyComplete);
        }

        void AIReplyComplete()
        {
            playerText.interactable = true;
            playerText.text = "";
            playerText.Select();
        }

        public void CancelRequests()
        {
            llmAgent1.CancelRequests();
            llmAgent2.CancelRequests();
            AIReplyComplete();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        void OnValidate()
        {
            if (onValidateWarning &&
                llmAgent1 != null &&
                !llmAgent1.remote &&
                llmAgent1.llm != null &&
                llmAgent1.llm.model == "")
            {
                Debug.LogWarning(
                    $"Please select a model in the {llmAgent1.llm.gameObject.name} GameObject!"
                );
                onValidateWarning = false;
            }
        }
    }
}
