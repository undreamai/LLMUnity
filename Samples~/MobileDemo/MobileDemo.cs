using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace LLMUnitySamples
{
    public class MobileDemo : MonoBehaviour
    {
        public LLMCharacter llmCharacter;

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
            await llmCharacter.Warmup();
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

        public void CancelRequests()
        {
            llmCharacter.CancelRequests();
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
            if (onValidateWarning && !llmCharacter.remote && llmCharacter.llm != null && llmCharacter.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmCharacter.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
            if (onValidateInfo)
            {
                Debug.Log($"Select 'Download On Start' in the {llmCharacter.llm.gameObject.name} GameObject to download the models when the app starts.");
                onValidateInfo = false;
            }
        }
    }
}
