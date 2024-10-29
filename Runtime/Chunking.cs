using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    [Serializable]
    public abstract class Chunking : SearchPlugin
    {
        public bool returnChunks = false;
        public Dictionary<int, List<int>> dataSplitToPhrases = new Dictionary<int, List<int>>();
        public Dictionary<int, int[]> phraseToSentences = new Dictionary<int, int[]>();
        public Dictionary<int, int> sentenceToPhrase = new Dictionary<int, int>();
        public Dictionary<int, int[]> hexToPhrase = new Dictionary<int, int[]>();
        [HideInInspector, SerializeField] protected int nextKey = 0;

        public abstract Task<List<(int, int)>> Split(string input);

        public override string Get(int key)
        {
            StringBuilder phraseBuilder = new StringBuilder();
            foreach (int sentenceId in phraseToSentences[key])
            {
                phraseBuilder.Append(search.Get(sentenceId));
            }
            return phraseBuilder.ToString();
        }

        public override async Task<int> Add(string inputString, int id = 0)
        {
            int key = nextKey++;
            // sentence -> phrase
            List<int> sentenceIds = new List<int>();
            foreach ((int startIndex, int endIndex) in await Split(inputString))
            {
                string sentenceText = inputString.Substring(startIndex, endIndex - startIndex + 1);
                int sentenceId = await search.Add(sentenceText, id);
                sentenceIds.Add(sentenceId);

                sentenceToPhrase[sentenceId] = key;
            }
            // phrase -> sentence
            phraseToSentences[key] = sentenceIds.ToArray();

            // data split -> phrase
            if (dataSplitToPhrases.TryGetValue(id, out List<int> dataSplitPhrases)) dataSplitPhrases.Add(key);
            else dataSplitToPhrases[id] = new List<int>(){key};

            // hex -> phrase
            int hash = inputString.GetHashCode();
            if (!hexToPhrase.TryGetValue(hash, out int[] entries)) entries = new int[0];
            List<int> matchingHash = new List<int>(entries);
            matchingHash.Add(key);

            hexToPhrase[hash] = matchingHash.ToArray();
            return key;
        }

        public override void Remove(int key)
        {
            if (!phraseToSentences.TryGetValue(key, out int[] sentenceIds)) return;
            int hash = Get(key).GetHashCode();

            // phrase -> sentence
            phraseToSentences.Remove(key);
            foreach (int sentenceId in sentenceIds)
            {
                search.Remove(sentenceId);
                // sentence -> phrase
                sentenceToPhrase.Remove(sentenceId);
            }

            // data split -> phrase
            foreach (var dataSplitPhrases in dataSplitToPhrases.Values) dataSplitPhrases.Remove(key);

            // hex -> phrase
            if (hexToPhrase.TryGetValue(hash, out int[] phraseIds))
            {
                List<int> updatedIds = phraseIds.ToList();
                updatedIds.Remove(key);
                if (updatedIds.Count == 0) hexToPhrase.Remove(hash);
                else hexToPhrase[hash] = updatedIds.ToArray();
            }
        }

        public override int Remove(string inputString, int id = 0)
        {
            int hash = inputString.GetHashCode();
            if (!hexToPhrase.TryGetValue(hash, out int[] entries)) return 0;
            List<int> removeIds = new List<int>();
            foreach (int key in entries)
            {
                if (dataSplitToPhrases[id].Contains(key) && Get(key) == inputString) removeIds.Add(key);
            }
            foreach (int removeId in removeIds) Remove(removeId);
            return removeIds.Count;
        }

        public override int Count()
        {
            return phraseToSentences.Count;
        }

        public override int Count(int id)
        {
            if (!dataSplitToPhrases.TryGetValue(id, out List<int> dataSplitPhrases)) return 0;
            return dataSplitPhrases.Count;
        }

        public override async Task<int> IncrementalSearch(string queryString, int id = 0)
        {
            return await search.IncrementalSearch(queryString, id);
        }

        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k)
        {
            if (returnChunks)
            {
                return search.IncrementalFetchKeys(fetchKey, k);
            }
            else
            {
                List<int> phraseKeys = new List<int>();
                List<float> distancesList = new List<float>();
                bool done = false;
                bool completed;
                do
                {
                    int[] resultKeys;
                    float[] distancesIter;
                    (resultKeys, distancesIter, completed) = search.IncrementalFetchKeys(fetchKey, k);
                    for (int i = 0; i < resultKeys.Length; i++)
                    {
                        int phraseId = sentenceToPhrase[resultKeys[i]];
                        if (phraseKeys.Contains(phraseId)) continue;
                        phraseKeys.Add(phraseId);
                        distancesList.Add(distancesIter[i]);
                        if (phraseKeys.Count() == k)
                        {
                            done = true;
                            break;
                        }
                    }
                    if (completed) break;
                }
                while (!done);
                if (completed) IncrementalSearchComplete(fetchKey);
                return (phraseKeys.ToArray(), distancesList.ToArray(), completed);
            }
        }

        public override void IncrementalSearchComplete(int fetchKey)
        {
            search.IncrementalSearchComplete(fetchKey);
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
