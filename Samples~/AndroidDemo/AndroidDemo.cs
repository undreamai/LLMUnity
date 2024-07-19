using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMUnitySamples
{
    public class AndroidDemo : MonoBehaviour
    {
        public LLM llm;
        public LLMCharacter llmCharacter;

        public GameObject ChatPanel;
        public InputField playerText;
        public Text AIText;

        public GameObject DownloadPanel;
        public Scrollbar progressBar;
        public Text progressText;
        int cores;

        void Awake()
        {
            ChatPanel.SetActive(false);
            DownloadPanel.SetActive(false);
        }

        void Start()
        {
            playerText.onSubmit.AddListener(onInputFieldSubmit);
            playerText.interactable = false;
            StartCoroutine(Loading());
        }

        IEnumerator<string> Loading()
        {
            DownloadPanel.SetActive(true);
            AIText.text = "Downloading model...";
            Task downloadTask = llm.DownloadModel(
                "https://huggingface.co/afrideva/smol_llama-220M-openhermes-GGUF/resolve/main/smol_llama-220m-openhermes.q4_k_m.gguf?download=true",
                SetProgress
            );
            while (!downloadTask.IsCompleted) yield return null;
            llm.SetTemplate("alpaca");
            DownloadPanel.SetActive(false);

            ChatPanel.SetActive(true);
            cores = LLMUnitySetup.AndroidGetNumBigCores();
            AIText.text += $"\nWarming up the model...\nWill use {cores} cores";
            Task warmup = llmCharacter.Warmup();
            while (!warmup.IsCompleted) yield return null;

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
