using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;

namespace UndreamAI.LlamaLib
{
    public class LlamaLib
    {
        public string architecture { get; private set; }

        // Function delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CharArrayCallback([MarshalAs(UnmanagedType.LPStr)] string charArray);

#if ANDROID || IOS || VISIONOS
        // Static P/Invoke declarations for mobile platforms
#if ANDROID_ARM64
        public const string DllName = "libllamalib_android-arm64";
#elif ANDROID_X64
        public const string DllName = "libllamalib_android-x64";
#else
        public const string DllName = "__Internal";
#endif

        public LlamaLib(bool gpu = false)
        {
#if ANDROID_ARM64
            architecture = "android-arm64";
#elif ANDROID_X64
            architecture = "android-x64";
#elif IOS
            architecture = "ios-arm64";
#elif VISIONOS
            architecture = "visionos-arm64";
#endif
        }

        // Base LLM functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Debug")]
        public static extern void LLM_Debug_Static(int debugLevel);
        public static void Debug(int debugLevel) => LLM_Debug_Static(debugLevel);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Logging_Callback")]
        public static extern void LLM_Logging_Callback_Static(CharArrayCallback callback);
        public static void LoggingCallback(CharArrayCallback callback) => LLM_Logging_Callback_Static(callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Logging_Stop")]
        public static extern void LLM_Logging_Stop_Static();
        public static void LoggingStop() => LLM_Logging_Stop_Static();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Enable_Reasoning")]
        public static extern void LLM_Enable_Reasoning_Static(IntPtr llm, [MarshalAs(UnmanagedType.I1)] bool enable_reasoning);
        public void LLM_Enable_Reasoning(IntPtr llm, bool enable_reasoning) => LLM_Enable_Reasoning_Static(llm, enable_reasoning);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Get_Template")]
        public static extern IntPtr LLM_Get_Template_Static(IntPtr llm);
        public IntPtr LLM_Get_Template(IntPtr llm) => LLM_Get_Template_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Apply_Template")]
        public static extern IntPtr LLM_Apply_Template_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string messages_as_json);
        public IntPtr LLM_Apply_Template(IntPtr llm, string messages_as_json) => LLM_Apply_Template_Static(llm, messages_as_json);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern IntPtr LLM_Tokenize_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query);
        public IntPtr LLM_Tokenize(IntPtr llm, string query) => LlamaLib.LLM_Tokenize_Static(llm, query);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern IntPtr LLM_Detokenize_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string tokens_as_json);
        public IntPtr LLM_Detokenize(IntPtr llm, string tokens_as_json) => LlamaLib.LLM_Detokenize_Static(llm, tokens_as_json);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Embeddings")]
        public static extern IntPtr LLM_Embeddings_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query);
        public IntPtr LLM_Embeddings(IntPtr llm, string query) => LlamaLib.LLM_Embeddings_Static(llm, query);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern IntPtr LLM_Completion_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query, CharArrayCallback callback, int id_slot = -1, [MarshalAs(UnmanagedType.I1)] bool return_response_json = false);
        public IntPtr LLM_Completion(IntPtr llm, string query, CharArrayCallback callback, int id_slot = -1, bool return_response_json = false) => LlamaLib.LLM_Completion_Static(llm, query, callback, id_slot, return_response_json);

        // LLMLocal functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_Template")]
        public static extern void LLM_Set_Template_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string template);
        public void LLM_Set_Template(IntPtr llm, string template) => LLM_Set_Template_Static(llm, template);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Save_Slot")]
        public static extern IntPtr LLM_Save_Slot_Static(IntPtr llm, int id_slot, [MarshalAs(UnmanagedType.LPStr)] string filepath);
        public IntPtr LLM_Save_Slot(IntPtr llm, int id_slot, string filepath) => LlamaLib.LLM_Save_Slot_Static(llm, id_slot, filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Load_Slot")]
        public static extern IntPtr LLM_Load_Slot_Static(IntPtr llm, int id_slot, [MarshalAs(UnmanagedType.LPStr)] string filepath);
        public IntPtr LLM_Load_Slot(IntPtr llm, int id_slot, string filepath) => LlamaLib.LLM_Load_Slot_Static(llm, id_slot, filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LLM_Cancel_Static(IntPtr llm, int idSlot);
        public void LLM_Cancel(IntPtr llm, int idSlot) => LlamaLib.LLM_Cancel_Static(llm, idSlot);

        // LLMProvider functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Lora_Weight")]
        public static extern bool LLM_Lora_Weight_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string loras_as_json);
        public bool LLM_Lora_Weight(IntPtr llm, string loras_as_json) => LlamaLib.LLM_Lora_Weight_Static(llm, loras_as_json);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Lora_List")]
        public static extern IntPtr LLM_Lora_List_Static(IntPtr llm);
        public IntPtr LLM_Lora_List(IntPtr llm) => LlamaLib.LLM_Lora_List_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LLM_Delete_Static(IntPtr llm);
        public void LLM_Delete(IntPtr llm) => LlamaLib.LLM_Delete_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LLM_Start_Static(IntPtr llm);
        public void LLM_Start(IntPtr llm) => LlamaLib.LLM_Start_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Started")]
        [return : MarshalAs(UnmanagedType.I1)]
        public static extern bool LLM_Started_Static(IntPtr llm);
        public bool LLM_Started(IntPtr llm) => LlamaLib.LLM_Started_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LLM_Stop_Static(IntPtr llm);
        public void LLM_Stop(IntPtr llm) => LlamaLib.LLM_Stop_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start_Server")]
        public static extern void LLM_Start_Server_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string host = "0.0.0.0",
            int port = -1,
            [MarshalAs(UnmanagedType.LPStr)] string apiKey = "");
        public void LLM_Start_Server(IntPtr llm, string host = "0.0.0.0", int port = -1, string apiKey = "") => LlamaLib.LLM_Start_Server_Static(llm, host, port, apiKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop_Server")]
        public static extern void LLM_Stop_Server_Static(IntPtr llm);
        public void LLM_Stop_Server(IntPtr llm) => LlamaLib.LLM_Stop_Server_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Join_Service")]
        public static extern void LLM_Join_Service_Static(IntPtr llm);
        public void LLM_Join_Service(IntPtr llm) => LlamaLib.LLM_Join_Service_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Join_Server")]
        public static extern void LLM_Join_Server_Static(IntPtr llm);
        public void LLM_Join_Server(IntPtr llm) => LlamaLib.LLM_Join_Server_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_SSL")]
        public static extern void LLM_Set_SSL_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string sslCert,
            [MarshalAs(UnmanagedType.LPStr)] string sslKey);
        public void LLM_Set_SSL(IntPtr llm, string sslCert, string sslKey) => LlamaLib.LLM_Set_SSL_Static(llm, sslCert, sslKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status_Code")]
        public static extern int LLM_Status_Code_Static();
        public int LLM_Status_Code() => LlamaLib.LLM_Status_Code_Static();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status_Message")]
        public static extern IntPtr LLM_Status_Message_Static();
        public IntPtr LLM_Status_Message() => LlamaLib.LLM_Status_Message_Static();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Embedding_Size")]
        public static extern int LLM_Embedding_Size_Static(IntPtr llm);
        public int LLM_Embedding_Size(IntPtr llm) => LlamaLib.LLM_Embedding_Size_Static(llm);

        // LLMService functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMService_Construct")]
        public static extern IntPtr LLMService_Construct_Static(
            [MarshalAs(UnmanagedType.LPStr)] string modelPath,
            int numSlots = 1,
            int numThreads = -1,
            int numGpuLayers = 0,
            [MarshalAs(UnmanagedType.I1)] bool flashAttention = false,
            int contextSize = 4096,
            int batchSize = 2048,
            [MarshalAs(UnmanagedType.I1)] bool embeddingOnly = false,
            int loraCount = 0,
            IntPtr loraPaths = default);
        public IntPtr LLMService_Construct(
            int numSlots = 1,
            string modelPath,
            int numThreads = -1,
            int numGpuLayers = 0,
            bool flashAttention = false,
            int contextSize = 4096,
            int batchSize = 2048,
            bool embeddingOnly = false,
            int loraCount = 0,
            IntPtr loraPaths = default)
            => LlamaLib.LLMService_Construct_Static(modelPath, numSlots, numThreads, numGpuLayers, flashAttention,
                contextSize, batchSize, embeddingOnly, loraCount, loraPaths);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMService_From_Command")]
        public static extern IntPtr LLMService_From_Command_Static([MarshalAs(UnmanagedType.LPStr)] string paramsString);
        public IntPtr LLMService_From_Command(string paramsString) => LlamaLib.LLMService_From_Command_Static(paramsString);

        // LLMClient functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Construct")]
        public static extern IntPtr LLMClient_Construct_Static(IntPtr llm);
        public IntPtr LLMClient_Construct(IntPtr llm) => LlamaLib.LLMClient_Construct_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Construct_Remote")]
        public static extern IntPtr LLMClient_Construct_Remote_Static(
            [MarshalAs(UnmanagedType.LPStr)] string url,
            int port,
            [MarshalAs(UnmanagedType.LPStr)] string apiKey = "",
            int numRetries = 5);
        public IntPtr LLMClient_Construct_Remote(string url, int port, string apiKey = "", int numRetries = 5) => LlamaLib.LLMClient_Construct_Remote_Static(url, port, apiKey, numRetries);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Set_SSL")]
        public static extern void LLMClient_Set_SSL_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string SSLCert);
        public void LLMClient_Set_SSL(IntPtr llm, string SSLCert) => LlamaLib.LLMClient_Set_SSL_Static(llm, SSLCert);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Is_Server_Alive")]
        public static extern bool LLMClient_Is_Server_Alive_Static(IntPtr llm);
        public bool LLMClient_Is_Server_Alive(IntPtr llm) => LlamaLib.LLMClient_Is_Server_Alive_Static(llm);

        // LLMAgent functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Construct")]
        public static extern IntPtr LLMAgent_Construct_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string systemPrompt = "",
            [MarshalAs(UnmanagedType.LPStr)] string userRole = "user",
            [MarshalAs(UnmanagedType.LPStr)] string assistantRole = "assistant");
        public IntPtr LLMAgent_Construct(IntPtr llm, string systemPrompt = "", string userRole = "user", string assistantRole = "assistant")
            => LLMAgent_Construct_Static(llm, systemPrompt, userRole, assistantRole);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_User_Role")]
        public static extern void LLMAgent_Set_User_Role_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string userRole);
        public void LLMAgent_Set_User_Role(IntPtr llm, string userRole) => LLMAgent_Set_User_Role_Static(llm, userRole);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_User_Role")]
        public static extern IntPtr LLMAgent_Get_User_Role_Static(IntPtr llm);
        public IntPtr LLMAgent_Get_User_Role(IntPtr llm) => LLMAgent_Get_User_Role_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_Assistant_Role")]
        public static extern void LLMAgent_Set_Assistant_Role_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string assistantRole);
        public void LLMAgent_Set_Assistant_Role(IntPtr llm, string assistantRole) => LLMAgent_Set_Assistant_Role_Static(llm, assistantRole);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_Assistant_Role")]
        public static extern IntPtr LLMAgent_Get_Assistant_Role_Static(IntPtr llm);
        public IntPtr LLMAgent_Get_Assistant_Role(IntPtr llm) => LLMAgent_Get_Assistant_Role_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_System_Prompt")]
        public static extern void LLMAgent_Set_System_Prompt_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string systemPrompt);
        public void LLMAgent_Set_System_Prompt(IntPtr llm, string systemPrompt) => LLMAgent_Set_System_Prompt_Static(llm, systemPrompt);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_System_Prompt")]
        public static extern IntPtr LLMAgent_Get_System_Prompt_Static(IntPtr llm);
        public IntPtr LLMAgent_Get_System_Prompt(IntPtr llm) => LLMAgent_Get_System_Prompt_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_Completion_Parameters")]
        public static extern void LLM_Set_Completion_Parameters_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string parameters);
        public void LLM_Set_Completion_Parameters(IntPtr llm, string parameters) => LLM_Set_Completion_Parameters_Static(llm, parameters);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Get_Completion_Parameters")]
        public static extern IntPtr LLM_Get_Completion_Parameters_Static(IntPtr llm);
        public IntPtr LLM_Get_Completion_Parameters(IntPtr llm) => LLM_Get_Completion_Parameters_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_Grammar")]
        public static extern void LLM_Set_Grammar_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string grammar);
        public void LLM_Set_Grammar(IntPtr llm, string grammar) => LLM_Set_Grammar_Static(llm, grammar);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Get_Grammar")]
        public static extern IntPtr LLM_Get_Grammar_Static(IntPtr llm);
        public IntPtr LLM_Get_Grammar(IntPtr llm) => LLM_Get_Grammar_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_Slot")]
        public static extern void LLMAgent_Set_Slot_Static(IntPtr llm, int slotId);
        public void LLMAgent_Set_Slot(IntPtr llm, int slotId) => LLMAgent_Set_Slot_Static(llm, slotId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_Slot")]
        public static extern IntPtr LLMAgent_Get_Slot_Static(IntPtr llm);
        public IntPtr LLMAgent_Get_Slot(IntPtr llm) => LLMAgent_Get_Slot_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Chat")]
        public static extern IntPtr LLMAgent_Chat_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string userPrompt,
            [MarshalAs(UnmanagedType.I1)] bool addToHistory = true,
            CharArrayCallback callback = null,
            [MarshalAs(UnmanagedType.I1)] bool returnResponseJson = false);
        public IntPtr LLMAgent_Chat(IntPtr llm, string userPrompt, bool addToHistory = true, CharArrayCallback callback = null, bool returnResponseJson = false)
            => LLMAgent_Chat_Static(llm, userPrompt, addToHistory, callback, returnResponseJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Clear_History")]
        public static extern void LLMAgent_Clear_History_Static(IntPtr llm);
        public void LLMAgent_Clear_History(IntPtr llm) => LLMAgent_Clear_History_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_History")]
        public static extern IntPtr LLMAgent_Get_History_Static(IntPtr llm);
        public IntPtr LLMAgent_Get_History(IntPtr llm) => LLMAgent_Get_History_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_History")]
        public static extern void LLMAgent_Set_History_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string historyJson);
        public void LLMAgent_Set_History(IntPtr llm, string historyJson) => LLMAgent_Set_History_Static(llm, historyJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Add_Message")]
        public static extern void LLMAgent_Add_Message_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string role,
            [MarshalAs(UnmanagedType.LPStr)] string content);
        public void LLMAgent_Add_Message(IntPtr llm, string role, string content) => LLMAgent_Add_Message_Static(llm, role, content);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Remove_Last_Message")]
        public static extern void LLMAgent_Remove_Last_Message_Static(IntPtr llm);
        public void LLMAgent_Remove_Last_Message(IntPtr llm) => LLMAgent_Remove_Last_Message_Static(llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Save_History")]
        public static extern void LLMAgent_Save_History_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);
        public void LLMAgent_Save_History(IntPtr llm, string filepath) => LLMAgent_Save_History_Static(llm, filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Load_History")]
        public static extern void LLMAgent_Load_History_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);
        public void LLMAgent_Load_History(IntPtr llm, string filepath) => LLMAgent_Load_History_Static(llm, filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_History_Size")]
        public static extern int LLMAgent_Get_History_Size_Static(IntPtr llm);
        public int LLMAgent_Get_History_Size(IntPtr llm) => LLMAgent_Get_History_Size_Static(llm);

#else
        // Desktop platform implementation with dynamic loading
        private static List<LlamaLib> instances = new List<LlamaLib>();
        private static readonly object runtimeLock = new object();
        public static string baseLibraryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static List<string> libraryExclusion = new List<string>();
        private static IntPtr runtimeLibraryHandle = IntPtr.Zero;
        private IntPtr libraryHandle = IntPtr.Zero;
        private static int debugLevelGlobal = 0;
        private static CharArrayCallback loggingCallbackGlobal = null;

        private void LoadRuntimeLibrary()
        {
            lock (runtimeLock)
            {
                if (runtimeLibraryHandle == IntPtr.Zero)
                {
                    runtimeLibraryHandle = LibraryLoader.LoadLibrary(GetRuntimeLibraryPath());
                    Has_GPU_Layers = LibraryLoader.GetSymbolDelegate<Has_GPU_Layers_Delegate>(runtimeLibraryHandle, "Has_GPU_Layers");
                    Available_Architectures = LibraryLoader.GetSymbolDelegate<Available_Architectures_Delegate>(runtimeLibraryHandle, "Available_Architectures");
                }
            }
        }

        public LlamaLib(bool gpu = false)
        {
            LoadRuntimeLibrary();
            LoadLibraries(gpu);
            lock (runtimeLock)
            {
                instances.Add(this);
                LLM_Debug(debugLevelGlobal);
                if (loggingCallbackGlobal != null) LLM_Logging_Callback(loggingCallbackGlobal);
            }
        }

        public static string GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux-x64";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return "osx-x64";
                else
                    return "osx-arm64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win-x64";
            else throw new ArgumentException("Unknown platform " + RuntimeInformation.OSDescription);
        }

        public virtual string FindLibrary(string libraryName)
        {
            List<string> lookupDirs = new List<string>();
            lookupDirs.Add(baseLibraryPath);
            lookupDirs.Add(Path.Combine(baseLibraryPath, "runtimes", GetPlatform(), "native"));

            foreach (string lookupDir in lookupDirs)
            {
                string libraryPath = Path.Combine(lookupDir, libraryName);
                if (File.Exists(libraryPath)) return libraryPath;
            }

            throw new InvalidOperationException($"Library {libraryName} not found!");
        }

        private string GetRuntimeLibraryPath()
        {
            string platform = GetPlatform();
            string libName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libName = "libllamalib_" + platform + "_runtime.so";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libName = "libllamalib_" + platform + "_runtime.dylib";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libName = "llamalib_" + platform + "_runtime.dll";
            else
                throw new ArgumentException("Unknown platform " + RuntimeInformation.OSDescription);
            return FindLibrary(libName);
        }

        private string[] GetAvailableArchitectures(bool gpu)
        {
            string architecturesString = Marshal.PtrToStringAnsi(Available_Architectures(gpu));
            if (string.IsNullOrEmpty(architecturesString))
            {
                throw new InvalidOperationException("No architectures available for the specified GPU setting.");
            }

            string[] librariesOptions = architecturesString.Split(',');
            List<string> libraries = new List<string>();
            foreach (string library in librariesOptions)
            {
                bool skip = false;
                string libraryLower = library.ToLower();
                foreach (string exclusionKeyword in libraryExclusion)
                {
                    if (libraryLower.Contains(exclusionKeyword))
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;
                libraries.Add(library);
            }
            return libraries.ToArray();
        }

        private void LoadLibraries(bool gpu)
        {
            string[] libraries = GetAvailableArchitectures(gpu);
            Exception lastException = null;
            foreach (string library in libraries)
            {
                try
                {
                    string libraryPath = FindLibrary(library.Trim());
                    if (debugLevelGlobal > 0) Console.WriteLine("Trying " + libraryPath);
                    libraryHandle = LibraryLoader.LoadLibrary(libraryPath);
                    LoadFunctionPointers();
                    architecture = library.Trim();
                    if (debugLevelGlobal > 0) Console.WriteLine("Successfully loaded: " + libraryPath);
                    return;
                }
                catch (Exception ex)
                {
                    if (debugLevelGlobal > 0) Console.WriteLine($"Failed to load library {library}: {ex.Message}.");
                    lastException = ex;
                    continue;
                }
            }

            // If we get here, no library was successfully loaded
            throw new InvalidOperationException($"Failed to load any library. Available libraries: {string.Join(", ", libraries)}", lastException);
        }

        private void LoadFunctionPointers()
        {
            LLM_Debug = LibraryLoader.GetSymbolDelegate<LLM_Debug_Delegate>(libraryHandle, "LLM_Debug");
            LLM_Logging_Callback = LibraryLoader.GetSymbolDelegate<LLM_Logging_Callback_Delegate>(libraryHandle, "LLM_Logging_Callback");
            LLM_Logging_Stop = LibraryLoader.GetSymbolDelegate<LLM_Logging_Stop_Delegate>(libraryHandle, "LLM_Logging_Stop");
            LLM_Get_Template = LibraryLoader.GetSymbolDelegate<LLM_Get_Template_Delegate>(libraryHandle, "LLM_Get_Template");
            LLM_Set_Template = LibraryLoader.GetSymbolDelegate<LLM_Set_Template_Delegate>(libraryHandle, "LLM_Set_Template");
            LLM_Enable_Reasoning = LibraryLoader.GetSymbolDelegate<LLM_Enable_Reasoning_Delegate>(libraryHandle, "LLM_Enable_Reasoning");
            LLM_Apply_Template = LibraryLoader.GetSymbolDelegate<LLM_Apply_Template_Delegate>(libraryHandle, "LLM_Apply_Template");
            LLM_Tokenize = LibraryLoader.GetSymbolDelegate<LLM_Tokenize_Delegate>(libraryHandle, "LLM_Tokenize");
            LLM_Detokenize = LibraryLoader.GetSymbolDelegate<LLM_Detokenize_Delegate>(libraryHandle, "LLM_Detokenize");
            LLM_Embeddings = LibraryLoader.GetSymbolDelegate<LLM_Embeddings_Delegate>(libraryHandle, "LLM_Embeddings");
            LLM_Completion = LibraryLoader.GetSymbolDelegate<LLM_Completion_Delegate>(libraryHandle, "LLM_Completion");
            LLM_Save_Slot = LibraryLoader.GetSymbolDelegate<LLM_Save_Slot_Delegate>(libraryHandle, "LLM_Save_Slot");
            LLM_Load_Slot = LibraryLoader.GetSymbolDelegate<LLM_Load_Slot_Delegate>(libraryHandle, "LLM_Load_Slot");
            LLM_Cancel = LibraryLoader.GetSymbolDelegate<LLM_Cancel_Delegate>(libraryHandle, "LLM_Cancel");
            LLM_Lora_Weight = LibraryLoader.GetSymbolDelegate<LLM_Lora_Weight_Delegate>(libraryHandle, "LLM_Lora_Weight");
            LLM_Lora_List = LibraryLoader.GetSymbolDelegate<LLM_Lora_List_Delegate>(libraryHandle, "LLM_Lora_List");
            LLM_Delete = LibraryLoader.GetSymbolDelegate<LLM_Delete_Delegate>(libraryHandle, "LLM_Delete");
            LLM_Start = LibraryLoader.GetSymbolDelegate<LLM_Start_Delegate>(libraryHandle, "LLM_Start");
            LLM_Started = LibraryLoader.GetSymbolDelegate<LLM_Started_Delegate>(libraryHandle, "LLM_Started");
            LLM_Stop = LibraryLoader.GetSymbolDelegate<LLM_Stop_Delegate>(libraryHandle, "LLM_Stop");
            LLM_Start_Server = LibraryLoader.GetSymbolDelegate<LLM_Start_Server_Delegate>(libraryHandle, "LLM_Start_Server");
            LLM_Stop_Server = LibraryLoader.GetSymbolDelegate<LLM_Stop_Server_Delegate>(libraryHandle, "LLM_Stop_Server");
            LLM_Join_Service = LibraryLoader.GetSymbolDelegate<LLM_Join_Service_Delegate>(libraryHandle, "LLM_Join_Service");
            LLM_Join_Server = LibraryLoader.GetSymbolDelegate<LLM_Join_Server_Delegate>(libraryHandle, "LLM_Join_Server");
            LLM_Set_SSL = LibraryLoader.GetSymbolDelegate<LLM_Set_SSL_Delegate>(libraryHandle, "LLM_Set_SSL");
            LLM_Status_Code = LibraryLoader.GetSymbolDelegate<LLM_Status_Code_Delegate>(libraryHandle, "LLM_Status_Code");
            LLM_Status_Message = LibraryLoader.GetSymbolDelegate<LLM_Status_Message_Delegate>(libraryHandle, "LLM_Status_Message");
            LLM_Embedding_Size = LibraryLoader.GetSymbolDelegate<LLM_Embedding_Size_Delegate>(libraryHandle, "LLM_Embedding_Size");
            LLMService_Construct = LibraryLoader.GetSymbolDelegate<LLMService_Construct_Delegate>(libraryHandle, "LLMService_Construct");
            LLMService_From_Command = LibraryLoader.GetSymbolDelegate<LLMService_From_Command_Delegate>(libraryHandle, "LLMService_From_Command");
            LLMClient_Construct = LibraryLoader.GetSymbolDelegate<LLMClient_Construct_Delegate>(libraryHandle, "LLMClient_Construct");
            LLMClient_Construct_Remote = LibraryLoader.GetSymbolDelegate<LLMClient_Construct_Remote_Delegate>(libraryHandle, "LLMClient_Construct_Remote");
            LLMClient_Set_SSL = LibraryLoader.GetSymbolDelegate<LLMClient_Set_SSL_Delegate>(libraryHandle, "LLMClient_Set_SSL");
            LLMClient_Is_Server_Alive = LibraryLoader.GetSymbolDelegate<LLMClient_Is_Server_Alive_Delegate>(libraryHandle, "LLMClient_Is_Server_Alive");
            LLMAgent_Construct = LibraryLoader.GetSymbolDelegate<LLMAgent_Construct_Delegate>(libraryHandle, "LLMAgent_Construct");
            LLMAgent_Set_User_Role = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_User_Role_Delegate>(libraryHandle, "LLMAgent_Set_User_Role");
            LLMAgent_Get_User_Role = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_User_Role_Delegate>(libraryHandle, "LLMAgent_Get_User_Role");
            LLMAgent_Set_Assistant_Role = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_Assistant_Role_Delegate>(libraryHandle, "LLMAgent_Set_Assistant_Role");
            LLMAgent_Get_Assistant_Role = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_Assistant_Role_Delegate>(libraryHandle, "LLMAgent_Get_Assistant_Role");
            LLMAgent_Set_System_Prompt = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_System_Prompt_Delegate>(libraryHandle, "LLMAgent_Set_System_Prompt");
            LLMAgent_Get_System_Prompt = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_System_Prompt_Delegate>(libraryHandle, "LLMAgent_Get_System_Prompt");
            LLMAgent_Set_Slot = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_Slot_Delegate>(libraryHandle, "LLMAgent_Set_Slot");
            LLMAgent_Get_Slot = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_Slot_Delegate>(libraryHandle, "LLMAgent_Get_Slot");
            LLM_Set_Completion_Parameters = LibraryLoader.GetSymbolDelegate<LLM_Set_Completion_Parameters_Delegate>(libraryHandle, "LLM_Set_Completion_Parameters");
            LLM_Get_Completion_Parameters = LibraryLoader.GetSymbolDelegate<LLM_Get_Completion_Parameters_Delegate>(libraryHandle, "LLM_Get_Completion_Parameters");
            LLM_Set_Grammar = LibraryLoader.GetSymbolDelegate<LLM_Set_Grammar_Delegate>(libraryHandle, "LLM_Set_Grammar");
            LLM_Get_Grammar = LibraryLoader.GetSymbolDelegate<LLM_Get_Grammar_Delegate>(libraryHandle, "LLM_Get_Grammar");
            LLMAgent_Chat = LibraryLoader.GetSymbolDelegate<LLMAgent_Chat_Delegate>(libraryHandle, "LLMAgent_Chat");
            LLMAgent_Clear_History = LibraryLoader.GetSymbolDelegate<LLMAgent_Clear_History_Delegate>(libraryHandle, "LLMAgent_Clear_History");
            LLMAgent_Get_History = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_History_Delegate>(libraryHandle, "LLMAgent_Get_History");
            LLMAgent_Set_History = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_History_Delegate>(libraryHandle, "LLMAgent_Set_History");
            LLMAgent_Add_Message = LibraryLoader.GetSymbolDelegate<LLMAgent_Add_Message_Delegate>(libraryHandle, "LLMAgent_Add_Message");
            LLMAgent_Remove_Last_Message = LibraryLoader.GetSymbolDelegate<LLMAgent_Remove_Last_Message_Delegate>(libraryHandle, "LLMAgent_Remove_Last_Message");
            LLMAgent_Save_History = LibraryLoader.GetSymbolDelegate<LLMAgent_Save_History_Delegate>(libraryHandle, "LLMAgent_Save_History");
            LLMAgent_Load_History = LibraryLoader.GetSymbolDelegate<LLMAgent_Load_History_Delegate>(libraryHandle, "LLMAgent_Load_History");
            LLMAgent_Get_History_Size = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_History_Size_Delegate>(libraryHandle, "LLMAgent_Get_History_Size");
        }

        // Delegate definitions for desktop platforms
        // Runtime lib
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Available_Architectures_Delegate([MarshalAs(UnmanagedType.I1)] bool gpu);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool Has_GPU_Layers_Delegate([MarshalAs(UnmanagedType.LPStr)] string command);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Debug_Delegate(int debugLevel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Logging_Callback_Delegate(CharArrayCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Logging_Stop_Delegate();

        // Main lib
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Get_Template_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Set_Template_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string template);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Enable_Reasoning_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.I1)] bool enable_reasoning);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Apply_Template_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string messages_as_json);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Tokenize_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Detokenize_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string tokens_as_json);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Embeddings_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Completion_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query, CharArrayCallback callback, int id_slot = -1, [MarshalAs(UnmanagedType.I1)] bool return_response_json = false);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Save_Slot_Delegate(IntPtr llm, int id_slot, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Load_Slot_Delegate(IntPtr llm, int id_slot, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Cancel_Delegate(IntPtr llm, int idSlot);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LLM_Lora_Weight_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string loras_as_json);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Lora_List_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Delete_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Start_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return : MarshalAs(UnmanagedType.I1)]
        public delegate bool LLM_Started_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Stop_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Start_Server_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string host = "0.0.0.0", int port = -1, [MarshalAs(UnmanagedType.LPStr)] string apiKey = "");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Stop_Server_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Join_Service_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Join_Server_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Set_SSL_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string sslCert, [MarshalAs(UnmanagedType.LPStr)] string sslKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LLM_Status_Code_Delegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Status_Message_Delegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LLM_Embedding_Size_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMService_Construct_Delegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelPath,
            int numSlots = 1,
            int numThreads = -1,
            int numGpuLayers = 0,
            [MarshalAs(UnmanagedType.I1)] bool flashAttention = false,
            int contextSize = 4096,
            int batchSize = 2048,
            [MarshalAs(UnmanagedType.I1)] bool embeddingOnly = false,
            int loraCount = 0,
            IntPtr loraPaths = default);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMService_From_Command_Delegate([MarshalAs(UnmanagedType.LPStr)] string paramsString);

        // LLMClient functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMClient_Construct_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMClient_Construct_Remote_Delegate(
            [MarshalAs(UnmanagedType.LPStr)] string url, int port, [MarshalAs(UnmanagedType.LPStr)] string apiKey = "", int numRetries = 5
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMClient_Set_SSL_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string SSLCert);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LLMClient_Is_Server_Alive_Delegate(IntPtr llm);

        // LLMAgent functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Construct_Delegate(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string systemPrompt = "",
            [MarshalAs(UnmanagedType.LPStr)] string userRole = "user",
            [MarshalAs(UnmanagedType.LPStr)] string assistantRole = "assistant");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Set_User_Role_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string userRole);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Get_User_Role_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Set_Assistant_Role_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string assistantRole);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Get_Assistant_Role_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Set_System_Prompt_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string systemPrompt);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Get_System_Prompt_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Set_Grammar_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string grammar);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Get_Grammar_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Set_Completion_Parameters_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string parameters);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLM_Get_Completion_Parameters_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Set_Slot_Delegate(IntPtr llm, int slotId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LLMAgent_Get_Slot_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Chat_Delegate(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string userPrompt,
            [MarshalAs(UnmanagedType.I1)] bool addToHistory = true,
            CharArrayCallback callback = null,
            [MarshalAs(UnmanagedType.I1)] bool returnResponseJson = false);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Clear_History_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Get_History_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Set_History_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string historyJson);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Add_Message_Delegate(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string role,
            [MarshalAs(UnmanagedType.LPStr)] string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Remove_Last_Message_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Save_History_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Load_History_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LLMAgent_Get_History_Size_Delegate(IntPtr llm);


        // Function pointers for desktop platforms
        // Runtime lib
        public static Available_Architectures_Delegate Available_Architectures;
        public static Has_GPU_Layers_Delegate Has_GPU_Layers;

        // Main lib
        public LLM_Debug_Delegate LLM_Debug;
        public LLM_Logging_Callback_Delegate LLM_Logging_Callback;
        public LLM_Logging_Stop_Delegate LLM_Logging_Stop;
        public LLM_Get_Template_Delegate LLM_Get_Template;
        public LLM_Set_Template_Delegate LLM_Set_Template;
        public LLM_Enable_Reasoning_Delegate LLM_Enable_Reasoning;
        public LLM_Apply_Template_Delegate LLM_Apply_Template;
        public LLM_Tokenize_Delegate LLM_Tokenize;
        public LLM_Detokenize_Delegate LLM_Detokenize;
        public LLM_Embeddings_Delegate LLM_Embeddings;
        public LLM_Completion_Delegate LLM_Completion;
        public LLM_Save_Slot_Delegate LLM_Save_Slot;
        public LLM_Load_Slot_Delegate LLM_Load_Slot;
        public LLM_Cancel_Delegate LLM_Cancel;
        public LLM_Lora_Weight_Delegate LLM_Lora_Weight;
        public LLM_Lora_List_Delegate LLM_Lora_List;
        public LLM_Delete_Delegate LLM_Delete;
        public LLM_Start_Delegate LLM_Start;
        public LLM_Started_Delegate LLM_Started;
        public LLM_Stop_Delegate LLM_Stop;
        public LLM_Start_Server_Delegate LLM_Start_Server;
        public LLM_Stop_Server_Delegate LLM_Stop_Server;
        public LLM_Join_Service_Delegate LLM_Join_Service;
        public LLM_Join_Server_Delegate LLM_Join_Server;
        public LLM_Set_SSL_Delegate LLM_Set_SSL;
        public LLM_Status_Code_Delegate LLM_Status_Code;
        public LLM_Status_Message_Delegate LLM_Status_Message;
        public LLM_Embedding_Size_Delegate LLM_Embedding_Size;
        public LLMService_Construct_Delegate LLMService_Construct;
        public LLMService_From_Command_Delegate LLMService_From_Command;
        public LLMClient_Construct_Delegate LLMClient_Construct;
        public LLMClient_Construct_Remote_Delegate LLMClient_Construct_Remote;
        public LLMClient_Set_SSL_Delegate LLMClient_Set_SSL;
        public LLMClient_Is_Server_Alive_Delegate LLMClient_Is_Server_Alive;
        public LLMAgent_Construct_Delegate LLMAgent_Construct;
        public LLMAgent_Set_User_Role_Delegate LLMAgent_Set_User_Role;
        public LLMAgent_Get_User_Role_Delegate LLMAgent_Get_User_Role;
        public LLMAgent_Set_Assistant_Role_Delegate LLMAgent_Set_Assistant_Role;
        public LLMAgent_Get_Assistant_Role_Delegate LLMAgent_Get_Assistant_Role;
        public LLMAgent_Set_System_Prompt_Delegate LLMAgent_Set_System_Prompt;
        public LLMAgent_Get_System_Prompt_Delegate LLMAgent_Get_System_Prompt;
        public LLMAgent_Set_Slot_Delegate LLMAgent_Set_Slot;
        public LLMAgent_Get_Slot_Delegate LLMAgent_Get_Slot;
        public LLM_Set_Completion_Parameters_Delegate LLM_Set_Completion_Parameters;
        public LLM_Get_Completion_Parameters_Delegate LLM_Get_Completion_Parameters;
        public LLM_Set_Grammar_Delegate LLM_Set_Grammar;
        public LLM_Get_Grammar_Delegate LLM_Get_Grammar;
        public LLMAgent_Chat_Delegate LLMAgent_Chat;
        public LLMAgent_Clear_History_Delegate LLMAgent_Clear_History;
        public LLMAgent_Get_History_Delegate LLMAgent_Get_History;
        public LLMAgent_Set_History_Delegate LLMAgent_Set_History;
        public LLMAgent_Add_Message_Delegate LLMAgent_Add_Message;
        public LLMAgent_Remove_Last_Message_Delegate LLMAgent_Remove_Last_Message;
        public LLMAgent_Save_History_Delegate LLMAgent_Save_History;
        public LLMAgent_Load_History_Delegate LLMAgent_Load_History;
        public LLMAgent_Get_History_Size_Delegate LLMAgent_Get_History_Size;

        public static void Debug(int debugLevel)
        {
            debugLevelGlobal = debugLevel;
            foreach (LlamaLib instance in instances)
            {
                instance.LLM_Debug(debugLevel);
            }
        }

        public static void LoggingCallback(CharArrayCallback callback)
        {
            loggingCallbackGlobal = callback;
            foreach (LlamaLib instance in instances)
            {
                instance.LLM_Logging_Callback(callback);
            }
        }

        public static void LoggingStop()
        {
            LoggingCallback(null);
        }

        public void Dispose()
        {
            LibraryLoader.FreeLibrary(libraryHandle);
            libraryHandle = IntPtr.Zero;

            lock (runtimeLock)
            {
                instances.Remove(this);
                if (instances.Count == 0)
                {
                    LibraryLoader.FreeLibrary(runtimeLibraryHandle);
                    runtimeLibraryHandle = IntPtr.Zero;
                }
            }
        }

        ~LlamaLib()
        {
            Dispose();
        }

#endif
    }
}
