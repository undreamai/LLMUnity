using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UndreamAI.LlamaLib
{
    // Data structures for LoRA operations
    public struct LoraIdScale
    {
        public int Id { get; set; }
        public float Scale { get; set; }

        public LoraIdScale(int id, float scale)
        {
            Id = id;
            Scale = scale;
        }
    }

    public struct LoraIdScalePath
    {
        public int Id { get; set; }
        public float Scale { get; set; }
        public string Path { get; set; }

        public LoraIdScalePath(int id, float scale, string path)
        {
            Id = id;
            Scale = scale;
            Path = path;
        }
    }

    // Base LLM class
    public abstract class LLM : IDisposable
    {
        public LlamaLib llamaLib = null;
        public IntPtr llm = IntPtr.Zero;
        protected readonly object _disposeLock = new object();
        public bool disposed = false;

        protected LLM() {}

        protected LLM(LlamaLib llamaLibInstance)
        {
            llamaLib = llamaLibInstance ?? throw new ArgumentNullException(nameof(llamaLibInstance));
        }

        public static void Debug(int debugLevel)
        {
            LlamaLib.Debug(debugLevel);
        }

        public static void LoggingCallback(LlamaLib.CharArrayCallback callback)
        {
            LlamaLib.LoggingCallback(callback);
        }

        public static void LoggingStop()
        {
            LlamaLib.LoggingStop();
        }

        protected void CheckLlamaLib()
        {
            if (disposed) throw new ObjectDisposedException(GetType().Name);
            if (llamaLib == null) throw new InvalidOperationException("LlamaLib instance is not initialized");
            if (llm == IntPtr.Zero) throw new InvalidOperationException("LLM instance is not initialized");
        }

        public virtual void Dispose() {}

        ~LLM()
        {
            Dispose();
        }

        public string ApplyTemplate(JArray messages = null)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));
            CheckLlamaLib();
            IntPtr result = llamaLib.LLM_Apply_Template(llm, messages.ToString() ?? string.Empty);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public List<int> Tokenize(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            CheckLlamaLib();
            IntPtr result = llamaLib.LLM_Tokenize(llm, content ?? string.Empty);
            string resultStr = Marshal.PtrToStringAnsi(result) ?? string.Empty;
            List<int> ret = new List<int>();
            try
            {
                JArray json = JArray.Parse(resultStr);
                ret = json?.ToObject<List<int>>();
            }
            catch {}
            return ret;
        }

        public string Detokenize(List<int> tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));

            CheckLlamaLib();
            JArray tokensJSON = JArray.FromObject(tokens);
            IntPtr result = llamaLib.LLM_Detokenize(llm, tokensJSON.ToString() ?? string.Empty);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public string Detokenize(int[] tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));
            return Detokenize(new List<int>(tokens));
        }

        public List<float> Embeddings(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            CheckLlamaLib();

            IntPtr result = llamaLib.LLM_Embeddings(llm, content ?? string.Empty);
            string resultStr = Marshal.PtrToStringAnsi(result) ?? string.Empty;

            List<float> ret = new List<float>();
            try
            {
                JArray json = JArray.Parse(resultStr);
                ret = json?.ToObject<List<float>>();
            }
            catch {}
            return ret;
        }

        public void SetCompletionParameters(JObject parameters = null)
        {
            CheckLlamaLib();
            llamaLib.LLM_Set_Completion_Parameters(llm, parameters?.ToString() ?? string.Empty);
        }

        public JObject GetCompletionParameters()
        {
            CheckLlamaLib();
            JObject parameters = new JObject();
            IntPtr result = llamaLib.LLM_Get_Completion_Parameters(llm);
            string parametersString = Marshal.PtrToStringAnsi(result) ?? "{}";
            try
            {
                parameters = JObject.Parse(parametersString);
            }
            catch {}
            return parameters;
        }

        public void SetGrammar(string grammar)
        {
            CheckLlamaLib();
            llamaLib.LLM_Set_Grammar(llm, grammar ?? string.Empty);
        }

        public string GetGrammar()
        {
            CheckLlamaLib();
            IntPtr result = llamaLib.LLM_Get_Grammar(llm);
            return Marshal.PtrToStringAnsi(result) ?? "";
        }

        public void CheckCompletionInternal(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentNullException(nameof(prompt));
            CheckLlamaLib();
        }

        public string CompletionInternal(string prompt, LlamaLib.CharArrayCallback callback, int idSlot)
        {
            IntPtr result;
            result = llamaLib.LLM_Completion(llm, prompt ?? string.Empty, callback, idSlot);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public string Completion(string prompt, LlamaLib.CharArrayCallback callback = null, int idSlot = -1)
        {
            CheckCompletionInternal(prompt);
            return CompletionInternal(prompt, callback, idSlot);
        }

        public async Task<string> CompletionAsync(string prompt, LlamaLib.CharArrayCallback callback = null, int idSlot = -1)
        {
            CheckCompletionInternal(prompt);
            return await Task.Run(() => CompletionInternal(prompt, callback, idSlot));
        }
    }

    // LLMLocal class
    public abstract class LLMLocal : LLM
    {
        protected LLMLocal() : base() {}

        protected LLMLocal(LlamaLib llamaLibInstance) : base(llamaLibInstance) {}

        public string SaveSlot(int idSlot, string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new ArgumentNullException(nameof(filepath));

            IntPtr result = llamaLib.LLM_Save_Slot(llm, idSlot, filepath ?? string.Empty);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public string LoadSlot(int idSlot, string filepath)
        {
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                throw new ArgumentNullException(nameof(filepath));

            IntPtr result = llamaLib.LLM_Load_Slot(llm, idSlot, filepath ?? string.Empty);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public void Cancel(int idSlot)
        {
            CheckLlamaLib();
            llamaLib.LLM_Cancel(llm, idSlot);
        }
    }

    // LLMProvider class
    public abstract class LLMProvider : LLMLocal
    {
        protected LLMProvider() : base() {}

        protected LLMProvider(LlamaLib llamaLibInstance) : base(llamaLibInstance) {}

        public void EnableReasoning(bool enableReasoning)
        {
            CheckLlamaLib();
            llamaLib.LLM_Enable_Reasoning(llm, enableReasoning);
        }

        // LoRA Weight methods
        public string BuildLoraWeightJSON(List<LoraIdScale> loras)
        {
            var jsonArray = new JArray();
            foreach (var lora in loras)
            {
                jsonArray.Add(new JObject { ["id"] = lora.Id, ["scale"] = lora.Scale });
            }
            return jsonArray.ToString();
        }

        public bool LoraWeight(List<LoraIdScale> loras)
        {
            if (loras == null)
                throw new ArgumentNullException(nameof(loras));

            var lorasJSON = BuildLoraWeightJSON(loras);
            return llamaLib.LLM_Lora_Weight(llm, lorasJSON ?? string.Empty);
        }

        public bool LoraWeight(params LoraIdScale[] loras)
        {
            return LoraWeight(new List<LoraIdScale>(loras));
        }

        // LoRA List methods
        public List<LoraIdScalePath> ParseLoraListJSON(string result)
        {
            var loras = new List<LoraIdScalePath>();
            try
            {
                var jsonArray = JArray.Parse(result);
                foreach (var item in jsonArray)
                {
                    int id = item["id"]?.ToObject<int>() ?? -1;
                    if (id < 0) continue;
                    loras.Add(new LoraIdScalePath(
                        id,
                        item["scale"]?.ToObject<float>() ?? 0.0f,
                        item["path"]?.ToString() ?? string.Empty
                    ));
                }
            }
            catch {}
            return loras;
        }

        public string LoraListJSON()
        {
            CheckLlamaLib();
            var result = llamaLib.LLM_Lora_List(llm);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public List<LoraIdScalePath> LoraList()
        {
            var jsonResult = LoraListJSON();
            return ParseLoraListJSON(jsonResult);
        }

        // Server methods
        public bool Start()
        {
            CheckLlamaLib();
            llamaLib.LLM_Start(llm);
            return llamaLib.LLM_Started(llm);
        }

        public async Task<bool> StartAsync()
        {
            CheckLlamaLib();
            return await Task.Run(() =>
            {
                llamaLib.LLM_Start(llm);
                return llamaLib.LLM_Started(llm);
            });
        }

        public bool Started()
        {
            CheckLlamaLib();
            return llamaLib.LLM_Started(llm);
        }

        public void Stop()
        {
            CheckLlamaLib();
            llamaLib.LLM_Stop(llm);
        }

        public void StartServer(string host = "0.0.0.0", int port = -1, string apiKey = "")
        {
            CheckLlamaLib();
            host = string.IsNullOrEmpty(host) ? "0.0.0.0" : host;
            apiKey = apiKey ?? string.Empty;

            llamaLib.LLM_Start_Server(llm, host, port, apiKey);
        }

        public void StopServer()
        {
            CheckLlamaLib();
            llamaLib.LLM_Stop_Server(llm);
        }

        public void JoinService()
        {
            CheckLlamaLib();
            llamaLib.LLM_Join_Service(llm);
        }

        public void JoinServer()
        {
            CheckLlamaLib();
            llamaLib.LLM_Join_Server(llm);
        }

        public void SetSSL(string sslCert, string sslKey)
        {
            if (string.IsNullOrEmpty(sslCert))
                throw new ArgumentNullException(nameof(sslCert));
            if (string.IsNullOrEmpty(sslKey))
                throw new ArgumentNullException(nameof(sslKey));

            CheckLlamaLib();
            llamaLib.LLM_Set_SSL(llm, sslCert ?? string.Empty, sslKey ?? string.Empty);
        }

        public int EmbeddingSize()
        {
            CheckLlamaLib();
            return llamaLib.LLM_Embedding_Size(llm);
        }

        public override void Dispose()
        {
            lock (_disposeLock)
            {
                if (!disposed)
                {
                    if (llm != IntPtr.Zero && llamaLib != null)
                    {
                        try
                        {
                            llamaLib.LLM_Delete(llm);
                        }
                        catch (Exception) {}
                    }
                    llamaLib?.Dispose();
                    llamaLib = null;
                    llm = IntPtr.Zero;
                }
                disposed = true;
            }
        }
    }
}
