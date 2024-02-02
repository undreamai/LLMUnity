using System.Collections.Generic;
using System;
using System.Linq;

namespace LLMUnity
{
    public class Dialogue
    {
        public string Title { get; private set; }

        char[] delimiters;
        Dictionary<int, string> sentences;
        Dictionary<int, string> phrases;
        Dictionary<string, List<int>> characterPhrases;
        Dictionary<int, List<int>> phraseSentences;
        Dictionary<int, int> sentencePhrase;
        int nextSentenceId;
        int nextPhraseId;

        public Dialogue(string title = "")
        {
            Title = title;
            sentences = new Dictionary<int, string>();
            phrases = new Dictionary<int, string>();
            characterPhrases = new Dictionary<string, List<int>>();
            phraseSentences = new Dictionary<int, List<int>>();
            sentencePhrase = new Dictionary<int, int>();
            nextSentenceId = 0;
            nextPhraseId = 0;
            SetDelimiters(new char[] { '.', '!', ':', ';', '?', '\n', '\r', });
        }

        public void SetDelimiters(char[] delimiters)
        {
            this.delimiters = delimiters;
        }

        public static void Add<K, V>(Dictionary<K, List<V>> dict, K key, V value)
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new List<V>();
            }
            dict[key].Add(value);
        }

        int AddSentence(int phraseId, string sentence)
        {
            int sentenceId = nextSentenceId++;
            sentences[sentenceId] = sentence;
            sentencePhrase[sentenceId] = phraseId;
            return sentenceId;
        }

        public void AddPhrase(string name, string phrase)
        {
            int phraseId = nextPhraseId++;
            phrases[phraseId] = phrase;
            string[] sentences = phrase.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < sentences.Length; i++)
            {
                string sentence = sentences[i].Trim();
                int sentenceId = AddSentence(phraseId, sentence);
                Add(phraseSentences, phraseId, sentenceId);
            }
            Add(characterPhrases, name, phraseId);
        }

        public List<string> GetCharacterPhrases(string name)
        {
            if (!characterPhrases.ContainsKey(name))
            {
                return null;
            }
            List<string> result = new List<string>();
            foreach (int phraseId in characterPhrases[name])
            {
                result.Add(phrases[phraseId]);
            }
            return result;
        }

        public List<string> GetCharacterSentences(string name)
        {
            if (!characterPhrases.ContainsKey(name))
            {
                return null;
            }
            List<string> result = new List<string>();
            foreach (int phraseId in characterPhrases[name])
            {
                foreach (int sentenceId in phraseSentences[phraseId])
                {
                    result.Add(sentences[sentenceId]);
                }
            }
            return result;
        }
        public List<string> GetSentences()
        {
            return sentences.Values.ToList();
        }
    }
}
