using NUnit.Framework;
using LLMUnity;
using System.IO;
using System.Collections.Generic;

namespace LLMUnityTests
{
    public class TestDialogueManager: TestWithEmbeddings
    {
        List<(string, string, string)> phrases = new List<(string, string, string)>(){
            ("To be, or not to be, that is the question. Whether tis nobler in the mind to suffer.", "Hamlet", "ACT I"),
            ("Or to take arms against a sea of troubles, and by opposing end them? To die—to sleep.", "Hamlet", "ACT I"),
            ("I humbly thank you; well, well, well.", "Hamlet", "ACT II"),
            ("Good my lord.", "Ophelia", "ACT II"),
            ("How does your honour for this many a day?", "Ophelia", "ACT II")
        };

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

            Assert.AreEqual(manager.GetPhrases("Hamlet", "ACT II"), new string[] { phrases[2].Item1 });
            string[] sentencesGT = phrases[2].Item1.Split(";");
            sentencesGT[0] += ";";
            sentencesGT[1] = sentencesGT[1].Trim();
            Assert.AreEqual(manager.GetSentences("Hamlet", "ACT II"), sentencesGT);

            manager.Add(phrases[3].Item1, phrases[3].Item2, phrases[3].Item3);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 3);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 3);
            manager.Remove(phrases[2].Item1);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 4);
            manager.Remove(phrases[3].Item1);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 1);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 1);
            manager.Add(phrases[0].Item1, "Ophelia", phrases[0].Item3);
            manager.Remove(phrases[0].Item1, "Hamlet");
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 2);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 3);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 1);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 2);

            sentencesGT = phrases[1].Item1.Split("?");
            sentencesGT[0] += "?";
            sentencesGT[1] = sentencesGT[1].Trim();
            Assert.AreEqual(manager.GetSentences("Hamlet"), sentencesGT);
            Assert.AreEqual(manager.GetPhrases("Ophelia"), new string[] { phrases[4].Item1, phrases[0].Item1 });
        }

        [Test]
        public void TestSaveLoad()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Dialogue manager = new Dialogue(model);
            manager.Save(path);
            Dialogue loadedManager = Dialogue.Load(model, path);
            File.Delete(path);

            Assert.AreEqual(manager.NumSentences(), loadedManager.NumSentences());
            Assert.AreEqual(manager.NumPhrases(), loadedManager.NumPhrases());

            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2, phrase.Item3);
            manager.Save(path);
            
            loadedManager = Dialogue.Load(model, path);
            File.Delete(path);

            Assert.AreEqual(manager.NumSentences(), loadedManager.NumSentences());
            Assert.AreEqual(manager.NumPhrases(), loadedManager.NumPhrases());

            manager.Remove(phrases[2].Item1);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 4);
        }

        [Test]
        public void TestSearch()
        {
            Dialogue manager = new Dialogue(model);
            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2, phrase.Item3);
            manager.Add(phrases[0].Item1, "Ophelia", phrases[0].Item3);

            string[] results = manager.SearchPhrases(phrases[0].Item1, 2);
            Assert.AreEqual(results, new string[] { phrases[0].Item1, phrases[0].Item1 });

            results = manager.SearchPhrases(phrases[0].Item1, 2, "Hamlet");
            Assert.AreEqual(results[0], phrases[0].Item1);
            Assert.AreNotEqual(results[1], phrases[0].Item1);
            results = manager.SearchPhrases(phrases[1].Item1, 1, "Ophelia");
            Assert.AreNotEqual(results[0], phrases[1].Item1);
        }
    }
}
