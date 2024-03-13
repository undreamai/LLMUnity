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
            while (!serverListening)
            {
                await Task.Delay(100);
            }
        }

        public void CallOnDestroy()
        {
            base.OnDestroy();
        }

        public List<ChatMessage> GetChat()
        {
            return chat;
        }
    }


    public class TestLLM
    {
        GameObject gameObject;
        LLMNoAwake llm;
        int port = 15555;
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
            _ = LLMUnitySetup.DownloadFile(modelUrl, fullModelPath, false, false, null, null, false);
            await llm.SetModel(fullModelPath);
            Assert.AreEqual(llm.model, modelPath);

            llm.port = port;
            llm.playerName = "Instruction";
            llm.AIName = "Response";
            llm.port = port;
            llm.prompt = prompt;
            llm.temperature = 0;
            llm.seed = 0;
            llm.stream = false;
            llm.numPredict = 20;
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");

            gameObject.SetActive(true);
        }

        public async Task RunTests()
        {
            error = null;
            try
            {
                await llm.CallAwake();
                TestAlive();
                await llm.Tokenize("I", TestTokens);
                await llm.Warmup();
                TestInitParameters((await llm.Tokenize(prompt)).Count, 1);
                TestWarmup();
                await llm.Chat("How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.", TestChat);
                TestPostChat(3);
                llm.SetPrompt(llm.prompt);
                llm.AIName = "False response";
                await llm.Chat("How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.", TestChat2);
                TestPostChat(3);
                await llm.Chat("bye!");
                TestPostChat(5);
                prompt = "How are you?";
                llm.SetPrompt(prompt);
                await llm.Chat("hi");
                TestInitParameters((await llm.Tokenize(prompt)).Count, 3);
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
            Assert.That(llm.serverListening);
        }

        public void TestInitParameters(int nkeep, int chats)
        {
            Assert.That(llm.nKeep == nkeep);
            Assert.That(llm.template.GetStop(llm.playerName, llm.AIName).Length > 0);
            Assert.That(llm.GetChat().Count == chats);
        }

        public void TestTokens(List<int> tokens)
        {
            Assert.AreEqual(tokens, new List<int> {306});
        }

        public void TestWarmup()
        {
            Assert.That(llm.GetChat().Count == 1);
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
            Assert.That(llm.GetChat().Count == num);
        }
    }
}
