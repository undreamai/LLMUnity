using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LLMUnity
{
    [DataContract]
    public abstract class SearchPlugin : ISearchable
    {
        [DataMember] public SearchMethod search;

        public abstract string Get(int key);
        public abstract Task<int> Add(string inputString);
        public abstract int Remove(string inputString);
        public abstract void Remove(int key);
        public abstract int Count();
        public abstract Task<string[]> Search(string queryString, int k);
        public abstract void Clear();

        public static string GetSavePath(string dirname = "")
        {
            return Path.Combine(dirname, "search.json");
        }

        public virtual void Save(string filePath, string dirname = "")
        {
            ArchiveSaver.Save(this, filePath, GetSavePath(dirname));
        }

        public virtual void Save(ZipArchive archive, string dirname = "")
        {
            ArchiveSaver.Save(this, archive, GetSavePath(dirname));
        }

        public static T Load<T>(string filePath, string dirname = "") where T : SearchMethod
        {
            return ArchiveSaver.Load<T>(filePath, GetSavePath(dirname));
        }

        public static T Load<T>(ZipArchive archive, string dirname = "") where T : SearchMethod
        {
            return ArchiveSaver.Load<T>(archive, GetSavePath(dirname));
        }
    }

    [DataContract]
    public class SentenceSplitter : SearchPlugin
    {
        public const string DefaultDelimiters = ".!:;?\n\r";
        public bool returnChunks = false;
        [DataMember] protected int nextKey = 0;
        [DataMember] public string delimiters = DefaultDelimiters;
        [DataMember] public Dictionary<int, int[]> phraseToSentences = new Dictionary<int, int[]>();
        [DataMember] public Dictionary<int, int> sentenceToPhrase = new Dictionary<int, int>();

        public List<(int, int)> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            int startIndex = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (delimiters.Contains(input[i]) || i == input.Length - 1)
                {
                    if (i > startIndex) indices.Add((startIndex, i));
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
                sentenceIds.Add(await search.Add(sentenceText));
            }
            phraseToSentences[key] = sentenceIds.ToArray();
            return key;
        }

        public override void Remove(int key)
        {
            phraseToSentences.TryGetValue(key, out int[] sentenceIds);
            if (sentenceIds == null) return;
            phraseToSentences.Remove(key);
            foreach (int sentenceId in sentenceIds) search.Remove(sentenceId);
        }

        public override int Remove(string inputString)
        {
            List<int> removeIds = new List<int>();
            foreach (var entry in phraseToSentences)
            {
                string phrase = "";
                foreach (int sentenceId in entry.Value)
                {
                    phrase += search.Get(sentenceId);
                    if (phrase.Length > inputString.Length) break;
                }
                if (phrase == inputString) removeIds.Add(entry.Key);
            }
            foreach (int id in removeIds) Remove(id);
            return removeIds.Count;
        }

        public override int Count()
        {
            return phraseToSentences.Count;
        }

        public override async Task<string[]> Search(string queryString, int k)
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
                bool complete;
                do
                {
                    int[] resultKeys;
                    (resultKeys, complete) = search.IncrementalFetchKeys(searchKey, k);
                    for (int i = 0; i < resultKeys.Length; i++)
                    {
                        int phraseId = sentenceToPhrase[resultKeys[i]];
                        if (phraseKeys.Contains(phraseId)) continue;
                        phraseKeys.Add(phraseId);
                        phrases.Add(Get(phraseId));
                        if (phraseKeys.Count() == k)
                        {
                            complete = true;
                            break;
                        }
                    }
                }
                while (!complete);
                return phrases.ToArray();
            }
        }

        public override void Clear()
        {
            nextKey = 0;
            phraseToSentences.Clear();
            sentenceToPhrase.Clear();
            search.Clear();
        }
    }
}
