using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Unity.Sentis;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using Cloud.Unum.USearch;

namespace LLMUnity
{
    [DataContract]
    public class Dialogue
    {
        Dictionary<string, Dictionary<string, SearchEngine>> dialogueParts;
        [DataMember]
        string delimiters;
        [DataMember]
        EmbeddingModel embedder;
        [DataMember]
        ScalarKind quantization;

        public Dialogue(
            EmbeddingModel embedder,
            string delimiters = SentenceSplitter.DefaultDelimiters,
            ScalarKind quantization = ScalarKind.Float16
        )
        {
            dialogueParts = new Dictionary<string, Dictionary<string, SearchEngine>>();
            this.embedder = embedder;
            this.delimiters = delimiters;
            this.quantization = quantization;
        }

        public EmbeddingModel GetEmbedder()
        {
            return embedder;
        }

        public void SetEmbedder(EmbeddingModel model)
        {
            embedder = model;
        }

        public void Add(string text, string actor="", string title = "")
        {
            if (!dialogueParts.ContainsKey(actor) || !dialogueParts[actor].ContainsKey(title))
            {
                SearchEngine search = new SearchEngine(embedder, delimiters, quantization);
                if (!dialogueParts.ContainsKey(actor))
                {
                    dialogueParts[actor] = new Dictionary<string, SearchEngine>();
                }
                dialogueParts[actor][title] = search;
            }
            dialogueParts[actor][title].Add(text);
        }

        public int Remove(string text, string actor=null, string title = null)
        {
            List<SearchEngine> dialogueParts = Filter(actor, title);
            int removed = 0;
            foreach (SearchEngine dialogue in dialogueParts)
            {
                removed += dialogue.Remove(text);
            }
            return removed;
        }

        public List<SearchEngine> Filter(string actor = null, string title = null)
        {
            List<SearchEngine> filtering = new List<SearchEngine>();
            foreach ((string actorName, Dictionary<string, SearchEngine> actorDialogues) in dialogueParts)
            {
                foreach ((string titleName, SearchEngine dialogue) in actorDialogues)
                {
                    if ((actor == null || actor == actorName) && (title == null || title == titleName))
                    {
                        filtering.Add(dialogue);
                    }
                }
            }
            return filtering;
        }

        public string[] Get(string actor = null, string title = null, bool returnSentences = false)
        {
            List<SearchEngine> dialogueParts = Filter(actor, title);
            List<string> result = new List<string>();
            foreach (SearchEngine dialogue in dialogueParts)
            {
                result.AddRange(returnSentences ? dialogue.GetSentences() : dialogue.GetPhrases());
            }
            return result.ToArray();
        }

        public string[] GetPhrases(string actor = null, string title = null)
        {
            return Get(actor, title, false);
        }

        public string[] GetSentences(string actor = null, string title = null)
        {
            return Get(actor, title, true);
        }

        public string[] Search(string queryString, int k, out float[] distances, string actor = null, string title = null, bool returnSentences = false)
        {
            List<SearchEngine> dialogueParts = Filter(actor, title);
            if (dialogueParts.Count == 0)
            {
                distances = null;
                return null;
            }
            if (dialogueParts.Count == 1)
            {
                return dialogueParts[0].Search(queryString, k, out distances, returnSentences);
            }

            TensorFloat encodingTensor = embedder.Encode(queryString);
            encodingTensor.MakeReadable();
            float[] encoding = encodingTensor.ToReadOnlyArray();

            ConcurrentBag<(string, float)> resultPairs = new ConcurrentBag<(string, float)>();
            Task.Run(() =>
            {
                Parallel.ForEach(dialogueParts, dialogue =>
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

        public string[] Search(string queryString, int k=1, string actor = null, string title = null, bool returnSentences = false)
        {
            return Search(queryString, k, out float[] distances, actor, title, returnSentences);
        }

        public string[] SearchPhrases(string queryString, int k, out float[] distances, string actor = null, string title = null)
        {
            return Search(queryString, k, out distances, actor, title, false);
        }

        public string[] SearchPhrases(string queryString, int k=1, string actor = null, string title = null)
        {
            return SearchPhrases(queryString, k, out float[] distances, actor, title);
        }

        public string[] SearchSentences(string queryString, int k, out float[] distances, string actor = null, string title = null)
        {
            return Search(queryString, k, out distances, actor, title, true);
        }

        public string[] SearchSentences(string queryString, int k=1, string actor = null, string title = null)
        {
            return SearchSentences(queryString, k, out float[] distances, actor, title);
        }

        public int NumPhrases(string actor = null, string title = null)
        {
            int num = 0;
            List<SearchEngine> dialogueParts = Filter(actor, title);
            foreach (SearchEngine dialogue in dialogueParts)
            {
                num += dialogue.NumPhrases();
            }
            return num;
        }

        public int NumSentences(string actor = null, string title = null)
        {
            int num = 0;
            List<SearchEngine> dialogueParts = Filter(actor, title);
            foreach (SearchEngine dialogue in dialogueParts)
            {
                num += dialogue.NumSentences();
            }
            return num;
        }

        public static string GetModelHashPath(string dirname)
        {
            return Path.Combine(dirname, "EmbedderHash.txt");
        }

        public static string GetDialoguePath(string dirname)
        {
            return Path.Combine(dirname, "Dialogue.json");
        }

        public static string GetDialogueEntriesPath(string dirname)
        {
            return Path.Combine(dirname, "DialogueEntries.csv");
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
            Saver.Save(this, archive, GetDialoguePath(dirname));
            
            embedder.SaveHashCode(archive, dirname);

            List<string> dialoguePartLines = new List<string>();
            foreach ((string actorName, Dictionary<string, SearchEngine> actorDialogues) in dialogueParts)
            {
                foreach ((string titleName, SearchEngine dialogue) in actorDialogues)
                {
                    string basedir = $"{dirname}/Dialogues/{Saver.EscapeFileName(actorName)}/{Saver.EscapeFileName(titleName)}";
                    dialogue.Save(archive, basedir);

                    dialoguePartLines.Add(actorName);
                    dialoguePartLines.Add(titleName);
                    dialoguePartLines.Add(basedir);
                }
            }

            ZipArchiveEntry dialoguesEntry = archive.CreateEntry(GetDialogueEntriesPath(dirname));
            using (StreamWriter writer = new StreamWriter(dialoguesEntry.Open()))
            {
                foreach (string line in dialoguePartLines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static Dialogue Load(EmbeddingModel embedder, string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Dialogue dialogue = Saver.Load<Dialogue>(archive, GetDialoguePath(dirname));

                int embedderHash = EmbeddingModel.LoadHashCode(archive, dirname);
                if (embedder.GetHashCode() != embedderHash)
                    throw new Exception($"The Dialogue object uses different embedding model than the Dialogue object stored in {filePath}");
                dialogue.SetEmbedder(embedder);

                ZipArchiveEntry dialoguesEntry = archive.GetEntry(GetDialogueEntriesPath(dirname));
                List<string> dialogueDirs = new List<string>();
                dialogue.dialogueParts = new Dictionary<string, Dictionary<string, SearchEngine>>();
                using (StreamReader reader = new StreamReader(dialoguesEntry.Open()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string actor = line;
                        string title = reader.ReadLine();
                        string basedir = reader.ReadLine();
                        if (!dialogue.dialogueParts.ContainsKey(actor))
                        {
                            dialogue.dialogueParts[actor] = new Dictionary<string, SearchEngine>();
                        }
                        dialogue.dialogueParts[actor][title] = SearchEngine.Load(embedder, archive, basedir);
                    }
                }
                return dialogue;
            }
        }
    }
}
