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
    public class LLMNoAwake : LLM
    {
        public new void Awake() {}
        public new void OnDestroy() {}

        public async Task CallAwake()
        {
            base.Awake();
            while (!started)
            {
                await Task.Delay(100);
            }
        }

        public void CallOnDestroy()
        {
            base.OnDestroy();
        }
    }


    public class TestLLM
    {
        GameObject gameObject;
        LLMNoAwake llm;
        LLMClient llmClient;
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

            llm = gameObject.AddComponent<LLMNoAwake>();
            string modelUrl = "https://huggingface.co/afrideva/smol_llama-220M-openhermes-GGUF/resolve/main/smol_llama-220m-openhermes.q4_k_m.gguf?download=true";
            string modelPath = "LLMUnityTests/smol_llama-220m-openhermes.q4_k_m.gguf";
            string fullModelPath = LLMUnitySetup.GetAssetPath(modelPath);
            _ = LLMUnitySetup.DownloadFile(modelUrl, fullModelPath, false, null, null, false);
            await llm.SetModel(fullModelPath);
            Assert.AreEqual(llm.model, modelPath);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");

            llmClient = gameObject.AddComponent<LLMClient>();
            llmClient.llm = llm;
            llmClient.playerName = "Instruction";
            llmClient.AIName = "Response";
            llmClient.prompt = prompt;
            llmClient.temperature = 0;
            llmClient.seed = 0;
            llmClient.stream = false;
            llmClient.numPredict = 20;

            gameObject.SetActive(true);
        }

        public async Task RunTests()
        {
            error = null;
            try
            {
                await llm.CallAwake();
                llmClient.Awake();
                TestAlive();
                await llmClient.Tokenize("I", TestTokens);
                await llmClient.Warmup();
                TestInitParameters((await llmClient.Tokenize(prompt)).Count + 2, 1);
                TestWarmup();
                await llmClient.Chat("How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.", TestChat);
                TestPostChat(3);
                llmClient.SetPrompt(llmClient.prompt);
                llmClient.AIName = "False response";
                await llmClient.Chat("How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.", TestChat2);
                TestPostChat(3);
                await llmClient.Chat("bye!");
                TestPostChat(5);
                prompt = "How are you?";
                llmClient.SetPrompt(prompt);
                await llmClient.Chat("hi");
                TestInitParameters((await llmClient.Tokenize(prompt)).Count + 2, 3);
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
            llm.CallOnDestroy();
            if (error != null)
            {
                Debug.LogError(error.ToString());
                throw (error);
            }
        }

        public void TestAlive()
        {
            Assert.That(llm.started);
        }

        public void TestInitParameters(int nkeep, int chats)
        {
            Assert.That(llmClient.nKeep == nkeep);
            Assert.That(ChatTemplate.GetTemplate(llm.chatTemplate).GetStop(llmClient.playerName, llmClient.AIName).Length > 0);
            Assert.That(llmClient.chat.Count == chats);
        }

        public void TestTokens(List<int> tokens)
        {
            Assert.AreEqual(tokens, new List<int> {306});
        }

        public void TestWarmup()
        {
            Assert.That(llmClient.chat.Count == 1);
        }

        public void TestChat(string reply)
        {
            string AIReply = "One way to increase your meme production/output is by creating a more complex and customized";
            Assert.That(reply.Trim() == AIReply);
        }

        public void TestChat2(string reply)
        {
            string AIReply = "One possible solution is to use a more advanced natural language processing library like NLTK or sp";
            Assert.That(reply.Trim() == AIReply);
        }

        public void TestPostChat(int num)
        {
            Assert.That(llmClient.chat.Count == num);
        }
    }
}
