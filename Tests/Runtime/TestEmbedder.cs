using NUnit.Framework;
using LLMUnity;
using Unity.Sentis;
using System.IO;


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
}
