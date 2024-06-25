using NUnit.Framework;
using LLMUnity;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.TestTools;

namespace LLMUnityTests
{
    public class TestLLM
    {
        GameObject gameObject;
        LLM llm;
        LLMCharacter llmCharacter;
        Exception error = null;
        string prompt = "Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.";

        public TestLLM()
        {
            Task task = Init();
            task.Wait();
        }

        public async Task Init()
        {
            gameObject = new GameObject();
            gameObject.SetActive(false);

            llm = gameObject.AddComponent<LLM>();
            string modelUrl = "https://huggingface.co/afrideva/smol_llama-220M-openhermes-GGUF/resolve/main/smol_llama-220m-openhermes.q4_k_m.gguf?download=true";
            string modelPath = "LLMUnityTests/smol_llama-220m-openhermes.q4_k_m.gguf";
            string fullModelPath = LLMUnitySetup.GetAssetPath(modelPath);
            _ = LLMUnitySetup.DownloadFile(modelUrl, fullModelPath, false, null, null, false);
            await llm.SetModel(fullModelPath);
            Assert.AreEqual(llm.model, modelPath);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");

            llmCharacter = gameObject.AddComponent<LLMCharacter>();
            llmCharacter.llm = llm;
            llmCharacter.playerName = "Instruction";
            llmCharacter.AIName = "Response";
            llmCharacter.prompt = prompt;
            llmCharacter.temperature = 0;
            llmCharacter.seed = 0;
            llmCharacter.stream = false;
            llmCharacter.numPredict = 20;

            gameObject.SetActive(true);
        }

        public async Task RunTests()
        {
            error = null;
            try
            {
                llm.Awake();
                llmCharacter.Awake();
                await llmCharacter.Tokenize("I", TestTokens);
                await llmCharacter.Warmup();
                TestInitParameters((await llmCharacter.Tokenize(prompt)).Count + 2, 1);
                TestWarmup();
                await llmCharacter.Chat("How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.", TestChat);
                TestPostChat(3);
                llmCharacter.SetPrompt(llmCharacter.prompt);
                llmCharacter.AIName = "False response";
                await llmCharacter.Chat("How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.", TestChat2);
                TestPostChat(3);
                await llmCharacter.Chat("bye!");
                TestPostChat(5);
                prompt = "How are you?";
                llmCharacter.SetPrompt(prompt);
                await llmCharacter.Chat("hi");
                TestInitParameters((await llmCharacter.Tokenize(prompt)).Count + 2, 3);
                llm.OnDestroy();
            }
            catch  (Exception e)
            {
                error = e;
            }
        }

        [UnityTest]
        public IEnumerator RunTestsWait()
        {
            Task task = RunTests();
            while (!task.IsCompleted) yield return null;
            if (error != null)
            {
                Debug.LogError(error.ToString());
                throw (error);
            }
        }

        public void TestInitParameters(int nkeep, int chats)
        {
            Assert.That(llmCharacter.nKeep == nkeep);
            Assert.That(ChatTemplate.GetTemplate(llm.chatTemplate).GetStop(llmCharacter.playerName, llmCharacter.AIName).Length > 0);
            Assert.That(llmCharacter.chat.Count == chats);
        }

        public void TestTokens(List<int> tokens)
        {
            Assert.AreEqual(tokens, new List<int> {306});
        }

        public void TestWarmup()
        {
            Assert.That(llmCharacter.chat.Count == 1);
        }

        public void TestChat(string reply)
        {
            string AIReply = "To increase your meme production/output, you can consider the following:\n1. Use";
            Assert.That(reply.Trim() == AIReply);
        }

        public void TestChat2(string reply)
        {
            string AIReply = "One possible solution is to use a more advanced natural language processing library like NLTK or sp";
            Assert.That(reply.Trim() == AIReply);
        }

        public void TestPostChat(int num)
        {
            Assert.That(llmCharacter.chat.Count == num);
        }
    }
}
