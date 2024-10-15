using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using Cloud.Unum.USearch;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;

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
        public const string DefaultDelimiters = ".!:;?\n\r";
        [DataMember]
        string delimiters;

        public SentenceSplitter(string delimiters = DefaultDelimiters)
        {
            this.delimiters = delimiters;
        }

        public List<(int, int)> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            int startIndex = 0;
            bool sawDelimiter = true;
            for (int i = 0; i < input.Length; i++)
            {
                if (sawDelimiter)
                {
                    while (char.IsWhiteSpace(input[i]) && i < input.Length - 1) i++;
                    startIndex = i;
                    sawDelimiter = false;
                }
                if (delimiters.Contains(input[i]) || i == input.Length - 1)
                {
                    int endIndex = i;
                    if (i == input.Length - 1)
                    {
                        while (char.IsWhiteSpace(input[endIndex]) && endIndex > startIndex) endIndex--;
                    }
                    if (endIndex > startIndex || (!char.IsWhiteSpace(input[startIndex]) && !delimiters.Contains(input[startIndex])))
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
    public class SearchEngine
    {
        [DataMember]
        SortedDictionary<int, Phrase> phrases;
        [DataMember]
        SortedDictionary<int, Sentence> sentences;
        [DataMember]
        int nextPhraseId = 0;
        [DataMember]
        int nextSentenceId = 0;
        [DataMember]
        SentenceSplitter sentenceSplitter;
        SearchMethod searchMethod;

        public SearchEngine(
            LLM llm,
            string delimiters = SentenceSplitter.DefaultDelimiters,
            ScalarKind quantization = ScalarKind.Float16,
            MetricKind metricKind = MetricKind.Cos,
            ulong connectivity = 32,
            ulong expansionAdd = 40,
            ulong expansionSearch = 16
        )
        {
            phrases = new SortedDictionary<int, Phrase>();
            sentences = new SortedDictionary<int, Sentence>();
            sentenceSplitter = delimiters == null ? null : new SentenceSplitter(delimiters);
            searchMethod = new ANNModelSearch(llm);
        }

        public void SetSearchMethod(ANNModelSearch searchMethod)
        {
            this.searchMethod = searchMethod;
        }

        public void SetLLM(LLM llm)
        {
            searchMethod.SetLLM(llm);
        }

        public string GetPhrase(Sentence sentence)
        {
            return phrases[sentence.phraseId].text;
        }

        public string GetSentence(Sentence sentence)
        {
            return GetPhrase(sentence).Substring(sentence.startIndex, sentence.endIndex - sentence.startIndex + 1);
        }

        public async void Add(string text)
        {
            List<(int, int)> subindices;
            if (sentenceSplitter == null) subindices = new List<(int, int)> { (0, text.Length - 1) };
            else subindices = sentenceSplitter.Split(text);

            int phraseId = nextPhraseId++;
            Phrase phrase = new Phrase(text);
            phrases[phraseId] = phrase;
            foreach ((int startIndex, int endIndex) in subindices)
            {
                int sentenceId = nextSentenceId++;
                Sentence sentence = new Sentence(phraseId, startIndex, endIndex);
                sentences[sentenceId] = sentence;
                phrase.sentenceIds.Add(sentenceId);
                string sentenceText = GetSentence(sentence);
                await searchMethod.Add(sentenceId, sentenceText);
            }
        }

        public int Remove(string text)
        {
            List<int> removePhraseIds = new List<int>();
            foreach (var phrasePair in phrases)
            {
                Phrase phrase = phrasePair.Value;
                if (phrase.text == text)
                {
                    foreach (int sentenceId in phrase.sentenceIds)
                    {
                        sentences.Remove(sentenceId);
                        searchMethod.Remove(sentenceId);
                    }
                    removePhraseIds.Add(phrasePair.Key);
                }
            }
            foreach (int phraseId in removePhraseIds)
            {
                phrases.Remove(phraseId);
            }
            return removePhraseIds.Count;
        }

        public List<string> GetPhrases()
        {
            List<string> phraseTexts = new List<string>();
            foreach (Phrase phrase in phrases.Values)
            {
                phraseTexts.Add(phrase.text);
            }
            return phraseTexts;
        }

        public List<string> GetSentences()
        {
            List<string> allSentences = new List<string>();
            foreach (Sentence sentence in sentences.Values)
            {
                allSentences.Add(GetSentence(sentence));
            }
            return allSentences;
        }

//TODO
/*
        public string[] Search(string queryString, int k, out float[] distances, bool returnSentences = false)
        {
            return Search(searchMethod.Encode(queryString), k, out distances, returnSentences);
        }

        public string[] Search(string queryString, int k = 1, bool returnSentences = false)
        {
            return Search(queryString, k, out float[] distances, returnSentences);
        }
        public string[] SearchPhrases(string queryString, int k, out float[] distances)
        {
            return Search(queryString, k, out distances, false);
        }
        public string[] SearchSentences(string queryString, int k, out float[] distances)
        {
            return Search(queryString, k, out distances, true);
        }
*/

        public async Task<string[]> Search(string queryString, int k, bool returnSentences = false)
        {
            return Search(await searchMethod.Encode(queryString), k, out float[] distances, returnSentences);
        }

        public string[] Search(float[] encoding, int k, out float[] distances, bool returnSentences = false)
        {
            if (returnSentences)
            {
                int[] keys = searchMethod.Search(encoding, k, out distances);
                string[] result = new string[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    Sentence sentence = sentences[keys[i]];
                    result[i] = returnSentences ? GetSentence(sentence) : GetPhrase(sentence);
                }
                return result;
            }
            else
            {
                List<int> phraseKeys;
                List<float> phraseDistances;
                int currK = k;
                do
                {
                    int[] keys = searchMethod.Search(encoding, currK, out float[] iterDistances);
                    phraseDistances = new List<float>();
                    phraseKeys = new List<int>();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        int phraseId = sentences[keys[i]].phraseId;
                        if (phraseKeys.Contains(phraseId)) continue;
                        phraseKeys.Add(phraseId);
                        phraseDistances.Add(iterDistances[i]);
                    }
                    if (currK >= searchMethod.Count()) break;
                    currK *= 2;
                }
                while (phraseKeys.Count() < k);

                distances = phraseDistances.ToArray();
                string[] result = new string[phraseKeys.Count];
                for (int i = 0; i < phraseKeys.Count; i++)
                    result[i] = phrases[phraseKeys[i]].text;
                return result;
            }
        }

        public string[] Search(float[] encoding, int k = 1, bool returnSentences = false)
        {
            return Search(encoding, k, out float[] distances, returnSentences);
        }

        public async Task<string[]> SearchPhrases(string queryString, int k = 1)
        {
            return await Search(queryString, k, false);
        }

        public async Task<string[]> SearchSentences(string queryString, int k = 1)
        {
            return await Search(queryString, k, true);
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

        public static string GetSearchPath(string dirname = "")
        {
            return Path.Combine(dirname, "SearchEngine.json");
        }

        public void Save(ZipArchive archive, string dirname = "")
        {
            ZipArchiveEntry mainEntry = archive.CreateEntry(GetSearchPath(dirname));
            using (Stream entryStream = mainEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(GetType());
                serializer.WriteObject(entryStream, this);
            }
            searchMethod.Save(archive, dirname);
        }

        public static SearchEngine Load(LLM llm, string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load(llm, archive, dirname);
            }
        }

        public static SearchEngine Load(LLM llm, ZipArchive archive, string dirname = "")
        {
            SearchEngine search;
            ZipArchiveEntry baseEntry = archive.GetEntry(GetSearchPath(dirname));
            using (Stream entryStream = baseEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SearchEngine));
                search = (SearchEngine)serializer.ReadObject(entryStream);
            }
            ANNModelSearch searchMethod = ANNModelSearch.Load(llm, archive, dirname);
            search.SetSearchMethod(searchMethod);
            return search;
        }
    }
}
