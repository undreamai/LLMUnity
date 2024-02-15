using NUnit.Framework;
using LLMUnity;
using Unity.Sentis;
using System.IO;
using UnityEngine;

namespace LLMUnityTests
{
    public class SetupTests
    {
        private static readonly object lockObject = new object();
        public static string modelPath;
        public static string tokenizerPath;

        public static (string, string) DownloadModel()
        {
            lock(lockObject){
                string modelUrl = "https://huggingface.co/undreamai/bge-small-en-v1.5-sentis/resolve/main/bge-small-en-v1.5.zip?download=true";
                (modelPath, tokenizerPath) = ModelDownloader.DownloadUndreamAI(modelUrl);
            }
            return (modelPath, tokenizerPath);
        }
    }

    public class TestWithEmbeddings
    {
        protected EmbeddingModel model;

        [SetUp]
        public void SetUp()
        {
            SetupTests.DownloadModel();
            model = new EmbeddingModel(SetupTests.modelPath, SetupTests.tokenizerPath, BackendType.CPU, "sentence_embedding", false, 384);
        }

        [TearDown]
        public void TearDown()
        {
            if (model != null) model.Destroy();
        }

        public bool ApproxEqual(float x1, float x2, float tolerance = 0.0001f)
        {
            return Mathf.Abs(x1 - x2) < tolerance;
        }
    }

    public class TestEmbeddingModel: TestWithEmbeddings
    {
        [Test]
        public void TestEquality()
        {
            EmbeddingModel skeleton1 = new EmbeddingModel(SetupTests.modelPath, SetupTests.tokenizerPath, BackendType.CPU, "last_hidden_state", true, 384);
            EmbeddingModel skeleton2 = new EmbeddingModel(SetupTests.modelPath, SetupTests.tokenizerPath, BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreEqual(skeleton1, skeleton2);
            Assert.AreEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(skeleton1.Equals(skeleton2));

            skeleton2 = new EmbeddingModel(SetupTests.modelPath, SetupTests.tokenizerPath, BackendType.CPU, "last_hidden_state2", true, 384);
            Assert.AreNotEqual(skeleton1, skeleton2);
            Assert.AreNotEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(!skeleton1.Equals(skeleton2));

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(SetupTests.tokenizerPath, path);
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("");
            }
            skeleton2 = new EmbeddingModel(SetupTests.modelPath, path, BackendType.CPU, "last_hidden_state", true, 384);

            Assert.AreNotEqual(skeleton1, skeleton2);
            Assert.AreNotEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(!skeleton1.Equals(skeleton2));
            File.Delete(path);
        }

        [Test]
        public void TestEncode()
        {
            float[] encoding = model.Encode("how is the weather today?").ToReadOnlyArray();
            Assert.That(ApproxEqual(encoding[0], -0.029100293293595314f));
            Assert.That(ApproxEqual(encoding[383], 0.017599990591406822f));
        }

        [Test]
        public void TestConcatSplit()
        {
            float[] x1 = new float[] { 0, 1, 2, 3, 4, 5 };
            float[] x2 = new float[] { 6, 7, 8, 9, 10, 11 };
            Tensor[] tensors = new Tensor[2];
            tensors[0] = new TensorFloat(new TensorShape(1, x1.Length), x1);
            tensors[1] = new TensorFloat(new TensorShape(1, x2.Length), x2);
            Tensor tensorsConcat = model.Concat(tensors);
            Assert.That(tensorsConcat.shape[0] == 2);
            Assert.That(tensorsConcat.shape[1] == 6);
            TensorFloat[] tensorsSplit = (TensorFloat[])model.Split(tensorsConcat);
            float[] x1Back = tensorsSplit[0].ToReadOnlyArray();
            float[] x2Back = tensorsSplit[1].ToReadOnlyArray();
            Assert.AreEqual(x1, x1Back);
            Assert.AreEqual(x2, x2Back);
        }

        [Test]
        public void TestSimilarity()
        {
            TensorFloat sentence1 = model.Encode("how is the weather today?");
            TensorFloat sentence2 = model.Encode("is it raining?");
            float trueSimilarity = 0.79276246f;
            float[] similarity = model.SimilarityScores(sentence1, sentence2);
            float[] distance = model.SimilarityDistances(sentence1, sentence2);
            Assert.That(ApproxEqual(similarity[0], trueSimilarity));
            Assert.That(ApproxEqual(distance[0], 1 - trueSimilarity));
        }

        [Test]
        public void TestSaveLoadHashCode()
        {
            EmbeddingModel model = new EmbeddingModel(SetupTests.modelPath, SetupTests.tokenizerPath, BackendType.CPU, "last_hidden_state", true, 384);
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            model.SaveHashCode(path);
            int hashcode = EmbeddingModel.LoadHashCode(path);
            File.Delete(path);
            Assert.AreEqual(model.GetHashCode(), hashcode);
        }
    }
}
