using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace LLMUnitySamples
{
    public class MobileDemo : MonoBehaviour
    {
        public LLMAgent llmAgent;

        public GameObject ChatPanel;
        public InputField playerText;
        public Text AIText;
        public GameObject ErrorText;

        public GameObject DownloadPanel;
        public Scrollbar progressBar;
        public Text progressText;

        async void Start()
        {
            playerText.onSubmit.AddListener(onInputFieldSubmit);
            playerText.interactable = false;
            await DownloadThenWarmup();
        }

        async Task DownloadThenWarmup()
        {
            ChatPanel.SetActive(false);
            DownloadPanel.SetActive(true);
            bool downloadOK = await LLM.WaitUntilModelSetup(SetProgress);
            if (!downloadOK)
            {
                ErrorText.SetActive(true);
            }
            else
            {
                DownloadPanel.SetActive(false);
                ChatPanel.SetActive(true);
                await WarmUp();
            }
        }

        async Task WarmUp()
        {
            AIText.text += $"Warming up the model...";
            await llmAgent.Warmup();
            AIText.text = "";
            AIReplyComplete();
        }

        void SetProgress(float progress)
        {
            progressText.text = ((int)(progress * 100)).ToString() + "%";
            progressBar.size = progress;
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
        bool onValidateInfo = true;
        void OnValidate()
        {
            if (onValidateWarning && !llmAgent.remote && llmAgent.llm != null && llmAgent.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmAgent.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
            if (onValidateInfo)
            {
                Debug.Log($"Select 'Download On Start' in the {llmAgent.llm.gameObject.name} GameObject to download the models when the app starts.");
                onValidateInfo = false;
            }
        }
    }
}
