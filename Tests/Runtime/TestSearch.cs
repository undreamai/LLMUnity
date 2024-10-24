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
    public abstract class TestSearchable<T> where T : ISearchable
    {
        protected string weather = "how is the weather today?";
        protected string raining = "is it raining?";
        protected string sometext = "something completely sometext";

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

        public static bool ApproxEqual(float x1, float x2, float tolerance = 0.0001f)
        {
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
            OnDestroy();
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

        public virtual void OnDestroy() {}


        public virtual async Task Tests()
        {
            await TestAdd();
            await TestSearch();
            await TestSaveLoad();
        }

        public virtual async Task TestAdd()
        {
            int key = await search.Add(weather);
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
        }

        public virtual async Task TestSearch()
        {
            await search.Add(weather);
            await search.Add(raining);
            await search.Add(sometext);

            string[] result = await search.Search(weather, 2);
            Assert.AreEqual(result[0], weather);
            Assert.AreEqual(result[1], raining);

            result = await search.Search(raining, 2);
            Assert.AreEqual(result[0], raining);
            Assert.AreEqual(result[1], weather);

            search.Clear();
        }

        public virtual async Task TestSaveLoad()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            await search.Add(weather);
            await search.Add(raining);
            await search.Add(sometext);
            search.Save(path);

            search.Clear();
            search.Load(path);
            File.Delete(path);

            Assert.That(search.Count() == 3);
            Assert.That(search.Get(0) == weather);
            Assert.That(search.Get(1) == raining);
            Assert.That(search.Get(2) == sometext);

            string[] result = await search.Search(raining, 2);
            Assert.AreEqual(result[0], raining);
            Assert.AreEqual(result[1], weather);

            search.Clear();
        }
    }

    public abstract class TestSearchMethod : TestSearchable<SearchMethod>
    {
        public override async Task Tests()
        {
            await base.Tests();
            await TestEncode();
            await TestSimilarity();
            await TestIncrementalSearch();
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
            float trueSimilarity = 0.7926437f;
            float similarity = SimpleSearch.DotProduct(sentence1, sentence2);
            float distance = SimpleSearch.InverseDotProduct(sentence1, sentence2);
            Assert.That(ApproxEqual(similarity, trueSimilarity));
            Assert.That(ApproxEqual(distance, 1 - trueSimilarity));
        }

        public async Task TestIncrementalSearch()
        {
            await search.Add(weather);
            await search.Add(raining);
            await search.Add(sometext);

            int searchKey = await search.IncrementalSearch(weather);
            string[] results;
            bool completed;
            (results, completed) = search.IncrementalFetch(searchKey, 1);
            Assert.That(searchKey == 0);
            Assert.That(results.Length == 1);
            Assert.AreEqual(results[0], weather);
            Assert.That(!completed);

            (results, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(results.Length == 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], sometext);
            Assert.That(completed);

            searchKey = await search.IncrementalSearch(weather);
            (results, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(searchKey == 1);
            Assert.That(results.Length == 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], raining);
            Assert.That(!completed);

            search.IncrementalSearchComplete(searchKey);
            search.Clear();
        }
    }

    public class TestSimpleSearch : TestSearchMethod
    {
        public override SearchMethod CreateSearch()
        {
            SimpleSearch search = gameObject.AddComponent<SimpleSearch>();
            search.llm = llm;
            return search;
        }
    }

    public class TestDBSearch : TestSearchMethod
    {
        public override SearchMethod CreateSearch()
        {
            DBSearch search = gameObject.AddComponent<DBSearch>();
            search.llm = llm;
            return search;
        }
    }

    public class TestSentenceSplitter : TestSearchable<SentenceSplitter>
    {
        public override SentenceSplitter CreateSearch()
        {
            SentenceSplitter search = gameObject.AddComponent<SentenceSplitter>();
            DBSearch searchMethod = gameObject.AddComponent<DBSearch>();
            searchMethod.llm = llm;
            search.search = searchMethod;
            return search;
        }

        public override async Task Tests()
        {
            await base.Tests();
            await TestSplit();
        }

        public (string, List<(int, int)>) GenerateText(int length)
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
                    generatedText[i] = search.delimiters[random.Next(search.delimiters.Length)];
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

        public async Task TestSplit()
        {
            string[] SplitSentences(string text)
            {
                List<(int, int)> indices = search.Split(text);
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
            sentencesBack = SplitSentences(text);
            Assert.AreEqual(sentencesBack, sentencesGT);
            key = await search.Add(text);
            Assert.AreEqual(search.Get(key), text);

            sentencesGT = (string[])sentences.Clone();
            sentencesGT[0] = "    " + sentencesGT[0] + "   ";
            sentencesGT[1] += " ; ";
            sentencesGT[2] += "....  ";
            sentencesGT[3] += "  ?";
            text = String.Join("", sentencesGT);
            sentencesBack = SplitSentences(text);
            Assert.AreEqual(sentencesBack, sentencesGT);
            key = await search.Add(text);
            Assert.AreEqual(search.Get(key), text);

            for (int length = 10; length <= 100; length += 10)
            {
                (string randomText, List<(int, int)> indicesGT) = GenerateText(length);
                List<(int, int)> indices = search.Split(randomText);
                Assert.AreEqual(indices.Count, indicesGT.Count);
                Assert.AreEqual(indices, indicesGT);
                key = await search.Add(randomText);
                Assert.AreEqual(search.Get(key), randomText);
            }

            search.Clear();
        }
    }
}
