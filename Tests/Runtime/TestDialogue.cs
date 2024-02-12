using NUnit.Framework;
using LLMUnity;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace LLMUnityTests
{
    public class TestDialogueManager
    {
        EmbeddingModel model;
        string modelPath;
        string tokenizerPath;
        List<(string, string, string)> phrases = new List<(string, string, string)>(){
            ("Hamlet", "To be, or not to be, that is the question. Whether tis nobler in the mind to suffer.", "ACT I"),
            ("Hamlet", "Or to take arms against a sea of troubles, and by opposing end them? To die—to sleep.", "ACT I"),
            ("Hamlet", "I humbly thank you; well, well, well.", "ACT II"),
            ("Ophelia", "Good my lord.", "ACT II"),
            ("Ophelia", "How does your honour for this many a day?", "ACT II")
        };

        [SetUp]
        public void SetUp()
        {
            modelPath = Path.Combine(Application.streamingAssetsPath, "bge-small-en-v1.5.sentis");
            tokenizerPath = Path.Combine(Application.streamingAssetsPath, "bge-small-en-v1.5.tokenizer.json");
            model = new EmbeddingModel(modelPath, tokenizerPath, Unity.Sentis.BackendType.CPU, "sentence_embedding", false, 384);
        }

        [TearDown]
        public void TearDown()
        {
            model.Destroy();
        }

        [Test]
        public void TestAdd()
        {
            Dialogue manager = new Dialogue(model);
            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2, phrase.Item3);
            Assert.AreEqual(manager.NumPhrases(), 5);
            Assert.AreEqual(manager.NumSentences(), 8);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 3);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 6);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 2);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 2);
            Assert.AreEqual(manager.NumPhrases(null, "ACT I"), 2);
            Assert.AreEqual(manager.NumSentences(null, "ACT I"), 4);
            Assert.AreEqual(manager.NumPhrases(null, "ACT II"), 3);
            Assert.AreEqual(manager.NumSentences(null, "ACT II"), 4);
            Assert.AreEqual(manager.NumPhrases("Hamlet", "ACT I"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet", "ACT II"), 2);

            Assert.AreEqual(manager.GetPhrases("Hamlet", "ACT II"), new string[] { phrases[2].Item2 });
            string[] sentencesGT = phrases[2].Item2.Split(";");
            sentencesGT[0] += ";";
            sentencesGT[1] = sentencesGT[1].Trim();
            Assert.AreEqual(manager.GetSentences("Hamlet", "ACT II"), sentencesGT);

            manager.Add(phrases[3].Item1, phrases[3].Item2, phrases[3].Item3);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 3);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 3);
            manager.Remove(null, phrases[2].Item2);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 4);
            manager.Remove(null, phrases[3].Item2);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 1);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 1);
            manager.Add("Ophelia", phrases[0].Item2, phrases[0].Item3);
            manager.Remove("Hamlet", phrases[0].Item2);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 2);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 3);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 1);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 2);

            sentencesGT = phrases[1].Item2.Split("?");
            sentencesGT[0] += "?";
            sentencesGT[1] = sentencesGT[1].Trim();
            Assert.AreEqual(manager.GetSentences("Hamlet"), sentencesGT);
            Assert.AreEqual(manager.GetPhrases("Ophelia"), new string[] { phrases[4].Item2, phrases[0].Item2 });
        }

        [Test]
        public void TestSaveLoad()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Dialogue manager = new Dialogue(model);
            manager.Save(path);
            Dialogue loadedManager = Dialogue.Load(path);

            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2, phrase.Item3);
            manager.Save(path);
            loadedManager = Dialogue.Load(path);
            File.Delete(path);

            Assert.AreEqual(manager.NumSentences(), loadedManager.NumSentences());
            Assert.AreEqual(manager.NumPhrases(), loadedManager.NumPhrases());

            manager.Remove(null, phrases[2].Item2);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 4);
        }

        [Test]
        public void TestSearch()
        {
            Dialogue manager = new Dialogue(model);
            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2, phrase.Item3);
            manager.Add("Ophelia", phrases[0].Item2, phrases[0].Item3);

            string[] results = manager.SearchPhrases(phrases[0].Item2, 2);
            Assert.AreEqual(results, new string[] { phrases[0].Item2, phrases[0].Item2 });

            results = manager.SearchPhrases(phrases[0].Item2, 2, "Hamlet");
            Assert.AreEqual(results[0], phrases[0].Item2);
            Assert.AreNotEqual(results[1], phrases[0].Item2);
            results = manager.SearchPhrases(phrases[1].Item2, 1, "Ophelia");
            Assert.AreNotEqual(results[0], phrases[1].Item2);
        }
    }
}
