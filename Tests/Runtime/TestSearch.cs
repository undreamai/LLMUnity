using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using System;
using UnityEngine.TestTools;
using System.Collections;

namespace LLMUnityTests
{
    public class TestSimpleSearch
    {
        string weather = "how is the weather today?";
        string raining = "is it raining?";
        string random = "something completely random";

        protected string modelNameLLManager;

        protected GameObject gameObject;
        protected LLM llm;
        public SearchMethod search;
        protected Exception error = null;

        public TestSimpleSearch()
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

        public virtual SearchMethod CreateSearch()
        {
            SimpleSearch search = gameObject.AddComponent<SimpleSearch>();
            search.llm = llm;
            search.stream = false;
            return search;
        }

        public virtual async Task DownloadModels()
        {
            modelNameLLManager = await LLMManager.DownloadModel(GetModelUrl());
        }

        protected string GetModelUrl()
        {
            return "https://huggingface.co/CompendiumLabs/bge-small-en-v1.5-gguf/resolve/main/bge-small-en-v1.5-f16.gguf";
        }

        public static bool ApproxEqual(float x1, float x2, float tolerance = 0.0001f)
        {
            return Mathf.Abs(x1 - x2) < tolerance;
        }

        [UnityTest]
        public IEnumerator RunTests()
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

        public async Task RunTestsTask()
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


        public async Task Tests()
        {
            await TestEncode();
            await TestSimilarity();
            await TestAdd();
            await TestSearch();
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

        public async Task TestAdd()
        {
            int key = await search.Add(weather);
            Assert.That(search.Get(key) == weather);
            Assert.That(search.Count() == 1);
            search.Remove(key);
            Assert.That(search.Count() == 0);

            await search.Add(weather);
            await search.Add(raining);
            await search.Add(random);
            Assert.That(search.Count() == 3);
            search.Clear();
            Assert.That(search.Count() == 0);
        }

        public async Task TestSearch()
        {
            await search.Add(weather);
            await search.Add(raining);
            await search.Add(random);

            string[] result = await search.Search(weather, 2);
            Assert.AreEqual(result[0], weather);
            Assert.AreEqual(result[1], raining);

            float[] encoding = await search.Encode(weather);
            result = search.Search(encoding, 2);
            Assert.AreEqual(result[0], weather);
            Assert.AreEqual(result[1], raining);

            search.Clear();
        }

        public async Task TestIncrementalSearch()
        {
            await search.Add(weather);
            await search.Add(raining);
            await search.Add(random);

            int searchKey = await search.IncrementalSearch(weather);
            string[] results;
            bool completed;
            (results, completed) = search.IncrementalFetch(searchKey, 1);
            Assert.That(results.Length == 1);
            Assert.AreEqual(results[0], weather);
            Assert.That(!completed);

            (results, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(results.Length == 2);
            Assert.AreEqual(results[0], raining);
            Assert.AreEqual(results[1], random);
            Assert.That(completed);

            searchKey = await search.IncrementalSearch(weather);
            (results, completed) = search.IncrementalFetch(searchKey, 2);
            Assert.That(results.Length == 2);
            Assert.AreEqual(results[0], weather);
            Assert.AreEqual(results[1], raining);
            Assert.That(!completed);

            search.IncrementalSearchComplete(searchKey);
            search.Clear();
        }

        public async Task TestSave()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            await search.Add(weather);
            await search.Add(raining);
            await search.Add(random);
            search.Save(path);
        }
    }

    public class TestDBSearch : TestSimpleSearch
    {
        public override SearchMethod CreateSearch()
        {
            DBSearch search = gameObject.AddComponent<DBSearch>();
            search.llm = llm;
            search.stream = false;
            return search;
        }
    }
}
