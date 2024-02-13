using NUnit.Framework;
using LLMUnity;
using Unity.Sentis;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Unity.Android.Types;


namespace LLMUnityTests
{

    // [SetUpFixture]
    public class SetupTests
    {
        public static ManualResetEvent downloadBlock = new ManualResetEvent(false);
        private static readonly object lockObject = new object();


        static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        public static string modelPath, tokenizerPath;
        public static (string, string) DownloadModel()
        {
            lock(lockObject){
                Debug.Log("down");
                string modelUrl = "https://huggingface.co/undreamai/bge-small-en-v1.5-sentis/resolve/main/bge-small-en-v1.5.zip?download=true";
                (modelPath, tokenizerPath) = ModelManager.DownloadUndreamAI(modelUrl);
  Debug.Log("up");
                // downloadBlock.Set();
            }
            return (modelPath, tokenizerPath);
        }
    }


    public class TestWithEmbeddings
    {
        protected EmbeddingModel model;
        public bool ApproxEqual(float x1, float x2, float tolerance = 0.0001f)
        {
            return Mathf.Abs(x1 - x2) < tolerance;
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("SetUp");
            SetupTests.DownloadModel();
            Debug.Log("Setdown");
            // await SetupTests.DownloadModel();
            // SetupTests.downloadBlock.WaitOne();
            model = new EmbeddingModel(SetupTests.modelPath, SetupTests.tokenizerPath, BackendType.CPU, "sentence_embedding", false, 384);
        }

        [TearDown]
        public void TearDown()
        {
            if (model != null) model.Destroy();
        }
    }

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

    public class TestEmbeddingModel: TestWithEmbeddings
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
            EmbeddingModel bge1 = ModelManager.BGEModel(model.ModelPath, model.TokenizerPath);
            Assert.AreEqual(ModelManager.Count(), 1);
            EmbeddingModel bge2 = ModelManager.BGEModel(model.ModelPath, model.TokenizerPath);
            Assert.AreEqual(ModelManager.Count(), 1);
            EmbeddingModel lm1 = ModelManager.MiniLMModel(model.ModelPath, model.TokenizerPath);
            Assert.AreEqual(ModelManager.Count(), 2);
            EmbeddingModel lm2 = ModelManager.Model(model.ModelPath, model.TokenizerPath, BackendType.CPU, "last_hidden_state", true, 384);
            Assert.AreEqual(ModelManager.Count(), 2);
            Assert.AreEqual(bge1, bge2);
            Assert.AreEqual(lm1, lm2);
        }
    }
}
