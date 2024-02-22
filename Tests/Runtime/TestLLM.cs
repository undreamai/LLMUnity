using NUnit.Framework;
using LLMUnity;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Sockets;
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
    }


    public class TestLLM
    {
        GameObject gameObject;
        LLMNoAwake llm;
        int port = 15555;


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
            llm.prompt = "";
            llm.temperature = 0;
            llm.seed = 0;
            llm.stream = false;
            llm.numPredict = 128;

            gameObject.SetActive(true);
        }

        [Test]
        public async void RunTests()
        {
            llm.CallAwake();
            await llm.Tokenize("I", TestTokens);
            await llm.Chat("hi", TestChat);
            llm.CallOnDestroy();
        }

        public void TestAlive()
        {
            Assert.That(llm.serverListening);
            TcpClient c = new TcpClient("localhost", port);
        }

        public void TestTokens(List<int> tokens)
        {
            Assert.AreEqual(tokens, new List<int> {44});
        }

        public void TestChat(string reply)
        {
            Assert.AreEqual(reply.Length, llm.numPredict + 1);
            foreach (char c in reply) Assert.AreEqual(c, ':');
        }
    }
}
