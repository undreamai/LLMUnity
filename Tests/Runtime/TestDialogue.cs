using NUnit.Framework;
using LLMUnity;
using System;
using System.Collections.Generic;

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
            }; ;
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
}
