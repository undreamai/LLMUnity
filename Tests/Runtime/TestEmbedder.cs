using NUnit.Framework;
using LLMUnity;
using Unity.Sentis;
using System.IO;
using UnityEngine;


namespace LLMUnityTests
{
    public class TestEmbeddingModelSkeleton
    {
        [Test]
        public void TestEquality()
        {
            EmbeddingModelSkeleton skeleton1 = new EmbeddingModelSkeleton("model", "config", BackendType.CPU, "last_hidden_state", true, 384);
            EmbeddingModelSkeleton skeleton2 = new EmbeddingModelSkeleton("model", "config", BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreEqual(skeleton1, skeleton2);
            Assert.AreEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(skeleton1.Equals(skeleton2));

            skeleton2 = new EmbeddingModelSkeleton("model", "config", BackendType.GPUCompute, "last_hidden_state", true, 384);
            Assert.AreNotEqual(skeleton1, skeleton2);
            Assert.AreNotEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(!skeleton1.Equals(skeleton2));

            skeleton2 = new EmbeddingModelSkeleton("model2", "config", BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreNotEqual(skeleton1, skeleton2);
            Assert.AreNotEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(!skeleton1.Equals(skeleton2));
        }

        [Test]
        public void TestSaveLoad()
        {
            EmbeddingModelSkeleton skeleton1 = new EmbeddingModelSkeleton("model", "config", BackendType.CPU, "last_hidden_state", true, 384);
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            skeleton1.Save(path);
            EmbeddingModelSkeleton skeleton2 = EmbeddingModelSkeleton.Load(path);
            Assert.AreEqual(skeleton1, skeleton2);
            File.Delete(path);
        }
    }

    public class TestEmbeddingModel
    {
        EmbeddingModel model;
        string modelPath;
        string tokenizerPath;

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

        [Test]
        public void TestEquality()
        {
            EmbeddingModelSkeleton skeleton1 = new EmbeddingModelSkeleton("model", "config", BackendType.CPU, "last_hidden_state", true, 384);
            EmbeddingModelSkeleton skeleton2 = new EmbeddingModelSkeleton("model", "config", BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreEqual(skeleton1, skeleton2);
            Assert.AreEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(skeleton1.Equals(skeleton2));

            skeleton2 = new EmbeddingModelSkeleton("model", "config", BackendType.GPUCompute, "last_hidden_state", true, 384);
            Assert.AreNotEqual(skeleton1, skeleton2);
            Assert.AreNotEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(!skeleton1.Equals(skeleton2));

            skeleton2 = new EmbeddingModelSkeleton("model2", "config", BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreNotEqual(skeleton1, skeleton2);
            Assert.AreNotEqual(skeleton1.GetHashCode(), skeleton2.GetHashCode());
            Assert.That(!skeleton1.Equals(skeleton2));
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
        public void TestModelManager()
        {
            EmbeddingModel bge1 = ModelManager.BGEModel(modelPath, tokenizerPath);
            Assert.AreEqual(ModelManager.Count(), 1);
            EmbeddingModel bge2 = ModelManager.BGEModel(modelPath, tokenizerPath);
            Assert.AreEqual(ModelManager.Count(), 1);
            EmbeddingModel lm1 = ModelManager.MiniLMModel(modelPath, tokenizerPath);
            Assert.AreEqual(ModelManager.Count(), 2);
            EmbeddingModel lm2 = ModelManager.Model(modelPath, tokenizerPath, BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreEqual(ModelManager.Count(), 2);
            Assert.AreEqual(bge1, bge2);
            Assert.AreEqual(lm1, lm2);
        }

        [TearDown]
        public void TearDown()
        {
            model.Destroy();
        }
    }
}
