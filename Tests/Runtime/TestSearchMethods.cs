using NUnit.Framework;
using LLMUnity;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace LLMUnityTests
{
    public class TestSearch: TestWithEmbeddings
    {
        string weather = "how is the weather today?";
        string raining = "is it raining?";
        string random = "something completely random";

        public void TestEncode(SearchMethod search)
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

        public void TestSaveLoad(SearchMethod search, string example)
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            search.Save(path, "");
            
            MethodInfo method = search.GetType().GetMethod(
                "Load", new System.Type[] { typeof(EmbeddingModel), typeof(string), typeof(string) }
            );
            object[] arguments = { model, path, "" };
            SearchMethod loadedSearch = (SearchMethod) method.Invoke(null, arguments);
            
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
            search.Remove(weather);
            Assert.AreEqual(search.Count(), 2);
            search.Add(weather);
            Assert.AreEqual(search.Count(), 3);
        }

        public void TestKeyAdd(ANNModelSearch search)
        {
            search.Add(1, weather);
            Assert.AreEqual(search.Count(), 1);
            search.Add(new List<int>() { 2, 3 }, new List<string>() { raining, random });
            Assert.AreEqual(search.Count(), 3);
            search.Remove(2);
            Assert.AreEqual(search.Count(), 2);
            search.Add(2, raining);
            Assert.AreEqual(search.Count(), 3);
        }

        public void TestSearchFunctions(SearchMethod search)
        {
            string[] result = search.Search(weather, 2, out float[] distances);
            Assert.AreEqual(result.Length, 2);
            Assert.AreEqual(distances.Length, 2);
            Assert.AreEqual(result[0], weather);
            Assert.AreEqual(result[1], raining);
            Assert.That(ApproxEqual(distances[0], 0));
            float trueSimilarity = 0.79276246f;
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));

            if (search.GetType() == typeof(ANNModelSearch))
            {
                TestSearchKey((ANNModelSearch)search);
            }
        }

        public void TestSearchKey(ANNModelSearch search)
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

        public void FullTest(SearchMethod search)
        {
            TestEncode(search);
            if (search.GetType() == typeof(ModelSearch))
            {
                TestAdd((ModelSearch)search);
            }
            else
            {
                TestKeyAdd((ANNModelSearch)search);
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
        public void FullTestANNModelSearch()
        {
            ANNModelSearch search = new ANNModelSearch(model);
            FullTest(search);
        }
    }
}
