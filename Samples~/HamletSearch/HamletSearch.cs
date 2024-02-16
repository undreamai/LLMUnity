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
    public Embedding embedding;
    public TextAsset GutenbergText;

    string Character;
    Dialogue dialogue;

    void Start()
    {
        StartCoroutine(InitDialogue());
        CharacterSelect.onValueChanged.AddListener(DropdownChange);
    }

    Dialogue CreateEmbeddings(EmbeddingModel model, string filename)
    {
        // read the hamlet play for the specified characters
        Dictionary<string, List<(string, string)>> hamlet = ReadGutenbergFile(GutenbergText.text);
        List<string> characters = new List<string>();
        foreach (var option in CharacterSelect.options)
        {
            characters.Add(option.text.ToUpper());
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        // build the embeddings
        Dialogue dialogue = new Dialogue(model);
        foreach ((string act, List<(string, string)> messages) in hamlet)
        {
            foreach ((string actor, string message) in messages)
            {
                if (characters.Contains(actor)) dialogue.Add(message, actor);
                // if you want only Hamlet, you could instead do:
                // if (actor == "HAMLET") dialogue.Add(message);
            }
        }
        Debug.Log($"embedded {dialogue.NumPhrases()} phrases, {dialogue.NumSentences()} sentences in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");
        // store the embeddings
        dialogue.Save(filename);
        return dialogue;
    }

    IEnumerator<string> InitDialogue()
    {
        PlayerText.interactable = false;

        EmbeddingModel model = embedding.GetModel();
        if (model == null)
        {
            throw new System.Exception("Please select an Embedding model in the HamletSearch GameObject!");
        }

        string filename = Path.Combine(Application.streamingAssetsPath, "HamletSearch_Embeddings.zip");
        if (File.Exists(filename))
        {
            // load the embeddings
            PlayerText.text = "Loading dialogues...";
            yield return null;
            dialogue = Dialogue.Load(model, filename);
        }
        else
        {
#if UNITY_EDITOR
            // create and store the embeddings (in Unity editor)
            PlayerText.text = "Creating Embeddings (only once)...";
            yield return null;
            dialogue = CreateEmbeddings(model, filename);
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

        // search for the most similar text and reply
        AIText.text = dialogue.Search(message, 1, Character)[0];
        // if you want only Hamlet, you could instead do:
        // AIText.text = dialogue.Search(message)[0];

        PlayerText.interactable = true;
        PlayerText.Select();
        PlayerText.text = "";
    }

    void DropdownChange(int selection)
    {
        // select another character
        Character = CharacterSelect.options[selection].text.ToUpper();
        Debug.Log($"{Character}: {dialogue.NumPhrases(Character)} phrases available");
    }

    public Dictionary<string, List<(string, string)>> ReadGutenbergFile(string text)
    {
        // read the Hamlet play from the Gutenberg file
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
            string lineTrim = line.Trim();
            if (lineTrim == "" || lineTrim.StartsWith("Re-enter ") || lineTrim.StartsWith("Enter ") || lineTrim.StartsWith("SCENE")) continue;

            numWords += line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            numLines++;

            if (line.StartsWith("ACT"))
            {
                if (dialogue != null && message != "")
                {
                    message = message.Trim();
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
                    message = message.Trim();
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
