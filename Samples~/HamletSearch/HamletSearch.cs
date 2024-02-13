using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using LLMUnity;
using System.IO;
using UnityEngine.UI;

class HamletSearch : MonoBehaviour
{
    public Dropdown CharacterSelect;
    public InputField PlayerText;
    public Text AIText;
    public Embedding Embedding;
    public TextAsset GutenbergText;

    string Character;
    Dialogue dialogue;

    void Start()
    {
        StartCoroutine(InitDialogue());
        CharacterSelect.onValueChanged.AddListener(DropdownChange);
    }

    IEnumerator<string> InitDialogue()
    {
        PlayerText.interactable = false;

        EmbeddingModel model = Embedding.GetModel();
        if (model == null)
        {
            throw new System.Exception("Please select an Embedding model in the HamletSearch GameObject!");
        }

        string sampleDir = Directory.GetDirectories(Application.dataPath, "HamletSearch", SearchOption.AllDirectories)[0];
        string filename = Path.Combine(sampleDir, "Embeddings.zip");
        if (File.Exists(filename))
        {
            PlayerText.text = "Loading dialogues...";
            yield return null;
            dialogue = Dialogue.Load(filename);
        }
        else
        {
            PlayerText.text = "Creating Embeddings...";
            yield return null;
            dialogue = CreateEmbeddings(filename);
        }

        PlayerText.interactable = true;
        InitPlayerText();
    }

    void InitPlayerText()
    {
        DropdownChange(CharacterSelect.value);
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
        AIText.text = dialogue.Search(message, 1, Character)[0];
        // if you want only Hamlet, you could instead do:
        // AIText.text = dialogue.Search(message)[0];
        PlayerText.interactable = true;
        PlayerText.Select();
        PlayerText.text = "";
    }

    void DropdownChange(int selection)
    {
        Character = CharacterSelect.options[selection].text.ToUpper();
        Debug.Log($"{Character}: {dialogue.NumPhrases(Character)} phrases available");
    }

    Dialogue CreateEmbeddings(string filename)
    {
        Dialogue dialogue;

        Dictionary<string, List<(string, string)>> hamlet = ReadGutenbergFile(GutenbergText.text);
        dialogue = new Dialogue(Embedding.GetModel());

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach ((string act, List<(string, string)> messages) in hamlet)
        {
            foreach ((string actor, string message) in messages)
            {
                dialogue.Add(message, actor);
                // if you want only Hamlet, you could instead do:
                // if (actor == "HAMLET") dialogue.Add(message);
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
        string name2 = null;
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
                    if (name2 != null) messages[act].Add((name2, message));
                }
                act = line.Replace(".", "");
                messages[act] = new List<(string, string)>();
                name = null;
                name2 = null;
                message = "";
            }
            else if (nameRegex.IsMatch(line))
            {
                if (name != null && message != "")
                {
                    messages[act].Add((name, message));
                    if (name2 != null) messages[act].Add((name2, message));
                }
                message = "";
                name = line.Replace(".", "");
                if (name.Contains("and"))
                {
                    string[] names = name.Split(" and ");
                    name = names[0];
                    name2 = names[1];
                }
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
