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
    public class TestLLMLoraAssignment
    {
        [Test]
        public void TestLoras()
        {
            GameObject gameObject = new GameObject();
            gameObject.SetActive(false);
            LLM llm = gameObject.AddComponent<LLM>();

            string lora1 = "/tmp/lala";
            string lora2Rel = "test/lala";
            string lora2 = LLMUnitySetup.GetAssetPath(lora2Rel);
            LLMUnitySetup.CreateEmptyFile(lora1);
            Directory.CreateDirectory(Path.GetDirectoryName(lora2));
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

            llm.SetLoraWeight(lora2Rel, 0.7f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.7");

            llm.RemoveLora(lora2Rel);
            Assert.AreEqual(llm.lora, lora1);
            Assert.AreEqual(llm.loraWeights, "0.8");

            llm.AddLora(lora2Rel);
            llm.SetLoraWeight(lora2Rel, 0.5f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.5");

            llm.SetLoraWeight(lora2, 0.1f);
            Assert.AreEqual(llm.lora, lora1 + " " + lora2);
            Assert.AreEqual(llm.loraWeights, "0.8 0.1");

            File.Delete(lora1);
            File.Delete(lora2);
        }
    }

    public class TestLLM
    {
        protected static string modelUrl = "https://huggingface.co/Qwen/Qwen2-0.5B-Instruct-GGUF/resolve/main/qwen2-0_5b-instruct-q4_k_m.gguf?download=true";
        protected string modelNameLLManager;

        protected GameObject gameObject;
        protected LLM llm;
        protected LLMCharacter llmCharacter;
        protected Exception error = null;
        protected string prompt;
        protected string query;
        protected string reply1;
        protected string reply2;
        protected int tokens1;
        protected int tokens2;


        public TestLLM()
        {
            Task task = Init();
            task.Wait();
        }

        public virtual async Task Init()
        {
            SetParameters();
            await DownloadModels();
            gameObject = new GameObject();
            gameObject.SetActive(false);
            llm = CreateLLM();
            llmCharacter = CreateLLMCharacter();
            gameObject.SetActive(true);
        }

        public virtual void SetParameters()
        {
            prompt = "Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.";
            query = "How can I increase my meme production/output? Currently, I only create them in ancient babylonian which is time consuming.";
            reply1 = "To increase your meme production/output, you can try using more modern tools and techniques. For instance,";
            reply2 = "To increase your meme production/output, you can try the following strategies:\n\n1. Use a meme generator";
            tokens1 = 32;
            tokens2 = 9;
        }

        public virtual async Task DownloadModels()
        {
            modelNameLLManager = await LLMManager.DownloadModel(modelUrl);
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

        public virtual LLM CreateLLM()
        {
            LLM llm = gameObject.AddComponent<LLM>();
            llm.SetModel(modelNameLLManager);
            llm.parallelPrompts = 1;
            return llm;
        }

        public virtual LLMCharacter CreateLLMCharacter()
        {
            LLMCharacter llmCharacter = gameObject.AddComponent<LLMCharacter>();
            llmCharacter.llm = llm;
            llmCharacter.playerName = "Instruction";
            llmCharacter.AIName = "Response";
            llmCharacter.prompt = prompt;
            llmCharacter.temperature = 0;
            llmCharacter.seed = 0;
            llmCharacter.stream = false;
            llmCharacter.numPredict = 20;
            return llmCharacter;
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
                await Tests();
                llm.OnDestroy();
            }
            catch  (Exception e)
            {
                error = e;
            }
        }

        public virtual async Task Tests()
        {
            await llmCharacter.Tokenize("I", TestTokens);
            await llmCharacter.Warmup();
            TestInitParameters(tokens1, 1);
            TestWarmup();
            await llmCharacter.Chat(query, (string reply) => TestChat(reply, reply1));
            TestPostChat(3);
            llmCharacter.SetPrompt(llmCharacter.prompt);
            llmCharacter.AIName = "False response";
            await llmCharacter.Chat(query, (string reply) => TestChat(reply, reply2));
            TestPostChat(3);
            await llmCharacter.Chat("bye!");
            TestPostChat(5);
            prompt = "How are you?";
            llmCharacter.SetPrompt(prompt);
            await llmCharacter.Chat("hi");
            TestInitParameters(tokens2, 3);
            List<float> embeddings = await llmCharacter.Embeddings("hi how are you?");
            TestEmbeddings(embeddings);
        }

        public void TestInitParameters(int nkeep, int chats)
        {
            Assert.That(llmCharacter.nKeep == nkeep);
            Assert.That(ChatTemplate.GetTemplate(llm.chatTemplate).GetStop(llmCharacter.playerName, llmCharacter.AIName).Length > 0);
            Assert.That(llmCharacter.chat.Count == chats);
        }

        public void TestTokens(List<int> tokens)
        {
            Assert.AreEqual(tokens, new List<int> {40});
        }

        public void TestWarmup()
        {
            Assert.That(llmCharacter.chat.Count == 1);
        }

        public void TestChat(string reply, string replyGT)
        {
            Assert.That(reply.Trim() == replyGT);
        }

        public void TestPostChat(int num)
        {
            Assert.That(llmCharacter.chat.Count == num);
        }

        public void TestEmbeddings(List<float> embeddings)
        {
            Assert.That(embeddings.Count == 896);
        }

        public virtual void OnDestroy() {}
    }

    public class TestLLM_LLMManager_Load : TestLLM
    {
        public override LLM CreateLLM()
        {
            LLM llm = gameObject.AddComponent<LLM>();
            string filename = Path.GetFileName(modelUrl).Split("?")[0];
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            filename = LLMManager.LoadModel(sourcePath);
            llm.SetModel(filename);
            llm.parallelPrompts = 1;
            return llm;
        }
    }

    public class TestLLM_StreamingAssets_Load : TestLLM
    {
        string loadPath;

        public override LLM CreateLLM()
        {
            LLM llm = gameObject.AddComponent<LLM>();
            string filename = Path.GetFileName(modelUrl).Split("?")[0];
            string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            loadPath = LLMUnitySetup.GetAssetPath(filename);
            if (!File.Exists(loadPath)) File.Copy(sourcePath, loadPath);
            llm.SetModel(loadPath);
            llm.parallelPrompts = 1;
            return llm;
        }

        public override void OnDestroy()
        {
            if (!File.Exists(loadPath)) File.Delete(loadPath);
        }
    }

    public class TestLLM_SetModel_Warning : TestLLM
    {
        public override LLM CreateLLM()
        {
            LLM llm = gameObject.AddComponent<LLM>();
            string filename = Path.GetFileName(modelUrl).Split("?")[0];
            string loadPath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
            llm.SetModel(loadPath);
            llm.parallelPrompts = 1;
            return llm;
        }
    }

    public class TestLLM_Lora : TestLLM
    {
        protected string loraUrl = "https://huggingface.co/undreamer/Qwen2-0.5B-Instruct-ru-lora/resolve/main/Qwen2-0.5B-Instruct-ru-lora.gguf?download=true";
        protected string loraNameLLManager;
        protected float loraWeight;

        public override async Task DownloadModels()
        {
            await base.DownloadModels();
            loraNameLLManager = await LLMManager.DownloadLora(loraUrl);
        }

        public override LLM CreateLLM()
        {
            LLM llm = base.CreateLLM();
            llm.AddLora(loraNameLLManager, loraWeight);
            return llm;
        }

        public override void SetParameters()
        {
            prompt = "";
            query = "кто ты?";
            reply1 = "Я - искусственный интеллект, создан для общения и понимания людей.";
            reply2 = "Идиот";
            tokens1 = 5;
            tokens2 = 9;
            loraWeight = 0.9f;
        }

        public override async Task Tests()
        {
            await base.Tests();
            TestModelPaths();
            await TestLoraWeight();
        }

        public void TestModelPaths()
        {
            Assert.AreEqual(llm.model, Path.Combine(LLMUnitySetup.modelDownloadPath, Path.GetFileName(modelUrl).Split("?")[0]));
            Assert.AreEqual(llm.lora, Path.Combine(LLMUnitySetup.modelDownloadPath, Path.GetFileName(loraUrl).Split("?")[0]));
        }

        public async Task TestLoraWeight()
        {
            string json = await llm.ListLoras();
            LoraWeightResultList loraRequest = JsonUtility.FromJson<LoraWeightResultList>("{\"loraWeights\": " + json + "}");
            Assert.AreEqual(loraRequest.loraWeights[0].scale, loraWeight);
        }
    }


    public class TestLLM_Lora_ChangeWeight : TestLLM_Lora
    {
        public override async Task Tests()
        {
            await base.Tests();
            loraWeight = 0.6f;
            llm.SetLoraWeight(loraNameLLManager, loraWeight);
            await TestLoraWeight();
        }
    }

    public class TestLLM_Double : TestLLM
    {
        LLM llm1;
        LLMCharacter lLMCharacter1;

        public override async Task Init()
        {
            SetParameters();
            await DownloadModels();
            gameObject = new GameObject();
            gameObject.SetActive(false);
            llm = CreateLLM();
            llmCharacter = CreateLLMCharacter();
            llm1 = CreateLLM();
            lLMCharacter1 = CreateLLMCharacter();
            gameObject.SetActive(true);
        }
    }
}
