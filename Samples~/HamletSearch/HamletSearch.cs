using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using LLMUnity;
using System.Linq;
using Unity.VisualScripting;

class HamletSearch : MonoBehaviour
{
    EmbeddingModel embedder;
    ANNModelSearch search;
    public bool fullPlay;
    public TextAsset gutenbergText;

    public void OnEnable()
    {
        embedder = new BGEModel(
            "Assets/StreamingAssets/bge-small-en-v1.5.sentis",
            "Assets/StreamingAssets/bge-small-en-v1.5.tokenizer.json"
        );
        // embedder = new BGEModel(
        //     "Assets/StreamingAssets/bge-base-en-v1.5.sentis",
        //     "Assets/StreamingAssets/bge-base-en-v1.5.tokenizer.json"
        // );
    }

    void Start()
    {
        Dictionary<string, List<(string, string)>> hamlet = ReadGutenbergFile(gutenbergText.text);
        DialogueManager dialogueManager = new DialogueManager();
        search = new ANNModelSearch(embedder);
        Stopwatch stopwatch = new Stopwatch();

        int numSentences = 0;
        float elapsedTotal = 0;
        foreach ((string act, List<(string, string)> messages) in hamlet)
        {
            if (!fullPlay && act != "ACT III") continue;
            foreach ((string actor, string message) in messages)
                dialogueManager.Add(actor, act, message);
            messages.Clear();

            List<string> sentences = dialogueManager.GetSentences(null, act);
            List<int> keys = new List<int>();
            for (int i = 0; i < sentences.Count; i++)
            {
                keys.Add(numSentences++);
            }

            stopwatch.Reset(); stopwatch.Start();
            search.Add(keys, sentences);
            stopwatch.Stop();

            elapsedTotal += (float)stopwatch.Elapsed.TotalMilliseconds / 1000f;
            Debug.Log($"act {act} embedded {sentences.Count} sentences in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");
        }
        Debug.Log($"embedded {numSentences} sentences in {elapsedTotal} secs");
        Debug.Log(search.Count());

        stopwatch.Reset(); stopwatch.Start();
        string[] similar = search.Search("should i be?", 10);
        stopwatch.Stop();
        Debug.Log($"search time: {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");

        Debug.Log("Similar sentences:");
        for (int i = 0; i < similar.Length; i++)
        {
            Debug.Log($"  {i + 1}: {similar[i]}");
        }
    }

    public Dictionary<string, List<(string, string)>> ReadGutenbergFile(string text)
    {
        string skipPattern = @"\[.*?\]";
        string namePattern = "^[A-Z and]+\\.$";
        Regex nameRegex = new Regex(namePattern);

        string act = null;
        string name = null;
        string message = "";
        bool add = false;
        Dialogue dialogue = null;
        int numWords = 0;
        int numLines = 0;
        Dictionary<string, List<(string, string)>> messages = new Dictionary<string, List<(string, string)>>();

        string[] lines = text.Split("\n");
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("***")) add = !add;
            if (!add) continue;

            line = line.Replace("\r", "");
            line = Regex.Replace(line, skipPattern, "");
            if (line == "") continue;
            numWords += line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            numLines++;

            if (line.StartsWith("ACT"))
            {
                if (dialogue != null && message != "")
                {
                    messages[act].Add((name, message));
                }
                act = line.Replace(".", "");
                messages[act] = new List<(string, string)>();
                name = null;
                message = "";
            }
            else if (nameRegex.IsMatch(line))
            {
                if (name != null && message != "")
                {
                    messages[act].Add((name, message));
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
        Debug.Log($"{numLines} lines, {numWords} words");
        return messages;
    }

    public void OnDisable()
    {
        embedder.Destroy();
    }
}
