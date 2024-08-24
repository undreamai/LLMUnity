using NUnit.Framework;
using LLMUnity;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections;
using System.IO;
using UnityEngine.TestTools;

namespace LLMUnityTests
{
    public class TestLLMLoras
    {
        [Test]
        public void TestLLMLorasAssign()
        {
            GameObject gameObject = new GameObject();
            gameObject.SetActive(false);
            LLM llm = gameObject.AddComponent<LLM>();

            string lora1 = "/tmp/lala";
            string lora2Rel = "test/lala";
            string lora2 = LLMUnitySetup.GetAssetPath(lora2Rel);
            LLMUnitySetup.CreateEmptyFile(lora1);
            LLMUnitySetup.CreateEmptyFile(lora2);

            llm.AddLora(lora1);
            llm.AddLora(lora2);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "1 1");

            llm.RemoveLoras();
            Assert.AreEqual(llm.lora, "");
            Assert.AreEqual(llm.loraWeights, "");

            llm.AddLora(lora1, 0.8f);
            llm.AddLora(lora2Rel, 0.9f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.9");

            llm.SetLoraScale(lora2Rel, 0.7f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.7");

            llm.RemoveLora(lora2Rel);
            Assert.AreEqual(llm.lora, lora1);
            Assert.AreEqual(llm.loraWeights, "0.8");

            llm.AddLora(lora2Rel);
            llm.SetLoraScale(lora2Rel, 0.5f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.5");

            llm.SetLoraScale(lora2, 0.1f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.1");

            File.Delete(lora1);
            File.Delete(lora2);
        }
    }

    public class TestLLM
    {
        protected static string modelUrl = "https://huggingface.co/afrideva/smol_llama-220M-openhermes-GGUF/resolve/main/smol_llama-220m-openhermes.q4_k_m.gguf?download=true";
        protected string modelNameLLManager;

        protected GameObject gameObject;
        protected LLM llm;
        protected LLMCharacter llmCharacter;
        Exception error = null;
        string prompt = "Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.";


        public TestLLM()
        {
            Task task = Init();
            task.Wait();
        }

        public virtual async Task Init()
        {
            modelNameLLManager = await LLMManager.DownloadModel(modelUrl);
            gameObject = new GameObject();
            gameObject.SetActive(false);
            SetLLM();
            SetLLMCharacter();
            gameObject.SetActive(true);
        }

        [Test]
        public void TestGetLLMManagerAssetRuntime()
        {
            string path = "";
            string managerPath = LLM.GetLLMManagerAssetRuntime(path);
            Assert.AreEqual(managerPath, path);

            path = "/tmp/lala";
            LLMUnitySetup.CreateEmptyFile(path);
            managerPath = LLM.GetLLMManagerAssetRuntime(path);
            Assert.AreEqual(managerPath, path);
            File.Delete(path);

            path = modelNameLLManager;
            managerPath = LLM.GetLLMManagerAssetRuntime(path);
            Assert.AreEqual(managerPath, LLMManager.GetAssetPath(path));

            path = LLMUnitySetup.GetAssetPath("lala");
            LLMUnitySetup.CreateEmptyFile(path);
            managerPath = LLM.GetLLMManagerAssetRuntime(path);
            Assert.AreEqual(managerPath, path);
            File.Delete(path);
        }

        [Test]
        public void TestGetLLMManagerAssetEditor()
        {
            string path = "";
            string managerPath = LLM.GetLLMManagerAssetEditor(path);
            Assert.AreEqual(managerPath, path);

            path = modelNameLLManager;
            managerPath = LLM.GetLLMManagerAssetEditor(path);
            Assert.AreEqual(managerPath, modelNameLLManager);

            path = LLMManager.Get(modelNameLLManager).path;
            managerPath = LLM.GetLLMManagerAssetEditor(path);
            Assert.AreEqual(managerPath, modelNameLLManager);

            string filename = "lala";
            path = LLMUnitySetup.GetAssetPath(filename);
            LLMUnitySetup.CreateEmptyFile(path);
            managerPath = LLM.GetLLMManagerAssetEditor(filename);
            Assert.AreEqual(managerPath, filename);
            managerPath = LLM.GetLLMManagerAssetEditor(path);
            Assert.AreEqual(managerPath, filename);
            File.Delete(path);

            path = "/tmp/lala";
            LLMUnitySetup.CreateEmptyFile(path);
            managerPath = LLM.GetLLMManagerAssetEditor(path);
            Assert.AreEqual(managerPath, path);
            File.Delete(path);
        }

        public virtual void SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            llm.SetModel(modelNameLLManager);
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

        [UnityTest]
        public IEnumerator RunTests()
        {
            Task task = RunTestsTask();
            while (!task.IsCompleted) yield return null;
            if (error != null)
            {
                Debug.LogError(error.ToString());
                throw (error);
            }
            OnDestroy();
        }

        public async Task RunTestsTask()
        {
            error = null;
            try
            {
                // await llm.WaitUntilReady();

                // llm.Awake();
                // llmCharacter.Awake();
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
                List<float> embeddings = await llmCharacter.Embeddings("hi how are you?");
                TestEmbeddings(embeddings);
                llm.OnDestroy();
            }
            catch  (Exception e)
            {
                error = e;
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
            Assert.That(llmCharacter.chat.Count == num);
        }

        public void TestEmbeddings(List<float> embeddings)
        {
            Assert.That(embeddings.Count == 1024);
        }

        public virtual void OnDestroy() {}
    }

    public class TestLLM_LLMManager_Load : TestLLM
    {
        public override void SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            string filename = Path.GetFileName(modelUrl).Split("?")[0];
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            filename = LLMManager.LoadModel(sourcePath);
            llm.SetModel(filename);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
        }
    }

    public class TestLLM_StreamingAssets_Load : TestLLM
    {
        string loadPath;

        public override void SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            string filename = Path.GetFileName(modelUrl).Split("?")[0];
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            loadPath = LLMUnitySetup.GetAssetPath(filename);
            if (!File.Exists(loadPath)) File.Copy(sourcePath, loadPath);
            llm.SetModel(loadPath);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
        }

        public override void OnDestroy()
        {
            if (!File.Exists(loadPath)) File.Delete(loadPath);
        }
    }

    public class TestLLM_SetModel_Warning : TestLLM
    {
        public override void SetLLM()
        {
            llm = gameObject.AddComponent<LLM>();
            string filename = Path.GetFileName(modelUrl).Split("?")[0];
            string loadPath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            llm.SetModel(loadPath);
            llm.parallelPrompts = 1;
            llm.SetTemplate("alpaca");
        }
    }
}
