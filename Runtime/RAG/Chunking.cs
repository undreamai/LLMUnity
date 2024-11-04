/// @file
/// @brief File implementing the chunking functionality
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMUnity
{
    /// @ingroup rag
    /// <summary>
    /// Class implementing the chunking functionality
    /// </summary>
    [Serializable]
    public abstract class Chunking : SearchPlugin
    {
        protected bool returnChunks = false;
        protected Dictionary<string, List<int>> dataSplitToPhrases = new Dictionary<string, List<int>>();
        protected Dictionary<int, int[]> phraseToSentences = new Dictionary<int, int[]>();
        protected Dictionary<int, int> sentenceToPhrase = new Dictionary<int, int>();
        protected Dictionary<int, int[]> hexToPhrase = new Dictionary<int, int[]>();
        protected int nextKey = 0;

        /// <summary>
        /// Set to true to return chunks or the direct input with the Search function
        /// </summary>
        /// <param name="returnChunks">whether to return chunks</param>
        public void ReturnChunks(bool returnChunks)
        {
            this.returnChunks = returnChunks;
        }

        /// <summary>
        /// Splits the provided phrase into chunks
        /// </summary>
        /// <param name="input">phrase</param>
        /// <returns>List of start/end indices of the split chunks</returns>
        public abstract Task<List<(int, int)>> Split(string input);

        /// <summary>
        /// Retrieves the phrase with the specific id
        /// </summary>
        /// <param name="key">phrase id</param>
        /// <returns>phrase</returns>
        public override string Get(int key)
        {
            StringBuilder phraseBuilder = new StringBuilder();
            foreach (int sentenceId in phraseToSentences[key])
            {
                phraseBuilder.Append(search.Get(sentenceId));
            }
            return phraseBuilder.ToString();
        }

        /// <summary>
        /// Adds a phrase to the search after splitting it into chunks.
        /// </summary>
        /// <param name="inputString">input phrase</param>
        /// <param name="group">data group to add it to </param>
        /// <returns>phrase id</returns>
        public override async Task<int> Add(string inputString, string group = "")
        {
            int key = nextKey++;
            // sentence -> phrase
            List<int> sentenceIds = new List<int>();
            foreach ((int startIndex, int endIndex) in await Split(inputString))
            {
                string sentenceText = inputString.Substring(startIndex, endIndex - startIndex + 1);
                int sentenceId = await search.Add(sentenceText, group);
                sentenceIds.Add(sentenceId);

                sentenceToPhrase[sentenceId] = key;
            }
            // phrase -> sentence
            phraseToSentences[key] = sentenceIds.ToArray();

            // data split -> phrase
            if (!dataSplitToPhrases.ContainsKey(group)) dataSplitToPhrases[group] = new List<int>(){key};
            else dataSplitToPhrases[group].Add(key);

            // hex -> phrase
            int hash = inputString.GetHashCode();
            if (!hexToPhrase.TryGetValue(hash, out int[] entries)) entries = new int[0];
            List<int> matchingHash = new List<int>(entries);
            matchingHash.Add(key);

            hexToPhrase[hash] = matchingHash.ToArray();
            return key;
        }

        /// <summary>
        /// Removes a phrase and the phrase chunks from the search
        /// </summary>
        /// <param name="key">phrase id</param>
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

        /// <summary>
        /// Removes a phrase and the phrase chunks from the search.
        /// </summary>
        /// <param name="inputString">input phrase</param>
        /// <param name="group">data group to remove it from </param>
        /// <returns>number of removed phrases</returns>
        public override int Remove(string inputString, string group = "")
        {
            int hash = inputString.GetHashCode();
            if (!hexToPhrase.TryGetValue(hash, out int[] entries)) return 0;
            List<int> removeIds = new List<int>();
            foreach (int key in entries)
            {
                if (dataSplitToPhrases[group].Contains(key) && Get(key) == inputString) removeIds.Add(key);
            }
            foreach (int removeId in removeIds) Remove(removeId);
            return removeIds.Count;
        }

        /// <summary>
        /// Returns a count of the phrases
        /// </summary>
        /// <returns>phrase count</returns>
        public override int Count()
        {
            return phraseToSentences.Count;
        }

        /// <summary>
        /// Returns a count of the phrases in a specific data group
        /// </summary>
        /// <param name="group">data group</param>
        /// <returns>phrase count</returns>
        public override int Count(string group)
        {
            if (!dataSplitToPhrases.TryGetValue(group, out List<int> dataSplitPhrases)) return 0;
            return dataSplitPhrases.Count;
        }

        /// <summary>
        /// Allows to do search and retrieve results in batches (incremental search).
        /// </summary>
        /// <param name="queryString">search query</param>
        /// <param name="group">data group to search in</param>
        /// <returns>incremental search key</returns>
        public override async Task<int> IncrementalSearch(string queryString, string group = "")
        {
            return await search.IncrementalSearch(queryString, group);
        }

        /// <summary>
        /// Retrieves the most similar search results in batches (incremental search).
        /// The phrase/chunk keys and distances are retrieved, as well as a parameter that dictates whether the search is exhausted.
        /// The returnChunks variable defines whether to return chunks or phrases.
        /// </summary>
        /// <param name="fetchKey">incremental search key</param>
        /// <param name="k">number of results to retrieve</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Array of retrieved keys (`int[]`).</description></item>
        /// <item><description>Array of distances for each result (`float[]`).</description></item>
        /// <item><description>`bool` indicating if the search is exhausted.</description></item>
        /// </list>
        /// </returns>
        public override ValueTuple<int[], float[], bool> IncrementalFetchKeys(int fetchKey, int k)
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

        /// <summary>
        /// Retrieves the most similar search results in batches (incremental search).
        /// The phrases/chunks and their distances are retrieved, as well as a parameter that dictates whether the search is exhausted.
        /// The returnChunks variable defines whether to return chunks or phrases.
        /// </summary>
        /// <param name="fetchKey">incremental search key</param>
        /// <param name="k">number of results to retrieve</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Array of retrieved phrases/chunks (`string[]`).</description></item>
        /// <item><description>Array of distances for each result (`float[]`).</description></item>
        /// <item><description>`bool` indicating if the search is exhausted.</description></item>
        /// </list>
        /// </returns>
        public override ValueTuple<string[], float[], bool> IncrementalFetch(int fetchKey, int k)
        {
            (int[] resultKeys, float[] distances, bool completed) = IncrementalFetchKeys(fetchKey, k);
            string[] results = new string[resultKeys.Length];
            for (int i = 0; i < resultKeys.Length; i++)
            {
                if (returnChunks) results[i] = search.Get(resultKeys[i]);
                else results[i] = Get(resultKeys[i]);
            }
            return (results, distances, completed);
        }

        /// <summary>
        /// Completes the search and clears the cached results for an incremental search
        /// </summary>
        /// <param name="fetchKey">incremental search key</param>
        public override void IncrementalSearchComplete(int fetchKey)
        {
            search.IncrementalSearchComplete(fetchKey);
        }

        /// <summary>
        /// Clears the object and the associated search object
        /// </summary>
        public override void Clear()
        {
            nextKey = 0;
            dataSplitToPhrases.Clear();
            phraseToSentences.Clear();
            sentenceToPhrase.Clear();
            hexToPhrase.Clear();
            search.Clear();
        }

        protected override void SaveInternal(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, dataSplitToPhrases, GetSavePath("dataSplitToPhrases"));
            ArchiveSaver.Save(archive, phraseToSentences, GetSavePath("phraseToSentences"));
            ArchiveSaver.Save(archive, sentenceToPhrase, GetSavePath("sentenceToPhrase"));
            ArchiveSaver.Save(archive, hexToPhrase, GetSavePath("hexToPhrase"));
            ArchiveSaver.Save(archive, nextKey, GetSavePath("nextKey"));
        }

        protected override void LoadInternal(ZipArchive archive)
        {
            dataSplitToPhrases = ArchiveSaver.Load<Dictionary<string, List<int>>>(archive, GetSavePath("dataSplitToPhrases"));
            phraseToSentences = ArchiveSaver.Load<Dictionary<int, int[]>>(archive, GetSavePath("phraseToSentences"));
            sentenceToPhrase = ArchiveSaver.Load<Dictionary<int, int>>(archive, GetSavePath("sentenceToPhrase"));
            hexToPhrase = ArchiveSaver.Load<Dictionary<int, int[]>>(archive, GetSavePath("hexToPhrase"));
            nextKey = ArchiveSaver.Load<int>(archive, GetSavePath("nextKey"));
        }
    }
}
