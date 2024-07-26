using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace LLMUnitySamples
{
    public class AndroidDemo : MonoBehaviour
    {
        public LLMCharacter llmCharacter;

        public GameObject ChatPanel;
        public InputField playerText;
        public Text AIText;
        public GameObject ErrorText;

        public GameObject DownloadPanel;
        public Scrollbar progressBar;
        public Text progressText;
        int cores;

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
            bool downloadOK = await LLM.WaitUntilModelsDownloaded(SetProgress);
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
            cores = LLMUnitySetup.AndroidGetNumBigCores();
            AIText.text += $"Warming up the model...\nWill use {cores} cores";
            await llmCharacter.Warmup();
            AIText.text = $"Ready when you are ({cores} cores)!";
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
        void OnValidate()
        {
            if (onValidateWarning && !llmCharacter.remote && llmCharacter.llm != null && llmCharacter.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmCharacter.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
        }
    }
}
