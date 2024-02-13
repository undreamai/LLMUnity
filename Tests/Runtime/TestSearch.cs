using NUnit.Framework;
using LLMUnity;
using System;
using System.IO;

namespace LLMUnityTests
{
    public class TestSentenceSplitter
    {
        [Test]
        public void TestSplit()
        {
            SentenceSplitter splitter = new SentenceSplitter();
            SentenceSplitter splitterQuestion = new SentenceSplitter("?");

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
        }
    }

    public class TestDialogue: TestWithEmbeddings
    {
        string[] sentences = new string[]{
            "hi.",
            "how are you today?",
            "how is the weather today?",
            "is it raining?"
        };

        [Test]
        public void TestAdd()
        {
            SearchEngine searchEngine = new SearchEngine(model);
            foreach (string sentence in sentences)
                searchEngine.Add(sentence);
            Assert.AreEqual(searchEngine.GetPhrases().ToArray(), sentences);
            Assert.AreEqual(searchEngine.GetSentences().ToArray(), sentences);
            Assert.AreEqual(searchEngine.NumPhrases(), sentences.Length);
            Assert.AreEqual(searchEngine.NumSentences(), sentences.Length);

            searchEngine.Remove(sentences[1]);
            string[] sentencesGT = new string[] { sentences[0], sentences[2], sentences[3] };
            Assert.AreEqual(searchEngine.GetPhrases().ToArray(), sentencesGT);
            Assert.AreEqual(searchEngine.GetSentences().ToArray(), sentencesGT);
            Assert.AreEqual(searchEngine.NumPhrases(), sentencesGT.Length);
            Assert.AreEqual(searchEngine.NumSentences(), sentencesGT.Length);

            searchEngine.Add(sentences[1]);
            searchEngine.Add(sentences[1]);
            searchEngine.Remove(sentences[1]);
            Assert.AreEqual(searchEngine.GetPhrases().ToArray(), sentencesGT);
            Assert.AreEqual(searchEngine.GetSentences().ToArray(), sentencesGT);
            Assert.AreEqual(searchEngine.NumPhrases(), sentencesGT.Length);
            Assert.AreEqual(searchEngine.NumSentences(), sentencesGT.Length);

            string[] phrases = new string[] { sentences[0] + " " + sentences[1], sentences[1] + sentences[2], sentences[2] + sentences[3] };
            searchEngine = new SearchEngine(model);
            foreach (string phrase in phrases)
                searchEngine.Add(phrase);
            searchEngine.Remove(phrases[1]);
            string[] phrasesGT = new string[] { phrases[0], phrases[2] };
            Assert.AreEqual(searchEngine.GetPhrases().ToArray(), new string[] { phrases[0], phrases[2] });
            Assert.AreEqual(searchEngine.GetSentences().ToArray(), sentences);
            Assert.AreEqual(searchEngine.NumPhrases(), 2);
            Assert.AreEqual(searchEngine.NumSentences(), 4);

            searchEngine.Add(phrases[1]);
            phrasesGT = new string[] { phrases[0], phrases[2], phrases[1] };
            sentencesGT = new string[] { sentences[0], sentences[1], sentences[2], sentences[3], sentences[1], sentences[2] };

            Assert.AreEqual(searchEngine.GetPhrases().ToArray(), phrasesGT);
            Assert.AreEqual(searchEngine.GetSentences().ToArray(), sentencesGT);
            Assert.AreEqual(searchEngine.NumPhrases(), phrasesGT.Length);
            Assert.AreEqual(searchEngine.NumSentences(), sentencesGT.Length);
        }

        [Test]
        public void TestSearch()
        {
            SearchEngine searchEngine = new SearchEngine(model);
            foreach (string sentence in sentences)
                searchEngine.Add(sentence);
            string[] result;
            string[] resultGT;
            float trueSimilarity = 0.79276246f;

            result = searchEngine.Search(sentences[3], 2);
            resultGT = new string[] { sentences[3], sentences[2] };
            Assert.AreEqual(result, resultGT);

            result = searchEngine.Search(sentences[3], 2, out float[] distances);
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
            searchEngine = new SearchEngine(model);
            foreach (string phrase in phrases)
                searchEngine.Add(phrase);

            result = searchEngine.SearchSentences(sentences[3], 5, out distances);
            resultGT = new string[] { sentences[3], sentences[3], sentences[3], sentences[3], sentences[2] };
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 5);
            for (int i = 0; i < 4; i++)
                Assert.That(ApproxEqual(distances[i], 0));
            Assert.That(ApproxEqual(distances[4], 1 - trueSimilarity));

            float trueSimilarity2 = 0.7036853f;
            result = searchEngine.SearchPhrases(sentences[3], 4, out distances);
            resultGT = new string[] { phrases[2], phrases[1], phrases[0] };
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 3);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));
            Assert.That(ApproxEqual(distances[2], 1 - trueSimilarity2));
        }

        [Test]
        public void TestSaveLoad()
        {
            SearchEngine searchEngine = new SearchEngine(model);
            foreach (string sentence in sentences)
                searchEngine.Add(sentence);

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            searchEngine.Save(path, "test");
            var loadedDialogue = SearchEngine.Load(path, "test");
            File.Delete(path);

            Assert.AreEqual(searchEngine.NumSentences(), loadedDialogue.NumSentences());
            Assert.AreEqual(searchEngine.NumPhrases(), loadedDialogue.NumPhrases());

            string[] result;
            string[] resultGT;
            float trueSimilarity = 0.79276246f;

            result = searchEngine.Search(sentences[3], 2);
            resultGT = new string[] { sentences[3], sentences[2] };
            Assert.AreEqual(result, resultGT);

            result = searchEngine.Search(sentences[3], 2, out float[] distances);
            Assert.AreEqual(result, resultGT);
            Assert.AreEqual(distances.Length, 2);
            Assert.That(ApproxEqual(distances[0], 0));
            Assert.That(ApproxEqual(distances[1], 1 - trueSimilarity));
        }
    }
}
