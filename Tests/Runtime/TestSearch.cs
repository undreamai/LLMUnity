using NUnit.Framework;
using LLMUnity;
using Unity.Sentis;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace LLMUnityTests
{
    public class TestSearch
    {
        EmbeddingModel model;
        string modelPath;
        string tokenizerPath;
        string weather = "how is the weather today?";
        string raining = "is it raining?";
        string random = "something completely random";

        public bool ApproxEqual(float x1, float x2, float tolerance = 0.00001f)
        {
            return Mathf.Abs(x1 - x2) < tolerance;
        }

        [SetUp]
        public void SetUp()
        {
            modelPath = Path.Combine(Application.streamingAssetsPath, "bge-small-en-v1.5.sentis");
            tokenizerPath = Path.Combine(Application.streamingAssetsPath, "bge-small-en-v1.5.tokenizer.json");
            model = new EmbeddingModel(modelPath, tokenizerPath, BackendType.CPU, "sentence_embedding", false, 384);
        }

        public void TestEncode(ModelSearchBase search)
        {
            string inputString = weather;
            List<string> inputStrings = new List<string>() { weather, weather };
            float[] encoding = search.Encode(inputString);
            float[][] encodings = search.Encode(inputStrings);
            foreach (float[] encodingArray in new List<float[]>() { encoding, encodings[0], encodings[1] })
            {
                Assert.That(ApproxEqual(encodingArray[0], -0.029100293293595314f));
                Assert.That(ApproxEqual(encodingArray[383], 0.017599990591406822f));
            }
        }

        public void TestSaveLoad(ModelSearchBase search, string example)
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            search.Save(path, "");
            var loadedSearch = ModelSearchBase.CastLoad(path, "");
            Assert.AreEqual(search.GetType(), loadedSearch.GetType());
            Assert.AreEqual(search.Count(), loadedSearch.Count());
            Assert.AreEqual(loadedSearch.Search(example, 1)[0], example);
            File.Delete(path);
        }

        public void TestAdd(ModelSearch search)
        {
            search.Add(weather);
            Assert.AreEqual(search.Count(), 1);
            search.Add(new List<string>() { raining, random });
            search.Add(weather);
            Assert.AreEqual(search.Count(), 3);
        }

        public void TestKeyAdd(ModelKeySearch search)
        {
            search.Add(1, weather);
            Assert.AreEqual(search.Count(), 1);
            search.Add(new List<int>() { 2, 3 }, new List<string>() { raining, random });
            Assert.AreEqual(search.Count(), 3);
        }

        public void TestSearchFunctions(ModelSearchBase search)
        {
            string[] result = search.Search(weather, 2, out float[] distances);
            Assert.AreEqual(result.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(result[0], weather);
            Assert.AreEqual(result[1], raining);
            Assert.That(ApproxEqual(distances[0], 0));
            float trueSimilarity = 0.79276246f;
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));

            if (search.GetType() == typeof(ModelKeySearch))
            {
                TestSearchKey((ModelKeySearch)search);
            }
        }

        public void TestSearchKey(ModelKeySearch search)
        {
            int[] result = search.SearchKey(weather, 2, out float[] distances);
            Assert.AreEqual(result.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(result[0], 1);
            Assert.AreEqual(result[1], 2);
            Assert.That(ApproxEqual(distances[0], 0));
            float trueSimilarity = 0.79276246f;
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));
        }

        public void FullTest(ModelSearchBase search)
        {
            TestEncode(search);
            if (search.GetType() == typeof(ModelSearch))
            {
                TestAdd((ModelSearch)search);
            }
            else
            {
                TestKeyAdd((ModelKeySearch)search);
            }
            TestSearchFunctions(search);
            TestSaveLoad(search, weather);
            TestSearchFunctions(search);
        }

        [Test]
        public void FullTestModelSearch()
        {
            ModelSearch search = new ModelSearch(model);
            FullTest(search);
        }

        [Test]
        public void FullTestModelKeySearch()
        {
            ModelKeySearch search = new ModelKeySearch(model);
            FullTest(search);
        }

        [Test]
        public void FullTestANNModelSearch()
        {
            ANNModelSearch search = new ANNModelSearch(model);
            FullTest(search);
        }

        [TearDown]
        public void TearDown()
        {
            model.Destroy();
        }
    }
}
