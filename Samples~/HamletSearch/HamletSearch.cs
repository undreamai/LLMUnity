using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using LLMUnity;
using System.IO;
using UnityEngine.UI;
using Unity.VisualScripting;

class HamletSearch : MonoBehaviour
{
    public InputField playerText;
    public Text AIText;
    public Embedding embedding;
    public TextAsset gutenbergText;

    Dialogue dialogue;

    void Start()
    {
        StartCoroutine(InitDialogue());
    }

    IEnumerator<string> InitDialogue()
    {
        playerText.interactable = false;

        EmbeddingModel model = embedding.GetModel();
        if (model == null)
        {
            throw new System.Exception("Please select an embedding model in the HamletSearch GameObject!");
        }

        string sampleDir = Directory.GetDirectories(Application.dataPath, "HamletSearch", SearchOption.AllDirectories)[0];
        string filename = Path.Combine(sampleDir, "embeddings.zip");
        if (File.Exists(filename))
        {
            playerText.text = "Loading dialogues...";
            yield return null;
            dialogue = Dialogue.Load(filename);
        }
        else
        {
            playerText.text = "Creating embeddings...";
            yield return null;
            dialogue = CreateEmbeddings(filename);
        }

        playerText.interactable = true;
        InitPlayerText();
    }

    void InitPlayerText()
    {
        playerText.onSubmit.AddListener(onInputFieldSubmit);
        playerText.onValueChanged.AddListener(onValueChanged);
        playerText.Select();
        playerText.text = "";
    }

    void onValueChanged(string newText)
    {
        // Get rid of newline character added when we press enter
        if (Input.GetKey(KeyCode.Return))
        {
            if (playerText.text.Trim() == "")
                playerText.text = "";
        }
    }

    void onInputFieldSubmit(string message)
    {
        playerText.interactable = false;
        AIText.text = dialogue.Search(message)[0];
        playerText.interactable = true;
        playerText.Select();
        playerText.text = "";
    }

    Dialogue CreateEmbeddings(string filename)
    {
        Dialogue dialogue;

        Dictionary<string, List<(string, string)>> hamlet = ReadGutenbergFile(gutenbergText.text);
        dialogue = new Dialogue(embedding.GetModel());

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach ((string act, List<(string, string)> messages) in hamlet)
        {
            foreach ((string actor, string message) in messages)
            {
                if (actor == "HAMLET") dialogue.Add(message);
            }
        }
        Debug.Log($"embedded {dialogue.NumPhrases()} phrases, {dialogue.NumSentences()} sentences in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");

        dialogue.Save(filename);
        return dialogue;
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
}
