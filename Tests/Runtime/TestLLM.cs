using NUnit.Framework;
using LLMUnity;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LLMUnityTests
{
    public class LLMNoAwake : LLM
    {
        public new void Awake() {}
        public new void OnDestroy() {}

        public void CallAwake()
        {
            base.Awake();
        }

        public void CallOnDestroy()
        {
            base.OnDestroy();
        }

        public List<ChatMessage> GetChat()
        {
            return chat;
        }

        public string GetCurrentPrompt()
        {
            return currentPrompt;
        }
    }


    public class TestLLM
    {
        GameObject gameObject;
        LLMNoAwake llm;
        int port = 15555;
        string AIReply = ":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::";


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

            string modelUrl = "https://huggingface.co/aladar/tiny-random-BloomForCausalLM-GGUF/resolve/main/tiny-random-BloomForCausalLM.gguf?download=true";
            string modelPath = "LLMUnityTests/tiny-random-BloomForCausalLM.gguf";
            string fullModelPath = LLMUnitySetup.GetAssetPath(modelPath);
            _ = LLMUnitySetup.DownloadFile(modelUrl, fullModelPath, false, false, null, null, false);
            await llm.SetModel(fullModelPath);
            Assert.AreEqual(llm.model, modelPath);

            llm.port = port;
            llm.prompt = "You";
            llm.temperature = 0;
            llm.seed = 0;
            llm.stream = false;
            llm.numPredict = 128;

            gameObject.SetActive(true);
        }

        [Test]
        public async void RunTests()
        {
            try
            {
                Assert.That(!llm.IsPortInUse());
                llm.CallAwake();
                TestAlive();
                await llm.Tokenize("I", TestTokens);
                await llm.Warmup();
                TestInitParameters();
                TestWarmup();
                await llm.Chat("hi", TestChat);
                TestPostChat();
                await llm.SetPrompt("You are");
                TestInitParameters();
            }
            finally
            {
                llm.CallOnDestroy();
            }
        }

        public void TestAlive()
        {
            Assert.That(llm.serverListening);
            Assert.That(llm.IsPortInUse());
        }

        public async void TestInitParameters()
        {
            Assert.That(llm.nKeep == (await llm.Tokenize(llm.prompt)).Count);
            Assert.That(llm.stop.Count > 0);
            Assert.That(llm.GetCurrentPrompt() == llm.prompt);
            Assert.That(llm.GetChat().Count == 1);
        }

        public void TestTokens(List<int> tokens)
        {
            Assert.AreEqual(tokens, new List<int> {44});
        }

        public void TestWarmup()
        {
            Assert.That(llm.GetChat().Count == 1);
            Assert.That(llm.GetCurrentPrompt() == llm.prompt);
        }

        public void TestChat(string reply)
        {
            Assert.That(llm.GetCurrentPrompt() == llm.prompt);
            Assert.That(reply == AIReply);
        }

        public void TestPostChat()
        {
            Assert.That(llm.GetChat().Count == 3);
            string newPrompt = llm.prompt + llm.RoleMessageString(llm.playerName, "hi") + llm.RoleMessageString(llm.AIName, AIReply);
            Assert.That(llm.GetCurrentPrompt() == newPrompt);
        }
    }
}
