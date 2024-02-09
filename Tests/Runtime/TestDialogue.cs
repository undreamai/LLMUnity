using NUnit.Framework;
using LLMUnity;
using System;
using System.IO;
using UnityEngine;

namespace LLMUnityTests
{
    public class TestSentenceSplitter
    {
        [Test]
        public void TestSplit()
        {
            SentenceSplitter splitter = new SentenceSplitter();
            SentenceSplitter splitterNoTrim = new SentenceSplitter(SentenceSplitter.DefaultDelimiters, false);
            SentenceSplitter splitterQuestion = new SentenceSplitter(new char[] { '?' }, true);
            SentenceSplitter splitterQuestionNoTrim = new SentenceSplitter(new char[] { '?' }, false);

            string[] sentences = new string[]{
                "hi.",
                "how are you today?",
                "the weather is nice!",
                "perfect"
            };
            string text = String.Join("", sentences);

            string[] sentencesSpace = (string[])sentences.Clone();
            sentencesSpace[0] = " " + sentencesSpace[0];
            sentencesSpace[1] = " " + sentencesSpace[1];
            sentencesSpace[3] = sentencesSpace[3] + " ";
            string textSpace = String.Join("", sentencesSpace);

            string[] sentencesMultiSpace = (string[])sentences.Clone();
            sentencesMultiSpace[0] = "    " + sentencesMultiSpace[0];
            sentencesMultiSpace[1] = "  " + sentencesMultiSpace[1] + " ";
            sentencesMultiSpace[3] = sentencesMultiSpace[3] + "   ";
            string textMultiSpace = String.Join("", sentencesMultiSpace);

            string[] sentencesBack, sentencesGT;

            //splitter
            sentencesBack = SentenceSplitter.IndicesToSentences(text, splitter.Split(text));
            Assert.AreEqual(sentences, sentencesBack);

            sentencesBack = SentenceSplitter.IndicesToSentences(textSpace, splitter.Split(textSpace));
            Assert.AreEqual(sentences, sentencesBack);

            sentencesBack = SentenceSplitter.IndicesToSentences(textMultiSpace, splitter.Split(textMultiSpace));
            Assert.AreEqual(sentences, sentencesBack);

            //splitterNoTrim
            sentencesBack = SentenceSplitter.IndicesToSentences(text, splitterNoTrim.Split(text));
            Assert.AreEqual(sentences, sentencesBack);

            sentencesBack = SentenceSplitter.IndicesToSentences(textSpace, splitterNoTrim.Split(textSpace));
            Assert.AreEqual(sentencesSpace, sentencesBack);

            //splitterQuestion
            sentencesGT = new string[] { sentences[0] + sentences[1], sentences[2] + sentences[3] };
            sentencesBack = SentenceSplitter.IndicesToSentences(text, splitterQuestion.Split(text));
            Assert.AreEqual(sentencesGT, sentencesBack);

            sentencesGT = new string[] { sentences[0] + " " + sentences[1], sentences[2] + sentences[3] };
            sentencesBack = SentenceSplitter.IndicesToSentences(textSpace, splitterQuestion.Split(textSpace));
            Assert.AreEqual(sentencesGT, sentencesBack);

            sentencesGT = new string[] { sentences[0] + "  " + sentences[1], sentences[2] + sentences[3] };
            sentencesBack = SentenceSplitter.IndicesToSentences(textMultiSpace, splitterQuestion.Split(textMultiSpace));
            Assert.AreEqual(sentencesGT, sentencesBack);

            //splitterQuestionNoTrim
            sentencesGT = new string[] { sentences[0] + sentences[1], sentences[2] + sentences[3] };
            sentencesBack = SentenceSplitter.IndicesToSentences(text, splitterQuestionNoTrim.Split(text));
            Assert.AreEqual(sentencesGT, sentencesBack);

            sentencesGT = new string[] { sentencesSpace[0] + sentencesSpace[1], sentencesSpace[2] + sentencesSpace[3] };
            sentencesBack = SentenceSplitter.IndicesToSentences(textSpace, splitterQuestionNoTrim.Split(textSpace));
            Assert.AreEqual(sentencesGT, sentencesBack);

        }
    }
    public class TestDialogue
    {
        EmbeddingModel model;
        string modelPath;
        string tokenizerPath;
        string[] sentences = new string[]{
            "hi.",
            "how are you today?",
            "how is the weather today?",
            "is it raining?"
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
            Dialogue dialogue = new Dialogue("", "", model);
            foreach (string sentence in sentences)
                dialogue.Add(sentence);
            Assert.AreEqual(dialogue.GetPhrases().ToArray(), sentences);
            Assert.AreEqual(dialogue.GetSentences().ToArray(), sentences);
            Assert.AreEqual(dialogue.NumPhrases(), sentences.Length);
            Assert.AreEqual(dialogue.NumSentences(), sentences.Length);

            dialogue.Remove(sentences[1]);
            string[] sentencesGT = new string[] { sentences[0], sentences[2], sentences[3] };
            Assert.AreEqual(dialogue.GetPhrases().ToArray(), sentencesGT);
            Assert.AreEqual(dialogue.GetSentences().ToArray(), sentencesGT);
            Assert.AreEqual(dialogue.NumPhrases(), sentencesGT.Length);
            Assert.AreEqual(dialogue.NumSentences(), sentencesGT.Length);

            dialogue.Add(sentences[1]);
            dialogue.Add(sentences[1]);
            dialogue.Remove(sentences[1]);
            Assert.AreEqual(dialogue.GetPhrases().ToArray(), sentencesGT);
            Assert.AreEqual(dialogue.GetSentences().ToArray(), sentencesGT);
            Assert.AreEqual(dialogue.NumPhrases(), sentencesGT.Length);
            Assert.AreEqual(dialogue.NumSentences(), sentencesGT.Length);

            string[] phrases = new string[] { sentences[0] + " " + sentences[1], sentences[1] + sentences[2], sentences[2] + sentences[3] };
            dialogue = new Dialogue("", "", model);
            foreach (string phrase in phrases)
                dialogue.Add(phrase);
            dialogue.Remove(phrases[1]);
            string[] phrasesGT = new string[] { phrases[0], phrases[2] };
            Assert.AreEqual(dialogue.GetPhrases().ToArray(), new string[] { phrases[0], phrases[2] });
            Assert.AreEqual(dialogue.GetSentences().ToArray(), sentences);
            Assert.AreEqual(dialogue.NumPhrases(), 2);
            Assert.AreEqual(dialogue.NumSentences(), 4);

            dialogue.Add(phrases[1]);
            phrasesGT = new string[] { phrases[0], phrases[2], phrases[1] };
            sentencesGT = new string[] { sentences[0], sentences[1], sentences[2], sentences[3], sentences[1], sentences[2] };

            Assert.AreEqual(dialogue.GetPhrases().ToArray(), phrasesGT);
            Assert.AreEqual(dialogue.GetSentences().ToArray(), sentencesGT);
            Assert.AreEqual(dialogue.NumPhrases(), phrasesGT.Length);
            Assert.AreEqual(dialogue.NumSentences(), sentencesGT.Length);
        }

        public bool ApproxEqual(float x1, float x2, float tolerance = 0.00001f)
        {
            return Mathf.Abs(x1 - x2) < tolerance;
        }

        [Test]
        public void TestSearch()
        {
            Dialogue dialogue = new Dialogue("", "", model);
            foreach (string sentence in sentences)
                dialogue.Add(sentence);
            string[] result;
            string[] resultGT;
            float trueSimilarity = 0.79276246f;

            result = dialogue.Search(sentences[3], 2);
            resultGT = new string[] { sentences[3], sentences[2] };
            Assert.AreEqual(result, resultGT);

            result = dialogue.Search(sentences[3], 2, out float[] distances);
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 2);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));

            string[] phrases = new string[]{
                sentences[0] + sentences[1],
                sentences[2],
                sentences[2] + sentences[2] + sentences[2] + sentences[2] +
                sentences[3] + sentences[3] + sentences[3] + sentences[3]
            };
            dialogue = new Dialogue("", "", model);
            foreach (string phrase in phrases)
                dialogue.Add(phrase);

            result = dialogue.SearchSentences(sentences[3], 5, out distances);
            resultGT = new string[] { sentences[3], sentences[3], sentences[3], sentences[3], sentences[2] };
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 5);
            for (int i = 0; i < 4; i++)
                Assert.That(ApproxEqual(distances[i], 0));
            Assert.That(ApproxEqual(distances[4], 1 - trueSimilarity));

            float trueSimilarity2 = 0.7036853f;
            result = dialogue.SearchPhrases(sentences[3], 4, out distances);
            resultGT = new string[] { phrases[2], phrases[1], phrases[0] };
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 3);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));
            Debug.Log(distances[2]);
            Assert.That(ApproxEqual(distances[2], 1 - trueSimilarity2));
        }

        [Test]
        public void TestSaveLoad()
        {
            Dialogue dialogue = new Dialogue("", "", model);
            foreach (string sentence in sentences)
                dialogue.Add(sentence);

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            dialogue.Save(path, "test");
            var loadedDialogue = Dialogue.Load(path, "test");
            File.Delete(path);

            Assert.AreEqual(dialogue.NumSentences(), loadedDialogue.NumSentences());
            Assert.AreEqual(dialogue.NumPhrases(), loadedDialogue.NumPhrases());

            string[] result;
            string[] resultGT;
            float trueSimilarity = 0.79276246f;

            result = dialogue.Search(sentences[3], 2);
            resultGT = new string[] { sentences[3], sentences[2] };
            Assert.AreEqual(result, resultGT);

            result = dialogue.Search(sentences[3], 2, out float[] distances);
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 2);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));
        }
    }
}
