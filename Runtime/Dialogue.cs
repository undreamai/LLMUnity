using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Unity.Sentis;

namespace LLMUnity
{
    public class Sentence
    {
        public int phraseId;
        public int startIndex;
        public int endIndex;

        public Sentence(int phraseId, int startIndex, int endIndex)
        {
            this.phraseId = phraseId;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }
    }

    public class Phrase
    {
        public string text;
        public List<int> sentenceIds;

        public Phrase(string text) : this(text, new List<int>()) {}

        public Phrase(string text, List<int> sentenceIds)
        {
            this.text = text;
            this.sentenceIds = sentenceIds;
        }
    }

    public class SentenceSplitter
    {
        public static char[] DefaultDelimiters = new char[] { '.', '!', ':', ';', '?', '\n', '\r', };
        char[] delimiters;
        bool trimSentences = true;

        public SentenceSplitter(char[] delimiters, bool trimSentences = true)
        {
            this.delimiters = delimiters;
            this.trimSentences = trimSentences;
        }

        public SentenceSplitter() : this(SentenceSplitter.DefaultDelimiters, true) {}

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
    }

    public class Dialogue
    {
        public string Title { get; private set; }
        public string Actor { get; private set; }

        List<Phrase> phrases;
        List<Sentence> sentences;
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

        public string[] Search(TensorFloat encoding, int k, out float[] distances, bool returnSentences = false)
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

        public string[] Search(TensorFloat encoding, int k, bool returnSentences = false)
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
    }

    public class DialogueManager
    {
        Dictionary<string, Dictionary<string, Dialogue>> dialogues;
        char[] delimiters = SentenceSplitter.DefaultDelimiters;
        bool trimSentences = true;
        EmbeddingModel embedder;
        Type modelSearchType;

        public DialogueManager(EmbeddingModel embedder = null) : this(embedder, typeof(ANNModelSearch)) {}

        public DialogueManager(EmbeddingModel embedder = null, Type modelSearchType = null)
        {
            dialogues = new Dictionary<string, Dictionary<string, Dialogue>>();
            this.embedder = embedder;
            this.modelSearchType = modelSearchType;
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

            var encoding = embedder.Encode(queryString);
            Parallel.ForEach(dialogues, dialogue =>
            {
                string[] searchResults = dialogue.Search(encoding, k, out float[] searchDistances, returnSentences);
                for (int i = 0; i < searchResults.Length; i++)
                {
                    resultPairs.Add((searchResults[i], searchDistances[i]));
                }
            });

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
    }
}
