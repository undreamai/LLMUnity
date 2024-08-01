using NUnit.Framework;
using LLMUnity;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.TestTools;
using System.IO;

namespace LLMUnityTests
{
    public class TestLLM
    {
        protected GameObject gameObject;
        protected LLM llm;
        protected LLMCharacter llmCharacter;
        protected static string modelUrl = "https://huggingface.co/afrideva/smol_llama-220M-openhermes-GGUF/resolve/main/smol_llama-220m-openhermes.q4_k_m.gguf?download=true";
        protected static string filename = Path.GetFileName(modelUrl).Split("?")[0];
        Exception error = null;
        string prompt = "Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.";

        public TestLLM()
        {
            LLMUnitySetup.SetDebugMode(LLMUnitySetup.DebugModeType.All);
            Task task = Init();
            task.Wait();
        }

        public virtual async Task Init()
        {
            gameObject = new GameObject();
            gameObject.SetActive(false);
            await SetLLM();
            SetLLMCharacter();
            gameObject.SetActive(true);
        }

        public async Task EmptyTask()
        {
            await Task.Delay(1);
        }

        public virtual async Task SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            string filename = await LLMManager.DownloadModel(modelUrl);
            llm.SetModel(filename);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
        }

        public virtual void SetLLMCharacter()
        {
            llmCharacter = gameObject.AddComponent<LLMCharacter>();
            llmCharacter.llm = llm;
            llmCharacter.playerName = "Instruction";
            llmCharacter.AIName = "Response";
            llmCharacter.prompt = prompt;
            llmCharacter.temperature = 0;
            llmCharacter.seed = 0;
            llmCharacter.stream = false;
            llmCharacter.numPredict = 20;
        }

        public virtual async Task RunTests()
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
            OnDestroy();
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

        public virtual void OnDestroy()
        {
            LLMManager.Remove(filename);
        }
    }

    public class TestLLM_LLMManager_Load : TestLLM
    {
        public override Task SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            filename = LLMManager.LoadModel(sourcePath);
            llm.SetModel(filename);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
            return Task.CompletedTask;
        }
    }

    public class TestLLM_StreamingAssets_Load : TestLLM
    {
        public override Task SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            string targetPath = LLMUnitySetup.GetAssetPath(filename);
            if (!File.Exists(targetPath)) File.Copy(sourcePath, targetPath);
            llm.SetModel(filename);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
            return Task.CompletedTask;
        }

        public override void OnDestroy()
        {
            string targetPath = LLMUnitySetup.GetAssetPath(filename);
            if (!File.Exists(targetPath)) File.Delete(targetPath);
        }
    }

    public class TestLLM_SetModel_Fail : TestLLM
    {
        public TestLLM_SetModel_Fail()
        {
            LLMUnitySetup.SetDebugMode(LLMUnitySetup.DebugModeType.None);
            Task task = Init();
            task.Wait();
        }

        public override Task SetLLM()
        {
            LLMUnitySetup.SetDebugMode(LLMUnitySetup.DebugModeType.None);
            llm = gameObject.AddComponent<LLM>();
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            llm.SetModel(sourcePath);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
            return Task.CompletedTask;
        }

        public override Task RunTests()
        {
            Assert.That(llm.model == "");
            llm.Awake();
            Assert.That(llm.failed);
            llm.OnDestroy();
            return Task.CompletedTask;
        }

        public override void OnDestroy() {}
    }
}
