using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Unity.Sentis;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;

namespace LLMUnity
{
    [DataContract]
    public class Sentence
    {
        [DataMember]
        public int phraseId;
        [DataMember]
        public int startIndex;
        [DataMember]
        public int endIndex;

        public Sentence(int phraseId, int startIndex, int endIndex)
        {
            this.phraseId = phraseId;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }
    }

    [DataContract]
    public class Phrase
    {
        [DataMember]
        public string text;
        [DataMember]
        public List<int> sentenceIds;

        public Phrase(string text) : this(text, new List<int>()) {}

        public Phrase(string text, List<int> sentenceIds)
        {
            this.text = text;
            this.sentenceIds = sentenceIds;
        }
    }

    [DataContract]
    public class SentenceSplitter
    {
        public static char[] DefaultDelimiters = new char[] { '.', '!', ':', ';', '?', '\n', '\r', };
        [DataMember]
        char[] delimiters;
        [DataMember]
        bool trimSentences = true;

        public SentenceSplitter(char[] delimiters, bool trimSentences = true)
        {
            this.delimiters = delimiters;
            this.trimSentences = trimSentences;
        }

        public SentenceSplitter() : this(DefaultDelimiters, true) {}

        public List<(int, int)> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            int startIndex = 0;
            bool sawDelimiter = true;
            for (int i = 0; i < input.Length; i++)
            {
                if (sawDelimiter && trimSentences)
                {
                    while (char.IsWhiteSpace(input[i]) && i < input.Length - 1) i++;
                    startIndex = i;
                    sawDelimiter = false;
                }
                if (delimiters.Contains(input[i]) || i == input.Length - 1)
                {
                    int endIndex = i;
                    if (i == input.Length - 1 && trimSentences)
                    {
                        while (char.IsWhiteSpace(input[endIndex]) && endIndex > startIndex) endIndex--;
                    }
                    if (endIndex > startIndex || !trimSentences || (trimSentences && !char.IsWhiteSpace(input[startIndex]) && !delimiters.Contains(input[startIndex])))
                    {
                        indices.Add((startIndex, endIndex));
                    }
                    startIndex = i + 1;
                    sawDelimiter = true;
                }
            }
            return indices;
        }

        public static string[] IndicesToSentences(string input, List<(int, int)> indices)
        {
            string[] sentences = new string[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                sentences[i] = input.Substring(indices[i].Item1, indices[i].Item2 - indices[i].Item1 + 1);
            }
            return sentences;
        }
    }

    [DataContract]
    public class Dialogue
    {
        [DataMember]
        public string Title { get; private set; }
        [DataMember]
        public string Actor { get; private set; }

        [DataMember]
        List<Phrase> phrases;
        [DataMember]
        List<Sentence> sentences;
        [DataMember]
        SentenceSplitter sentenceSplitter;
        ModelKeySearch search;

        public Dialogue(string actor = "", string title = "", EmbeddingModel embedder = null) : this(actor, title, embedder, typeof(ANNModelSearch)) {}

        public Dialogue(string actor = "", string title = "", EmbeddingModel embedder = null, Type modelSearchType = null)
        {
            Actor = actor;
            Title = title;
            phrases = new List<Phrase>();
            sentences = new List<Sentence>();
            sentenceSplitter = new SentenceSplitter();
            search = null;
            if (embedder != null && modelSearchType != null)
            {
                search = (ModelKeySearch)Activator.CreateInstance(modelSearchType, embedder);
            }
        }

        public void SetSentenceSplitting(char[] delimiters, bool trimSentences = true)
        {
            if (sentences.Count > 0) throw new Exception("Sentence splitting can't change when there are phrases in the Dialogue");
            if (delimiters == null)
            {
                sentenceSplitter = null;
            }
            else
            {
                sentenceSplitter = new SentenceSplitter(delimiters, trimSentences);
            }
        }

        public void SetNoSentenceSplitting()
        {
            SetSentenceSplitting(null);
        }

        public void SetSearch(ModelKeySearch search)
        {
            this.search = search;
        }

        public void SetEmbedder(EmbeddingModel embedder)
        {
            search.SetEmbedder(embedder);
        }

        public string GetPhrase(Sentence sentence)
        {
            return phrases[sentence.phraseId].text;
        }

        public string GetSentence(Sentence sentence)
        {
            return GetPhrase(sentence).Substring(sentence.startIndex, sentence.endIndex - sentence.startIndex + 1);
        }

        public void Add(string text)
        {
            List<(int, int)> subindices;
            if (sentenceSplitter == null) subindices = new List<(int, int)> { (0, text.Length - 1) };
            else subindices = sentenceSplitter.Split(text);

            int phraseId = phrases.Count;
            Phrase phrase = new Phrase(text);
            phrases.Add(phrase);
            foreach ((int startIndex, int endIndex) in subindices)
            {
                int sentenceId = sentences.Count;
                Sentence sentence = new Sentence(phraseId, startIndex, endIndex);

                sentences.Add(sentence);
                phrase.sentenceIds.Add(sentenceId);
                search?.Add(sentenceId, GetSentence(sentence));
            }
        }

        public List<string> GetPhrases()
        {
            List<string> phraseTexts = new List<string>();
            foreach (Phrase phrase in phrases) phraseTexts.Add(phrase.text);
            return phraseTexts;
        }

        public List<string> GetSentences()
        {
            List<string> allSentences = new List<string>();
            foreach (Sentence sentence in sentences)
            {
                allSentences.Add(GetSentence(sentence));
            }
            return allSentences;
        }

        public string[] Search(string queryString, int k, out float[] distances, bool returnSentences = false)
        {
            return Search(search.Encode(queryString), k, out distances, returnSentences);
        }

        public string[] Search(float[] encoding, int k, out float[] distances, bool returnSentences = false)
        {
            if (search == null) throw new Exception("No search method defined!");
            int[] keys = search.SearchKey(encoding, k, out distances);
            string[] result = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                Sentence sentence = sentences[keys[i]];
                result[i] = returnSentences ? GetSentence(sentence) : GetPhrase(sentence);
            }
            return result;
        }

        public string[] Search(float[] encoding, int k, bool returnSentences = false)
        {
            return Search(encoding, k, out float[] distances, returnSentences);
        }

        public string[] Search(string queryString, int k, bool returnSentences = false)
        {
            return Search(queryString, k, out float[] distances, returnSentences);
        }

        public string[] SearchPhrases(string queryString, int k, out float[] distances)
        {
            return Search(queryString, k, out distances, false);
        }

        public string[] SearchPhrases(string queryString, int k)
        {
            return SearchPhrases(queryString, k, out float[] distances);
        }

        public string[] SearchSentences(string queryString, int k, out float[] distances)
        {
            return Search(queryString, k, out distances, true);
        }

        public string[] SearchSentences(string queryString, int k)
        {
            return SearchSentences(queryString, k, out float[] distances);
        }

        public int NumPhrases()
        {
            return phrases.Count;
        }

        public int NumSentences()
        {
            return sentences.Count;
        }

        public void Save(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(archive, dirname);
            }
        }

        public static string GetDialoguePath(string dirname)
        {
            return Path.Combine(dirname, "Dialogue.json");
        }

        public void Save(ZipArchive archive, string dirname)
        {
            Saver.Save(this, archive, GetDialoguePath(dirname));
            search.Save(archive, dirname);
        }

        public static Dialogue Load(string filePath, string dirname)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load(archive, dirname);
            }
        }

        public static Dialogue Load(ZipArchive archive, string dirname)
        {
            Dialogue dialogue = Saver.Load<Dialogue>(archive, GetDialoguePath(dirname));
            ModelKeySearch search = (ModelKeySearch)ModelSearchBase.CastLoad(archive, dirname);
            dialogue.SetSearch(search);
            return dialogue;
        }
    }

    [DataContract]
    public class DialogueManager
    {
        Dictionary<string, Dictionary<string, Dialogue>> dialogues;
        [DataMember]
        char[] delimiters = SentenceSplitter.DefaultDelimiters;
        [DataMember]
        bool trimSentences = true;
        [DataMember]
        EmbeddingModel embedder;
        Type modelSearchType;
        [DataMember]
        string modelSearchTypeName;

        public DialogueManager(EmbeddingModel embedder = null) : this(embedder, typeof(ANNModelSearch)) {}

        public DialogueManager(EmbeddingModel embedder = null, Type modelSearchType = null)
        {
            dialogues = new Dictionary<string, Dictionary<string, Dialogue>>();
            this.embedder = embedder;
            this.modelSearchType = modelSearchType;
            this.modelSearchTypeName = modelSearchType.FullName;
        }

        public void SetSentenceSplitting(char[] delimiters, bool trimSentences = true)
        {
            this.delimiters = delimiters;
            this.trimSentences = trimSentences;
        }

        public void SetNoSentenceSplitting()
        {
            SetSentenceSplitting(null);
        }

        public void AddDialogue(string actor, string title, Dialogue dialogue)
        {
            if (!dialogues.ContainsKey(actor))
            {
                dialogues[actor] = new Dictionary<string, Dialogue>();
            }
            dialogues[actor][title] = dialogue;
        }

        public void Add(string actor, string title, string text)
        {
            if (!dialogues.ContainsKey(actor) || !dialogues[actor].ContainsKey(title))
            {
                Dialogue dialogue = new Dialogue(actor, title, embedder, modelSearchType);
                dialogue.SetSentenceSplitting(delimiters, trimSentences);
                if (!dialogues.ContainsKey(actor))
                {
                    dialogues[actor] = new Dictionary<string, Dialogue>();
                }
                dialogues[actor][title] = dialogue;
            }
            dialogues[actor][title].Add(text);
        }

        public void Add(string actor, string text)
        {
            Add(actor, null, text);
        }

        public List<Dialogue> Filter(string actor = null, string title = null)
        {
            List<Dialogue> filtering = new List<Dialogue>();
            foreach ((string actorName, Dictionary<string, Dialogue> actorDialogues) in dialogues)
            {
                foreach ((string titleName, Dialogue dialogue) in actorDialogues)
                {
                    if ((actor == null || actor == actorName) && (title == null || title == titleName))
                    {
                        filtering.Add(dialogue);
                    }
                }
            }
            return filtering;
        }

        public List<string> Get(string actor = null, string title = null, bool returnSentences = false)
        {
            List<Dialogue> dialogues = Filter(actor, title);
            List<string> result = new List<string>();
            foreach (Dialogue dialogue in dialogues)
            {
                result.AddRange(returnSentences ? dialogue.GetSentences() : dialogue.GetPhrases());
            }
            return result;
        }

        public List<string> GetPhrases(string actor = null, string title = null)
        {
            return Get(actor, title, false);
        }

        public List<string> GetSentences(string actor = null, string title = null)
        {
            return Get(actor, title, true);
        }

        public string[] Search(string queryString, int k, out float[] distances, string actor = null, string title = null, bool returnSentences = false)
        {
            if (embedder == null) throw new Exception("No search method defined!");
            List<Dialogue> dialogues = Filter(actor, title);
            ConcurrentBag<(string, float)> resultPairs = new ConcurrentBag<(string, float)>();

            TensorFloat encodingTensor = embedder.Encode(queryString);
            encodingTensor.MakeReadable();
            float[] encoding = encodingTensor.ToReadOnlyArray();
            Task.Run(() =>
            {
                Parallel.ForEach(dialogues, dialogue =>
                {
                    string[] searchResults = dialogue.Search(encoding, k, out float[] searchDistances, returnSentences);
                    for (int i = 0; i < searchResults.Length; i++)
                    {
                        resultPairs.Add((searchResults[i], searchDistances[i]));
                    }
                });
            }).Wait();

            var sortedLists = resultPairs.OrderBy(item => item.Item2).ToList();
            int kmax = k == -1 ? sortedLists.Count : Math.Min(k, sortedLists.Count);
            string[] results = new string[kmax];
            distances = new float[kmax];
            for (int i = 0; i < kmax; i++)
            {
                results[i] = sortedLists[i].Item1;
                distances[i] = sortedLists[i].Item2;
            }
            return results;
        }

        public string[] Search(string queryString, int k, string actor = null, string title = null, bool returnSentences = false)
        {
            return Search(queryString, k, out float[] distances, actor, title, returnSentences);
        }

        public string[] SearchPhrases(string queryString, int k, out float[] distances, string actor = null, string title = null)
        {
            return Search(queryString, k, out distances, actor, title, false);
        }

        public string[] SearchPhrases(string queryString, int k, string actor = null, string title = null)
        {
            return SearchPhrases(queryString, k, out float[] distances, actor, title);
        }

        public string[] SearchSentences(string queryString, int k, out float[] distances, string actor = null, string title = null)
        {
            return Search(queryString, k, out distances, actor, title, true);
        }

        public string[] SearchSentences(string queryString, int k, string actor = null, string title = null)
        {
            return SearchSentences(queryString, k, out float[] distances, actor, title);
        }

        public int NumPhrases(string actor = null, string title = null)
        {
            int num = 0;
            List<Dialogue> dialogues = Filter(actor, title);
            foreach (Dialogue dialogue in dialogues)
            {
                num += dialogue.NumPhrases();
            }
            return num;
        }

        public int NumSentences(string actor = null, string title = null)
        {
            int num = 0;
            List<Dialogue> dialogues = Filter(actor, title);
            foreach (Dialogue dialogue in dialogues)
            {
                num += dialogue.NumSentences();
            }
            return num;
        }

        public static string GetDialogueManagerPath(string dirname)
        {
            return Path.Combine(dirname, "DialogueManager.json");
        }
        public static string GetDialoguesPath(string dirname)
        {
            return Path.Combine(dirname, "Dialogues.json");
        }

        public void Save(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(archive, dirname);
            }
        }

        public void Save(ZipArchive archive, string dirname = "")
        {
            Saver.Save(this, archive, GetDialogueManagerPath(dirname));

            embedder.Save(archive, dirname);

            List<string> dialogueDirs = new List<string>();
            foreach ((string actorName, Dictionary<string, Dialogue> actorDialogues) in dialogues)
            {
                foreach ((string titleName, Dialogue dialogue) in actorDialogues)
                {
                    string basedir = $"{dirname}/Dialogues/{Saver.EscapeFileName(actorName)}/{Saver.EscapeFileName(titleName)}";
                    dialogue.Save(archive, basedir);
                    dialogueDirs.Add(basedir);
                }
            }

            ZipArchiveEntry dialoguesEntry = archive.CreateEntry(GetDialoguesPath(dirname));
            using (StreamWriter writer = new StreamWriter(dialoguesEntry.Open()))
            {
                writer.Write(string.Join("\n", dialogueDirs));
            }
        }

        public static DialogueManager Load(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                DialogueManager dialogueManager = Saver.Load<DialogueManager>(archive, GetDialogueManagerPath(dirname));
                dialogueManager.modelSearchType = Type.GetType(dialogueManager.modelSearchTypeName);

                dialogueManager.embedder = EmbeddingModel.Load(archive, dirname);

                ZipArchiveEntry dialoguesEntry = archive.GetEntry(GetDialoguesPath(dirname));
                List<string> dialogueDirs = new List<string>();
                dialogueManager.dialogues = new Dictionary<string, Dictionary<string, Dialogue>>();
                using (StreamReader reader = new StreamReader(dialoguesEntry.Open()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        dialogueDirs.Add(line);
                    }
                }

                foreach (string basedir in dialogueDirs)
                {
                    Dialogue dialogue = Dialogue.Load(archive, basedir);
                    dialogueManager.AddDialogue(dialogue.Actor, dialogue.Title, dialogue);
                }
                return dialogueManager;
            }
        }
    }
}
