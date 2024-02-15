using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using LLMUnity;
using System.IO;
using UnityEngine.UI;
using System.Net;
using System;

class LLMUnityBot : MonoBehaviour
{
    public InputField PlayerText;
    public Text AIText;
    public Embedding embedding;
    public LLM llm;

    SearchEngine search;

    void Start()
    {
        StartCoroutine(InitDialogue());
    }

    SearchEngine CreateEmbeddings(EmbeddingModel model, string filename)
    {
        // download the LLMUnity readme
        string tmpPath = Path.Combine(Application.temporaryCachePath, "LLMUnity_README.md");
        WebClient client = new WebClient();
        client.DownloadFile("https://raw.githubusercontent.com/undreamai/LLMUnity/main/README.md", tmpPath);
        string fileContents = File.ReadAllText(tmpPath);
        File.Delete(tmpPath);

        // Remove emojis
        fileContents = Regex.Replace(fileContents, @"\p{Cs}", "");

        SearchEngine search = new SearchEngine(model);
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // build the embeddings in chunks of 4 sentences
        string[] parts = SplitText(fileContents, 4);
        foreach (string part in parts)
        {
            search.Add(part);
        }
        Debug.Log($"embedded {search.NumPhrases()} chapters, {search.NumSentences()} sentences in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");

        // store the embeddings
        search.Save(filename);
        return search;
    }

    IEnumerator<string> InitDialogue()
    {
        PlayerText.interactable = false;

        EmbeddingModel model = embedding.GetModel();
        if (model == null)
        {
            throw new System.Exception("Please select an Embedding model in the LLMUnityBot GameObject!");
        }

        string filename;
#if UNITY_EDITOR
        string sampleDir = Directory.GetDirectories(Application.dataPath, "LLMUnityBot", SearchOption.AllDirectories)[0];
        filename = Path.Combine(sampleDir, "Embeddings.zip");
#else
        filename = Path.Combine(Application.streamingAssetsPath, "Embeddings.zip");
#endif

        if (File.Exists(filename))
        {
            // load the embeddings
            PlayerText.text = "Loading embeddings...";
            yield return null;
            search = SearchEngine.Load(model, filename);
        }
        else
        {
#if UNITY_EDITOR
            // create and store the embeddings (in Unity editor)
            PlayerText.text = "Creating Embeddings...";
            yield return null;
            search = CreateEmbeddings(model, filename);
#else
            // if in play mode throw an error
            throw new System.Exception("The embeddings could not be found!");
#endif
        }

        PlayerText.interactable = true;
        InitPlayerText();
    }

    void InitPlayerText()
    {
        PlayerText.onSubmit.AddListener(OnInputFieldSubmit);
        PlayerText.onValueChanged.AddListener(OnValueChanged);
        PlayerText.Select();
        PlayerText.text = "";
    }

    void OnValueChanged(string newText)
    {
        // Get rid of newline character added when we press enter
        if (Input.GetKey(KeyCode.Return))
        {
            if (PlayerText.text.Trim() == "")
                PlayerText.text = "";
        }
    }

    void OnInputFieldSubmit(string message)
    {
        PlayerText.interactable = false;

        // find the 2 most similar sentences
        string[] similar = search.Search(message, 2);
        string context = String.Join("\n", similar);

        // create a prompt using the similar sentences as context
        string prompt = $"Answer the following question based on the given context.\n\nContext:{context}\n\nQuestion:{message}";
        _ = llm.Chat(prompt, SetAIText, AIReplyComplete, false);

    }

    public void SetAIText(string text)
    {
        AIText.text = text;
    }

    public void AIReplyComplete()
    {
        PlayerText.interactable = true;
        PlayerText.Select();
        PlayerText.text = "";
    }

    string[] SplitText(string inputText, int chunkSentences)
    {
        // Split the input text into sentences
        string[] chapters = inputText.Split("###");
        List<string> sentences = new List<string>();
        foreach (string chapter in chapters)
        {
            sentences.AddRange(chapter.Split(new char[]{'\n', '\r', '.', '!'}));
        }
        
        List<string> chunks = new List<string>();
        for (int i=0; i<sentences.Count; i+=chunkSentences)
        {
            int count = i + chunkSentences >= sentences.Count? sentences.Count - i: chunkSentences;
            chunks.Add(String.Join(".", sentences.GetRange(i, count)));
        }

        return chunks.ToArray();
    }
}
