using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using System;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

namespace LLMUnityTests
{
    public abstract class TestSearchable<T> where T : Searchable
    {
        protected string weather = "how is the weather today?";
        protected string raining = "is it raining?";
        protected string sometext = "something completely sometext";
        protected float weatherRainingDiff = 0.2073563f;
        protected float weatherSometextDiff = 0.4837922f;

        protected string modelNameLLManager;
        protected GameObject gameObject;
        protected LLM llm;
        public T search;
        protected Exception error = null;

        public TestSearchable()
        {
            Task task = Init();
            task.Wait();
        }

        public virtual async Task Init()
        {
            await DownloadModels();
            gameObject = new GameObject();
            gameObject.SetActive(false);
            llm = CreateLLM();
            search = CreateSearch();
            gameObject.SetActive(true);
        }

        public virtual LLM CreateLLM()
        {
            LLM llm = gameObject.AddComponent<LLM>();
            llm.SetModel(modelNameLLManager);
            llm.parallelPrompts = 1;
            return llm;
        }

        public abstract T CreateSearch();

        public virtual async Task DownloadModels()
        {
            modelNameLLManager = await LLMManager.DownloadModel(GetModelUrl());
        }

        protected virtual string GetModelUrl()
        {
            return "https://huggingface.co/CompendiumLabs/bge-small-en-v1.5-gguf/resolve/main/bge-small-en-v1.5-f16.gguf";
        }

        public static bool ApproxEqual(float x1, float x2)
        {
            float tolerance = (Application.platform == RuntimePlatform.OSXPlayer) ? 0.001f : 0.0001f;
            return Mathf.Abs(x1 - x2) < tolerance;
        }

        [UnityTest]
        public virtual IEnumerator RunTests()
        {
            Task task = RunTestsTask();
            while (!task.IsCompleted) yield return null;
            if (error != null)
            {
                Debug.LogError(error.ToString());
                throw (error);
            }
        }

        public virtual async Task RunTestsTask()
        {
            error = null;
            try
            {
                await Tests();
                llm.OnDestroy();
            }
            catch  (Exception e)
            {
                error = e;
            }
        }

        public virtual async Task Tests()
        {
            await TestAdd();
            await TestSearch();
            await TestIncrementalSearch();
            await TestSaveLoad();
        }

        public virtual async Task TestAdd()
        {
            void CheckCount(int[] nums)
            {
                int sum = 0;
                for (int i = 0; i < nums.Length; i++)
                {
                    Assert.That(search.Count(i.ToString()) == nums[i]);
                    sum += nums[i];
                }
                Assert.That(search.Count() == sum);
            }

            int key, num;
            key = await search.Add(weather);
            Assert.That(key == 0);
            Assert.That(search.Get(key) == weather);
            Assert.That(search.Count() == 1);
            search.Remove(key);
            Assert.That(search.Count() == 0);

            key = await search.Add(weather);
            Assert.That(key == 1);
            key = await search.Add(raining);
            Assert.That(key == 2);
            key = await search.Add(sometext);
            Assert.That(key == 3);
            Assert.That(search.Count() == 3);
            search.Clear();
            Assert.That(search.Count() == 0);

            key = await search.Add(weather, "0");
            Assert.That(key == 0);
            key = await search.Add(raining, "0");
            Assert.That(key == 1);
            key = await search.Add(weather, "1");
            Assert.That(key == 2);
            key = await search.Add(sometext, "1");
            Assert.That(key == 3);
            key = await search.Add(sometext, "2");
            Assert.That(key == 4);
            CheckCount(new int[] {2, 2, 1});
            num = search.Remove(weather, "0");
            Assert.That(num == 1);
            CheckCount(new int[] {1, 2, 1});
            num = search.Remove(weather, "1");
            Assert.That(num == 1);
            CheckCount(new int[] {1, 1, 1});
            num = search.Remove(weather, "0");
            Assert.That(num == 0);
            CheckCount(new int[] {1, 1, 1});
            num = search.Remove(raining, "0");
            Assert.That(num == 1);
            CheckCount(new int[] {0, 1, 1});
            num = search.Remove(sometext, "1");
            Assert.That(num == 1);
            CheckCount(new int[] {0, 0, 1});
            num = search.Remove(sometext, "2");
            Assert.That(num == 1);
            CheckCount(new int[] {0, 0, 0});

            search.Clear();
            Assert.That(search.Count() == 0);
        }

        public virtual async Task TestSearch()
        {
            string[] results;
            float[] distances;

            (results, distances) = await search.Search(weather, 1);
            Assert.That(results.Length == 0);
            Assert.That(distances.Length == 0);

            await search.Add(weather);
            await search.Add(raining);
            await search.Add(sometext);

            (results, distances) = await search.Search(weather, 2);
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], raining);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherRainingDiff));

            (results, distances) = await search.Search(raining, 2);
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], weather);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherRainingDiff));

            search.Clear();

            await search.Add(weather, "0");
            await search.Add(raining, "1");
            await search.Add(sometext, "0");
            await search.Add(sometext, "1");

            (results, distances) = await search.Search(weather, 2, "0");
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));

            (results, distances) = await search.Search(weather, 2, "0");
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));

            (results, distances) = await search.Search(weather, 2, "1");
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));

            (results, distances) = await search.Search(weather, 3, "1");
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));
            search.Clear();
        }

        public async Task TestIncrementalSearch()
        {
            string[] results;
            float[] distances;
            bool completed;

            int searchKey = await search.IncrementalSearch(weather);
            (results, distances, completed) = search.IncrementalFetch(searchKey, 1);
            Assert.That(searchKey == 0);
            Assert.That(results.Length == 0);
            Assert.That(distances.Length == 0);
            Assert.That(completed);
            search.Clear();

            await search.Add(weather);
            await search.Add(raining);
            await search.Add(sometext);

            searchKey = await search.IncrementalSearch(weather);
            (results, distances, completed) = search.IncrementalFetch(searchKey, 1);
            Assert.That(searchKey == 0);
            Assert.That(results.Length == 1);
            Assert.That(distances.Length == 1);
            Assert.AreEqual(results[0], weather);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(!completed);

            (results, distances, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(results.Length == 2);
            Assert.That(distances.Length == 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], weatherRainingDiff));
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));
            Assert.That(completed);

            searchKey = await search.IncrementalSearch(weather);
            (results, distances, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(searchKey == 1);
            Assert.That(results.Length == 2);
            Assert.That(distances.Length == 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], raining);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherRainingDiff));
            Assert.That(!completed);

            search.IncrementalSearchComplete(searchKey);
            search.Clear();

            await search.Add(weather, "0");
            await search.Add(raining, "1");
            await search.Add(sometext, "0");
            await search.Add(sometext, "1");

            searchKey = await search.IncrementalSearch(weather, "0");
            (results, distances, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(searchKey == 0);
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));
            Assert.That(completed);

            searchKey = await search.IncrementalSearch(weather, "0");
            (results, distances, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(searchKey == 1);
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));
            Assert.That(completed);

            searchKey = await search.IncrementalSearch(weather, "1");
            (results, distances, completed) = search.IncrementalFetch(searchKey, 1);
            Assert.That(searchKey == 2);
            Assert.AreEqual(results.Length, 1);
            Assert.AreEqual(distances.Length, 1);
            Assert.AreEqual(results[0], raining);
            Assert.That(!completed);

            (results, distances, completed) = search.IncrementalFetch(searchKey, 1);
            Assert.AreEqual(results.Length, 1);
            Assert.AreEqual(distances.Length, 1);
            Assert.AreEqual(results[0], sometext);
            Assert.That(ApproxEqual(distances[0], weatherSometextDiff));
            Assert.That(completed);

            searchKey = await search.IncrementalSearch(weather, "1");
            (results, distances, completed) = search.IncrementalFetch(searchKey, 3);
            Assert.That(searchKey == 3);
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));
            Assert.That(completed);
            search.Clear();
        }

        public virtual async Task TestSaveLoad()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string[] results;
            float[] distances;

            await search.Add(weather);
            await search.Add(raining);
            await search.Add(sometext);
            search.Save(path);

            search.Clear();
            await search.Load(path);
            File.Delete(path);

            Assert.That(search.Count() == 3);
            Assert.That(search.Get(0) == weather);
            Assert.That(search.Get(1) == raining);
            Assert.That(search.Get(2) == sometext);

            (results, distances) = await search.Search(raining, 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], weather);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], weatherRainingDiff));

            search.Clear();

            await search.Add(weather, "0");
            await search.Add(raining, "1");
            await search.Add(sometext, "0");
            await search.Add(sometext, "1");
            search.Save(path);

            search.Clear();
            await search.Load(path);
            File.Delete(path);

            Assert.That(search.Count() == 4);
            Assert.That(search.Count("0") == 2);
            Assert.That(search.Count("1") == 2);
            Assert.That(search.Get(0) == weather);
            Assert.That(search.Get(1) == raining);
            Assert.That(search.Get(2) == sometext);
            Assert.That(search.Get(3) == sometext);

            (results, distances) = await search.Search(raining, 2, "0");
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], weatherRainingDiff));

            search.Clear();
        }
    }

    public abstract class TestSearchMethod<T> : TestSearchable<T> where T : SearchMethod
    {
        public override T CreateSearch()
        {
            T search = gameObject.AddComponent<T>();
            search.SetLLM(llm);
            return search;
        }

        public override async Task Tests()
        {
            await base.Tests();
            await TestEncode();
            await TestSimilarity();
            await TestSearchFromList();
        }

        public async Task TestEncode()
        {
            float[] encoding = await search.Encode(weather);
            Assert.That(ApproxEqual(encoding[0], -0.02910374f));
            Assert.That(ApproxEqual(encoding[383], 0.01764517f));
        }

        public async Task TestSimilarity()
        {
            float[] sentence1 = await search.Encode(weather);
            float[] sentence2 = await search.Encode(raining);
            float similarity = SimpleSearch.DotProduct(sentence1, sentence2);
            float distance = SimpleSearch.InverseDotProduct(sentence1, sentence2);
            Assert.That(ApproxEqual(similarity, 1 - weatherRainingDiff));
            Assert.That(ApproxEqual(distance, weatherRainingDiff));
        }

        public async Task TestSearchFromList()
        {
            (string[] results, float[] distances) = await search.SearchFromList(weather, new string[] {sometext, raining});
            Assert.AreEqual(results.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], sometext);
            Assert.That(ApproxEqual(distances[0], weatherRainingDiff));
            Assert.That(ApproxEqual(distances[1], weatherSometextDiff));
        }
    }

    public class TestSimpleSearch : TestSearchMethod<SimpleSearch> {}

    public class TestDBSearch : TestSearchMethod<DBSearch> {}

    public abstract class TestSplitter<T> : TestSearchable<T> where T : Chunking
    {
        public override T CreateSearch()
        {
            T search = gameObject.AddComponent<T>();
            DBSearch searchMethod = gameObject.AddComponent<DBSearch>();
            searchMethod.SetLLM(llm);
            search.SetSearch(searchMethod);
            return search;
        }

        public static (string, List<(int, int)>) GenerateText(int length)
        {
            System.Random random = new System.Random();
            char[] characters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

            char[] generatedText = new char[length];
            List<(int, int)> indices = new List<(int, int)>();
            bool delimited = false;
            bool seenChar = false;
            int preI = 0;
            for (int i = 0; i < length; i++)
            {
                int charType = random.Next(0, 20);
                if (charType == 0)
                {
                    generatedText[i] = SentenceSplitter.DefaultDelimiters[random.Next(SentenceSplitter.DefaultDelimiters.Length)];
                    delimited = seenChar && true;
                }
                else if (charType < 3)
                {
                    generatedText[i] = ' ';
                }
                else
                {
                    generatedText[i] = characters[random.Next(characters.Length)];
                    if (delimited)
                    {
                        indices.Add((preI, i - 1));
                        preI = i;
                        delimited = false;
                    }
                    seenChar = true;
                }
            }
            indices.Add((preI, length - 1));
            return (new string(generatedText), indices);
        }

        public override async Task Tests()
        {
            await base.Tests();
            await TestProperSplit();
        }

        public async Task TestProperSplit()
        {
            for (int length = 50; length <= 500; length += 50)
            {
                (string randomText, _) = GenerateText(length);
                List<(int, int)> indices = await search.Split(randomText);
                int currIndex = 0;
                foreach ((int startIndex, int endIndex) in indices)
                {
                    Assert.AreEqual(currIndex, startIndex);
                    currIndex = endIndex + 1;
                }
                Assert.AreEqual(currIndex, length);
                int key = await search.Add(randomText);
                Assert.AreEqual(search.Get(key), randomText);
            }
        }
    }

    public class TestTokenSplitter : TestSplitter<TokenSplitter> {}

    public class TestWordSplitter : TestSplitter<WordSplitter>
    {
        public override async Task Tests()
        {
            await base.Tests();
            await TestSplit();
        }

        public async Task TestSplit()
        {
            System.Random random = new System.Random();
            char[] characters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            char[] boundary = "       .,!".ToCharArray();
            int w = 0;
            for (int numSplits = 0; numSplits < 10; numSplits++)
            {
                List<string> splits = new List<string> {};
                for (int splitNr = 0; splitNr < numSplits; splitNr++)
                {
                    int numWords = search.numWords;
                    if (splitNr == numSplits - 1) numWords -= random.Next(search.numWords);

                    string split = "";
                    for (int wi = 0; wi < search.numWords; wi++)
                    {
                        split += "w" + characters[w++ % characters.Length] + boundary[random.Next(boundary.Length)];
                    }
                    splits.Add(split.TrimEnd());
                }

                string text = String.Join(" ", splits);
                List<(int, int)> indices = await search.Split(text);
                for (int i = 0; i < indices.Count; i++)
                {
                    (int startIndex, int endIndex) = indices[i];
                    string splitPred = text.Substring(startIndex, endIndex - startIndex + 1);
                    if (i != indices.Count - 1) splitPred = splitPred.Substring(0, splitPred.Length - 1);
                    Assert.AreEqual(splitPred, splits[i]);
                }
            }
        }
    }

    public class TestSentenceSplitter : TestSplitter<SentenceSplitter>
    {
        public override async Task Tests()
        {
            await base.Tests();
            await TestSplit();
        }

        public async Task TestSplit()
        {
            async Task<string[]> SplitSentences(string text)
            {
                List<(int, int)> indices = await search.Split(text);
                List<string> sentences = new List<string>();
                foreach ((int startIndex, int endIndex) in indices) sentences.Add(text.Substring(startIndex, endIndex - startIndex + 1));
                return sentences.ToArray();
            }

            string[] sentences = new string[]
            {
                "hi.",
                "how are you today?",
                "the weather is nice!",
                "perfect"
            };
            string text;
            string[] sentencesBack, sentencesGT;
            int key;

            sentencesGT = (string[])sentences.Clone();
            text = String.Join("", sentencesGT);
            sentencesBack = await SplitSentences(text);
            Assert.AreEqual(sentencesBack, sentencesGT);
            key = await search.Add(text);
            Assert.AreEqual(search.Get(key), text);

            sentencesGT = (string[])sentences.Clone();
            sentencesGT[0] = "    " + sentencesGT[0] + "   ";
            sentencesGT[1] += " ; ";
            sentencesGT[2] += "....  ";
            sentencesGT[3] += "  ?";
            text = String.Join("", sentencesGT);
            sentencesBack = await SplitSentences(text);
            Assert.AreEqual(sentencesBack, sentencesGT);
            key = await search.Add(text);
            Assert.AreEqual(search.Get(key), text);

            for (int length = 10; length <= 100; length += 10)
            {
                (string randomText, List<(int, int)> indicesGT) = GenerateText(length);
                List<(int, int)> indices = await search.Split(randomText);
                Assert.AreEqual(indices.Count, indicesGT.Count);
                Assert.AreEqual(indices, indicesGT);
                key = await search.Add(randomText);
                Assert.AreEqual(search.Get(key), randomText);
            }

            search.Clear();
        }
    }

    public abstract class TestRAG : TestSearchable<RAG>
    {
        public override RAG CreateSearch()
        {
            RAG rag = gameObject.AddComponent<RAG>();
            rag.Init(GetSearchMethod(), GetChunkingMethod(), llm);
            return rag;
        }

        public abstract SearchMethods GetSearchMethod();
        public abstract ChunkingMethods GetChunkingMethod();
    }

    public class TestRAG_SimpleSearch_NoChunking : TestRAG
    {
        public override SearchMethods GetSearchMethod() { return SearchMethods.SimpleSearch; }
        public override ChunkingMethods GetChunkingMethod() { return ChunkingMethods.NoChunking; }
    }

    public class TestRAG_DBSearch_NoChunking : TestRAG
    {
        public override SearchMethods GetSearchMethod() { return SearchMethods.DBSearch; }
        public override ChunkingMethods GetChunkingMethod() { return ChunkingMethods.NoChunking; }
    }

    public class TestRAG_SimpleSearch_WordSplitter : TestRAG
    {
        public override SearchMethods GetSearchMethod() { return SearchMethods.SimpleSearch; }
        public override ChunkingMethods GetChunkingMethod() { return ChunkingMethods.TokenSplitter; }
    }

    public class TestRAG_DBSearch_TokenSplitter : TestRAG
    {
        public override SearchMethods GetSearchMethod() { return SearchMethods.DBSearch; }
        public override ChunkingMethods GetChunkingMethod() { return ChunkingMethods.TokenSplitter; }
    }

    public abstract class TestRAG_Chunking : TestRAG
    {
        public override async Task TestSearch()
        {
            await base.TestSearch();

            string[] results;
            float[] distances;

            await search.Add(weather + raining);
            await search.Add(sometext);

            search.ReturnChunks(false);
            (results, distances) = await search.Search(weather, 1);
            Assert.That(results.Length == 1);
            Assert.That(distances.Length == 1);
            Assert.AreEqual(results[0], weather + raining);

            search.ReturnChunks(true);
            (results, distances) = await search.Search(weather, 1);
            Assert.That(results.Length == 1);
            Assert.That(distances.Length == 1);
            Assert.AreEqual(results[0], weather);
            search.Clear();
        }
    }

    public class TestRAG_DBSearch_SentenceSplitter : TestRAG_Chunking
    {
        public override SearchMethods GetSearchMethod() { return SearchMethods.DBSearch; }
        public override ChunkingMethods GetChunkingMethod() { return ChunkingMethods.SentenceSplitter; }
    }

    public class TestRAG_SimpleSearch_SentenceSplitter : TestRAG_Chunking
    {
        public override SearchMethods GetSearchMethod() { return SearchMethods.SimpleSearch; }
        public override ChunkingMethods GetChunkingMethod() { return ChunkingMethods.SentenceSplitter; }
    }
}
