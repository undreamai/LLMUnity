using System.Collections.Generic;
using System;
using System.Linq;

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

        Dictionary<int, string> phrases;
        Dictionary<int, List<Sentence>> phraseSentences;
        int nextPhraseId;
        SentenceSplitter sentenceSplitter;

        public Dialogue(string actor = "", string title = "")
        {
            Actor = actor;
            Title = title;
            phrases = new Dictionary<int, string>();
            phraseSentences = new Dictionary<int, List<Sentence>>();
            nextPhraseId = 0;
            sentenceSplitter = new SentenceSplitter();
        }

        public void SetSentenceSplitting(char[] delimiters, bool trimSentences = true)
        {
            if (nextPhraseId > 0) throw new Exception("Sentence splitting can't change when there are phrases in the Dialogue");
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

        public void Add(string text)
        {
            int phraseId = nextPhraseId++;
            phrases[phraseId] = text;
            if (sentenceSplitter == null)
            {
                phraseSentences[phraseId] = new List<Sentence> { new Sentence(phraseId, 0, text.Length - 1) };
            }
            else
            {
                phraseSentences[phraseId] = new List<Sentence>();
                List<(int, int)> subindices = sentenceSplitter.Split(text);
                foreach ((int startIndex, int endIndex) in subindices)
                {
                    phraseSentences[phraseId].Add(new Sentence(phraseId, startIndex, endIndex));
                }
            }
        }

        public List<string> GetPhrases()
        {
            return phrases.Values.ToList();
        }

        public List<string> GetSentences()
        {
            List<string> allSentences = new List<string>();
            foreach ((int phraseId, List<Sentence> sentences) in phraseSentences)
            {
                string phrase = phrases[phraseId];
                foreach (Sentence sentence in sentences)
                {
                    allSentences.Add(phrase.Substring(sentence.startIndex, sentence.endIndex - sentence.startIndex + 1));
                }
            }
            return allSentences;
        }

        public void Clear()
        {
            phrases.Clear();
            foreach (List<Sentence> sentences in phraseSentences.Values) sentences.Clear();
        }
    }

    public class DialogueManager
    {
        Dictionary<string, Dictionary<string, Dialogue>> dialogues;
        char[] delimiters = SentenceSplitter.DefaultDelimiters;
        bool trimSentences = true;

        public DialogueManager()
        {
            dialogues = new Dictionary<string, Dictionary<string, Dialogue>>();
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
                Dialogue dialogue = new Dialogue(actor, title);
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
            Add(actor, "", text);
        }

        public List<string> GetPhrases(string actor = null, string title = null)
        {
            List<string> phrases = new List<string>();
            foreach ((string actorName, Dictionary<string, Dialogue> actorDialogues) in dialogues)
            {
                foreach ((string titleName, Dialogue dialogue) in actorDialogues)
                {
                    if ((actor == null || actor == actorName) && (title == null || title == titleName))
                    {
                        phrases.AddRange(dialogue.GetPhrases());
                    }
                }
            }
            return phrases;
        }

        public List<string> GetSentences(string actor = null, string title = null)
        {
            List<string> sentences = new List<string>();
            foreach ((string actorName, Dictionary<string, Dialogue> actorDialogues) in dialogues)
            {
                foreach ((string titleName, Dialogue dialogue) in actorDialogues)
                {
                    if ((actor == null || actor == actorName) && (title == null || title == titleName))
                    {
                        sentences.AddRange(dialogue.GetSentences());
                    }
                }
            }
            return sentences;
        }

        public void Clear()
        {
            foreach (Dictionary<string, Dialogue> actorDialogues in dialogues.Values)
            {
                foreach (Dialogue dialogue in actorDialogues.Values)
                {
                    dialogue.Clear();
                }
            }
        }
    }
}
