using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    [Serializable]
    public class SentenceSplitter : SearchPlugin
    {
        public const string DefaultDelimiters = ".!:;?\n\r";
        public char[] delimiters = DefaultDelimiters.ToCharArray();
        public bool returnChunks = false;
        public Dictionary<int, int[]> phraseToSentences = new Dictionary<int, int[]>();
        public Dictionary<int, int> sentenceToPhrase = new Dictionary<int, int>();
        public Dictionary<int, int[]> hexToPhrase = new Dictionary<int, int[]>();
        [HideInInspector, SerializeField] protected int nextKey = 0;

        public List<(int, int)> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            int startIndex = 0;
            bool seenChar = false;
            for (int i = 0; i < input.Length; i++)
            {
                bool isDelimiter = delimiters.Contains(input[i]);
                if (isDelimiter)
                {
                    while ((i < input.Length - 1) && (delimiters.Contains(input[i + 1]) || char.IsWhiteSpace(input[i + 1]))) i++;
                }
                else
                {
                    if (!seenChar) seenChar = !char.IsWhiteSpace(input[i]);
                }
                if ((i == input.Length - 1) || (isDelimiter && seenChar))
                {
                    indices.Add((startIndex, i));
                    startIndex = i + 1;
                }
            }
            return indices;
        }

        public override string Get(int key)
        {
            string phrase = "";
            foreach (int sentenceId in phraseToSentences[key]) phrase += search.Get(sentenceId);
            return phrase;
        }

        public override async Task<int> Add(string inputString)
        {
            int key = nextKey++;
            List<int> sentenceIds = new List<int>();
            foreach ((int startIndex, int endIndex) in Split(inputString).ToArray())
            {
                string sentenceText = inputString.Substring(startIndex, endIndex - startIndex + 1);
                int sentenceId = await search.Add(sentenceText);
                sentenceIds.Add(sentenceId);

                sentenceToPhrase[sentenceId] = key; // sentence -> phrase
            }
            phraseToSentences[key] = sentenceIds.ToArray(); // phrase -> sentence

            int hash = inputString.GetHashCode();
            if (!hexToPhrase.TryGetValue(hash, out int[] entries)) entries = new int[0];
            List<int> matchingHash = new List<int>(entries);
            matchingHash.Add(key);

            hexToPhrase[hash] = matchingHash.ToArray(); // hex -> phrase
            return key;
        }

        public override void Remove(int key)
        {
            if (!phraseToSentences.TryGetValue(key, out int[] sentenceIds)) return;
            int hash = Get(key).GetHashCode();

            phraseToSentences.Remove(key); // phrase -> sentence
            foreach (int sentenceId in sentenceIds)
            {
                search.Remove(sentenceId);
                sentenceToPhrase.Remove(sentenceId); // sentence -> phrase
            }

            if (hexToPhrase.TryGetValue(hash, out int[] phraseIds))
            {
                List<int> updatedIds = phraseIds.ToList();
                updatedIds.Remove(key);
                if (updatedIds.Count == 0) hexToPhrase.Remove(hash); // hex -> phrase
                else hexToPhrase[hash] = updatedIds.ToArray();
            }
        }

        public override int Remove(string inputString)
        {
            int hash = inputString.GetHashCode();
            if (!hexToPhrase.TryGetValue(hash, out int[] entries)) return 0;
            List<int> removeIds = new List<int>();
            foreach (int key in entries)
            {
                if (Get(key) == inputString) removeIds.Add(key);
            }
            foreach (int id in removeIds) Remove(id);
            return removeIds.Count;
        }

        public override int Count()
        {
            return phraseToSentences.Count;
        }

        public override async Task<(string[], float[])> Search(string queryString, int k)
        {
            if (returnChunks)
            {
                return await search.Search(queryString, k);
            }
            else
            {
                int searchKey = await search.IncrementalSearch(queryString);
                List<int> phraseKeys = new List<int>();
                List<string> phrases = new List<string>();
                List<float> distancesList = new List<float>();
                bool complete;
                do
                {
                    int[] resultKeys;
                    float[] distancesIter;
                    (resultKeys, distancesIter, complete) = search.IncrementalFetchKeys(searchKey, k);
                    for (int i = 0; i < resultKeys.Length; i++)
                    {
                        int phraseId = sentenceToPhrase[resultKeys[i]];
                        if (phraseKeys.Contains(phraseId)) continue;
                        phraseKeys.Add(phraseId);
                        phrases.Add(Get(phraseId));
                        distancesList.Add(distancesIter[i]);
                        if (phraseKeys.Count() == k)
                        {
                            complete = true;
                            break;
                        }
                    }
                }
                while (!complete);
                return (phrases.ToArray(), distancesList.ToArray());
            }
        }

        public override void Clear()
        {
            nextKey = 0;
            phraseToSentences.Clear();
            sentenceToPhrase.Clear();
            hexToPhrase.Clear();
            search.Clear();
        }

        protected override void SaveInternal(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, phraseToSentences, "SentenceSplitter_phraseToSentences");
            ArchiveSaver.Save(archive, sentenceToPhrase, "SentenceSplitter_sentenceToPhrase");
            ArchiveSaver.Save(archive, hexToPhrase, "SentenceSplitter_hexToPhrase");
            search.Save(archive);
        }

        protected override void LoadInternal(ZipArchive archive)
        {
            phraseToSentences = ArchiveSaver.Load<Dictionary<int, int[]>>(archive, "SentenceSplitter_phraseToSentences");
            sentenceToPhrase = ArchiveSaver.Load<Dictionary<int, int>>(archive, "SentenceSplitter_sentenceToPhrase");
            hexToPhrase = ArchiveSaver.Load<Dictionary<int, int[]>>(archive, "SentenceSplitter_hexToPhrase");
            search.Load(archive);
        }
    }
}
