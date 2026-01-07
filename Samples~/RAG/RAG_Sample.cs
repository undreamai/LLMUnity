using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LLMUnitySamples
{
    public class RAGSample : MonoBehaviour
    {
        public RAG rag;
        public InputField playerText;
        public Text AIText;
        public TextAsset HamletText;
        List<string> phrases;
        string ragPath = "RAGSample.zip";

        async void Start()
        {
            CheckLLMs(false);
            playerText.interactable = false;
            LoadPhrases();
            await CreateEmbeddings();
            playerText.onSubmit.AddListener(onInputFieldSubmit);
            AIReplyComplete();
        }

        public void LoadPhrases()
        {
            phrases = RAGUtils.ReadGutenbergFile(HamletText.text)["HAMLET"];
        }

        public async Task CreateEmbeddings()
        {
            bool loaded = await rag.Load(ragPath);
            if (!loaded)
            {
    #if UNITY_EDITOR
                // build the embeddings
                playerText.text += $"Creating Embeddings (only once)...\n";
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (string phrase in phrases) await rag.Add(phrase);
                stopwatch.Stop();
                Debug.Log($"embedded {rag.Count()} phrases in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");
                // store the embeddings
                rag.Save(ragPath);
    #else
                // if in play mode throw an error
                Debug.LogError("The embeddings could not be found!");
    #endif
            }
        }

        protected virtual async void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            AIText.text = "...";
            (string[] similarPhrases, float[] distances) = await rag.Search(message, 1);
            AIText.text = similarPhrases[0];

            await Task.Yield();
            AIReplyComplete();
        }

        public void SetAIText(string text)
        {
            AIText.text = text;
        }

        public void AIReplyComplete()
        {
            playerText.text = "";
            playerText.interactable = true;
            playerText.Select();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        protected void CheckLLM(LLMClient llmClient, bool debug)
        {
            if (!llmClient.remote && llmClient.llm != null && llmClient.llm.model == "")
            {
                string error = $"Please select a llm model in the {llmClient.llm.gameObject.name} GameObject!";
                if (debug) Debug.LogWarning(error);
                else Debug.LogError(error);
            }
        }

        protected virtual void CheckLLMs(bool debug)
        {
            CheckLLM(rag.search.llmEmbedder, debug);
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning)
            {
                CheckLLMs(true);
                onValidateWarning = false;
            }
        }
    }
}
