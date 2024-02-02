using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using LLMUnity;

class HamletSearch : MonoBehaviour
{
    EmbeddingModel embedder;
    ANNModelSearch search;
    public bool fullPlay;
    public TextAsset gutenbergText;

    public void OnEnable()
    {
        embedder = new BGEModel("Assets/StreamingAssets/bge-small-en-v1.5.sentis", "Assets/StreamingAssets/bge-small-en-v1.5.tokenizer.json");
    }

    void Start()
    {
        List<Dialogue> dialogues = ReadGutenbergFile(gutenbergText.text);
        search = new ANNModelSearch(embedder);

        Stopwatch stopwatch = new Stopwatch();
        float elapsed_total = 0;

        List<Dialogue> dialoguesToDo = fullPlay ? dialogues : new List<Dialogue> { dialogues[2] };
        foreach (Dialogue dialogue in dialoguesToDo)
        {
            stopwatch.Reset(); stopwatch.Start();
            search.Add(dialogue.GetSentences().ToArray());
            stopwatch.Stop();

            float elapsed = (float)stopwatch.Elapsed.TotalMilliseconds / 1000f;
            Debug.Log($"{dialogue.Title} embed time: {elapsed} secs");
            elapsed_total += elapsed;
        }
        Debug.Log($"embedded {search.Count()} sentences in {elapsed_total} secs");

        stopwatch.Reset(); stopwatch.Start();
        List<string> similar = search.RetrieveSimilar("should i exist?", 5);
        stopwatch.Stop();
        Debug.Log($"search time: {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");

        Debug.Log("Similar sentences:");
        for (int i = 0; i < similar.Count; i++)
        {
            Debug.Log($"  {i + 1}: {similar[i]}");
        }
    }

    public List<Dialogue> ReadGutenbergFile(string text)
    {
        List<Dialogue> dialogues = new List<Dialogue>();
        string skipPattern = @"\[.*?\]";
        string namePattern = "^[A-Z and]+\\.$";
        Regex nameRegex = new Regex(namePattern);

        string act = null;
        string name = null;
        string message = "";
        bool add = false;
        Dialogue dialogue = null;

        string[] lines = text.Split("\n");
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("***")) add = !add;
            if (!add) continue;

            line = line.Replace("\r", "");
            line = Regex.Replace(line, skipPattern, "");
            if (line == "") continue;

            if (line.StartsWith("ACT"))
            {
                if (dialogue != null && message != "")
                {
                    dialogue.AddPhrase(name, message);
                }
                act = line.Replace(".", "");
                dialogue = new Dialogue(act);
                dialogues.Add(dialogue);
                name = null;
                message = "";
            }
            else if (nameRegex.IsMatch(line))
            {
                if (name != null && message != "")
                {
                    dialogue.AddPhrase(name, message);
                }
                message = "";
                name = line.Replace(".", "");
            }
            else if (name != null)
            {
                if (message != "") message += " ";
                message += line;
            }

        }
        return dialogues;
    }

    public void OnDisable()
    {
        embedder.Destroy();
    }
}
