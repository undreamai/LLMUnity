using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Collections;

namespace LLMUnitySamples
{
    public class AsyncStartup : MonoBehaviour
    {
        public LLM llm;
        public InputField playerText;
        public Text AIText;
        public GameObject LoadingScreen;
        public Text LoadingText;

        void Start()
        {
            playerText.onSubmit.AddListener(onInputFieldSubmit);
            StartCoroutine(Loading());
        }

        IEnumerator Loading()
        {
            LoadingText.text = "Starting server...";
            LoadingScreen.gameObject.SetActive(true);
            playerText.interactable = false;
            // wait until server is up
            while (!llm.serverStarted)
            {
                yield return null;
            }
            //warm-up the model
            LoadingText.text = "Warming-up the model...";
            _ = llm.Warmup(LoadingComplete);
        }

        void LoadingComplete()
        {
            playerText.interactable = true;
            LoadingScreen.gameObject.SetActive(false);
            playerText.Select();
        }

        void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            AIText.text = "...";
            _ = llm.Chat(message, SetAIText, AIReplyComplete);
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
            llm.CancelRequests();
            AIReplyComplete();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }
    }
}
