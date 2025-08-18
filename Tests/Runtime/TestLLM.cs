// using NUnit.Framework;
// using LLMUnity;
// using UnityEngine;
// using System.Threading.Tasks;
// using System.Collections.Generic;
// using System;
// using System.Collections;
// using System.IO;
// using System.Linq;
// using System.Threading;
// using UnityEngine.TestTools;

// namespace LLMUnityTests
// {
//     public class TestLLMLoraAssignment
//     {
//         [Test]
//         public void TestLoras()
//         {
//             GameObject gameObject = new GameObject();
//             gameObject.SetActive(false);
//             LLM llm = gameObject.AddComponent<LLM>();

//             string lora1 = LLMUnitySetup.GetFullPath("lala");
//             string lora2Rel = "test/lala";
//             string lora2 = LLMUnitySetup.GetAssetPath(lora2Rel);
//             LLMUnitySetup.CreateEmptyFile(lora1);
//             Directory.CreateDirectory(Path.GetDirectoryName(lora2));
//             LLMUnitySetup.CreateEmptyFile(lora2);

//             llm.AddLora(lora1);
//             llm.AddLora(lora2);
//             Assert.AreEqual(llm.lora, lora1 + "," + lora2);
//             Assert.AreEqual(llm.loraWeights, "1,1");

//             llm.RemoveLoras();
//             Assert.AreEqual(llm.lora, "");
//             Assert.AreEqual(llm.loraWeights, "");

//             llm.AddLora(lora1, 0.8f);
//             llm.AddLora(lora2Rel, 0.9f);
//             Assert.AreEqual(llm.lora, lora1 + "," + lora2);
//             Assert.AreEqual(llm.loraWeights, "0.8,0.9");

//             llm.SetLoraWeight(lora2Rel, 0.7f);
//             Assert.AreEqual(llm.lora, lora1 + "," + lora2);
//             Assert.AreEqual(llm.loraWeights, "0.8,0.7");

//             llm.RemoveLora(lora2Rel);
//             Assert.AreEqual(llm.lora, lora1);
//             Assert.AreEqual(llm.loraWeights, "0.8");

//             llm.AddLora(lora2Rel);
//             llm.SetLoraWeight(lora2Rel, 0.5f);
//             Assert.AreEqual(llm.lora, lora1 + "," + lora2);
//             Assert.AreEqual(llm.loraWeights, "0.8,0.5");

//             llm.SetLoraWeight(lora2, 0.1f);
//             Assert.AreEqual(llm.lora, lora1 + "," + lora2);
//             Assert.AreEqual(llm.loraWeights, "0.8,0.1");

//             Dictionary<string, float> loraToWeight = new Dictionary<string, float>();
//             loraToWeight[lora1] = 0;
//             loraToWeight[lora2] = 0.2f;
//             llm.SetLoraWeights(loraToWeight);
//             Assert.AreEqual(llm.lora, lora1 + "," + lora2);
//             Assert.AreEqual(llm.loraWeights, "0,0.2");

//             File.Delete(lora1);
//             File.Delete(lora2);
//         }
//     }

//     public class TestLLM
//     {
//         protected string modelNameLLManager;

//         protected GameObject gameObject;
//         protected LLM llm;
//         protected LLMAgent llmAgent;
//         protected Exception error = null;
//         protected string prompt;
//         protected string query;
//         protected string reply1;
//         protected string reply2;
//         protected int tokens1;
//         protected int tokens2;
//         protected int port;

//         static readonly object _lock = new object();

//         public TestLLM()
//         {
//             Task task = Init();
//             task.Wait();
//         }

//         public virtual async Task Init()
//         {
//             Monitor.Enter(_lock);
//             port = new System.Random().Next(10000, 20000);
//             SetParameters();
//             await DownloadModels();
//             gameObject = new GameObject();
//             gameObject.SetActive(false);
//             llm = CreateLLM();
//             llmAgent = CreateLLMCharacter();
//             gameObject.SetActive(true);
//         }

//         public virtual void SetParameters()
//         {
//             prompt = "You are a scientific assistant and provide short and concise info on the user questions";
//             query = "Can you tell me some fun fact about ants in one sentence?";
//             reply1 = "Sure! Here's a fun fact: Ants work together to build complex structures like nests, even though they don't have human-like intelligence.";
//             reply2 = "Sure! Here's a fun fact: Ants work together to build complex structures like nests, which is a fascinating example of teamwork.";
//             tokens1 = 20;
//             tokens2 = 9;
//         }

//         protected virtual string GetModelUrl()
//         {
//             return "https://huggingface.co/unsloth/Qwen3-0.6B-GGUF/resolve/main/Qwen3-0.6B-Q4_K_M.gguf";
//         }

//         public virtual async Task DownloadModels()
//         {
//             modelNameLLManager = await LLMManager.DownloadModel(GetModelUrl());
//         }

//         [Test]
//         public void TestGetLLMManagerAssetRuntime()
//         {
//             string path = "";
//             string managerPath = LLM.GetLLMManagerAssetRuntime(path);
//             Assert.AreEqual(managerPath, path);

//             string filename = "lala";
//             path = LLMUnitySetup.GetFullPath(filename);
//             LLMUnitySetup.CreateEmptyFile(path);
//             managerPath = LLM.GetLLMManagerAssetRuntime(path);
//             Assert.AreEqual(managerPath, path);
//             File.Delete(path);

//             path = modelNameLLManager;
//             managerPath = LLM.GetLLMManagerAssetRuntime(path);
//             Assert.AreEqual(managerPath, LLMManager.GetAssetPath(path));

//             path = LLMUnitySetup.GetAssetPath("lala");
//             LLMUnitySetup.CreateEmptyFile(path);
//             managerPath = LLM.GetLLMManagerAssetRuntime(path);
//             Assert.AreEqual(managerPath, path);
//             File.Delete(path);
//         }

//         [Test]
//         public void TestGetLLMManagerAssetEditor()
//         {
//             string path = "";
//             string managerPath = LLM.GetLLMManagerAssetEditor(path);
//             Assert.AreEqual(managerPath, path);

//             path = modelNameLLManager;
//             managerPath = LLM.GetLLMManagerAssetEditor(path);
//             Assert.AreEqual(managerPath, modelNameLLManager);

//             path = LLMManager.Get(modelNameLLManager).path;
//             managerPath = LLM.GetLLMManagerAssetEditor(path);
//             Assert.AreEqual(managerPath, modelNameLLManager);

//             string filename = "lala";
//             path = LLMUnitySetup.GetAssetPath(filename);
//             LLMUnitySetup.CreateEmptyFile(path);
//             managerPath = LLM.GetLLMManagerAssetEditor(filename);
//             Assert.AreEqual(managerPath, filename);
//             managerPath = LLM.GetLLMManagerAssetEditor(path);
//             Assert.AreEqual(managerPath, filename);

//             path = LLMUnitySetup.GetFullPath(filename);
//             LLMUnitySetup.CreateEmptyFile(path);
//             managerPath = LLM.GetLLMManagerAssetEditor(path);
//             Assert.AreEqual(managerPath, path);
//             File.Delete(path);
//         }

//         public virtual LLM CreateLLM()
//         {
//             LLM llm = gameObject.AddComponent<LLM>();
//             llm.SetModel(modelNameLLManager);
//             llm.parallelPrompts = 1;
//             llm.port = port;
//             return llm;
//         }

//         public virtual LLMAgent CreateLLMCharacter()
//         {
//             LLMAgent llmAgent = gameObject.AddComponent<LLMAgent>();
//             llmAgent.llm = llm;
//             llmAgent.userRole = "User";
//             llmAgent.assistantRole = "Assistant";
//             llmAgent.prompt = prompt;
//             llmAgent.temperature = 0;
//             llmAgent.seed = 0;
//             llmAgent.stream = false;
//             llmAgent.numPredict = 50;
//             llmAgent.port = port;
//             return llmAgent;
//         }

//         [UnityTest]
//         public IEnumerator RunTests()
//         {
//             Task task = RunTestsTask();
//             while (!task.IsCompleted) yield return null;
//             if (error != null)
//             {
//                 Debug.LogError(error.ToString());
//                 throw (error);
//             }
//             OnDestroy();
//         }

//         public async Task RunTestsTask()
//         {
//             error = null;
//             try
//             {
//                 await Tests();
//                 llm.OnDestroy();
//             }
//             catch (Exception e)
//             {
//                 error = e;
//             }
//         }

//         public virtual async Task Tests()
//         {
//             await llmAgent.Tokenize("I", TestTokens);
//             await llmAgent.Warmup();
//             TestArchitecture();
//             TestInitParameters(tokens1, 1);
//             TestWarmup();
//             await llmAgent.Chat(query, (string reply) => TestChat(reply, reply1));
//             TestPostChat(3);
//             llmAgent.SetPrompt(llmAgent.prompt);
//             llmAgent.assistantRole = "False response";
//             await llmAgent.Chat(query, (string reply) => TestChat(reply, reply2));
//             TestPostChat(3);
//             await llmAgent.Chat("bye!");
//             TestPostChat(5);
//             prompt = "How are you?";
//             llmAgent.SetPrompt(prompt);
//             await llmAgent.Chat("hi");
//             TestInitParameters(tokens2, 3);
//             List<float> embeddings = await llmAgent.Embeddings("hi how are you?");
//             TestEmbeddings(embeddings);
//         }

//         public virtual void TestArchitecture()
//         {
//             Assert.That(llm.architecture.Contains("avx"));
//         }

//         public void TestInitParameters(int nkeep, int chats)
//         {
//             Assert.AreEqual(llmAgent.nKeep, nkeep);
//             Assert.That(ChatTemplate.GetTemplate(llm.chatTemplate).GetStop(llmAgent.userRole, llmAgent.assistantRole).Length > 0);
//             Assert.AreEqual(llmAgent.chat.Count, chats);
//         }

//         public void TestTokens(List<int> tokens)
//         {
//             Assert.AreEqual(tokens, new List<int> { 40 });
//         }

//         public void TestWarmup()
//         {
//             Assert.That(llmAgent.chat.Count == 1);
//         }

//         public void TestChat(string reply, string replyGT)
//         {
//             Debug.Log(reply.Trim());
//             var words1 = reply.Trim().Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
//             var words2 = replyGT.Trim().Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
//             var commonWords = words1.Intersect(words2).Count();
//             var totalWords = Math.Max(words1.Length, words2.Length);

//             Assert.That((double)commonWords / totalWords >= 0.7);
//         }

//         public void TestPostChat(int num)
//         {
//             Assert.That(llmAgent.chat.Count == num);
//         }

//         public void TestEmbeddings(List<float> embeddings)
//         {
//             Assert.That(embeddings.Count == 1024);
//         }

//         public virtual void OnDestroy()
//         {
//             if (Monitor.IsEntered(_lock))
//             {
//                 Monitor.Exit(_lock);
//             }
//         }
//     }

//     public class TestLLM_LLMManager_Load : TestLLM
//     {
//         public override LLM CreateLLM()
//         {
//             LLM llm = gameObject.AddComponent<LLM>();
//             string filename = Path.GetFileName(GetModelUrl()).Split("?")[0];
//             string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
//             filename = LLMManager.LoadModel(sourcePath);
//             llm.SetModel(filename);
//             llm.parallelPrompts = 1;
//             return llm;
//         }
//     }

//     public class TestLLM_StreamingAssets_Load : TestLLM
//     {
//         string loadPath;

//         public override LLM CreateLLM()
//         {
//             LLM llm = gameObject.AddComponent<LLM>();
//             string filename = Path.GetFileName(GetModelUrl()).Split("?")[0];
//             string sourcePath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
//             loadPath = LLMUnitySetup.GetAssetPath(filename);
//             if (!File.Exists(loadPath)) File.Copy(sourcePath, loadPath);
//             llm.SetModel(loadPath);
//             llm.parallelPrompts = 1;
//             return llm;
//         }

//         public override void OnDestroy()
//         {
//             base.OnDestroy();
//             if (!File.Exists(loadPath)) File.Delete(loadPath);
//         }
//     }

//     public class TestLLM_SetModel_Warning : TestLLM
//     {
//         public override LLM CreateLLM()
//         {
//             LLM llm = gameObject.AddComponent<LLM>();
//             string filename = Path.GetFileName(GetModelUrl()).Split("?")[0];
//             string loadPath = Path.Combine(LLMUnitySetup.modelDownloadPath, filename);
//             llm.SetModel(loadPath);
//             llm.parallelPrompts = 1;
//             return llm;
//         }
//     }

//     public class TestLLM_Lora : TestLLM
//     {
//         protected string loraUrl = "https://huggingface.co/phh/Qwen3-0.6B-TLDR-Lora/resolve/main/Qwen3-0.6B-tldr-lora-f16.gguf";
//         protected string loraNameLLManager;
//         protected float loraWeight;

//         public override async Task DownloadModels()
//         {
//             await base.DownloadModels();
//             loraNameLLManager = await LLMManager.DownloadLora(loraUrl);
//         }

//         public override LLM CreateLLM()
//         {
//             LLM llm = base.CreateLLM();
//             llm.AddLora(loraNameLLManager, loraWeight);
//             return llm;
//         }

//         public override void SetParameters()
//         {
//             prompt = "";
//             if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
//             {
//                 reply1 = "I am sorry, but I cannot assist with this request. Please try again or ask a different question.";
//             }
//             else
//             {
//                 reply1 = "I am sorry, but I cannot respond to your message as it is empty.Could you please provide a meaningful query or content ?";
//             }
//             reply2 = "False response.";
//             tokens1 = 5;
//             tokens2 = 9;
//             loraWeight = 0.9f;
//         }

//         public override async Task Tests()
//         {
//             await base.Tests();
//             TestModelPaths();
//             TestLoraWeight();
//             loraWeight = 0.6f;
//             llm.SetLoraWeight(loraNameLLManager, loraWeight);
//             TestLoraWeight();
//         }

//         public void TestModelPaths()
//         {
//             Assert.AreEqual(llm.model, Path.Combine(LLMUnitySetup.modelDownloadPath, Path.GetFileName(GetModelUrl()).Split("?")[0]).Replace('\\', '/'));
//             Assert.AreEqual(llm.lora, Path.Combine(LLMUnitySetup.modelDownloadPath, Path.GetFileName(loraUrl).Split("?")[0]).Replace('\\', '/'));
//         }

//         public void TestLoraWeight()
//         {
//             List<UndreamAI.LlamaLib.LoraIdScalePath> loras = llm.ListLoras();
//             Assert.AreEqual(loras[0].Scale, loraWeight);
//         }
//     }

//     public class TestLLM_Remote : TestLLM
//     {
//         public override LLM CreateLLM()
//         {
//             LLM llm = base.CreateLLM();
//             llm.remote = true;
//             return llm;
//         }

//         public override LLMAgent CreateLLMCharacter()
//         {
//             LLMAgent llmAgent = base.CreateLLMCharacter();
//             llmAgent.remote = true;
//             return llmAgent;
//         }
//     }

//     public class TestLLM_Lora_Remote : TestLLM_Lora
//     {
//         public override LLM CreateLLM()
//         {
//             LLM llm = base.CreateLLM();
//             llm.remote = true;
//             return llm;
//         }

//         public override LLMAgent CreateLLMCharacter()
//         {
//             LLMAgent llmAgent = base.CreateLLMCharacter();
//             llmAgent.remote = true;
//             return llmAgent;
//         }
//     }

//     public class TestLLM_Double : TestLLM
//     {
//         LLM llm1;
//         LLMAgent llmCharacter1;

//         public override async Task Init()
//         {
//             SetParameters();
//             await DownloadModels();
//             gameObject = new GameObject();
//             gameObject.SetActive(false);
//             llm = CreateLLM();
//             llmAgent = CreateLLMCharacter();
//             llm1 = CreateLLM();
//             llmCharacter1 = CreateLLMCharacter();
//             gameObject.SetActive(true);
//         }
//     }

//     public class TestLLMCharacter_Save : TestLLM
//     {
//         string saveName = "TestLLMCharacter_Save";

//         public override LLMAgent CreateLLMCharacter()
//         {
//             LLMAgent llmAgent = base.CreateLLMCharacter();
//             llmAgent.save = saveName;
//             llmAgent.saveCache = true;
//             foreach (string filename in new string[]
//                  {
//                      llmAgent.GetJsonSavePath(saveName),
//                      llmAgent.GetCacheSavePath(saveName)
//                  }) if (File.Exists(filename)) File.Delete(filename);
//             return llmAgent;
//         }

//         public override async Task Tests()
//         {
//             await base.Tests();
//             TestSave();
//         }

//         public void TestSave()
//         {
//             string jsonPath = llmAgent.GetJsonSavePath(saveName);
//             string cachePath = llmAgent.GetCacheSavePath(saveName);
//             Assert.That(File.Exists(jsonPath));
//             Assert.That(File.Exists(cachePath));
//             string json = File.ReadAllText(jsonPath);
//             File.Delete(jsonPath);
//             File.Delete(cachePath);

//             List<ChatMessage> chatHistory = JsonUtility.FromJson<ChatListWrapper>(json).chat;
//             Assert.AreEqual(chatHistory.Count, 2);
//             Assert.AreEqual(chatHistory[0].role, llmAgent.userRole);
//             Assert.AreEqual(chatHistory[0].content, "hi");
//             Assert.AreEqual(chatHistory[1].role, llmAgent.assistantRole);

//             Assert.AreEqual(llmAgent.chat.Count, chatHistory.Count + 1);
//             for (int i = 0; i < chatHistory.Count; i++)
//             {
//                 Assert.AreEqual(chatHistory[i].role, llmAgent.chat[i + 1].role);
//                 Assert.AreEqual(chatHistory[i].content, llmAgent.chat[i + 1].content);
//             }
//         }
//     }

//     public class TestLLM_CUDA : TestLLM
//     {
//         public override LLM CreateLLM()
//         {
//             LLM llm = base.CreateLLM();
//             llm.numGPULayers = 10;
//             return llm;
//         }

//         public override void SetParameters()
//         {
//             base.SetParameters();
//             reply1 = "Sure! Here's a fun fact: Ants work together to build complex structures like nests, even though they don't have a brain.";
//             if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
//             {
//                 reply2 = "Sure! Here's a fun fact: Ants work together to build complex structures like nests, which is a fascinating example of teamwork.";
//             }
//         }

//         public override void TestArchitecture()
//         {
//             Assert.That(llm.architecture.Contains("cuda"));
//         }
//     }

//     public class TestLLM_CUDA_full : TestLLM_CUDA
//     {
//         public override void TestArchitecture()
//         {
//             Assert.That(llm.architecture.Contains("cuda") && llm.architecture.Contains("full"));
//         }
//     }

//     public class TestLLM_CUDA_full_attention : TestLLM_CUDA_full
//     {
//         public override LLM CreateLLM()
//         {
//             LLM llm = base.CreateLLM();
//             llm.flashAttention = true;
//             return llm;
//         }

//         public override void SetParameters()
//         {
//             base.SetParameters();
//             if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
//             {
//                 reply2 = "Sure! Here's a fun fact: Ants work together to build complex structures like nests, even though they don't have human-like intelligence.";
//             }
//         }
//     }
// }
