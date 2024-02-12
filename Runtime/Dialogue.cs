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
    public class Dialogue
    {
        Dictionary<string, Dictionary<string, SearchEngine>> dialogueParts;
        [DataMember]
        char[] delimiters = SentenceSplitter.DefaultDelimiters;
        [DataMember]
        bool trimSentences = true;
        [DataMember]
        EmbeddingModel embedder;

        public Dialogue(EmbeddingModel embedder)
        {
            dialogueParts = new Dictionary<string, Dictionary<string, SearchEngine>>();
            this.embedder = embedder;
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
            if (!dialogueParts.ContainsKey(actor) || !dialogueParts[actor].ContainsKey(title))
            {
                SearchEngine search = new SearchEngine(embedder);
                search.SetSentenceSplitting(delimiters, trimSentences);
                if (!dialogueParts.ContainsKey(actor))
                {
                    dialogueParts[actor] = new Dictionary<string, SearchEngine>();
                }
                dialogueParts[actor][title] = search;
            }
            dialogueParts[actor][title].Add(text);
        }

        public void Add(string actor, string text)
        {
            Add(actor, null, text);
        }

        public int Remove(string actor, string title, string text)
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
            ConcurrentBag<(string, float)> resultPairs = new ConcurrentBag<(string, float)>();

            TensorFloat encodingTensor = embedder.Encode(queryString);
            encodingTensor.MakeReadable();
            float[] encoding = encodingTensor.ToReadOnlyArray();
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

            embedder.Save(archive, dirname);

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

        public static Dialogue Load(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Dialogue dialogue = Saver.Load<Dialogue>(archive, GetDialoguePath(dirname));

                dialogue.embedder = EmbeddingModel.Load(archive, dirname);

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
                        dialogue.dialogueParts[actor][title] = SearchEngine.Load(archive, basedir);
                    }
                }
                return dialogue;
            }
        }
    }
}
