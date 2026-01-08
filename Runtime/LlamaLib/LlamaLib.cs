using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;

namespace UndreamAI.LlamaLib
{
    public class LlamaLib
    {
        //################################################## FUNCTION DELEGATES ##################################################//
#if ENABLE_IL2CPP
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CharArrayCallback(IntPtr charArray);
#else
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CharArrayCallback([MarshalAs(UnmanagedType.LPStr)] string charArray);
#endif

        // Main lib
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMService_Command_Delegate(IntPtr llm);

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
        public delegate IntPtr LLMAgent_Construct_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string systemPrompt = "");

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
            [MarshalAs(UnmanagedType.I1)] bool returnResponseJson = false,
            [MarshalAs(UnmanagedType.I1)] bool debugPrompt = false);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Clear_History_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LLMAgent_Get_History_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Set_History_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string historyJson);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Add_User_Message_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Add_Assistant_Message_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Remove_Last_Message_Delegate(IntPtr llm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Save_History_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLMAgent_Load_History_Delegate(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LLMAgent_Get_History_Size_Delegate(IntPtr llm);

        //################################################## FUNCTION POINTERS ##################################################//

        // Main lib
        public LLM_Enable_Reasoning_Delegate LLM_Enable_Reasoning_Internal;
        public LLM_Apply_Template_Delegate LLM_Apply_Template_Internal;
        public LLM_Tokenize_Delegate LLM_Tokenize_Internal;
        public LLM_Detokenize_Delegate LLM_Detokenize_Internal;
        public LLM_Embeddings_Delegate LLM_Embeddings_Internal;
        public LLM_Completion_Delegate LLM_Completion_Internal;
        public LLM_Save_Slot_Delegate LLM_Save_Slot_Internal;
        public LLM_Load_Slot_Delegate LLM_Load_Slot_Internal;
        public LLM_Cancel_Delegate LLM_Cancel_Internal;
        public LLM_Lora_Weight_Delegate LLM_Lora_Weight_Internal;
        public LLM_Lora_List_Delegate LLM_Lora_List_Internal;
        public LLM_Delete_Delegate LLM_Delete_Internal;
        public LLM_Start_Delegate LLM_Start_Internal;
        public LLM_Started_Delegate LLM_Started_Internal;
        public LLM_Stop_Delegate LLM_Stop_Internal;
        public LLM_Start_Server_Delegate LLM_Start_Server_Internal;
        public LLM_Stop_Server_Delegate LLM_Stop_Server_Internal;
        public LLM_Join_Service_Delegate LLM_Join_Service_Internal;
        public LLM_Join_Server_Delegate LLM_Join_Server_Internal;
        public LLM_Set_SSL_Delegate LLM_Set_SSL_Internal;
        public LLM_Status_Code_Delegate LLM_Status_Code_Internal;
        public LLM_Status_Message_Delegate LLM_Status_Message_Internal;
        public LLM_Embedding_Size_Delegate LLM_Embedding_Size_Internal;
        public LLMService_Command_Delegate LLMService_Command_Internal;
        public LLMClient_Construct_Delegate LLMClient_Construct_Internal;
        public LLMClient_Construct_Remote_Delegate LLMClient_Construct_Remote_Internal;
        public LLMClient_Set_SSL_Delegate LLMClient_Set_SSL_Internal;
        public LLMClient_Is_Server_Alive_Delegate LLMClient_Is_Server_Alive_Internal;
        public LLMAgent_Construct_Delegate LLMAgent_Construct_Internal;
        public LLMAgent_Set_System_Prompt_Delegate LLMAgent_Set_System_Prompt_Internal;
        public LLMAgent_Get_System_Prompt_Delegate LLMAgent_Get_System_Prompt_Internal;
        public LLMAgent_Set_Slot_Delegate LLMAgent_Set_Slot_Internal;
        public LLMAgent_Get_Slot_Delegate LLMAgent_Get_Slot_Internal;
        public LLM_Set_Completion_Parameters_Delegate LLM_Set_Completion_Parameters_Internal;
        public LLM_Get_Completion_Parameters_Delegate LLM_Get_Completion_Parameters_Internal;
        public LLM_Set_Grammar_Delegate LLM_Set_Grammar_Internal;
        public LLM_Get_Grammar_Delegate LLM_Get_Grammar_Internal;
        public LLMAgent_Chat_Delegate LLMAgent_Chat_Internal;
        public LLMAgent_Clear_History_Delegate LLMAgent_Clear_History_Internal;
        public LLMAgent_Get_History_Delegate LLMAgent_Get_History_Internal;
        public LLMAgent_Set_History_Delegate LLMAgent_Set_History_Internal;
        public LLMAgent_Add_User_Message_Delegate LLMAgent_Add_User_Message_Internal;
        public LLMAgent_Add_Assistant_Message_Delegate LLMAgent_Add_Assistant_Message_Internal;
        public LLMAgent_Remove_Last_Message_Delegate LLMAgent_Remove_Last_Message_Internal;
        public LLMAgent_Save_History_Delegate LLMAgent_Save_History_Internal;
        public LLMAgent_Load_History_Delegate LLMAgent_Load_History_Internal;
        public LLMAgent_Get_History_Size_Delegate LLMAgent_Get_History_Size_Internal;

        //################################################## STATUS CHECKING WRAPPER ##################################################//

        public void CheckStatus(bool crashesOnly = false)
        {
            int status = LLM_Status_Code_Internal();
            if (status > 0 || (status < 0 && !crashesOnly))
            {
                string msg = Marshal.PtrToStringAnsi(LLM_Status_Message_Internal()) ?? "";
                throw new InvalidOperationException($"LlamaLib error {status}: {msg}");
            }
        }

        private T CallWithStatus<T>(Func<T> f)
        {
            CheckStatus(true);
            T r = f();
            CheckStatus();
            return r;
        }

        private void CallWithStatus(Action a)
        {
            CheckStatus(true);
            a();
            CheckStatus();
        }

        //################################################## COMMON IMPLEMENTATION ##################################################//

        public string architecture { get; private set; }

        public void LLM_Enable_Reasoning(IntPtr llm, bool enable_reasoning) => CallWithStatus(() => LLM_Enable_Reasoning_Internal(llm, enable_reasoning));
        public IntPtr LLM_Apply_Template(IntPtr llm, string messages_as_json) => CallWithStatus(() => LLM_Apply_Template_Internal(llm, messages_as_json));
        public IntPtr LLM_Tokenize(IntPtr llm, string query) => CallWithStatus(() => LLM_Tokenize_Internal(llm, query));
        public IntPtr LLM_Detokenize(IntPtr llm, string tokens_as_json) => CallWithStatus(() => LLM_Detokenize_Internal(llm, tokens_as_json));
        public IntPtr LLM_Embeddings(IntPtr llm, string query) => CallWithStatus(() => LLM_Embeddings_Internal(llm, query));
        public IntPtr LLM_Completion(IntPtr llm, string query, CharArrayCallback callback, int id_slot = -1, bool return_response_json = false) => CallWithStatus(() => LLM_Completion_Internal(llm, query, callback, id_slot, return_response_json));
        public IntPtr LLM_Save_Slot(IntPtr llm, int id_slot, string filepath) => CallWithStatus(() => LLM_Save_Slot_Internal(llm, id_slot, filepath));
        public IntPtr LLM_Load_Slot(IntPtr llm, int id_slot, string filepath) => CallWithStatus(() => LLM_Load_Slot_Internal(llm, id_slot, filepath));
        public void LLM_Cancel(IntPtr llm, int idSlot) => CallWithStatus(() => LLM_Cancel_Internal(llm, idSlot));
        public bool LLM_Lora_Weight(IntPtr llm, string loras_as_json) => CallWithStatus(() => LLM_Lora_Weight_Internal(llm, loras_as_json));
        public IntPtr LLM_Lora_List(IntPtr llm) => CallWithStatus(() => LLM_Lora_List_Internal(llm));
        public void LLM_Delete(IntPtr llm) => CallWithStatus(() => LLM_Delete_Internal(llm));
        public void LLM_Start(IntPtr llm) => CallWithStatus(() => LLM_Start_Internal(llm));
        public bool LLM_Started(IntPtr llm) => CallWithStatus(() => LLM_Started_Internal(llm));
        public void LLM_Stop(IntPtr llm) => CallWithStatus(() => LLM_Stop_Internal(llm));
        public void LLM_Start_Server(IntPtr llm, string host = "0.0.0.0", int port = -1, string apiKey = "") => CallWithStatus(() => LLM_Start_Server_Internal(llm, host, port, apiKey));
        public void LLM_Stop_Server(IntPtr llm) => CallWithStatus(() => LLM_Stop_Server_Internal(llm));
        public void LLM_Join_Service(IntPtr llm) => CallWithStatus(() => LLM_Join_Service_Internal(llm));
        public void LLM_Join_Server(IntPtr llm) => CallWithStatus(() => LLM_Join_Server_Internal(llm));
        public void LLM_Set_SSL(IntPtr llm, string sslCert, string sslKey) => CallWithStatus(() => LLM_Set_SSL_Internal(llm, sslCert, sslKey));
        public int LLM_Status_Code() => CallWithStatus(() => LLM_Status_Code_Internal());
        public IntPtr LLM_Status_Message() => CallWithStatus(() => LLM_Status_Message_Internal());
        public int LLM_Embedding_Size(IntPtr llm) => CallWithStatus(() => LLM_Embedding_Size_Internal(llm));
        public IntPtr LLMService_Construct(
            string modelPath,
            int numSlots = 1,
            int numThreads = -1,
            int numGpuLayers = 0,
            bool flashAttention = false,
            int contextSize = 4096,
            int batchSize = 2048,
            bool embeddingOnly = false,
            int loraCount = 0,
            IntPtr loraPaths = default
        ) => CallWithStatus(() => LLMService_Construct_Internal(modelPath, numSlots, numThreads, numGpuLayers, flashAttention, contextSize, batchSize, embeddingOnly, loraCount, loraPaths));
        public IntPtr LLMService_From_Command(string paramsString) => CallWithStatus(() => LLMService_From_Command_Internal(paramsString));
        public IntPtr LLMService_Command(IntPtr llm) => CallWithStatus(() => LLMService_Command_Internal(llm));
        public IntPtr LLMClient_Construct(IntPtr llm) => CallWithStatus(() => LLMClient_Construct_Internal(llm));
        public IntPtr LLMClient_Construct_Remote(string url, int port, string apiKey = "", int numRetries = 5) => CallWithStatus(() => LLMClient_Construct_Remote_Internal(url, port, apiKey, numRetries));
        public void LLMClient_Set_SSL(IntPtr llm, string SSLCert) => CallWithStatus(() => LLMClient_Set_SSL_Internal(llm, SSLCert));
        public bool LLMClient_Is_Server_Alive(IntPtr llm) => CallWithStatus(() => LLMClient_Is_Server_Alive_Internal(llm));
        public IntPtr LLMAgent_Construct(IntPtr llm, string systemPrompt = "") => CallWithStatus(() => LLMAgent_Construct_Internal(llm, systemPrompt));
        public void LLMAgent_Set_System_Prompt(IntPtr llm, string systemPrompt) => CallWithStatus(() => LLMAgent_Set_System_Prompt_Internal(llm, systemPrompt));
        public IntPtr LLMAgent_Get_System_Prompt(IntPtr llm) => CallWithStatus(() => LLMAgent_Get_System_Prompt_Internal(llm));
        public void LLM_Set_Completion_Parameters(IntPtr llm, string parameters) => CallWithStatus(() => LLM_Set_Completion_Parameters_Internal(llm, parameters));
        public IntPtr LLM_Get_Completion_Parameters(IntPtr llm) => CallWithStatus(() => LLM_Get_Completion_Parameters_Internal(llm));
        public void LLM_Set_Grammar(IntPtr llm, string grammar) => CallWithStatus(() => LLM_Set_Grammar_Internal(llm, grammar));
        public IntPtr LLM_Get_Grammar(IntPtr llm) => CallWithStatus(() => LLM_Get_Grammar_Internal(llm));
        public void LLMAgent_Set_Slot(IntPtr llm, int slotId) => CallWithStatus(() => LLMAgent_Set_Slot_Internal(llm, slotId));
        public int LLMAgent_Get_Slot(IntPtr llm) => CallWithStatus(() => LLMAgent_Get_Slot_Internal(llm));
        public IntPtr LLMAgent_Chat(IntPtr llm, string userPrompt, bool addToHistory = true, CharArrayCallback callback = null, bool returnResponseJson = false, bool debugPrompt = false)
            => CallWithStatus(() => LLMAgent_Chat_Internal(llm, userPrompt, addToHistory, callback, returnResponseJson, debugPrompt));
        public void LLMAgent_Clear_History(IntPtr llm) => CallWithStatus(() => LLMAgent_Clear_History_Internal(llm));
        public IntPtr LLMAgent_Get_History(IntPtr llm) => CallWithStatus(() => LLMAgent_Get_History_Internal(llm));
        public void LLMAgent_Set_History(IntPtr llm, string historyJson) => CallWithStatus(() => LLMAgent_Set_History_Internal(llm, historyJson));
        public void LLMAgent_Add_User_Message(IntPtr llm, string content) => CallWithStatus(() => LLMAgent_Add_User_Message_Internal(llm, content));
        public void LLMAgent_Add_Assistant_Message(IntPtr llm, string content) => CallWithStatus(() => LLMAgent_Add_Assistant_Message_Internal(llm, content));
        public void LLMAgent_Remove_Last_Message(IntPtr llm) => CallWithStatus(() => LLMAgent_Remove_Last_Message_Internal(llm));
        public void LLMAgent_Save_History(IntPtr llm, string filepath) => CallWithStatus(() => LLMAgent_Save_History_Internal(llm, filepath));
        public void LLMAgent_Load_History(IntPtr llm, string filepath) => CallWithStatus(() => LLMAgent_Load_History_Internal(llm, filepath));
        public int LLMAgent_Get_History_Size(IntPtr llm) => CallWithStatus(() => LLMAgent_Get_History_Size_Internal(llm));

        //################################################## MOBILE IMPLEMENTATION ##################################################//

#if (ANDROID || IOS || VISIONOS) || ((UNITY_ANDROID || UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR)
#if UNITY_ANDROID
        public const string DllName = "libllamalib_android";
#elif ANDROID_ARM64
        public const string DllName = "libllamalib_android-arm64";
#elif ANDROID_X64
        public const string DllName = "libllamalib_android-x64";
#else
        public const string DllName = "__Internal";
#endif

        // Static functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Debug")]
        public static extern void LLM_Debug_Static(int debugLevel);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Logging_Callback")]
        public static extern void LLM_Logging_Callback_Static(CharArrayCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Logging_Stop")]
        public static extern void LLM_Logging_Stop_Static();

        public static void Debug(int debugLevel) => LLM_Debug_Static(debugLevel);
        public static void LoggingCallback(CharArrayCallback callback) => LLM_Logging_Callback_Static(callback);
        public static void LoggingStop() => LLM_Logging_Stop_Static();

        // LLM functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Enable_Reasoning")]
        public static extern void LLM_Enable_Reasoning_Static(IntPtr llm, [MarshalAs(UnmanagedType.I1)] bool enable_reasoning);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Apply_Template")]
        public static extern IntPtr LLM_Apply_Template_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string messages_as_json);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern IntPtr LLM_Tokenize_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern IntPtr LLM_Detokenize_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string tokens_as_json);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Embeddings")]
        public static extern IntPtr LLM_Embeddings_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern IntPtr LLM_Completion_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string query, CharArrayCallback callback, int id_slot = -1, [MarshalAs(UnmanagedType.I1)] bool return_response_json = false);

        // LLMLocal functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Save_Slot")]
        public static extern IntPtr LLM_Save_Slot_Static(IntPtr llm, int id_slot, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Load_Slot")]
        public static extern IntPtr LLM_Load_Slot_Static(IntPtr llm, int id_slot, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LLM_Cancel_Static(IntPtr llm, int idSlot);

        // LLMProvider functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Lora_Weight")]
        public static extern bool LLM_Lora_Weight_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string loras_as_json);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Lora_List")]
        public static extern IntPtr LLM_Lora_List_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LLM_Delete_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LLM_Start_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Started")]
        [return : MarshalAs(UnmanagedType.I1)]
        public static extern bool LLM_Started_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LLM_Stop_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start_Server")]
        public static extern void LLM_Start_Server_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string host = "0.0.0.0",
            int port = -1,
            [MarshalAs(UnmanagedType.LPStr)] string apiKey = "");

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop_Server")]
        public static extern void LLM_Stop_Server_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Join_Service")]
        public static extern void LLM_Join_Service_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Join_Server")]
        public static extern void LLM_Join_Server_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_SSL")]
        public static extern void LLM_Set_SSL_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string sslCert,
            [MarshalAs(UnmanagedType.LPStr)] string sslKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status_Code")]
        public static extern int LLM_Status_Code_Static();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status_Message")]
        public static extern IntPtr LLM_Status_Message_Static();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Embedding_Size")]
        public static extern int LLM_Embedding_Size_Static(IntPtr llm);

        // LLMService functions
        public LLMService_Construct_Delegate LLMService_Construct_Internal;
        public LLMService_From_Command_Delegate LLMService_From_Command_Internal;

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

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMService_From_Command")]
        public static extern IntPtr LLMService_From_Command_Static([MarshalAs(UnmanagedType.LPStr)] string paramsString);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMService_Command")]
        public static extern IntPtr LLMService_Command_Static(IntPtr llm);

        // LLMClient functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Construct")]
        public static extern IntPtr LLMClient_Construct_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Construct_Remote")]
        public static extern IntPtr LLMClient_Construct_Remote_Static(
            [MarshalAs(UnmanagedType.LPStr)] string url,
            int port,
            [MarshalAs(UnmanagedType.LPStr)] string apiKey = "",
            int numRetries = 5);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Set_SSL")]
        public static extern void LLMClient_Set_SSL_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string SSLCert);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMClient_Is_Server_Alive")]
        public static extern bool LLMClient_Is_Server_Alive_Static(IntPtr llm);

        // LLMAgent functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Construct")]
        public static extern IntPtr LLMAgent_Construct_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string systemPrompt = "");

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_System_Prompt")]
        public static extern void LLMAgent_Set_System_Prompt_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string systemPrompt);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_System_Prompt")]
        public static extern IntPtr LLMAgent_Get_System_Prompt_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_Completion_Parameters")]
        public static extern void LLM_Set_Completion_Parameters_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string parameters);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Get_Completion_Parameters")]
        public static extern IntPtr LLM_Get_Completion_Parameters_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Set_Grammar")]
        public static extern void LLM_Set_Grammar_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string grammar);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Get_Grammar")]
        public static extern IntPtr LLM_Get_Grammar_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_Slot")]
        public static extern void LLMAgent_Set_Slot_Static(IntPtr llm, int slotId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_Slot")]
        public static extern int LLMAgent_Get_Slot_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Chat")]
        public static extern IntPtr LLMAgent_Chat_Static(IntPtr llm,
            [MarshalAs(UnmanagedType.LPStr)] string userPrompt,
            [MarshalAs(UnmanagedType.I1)] bool addToHistory = true,
            CharArrayCallback callback = null,
            [MarshalAs(UnmanagedType.I1)] bool returnResponseJson = false,
            [MarshalAs(UnmanagedType.I1)] bool debugPrompt = false);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Clear_History")]
        public static extern void LLMAgent_Clear_History_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_History")]
        public static extern IntPtr LLMAgent_Get_History_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Set_History")]
        public static extern void LLMAgent_Set_History_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string historyJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Add_User_Message")]
        public static extern void LLMAgent_Add_User_Message_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string content);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Add_Assistant_Message")]
        public static extern void LLMAgent_Add_Assistant_Message_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string content);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Remove_Last_Message")]
        public static extern void LLMAgent_Remove_Last_Message_Static(IntPtr llm);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Save_History")]
        public static extern void LLMAgent_Save_History_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Load_History")]
        public static extern void LLMAgent_Load_History_Static(IntPtr llm, [MarshalAs(UnmanagedType.LPStr)] string filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLMAgent_Get_History_Size")]
        public static extern int LLMAgent_Get_History_Size_Static(IntPtr llm);

        public static IntPtr Available_Architectures([MarshalAs(UnmanagedType.I1)] bool gpu) { return IntPtr.Zero; }
        public static bool Has_GPU_Layers([MarshalAs(UnmanagedType.LPStr)] string command) { return false; }

        public LlamaLib(bool gpu = false)
        {
#if UNITY_ANDROID
            architecture = "android";
#elif ANDROID_ARM64
            architecture = "android-arm64";
#elif ANDROID_X64
            architecture = "android-x64";
#elif IOS || UNITY_IOS
            architecture = "ios-arm64";
#elif VISIONOS || UNITY_VISIONOS
            architecture = "visionos-arm64";
#endif

            LLM_Enable_Reasoning_Internal = (llm, enable_reasoning) => LLM_Enable_Reasoning_Static(llm, enable_reasoning);
            LLM_Apply_Template_Internal = (llm, messages_as_json) => LLM_Apply_Template_Static(llm, messages_as_json);
            LLM_Tokenize_Internal = (llm, query) => LLM_Tokenize_Static(llm, query);
            LLM_Detokenize_Internal = (llm, tokens_as_json) => LLM_Detokenize_Static(llm, tokens_as_json);
            LLM_Embeddings_Internal = (llm, query) => LLM_Embeddings_Static(llm, query);
            LLM_Completion_Internal = (llm, query, callback, id_slot, return_response_json) => LLM_Completion_Static(llm, query, callback, id_slot, return_response_json);
            LLM_Save_Slot_Internal = (llm, id_slot, filepath) => LLM_Save_Slot_Static(llm, id_slot, filepath);
            LLM_Load_Slot_Internal = (llm, id_slot, filepath) => LLM_Load_Slot_Static(llm, id_slot, filepath);
            LLM_Cancel_Internal = (llm, idSlot) => LLM_Cancel_Static(llm, idSlot);
            LLM_Lora_Weight_Internal = (llm, loras_as_json) => LLM_Lora_Weight_Static(llm, loras_as_json);
            LLM_Lora_List_Internal = (llm) => LLM_Lora_List_Static(llm);
            LLM_Delete_Internal = (llm) => LLM_Delete_Static(llm);
            LLM_Start_Internal = (llm) => LLM_Start_Static(llm);
            LLM_Started_Internal = (llm) => LLM_Started_Static(llm);
            LLM_Stop_Internal = (llm) => LLM_Stop_Static(llm);
            LLM_Start_Server_Internal = (llm, host, port, apiKey) => LLM_Start_Server_Static(llm, host, port, apiKey);
            LLM_Stop_Server_Internal = (llm) => LLM_Stop_Server_Static(llm);
            LLM_Join_Service_Internal = (llm) => LLM_Join_Service_Static(llm);
            LLM_Join_Server_Internal = (llm) => LLM_Join_Server_Static(llm);
            LLM_Set_SSL_Internal = (llm, sslCert, sslKey) => LLM_Set_SSL_Static(llm, sslCert, sslKey);
            LLM_Status_Code_Internal = () => LLM_Status_Code_Static();
            LLM_Status_Message_Internal = () => LLM_Status_Message_Static();
            LLM_Embedding_Size_Internal = (llm) => LLM_Embedding_Size_Static(llm);
            LLMService_Construct_Internal = (modelPath, numSlots, numThreads, numGpuLayers, flashAttention, contextSize, batchSize, embeddingOnly, loraCount, loraPaths) => LLMService_Construct_Static(modelPath, numSlots, numThreads, numGpuLayers, flashAttention, contextSize, batchSize, embeddingOnly, loraCount, loraPaths);
            LLMService_From_Command_Internal = (paramsString) => LLMService_From_Command_Static(paramsString);
            LLMService_Command_Internal = (llm) => LLMService_Command_Static(llm);
            LLMClient_Construct_Internal = (llm) => LLMClient_Construct_Static(llm);
            LLMClient_Construct_Remote_Internal = (url, port, apiKey, numRetries) => LLMClient_Construct_Remote_Static(url, port, apiKey, numRetries);
            LLMClient_Set_SSL_Internal = (llm, SSLCert) => LLMClient_Set_SSL_Static(llm, SSLCert);
            LLMClient_Is_Server_Alive_Internal = (llm) => LLMClient_Is_Server_Alive_Static(llm);
            LLMAgent_Construct_Internal = (llm, systemPrompt) => LLMAgent_Construct_Static(llm, systemPrompt);
            LLMAgent_Set_System_Prompt_Internal = (llm, systemPrompt) => LLMAgent_Set_System_Prompt_Static(llm, systemPrompt);
            LLMAgent_Get_System_Prompt_Internal = (llm) => LLMAgent_Get_System_Prompt_Static(llm);
            LLM_Set_Completion_Parameters_Internal = (llm, parameters) => LLM_Set_Completion_Parameters_Static(llm, parameters);
            LLM_Get_Completion_Parameters_Internal = (llm) => LLM_Get_Completion_Parameters_Static(llm);
            LLM_Set_Grammar_Internal = (llm, grammar) => LLM_Set_Grammar_Static(llm, grammar);
            LLM_Get_Grammar_Internal = (llm) => LLM_Get_Grammar_Static(llm);
            LLMAgent_Set_Slot_Internal = (llm, slotId) => LLMAgent_Set_Slot_Static(llm, slotId);
            LLMAgent_Get_Slot_Internal = (llm) => LLMAgent_Get_Slot_Static(llm);
            LLMAgent_Chat_Internal = (llm, userPrompt, addToHistory, callback, returnResponseJson, debugPrompt) => LLMAgent_Chat_Static(llm, userPrompt, addToHistory, callback, returnResponseJson, debugPrompt);
            LLMAgent_Clear_History_Internal = (llm) => LLMAgent_Clear_History_Static(llm);
            LLMAgent_Get_History_Internal = (llm) => LLMAgent_Get_History_Static(llm);
            LLMAgent_Set_History_Internal = (llm, historyJson) => LLMAgent_Set_History_Static(llm, historyJson);
            LLMAgent_Add_User_Message_Internal = (llm, content) => LLMAgent_Add_User_Message_Static(llm, content);
            LLMAgent_Add_Assistant_Message_Internal = (llm, content) => LLMAgent_Add_Assistant_Message_Static(llm, content);
            LLMAgent_Remove_Last_Message_Internal = (llm) => LLMAgent_Remove_Last_Message_Static(llm);
            LLMAgent_Save_History_Internal = (llm, filepath) => LLMAgent_Save_History_Static(llm, filepath);
            LLMAgent_Load_History_Internal = (llm, filepath) => LLMAgent_Load_History_Static(llm, filepath);
            LLMAgent_Get_History_Size_Internal = (llm) => LLMAgent_Get_History_Size_Static(llm);
        }

        public void Dispose() {}

#else
        // Desktop platform implementation with dynamic loading
        private static List<LlamaLib> instances = new List<LlamaLib>();
        private static readonly object runtimeLock = new object();
        public static string baseLibraryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static List<string> libraryExclusion = new List<string>();
        private static IntPtr runtimeLibraryHandle = IntPtr.Zero;
        private IntPtr libraryHandle = IntPtr.Zero;
        private List<IntPtr> dependencyHandles = new List<IntPtr>();
        private static int debugLevelGlobal = 0;
        private static CharArrayCallback loggingCallbackGlobal = null;
        private string[] availableLibraries = null;
        private int currentLibraryIndex = 0;

        // Runtime lib
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Available_Architectures_Delegate([MarshalAs(UnmanagedType.I1)] bool gpu);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool Has_GPU_Layers_Delegate([MarshalAs(UnmanagedType.LPStr)] string command);

        public static Available_Architectures_Delegate Available_Architectures;
        public static Has_GPU_Layers_Delegate Has_GPU_Layers;

        // LLM construction of single library
        public LLMService_Construct_Delegate LLMService_Construct_Internal_Single;
        public LLMService_From_Command_Delegate LLMService_From_Command_Internal_Single;

        public IntPtr LLMService_Construct_Internal(
            [MarshalAs(UnmanagedType.LPStr)] string modelPath,
            int numSlots = 1,
            int numThreads = -1,
            int numGpuLayers = 0,
            [MarshalAs(UnmanagedType.I1)] bool flashAttention = false,
            int contextSize = 4096,
            int batchSize = 2048,
            [MarshalAs(UnmanagedType.I1)] bool embeddingOnly = false,
            int loraCount = 0,
            IntPtr loraPaths = default) => CreateLLMWithFallback(() => LLMService_Construct_Internal_Single(modelPath, numSlots, numThreads, numGpuLayers, flashAttention, contextSize, batchSize, embeddingOnly, loraCount, loraPaths));

        public IntPtr LLMService_From_Command_Internal([MarshalAs(UnmanagedType.LPStr)] string paramsString) => CreateLLMWithFallback(() => LLMService_From_Command_Internal_Single(paramsString));


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
            availableLibraries = GetAvailableArchitectures(gpu);
            currentLibraryIndex = -1;

            if (!TryNextLibrary())
            {
                throw new InvalidOperationException($"Failed to load any library. Available libraries: {string.Join(", ", availableLibraries)}");
            }
        }

        public static List<string> GetArchitectureDependencies(string library, string libraryPath)
        {
            List<string> dependencies = new List<string>();
            if (library.Contains("cublas"))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dependencies.Add(Path.Combine(libraryPath, "cudart64_12.dll"));
                    dependencies.Add(Path.Combine(libraryPath, "cublasLt64_12.dll"));
                    dependencies.Add(Path.Combine(libraryPath, "cublas64_12.dll"));
                }
            }
            else if (library.Contains("vulkan"))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dependencies.Add(Path.Combine(libraryPath, "vulkan-1.dll"));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dependencies.Add(Path.Combine(libraryPath, "libvulkan.so.1"));
                }
            }
            return dependencies;
        }

        public bool TryNextLibrary()
        {
            if (availableLibraries == null)
                return false;

            if (libraryHandle != IntPtr.Zero)
            {
                try { LibraryLoader.FreeLibrary(libraryHandle); } catch {}
                libraryHandle = IntPtr.Zero;
            }

            while (++currentLibraryIndex < availableLibraries.Length)
            {
                string library = availableLibraries[currentLibraryIndex];
                try
                {
                    string libraryPath = FindLibrary(library.Trim());
                    if (debugLevelGlobal > 0) Console.WriteLine("Trying " + libraryPath);

                    foreach (string dependency in GetArchitectureDependencies(library, Path.GetDirectoryName(libraryPath)))
                    {
                        dependencyHandles.Add(LibraryLoader.LoadLibrary(dependency));
                    }
                    libraryHandle = LibraryLoader.LoadLibrary(libraryPath);

                    LoadFunctionPointers();
                    architecture = library.Trim();
                    if (debugLevelGlobal > 0) Console.WriteLine("Successfully loaded: " + libraryPath);
                    return true;
                }
                catch (Exception ex)
                {
                    if (libraryHandle != IntPtr.Zero)
                    {
                        if (debugLevelGlobal > 0) Console.WriteLine($"Failed to load library {library}: {ex.Message}.");
                        try { LibraryLoader.FreeLibrary(libraryHandle); } catch {}
                        libraryHandle = IntPtr.Zero;
                    }
                }
            }

            return false;
        }

        public IntPtr CreateLLMWithFallback(Func<IntPtr> createFunc)
        {
            while (true)
            {
                try
                {
                    IntPtr llmInstance = createFunc();
                    if (llmInstance == IntPtr.Zero) throw new InvalidOperationException("LLMService construction returned null pointer");
                    CheckStatus();
                    return llmInstance;
                }
                catch (Exception ex)
                {
                    if (!TryNextLibrary())
                    {
                        throw new InvalidOperationException(
                            $"Failed LLMService construction with all available libraries. Last error: {ex.Message}",
                            ex);
                    }
                }
            }
        }

        private void LoadFunctionPointers()
        {
            LLM_Debug = LibraryLoader.GetSymbolDelegate<LLM_Debug_Delegate>(libraryHandle, "LLM_Debug");
            LLM_Logging_Callback = LibraryLoader.GetSymbolDelegate<LLM_Logging_Callback_Delegate>(libraryHandle, "LLM_Logging_Callback");
            LLM_Logging_Stop = LibraryLoader.GetSymbolDelegate<LLM_Logging_Stop_Delegate>(libraryHandle, "LLM_Logging_Stop");

            LLM_Enable_Reasoning_Internal = LibraryLoader.GetSymbolDelegate<LLM_Enable_Reasoning_Delegate>(libraryHandle, "LLM_Enable_Reasoning");
            LLM_Apply_Template_Internal = LibraryLoader.GetSymbolDelegate<LLM_Apply_Template_Delegate>(libraryHandle, "LLM_Apply_Template");
            LLM_Tokenize_Internal = LibraryLoader.GetSymbolDelegate<LLM_Tokenize_Delegate>(libraryHandle, "LLM_Tokenize");
            LLM_Detokenize_Internal = LibraryLoader.GetSymbolDelegate<LLM_Detokenize_Delegate>(libraryHandle, "LLM_Detokenize");
            LLM_Embeddings_Internal = LibraryLoader.GetSymbolDelegate<LLM_Embeddings_Delegate>(libraryHandle, "LLM_Embeddings");
            LLM_Completion_Internal = LibraryLoader.GetSymbolDelegate<LLM_Completion_Delegate>(libraryHandle, "LLM_Completion");
            LLM_Save_Slot_Internal = LibraryLoader.GetSymbolDelegate<LLM_Save_Slot_Delegate>(libraryHandle, "LLM_Save_Slot");
            LLM_Load_Slot_Internal = LibraryLoader.GetSymbolDelegate<LLM_Load_Slot_Delegate>(libraryHandle, "LLM_Load_Slot");
            LLM_Cancel_Internal = LibraryLoader.GetSymbolDelegate<LLM_Cancel_Delegate>(libraryHandle, "LLM_Cancel");
            LLM_Lora_Weight_Internal = LibraryLoader.GetSymbolDelegate<LLM_Lora_Weight_Delegate>(libraryHandle, "LLM_Lora_Weight");
            LLM_Lora_List_Internal = LibraryLoader.GetSymbolDelegate<LLM_Lora_List_Delegate>(libraryHandle, "LLM_Lora_List");
            LLM_Delete_Internal = LibraryLoader.GetSymbolDelegate<LLM_Delete_Delegate>(libraryHandle, "LLM_Delete");
            LLM_Start_Internal = LibraryLoader.GetSymbolDelegate<LLM_Start_Delegate>(libraryHandle, "LLM_Start");
            LLM_Started_Internal = LibraryLoader.GetSymbolDelegate<LLM_Started_Delegate>(libraryHandle, "LLM_Started");
            LLM_Stop_Internal = LibraryLoader.GetSymbolDelegate<LLM_Stop_Delegate>(libraryHandle, "LLM_Stop");
            LLM_Start_Server_Internal = LibraryLoader.GetSymbolDelegate<LLM_Start_Server_Delegate>(libraryHandle, "LLM_Start_Server");
            LLM_Stop_Server_Internal = LibraryLoader.GetSymbolDelegate<LLM_Stop_Server_Delegate>(libraryHandle, "LLM_Stop_Server");
            LLM_Join_Service_Internal = LibraryLoader.GetSymbolDelegate<LLM_Join_Service_Delegate>(libraryHandle, "LLM_Join_Service");
            LLM_Join_Server_Internal = LibraryLoader.GetSymbolDelegate<LLM_Join_Server_Delegate>(libraryHandle, "LLM_Join_Server");
            LLM_Set_SSL_Internal = LibraryLoader.GetSymbolDelegate<LLM_Set_SSL_Delegate>(libraryHandle, "LLM_Set_SSL");
            LLM_Status_Code_Internal = LibraryLoader.GetSymbolDelegate<LLM_Status_Code_Delegate>(libraryHandle, "LLM_Status_Code");
            LLM_Status_Message_Internal = LibraryLoader.GetSymbolDelegate<LLM_Status_Message_Delegate>(libraryHandle, "LLM_Status_Message");
            LLM_Embedding_Size_Internal = LibraryLoader.GetSymbolDelegate<LLM_Embedding_Size_Delegate>(libraryHandle, "LLM_Embedding_Size");
            LLMService_Construct_Internal_Single = LibraryLoader.GetSymbolDelegate<LLMService_Construct_Delegate>(libraryHandle, "LLMService_Construct");
            LLMService_From_Command_Internal_Single = LibraryLoader.GetSymbolDelegate<LLMService_From_Command_Delegate>(libraryHandle, "LLMService_From_Command");
            LLMService_Command_Internal = LibraryLoader.GetSymbolDelegate<LLMService_Command_Delegate>(libraryHandle, "LLMService_Command");
            LLMClient_Construct_Internal = LibraryLoader.GetSymbolDelegate<LLMClient_Construct_Delegate>(libraryHandle, "LLMClient_Construct");
            LLMClient_Construct_Remote_Internal = LibraryLoader.GetSymbolDelegate<LLMClient_Construct_Remote_Delegate>(libraryHandle, "LLMClient_Construct_Remote");
            LLMClient_Set_SSL_Internal = LibraryLoader.GetSymbolDelegate<LLMClient_Set_SSL_Delegate>(libraryHandle, "LLMClient_Set_SSL");
            LLMClient_Is_Server_Alive_Internal = LibraryLoader.GetSymbolDelegate<LLMClient_Is_Server_Alive_Delegate>(libraryHandle, "LLMClient_Is_Server_Alive");
            LLMAgent_Construct_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Construct_Delegate>(libraryHandle, "LLMAgent_Construct");
            LLMAgent_Set_System_Prompt_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_System_Prompt_Delegate>(libraryHandle, "LLMAgent_Set_System_Prompt");
            LLMAgent_Get_System_Prompt_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_System_Prompt_Delegate>(libraryHandle, "LLMAgent_Get_System_Prompt");
            LLMAgent_Set_Slot_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_Slot_Delegate>(libraryHandle, "LLMAgent_Set_Slot");
            LLMAgent_Get_Slot_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_Slot_Delegate>(libraryHandle, "LLMAgent_Get_Slot");
            LLM_Set_Completion_Parameters_Internal = LibraryLoader.GetSymbolDelegate<LLM_Set_Completion_Parameters_Delegate>(libraryHandle, "LLM_Set_Completion_Parameters");
            LLM_Get_Completion_Parameters_Internal = LibraryLoader.GetSymbolDelegate<LLM_Get_Completion_Parameters_Delegate>(libraryHandle, "LLM_Get_Completion_Parameters");
            LLM_Set_Grammar_Internal = LibraryLoader.GetSymbolDelegate<LLM_Set_Grammar_Delegate>(libraryHandle, "LLM_Set_Grammar");
            LLM_Get_Grammar_Internal = LibraryLoader.GetSymbolDelegate<LLM_Get_Grammar_Delegate>(libraryHandle, "LLM_Get_Grammar");
            LLMAgent_Chat_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Chat_Delegate>(libraryHandle, "LLMAgent_Chat");
            LLMAgent_Clear_History_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Clear_History_Delegate>(libraryHandle, "LLMAgent_Clear_History");
            LLMAgent_Get_History_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_History_Delegate>(libraryHandle, "LLMAgent_Get_History");
            LLMAgent_Set_History_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Set_History_Delegate>(libraryHandle, "LLMAgent_Set_History");
            LLMAgent_Add_User_Message_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Add_User_Message_Delegate>(libraryHandle, "LLMAgent_Add_User_Message");
            LLMAgent_Add_Assistant_Message_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Add_Assistant_Message_Delegate>(libraryHandle, "LLMAgent_Add_Assistant_Message");
            LLMAgent_Remove_Last_Message_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Remove_Last_Message_Delegate>(libraryHandle, "LLMAgent_Remove_Last_Message");
            LLMAgent_Save_History_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Save_History_Delegate>(libraryHandle, "LLMAgent_Save_History");
            LLMAgent_Load_History_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Load_History_Delegate>(libraryHandle, "LLMAgent_Load_History");
            LLMAgent_Get_History_Size_Internal = LibraryLoader.GetSymbolDelegate<LLMAgent_Get_History_Size_Delegate>(libraryHandle, "LLMAgent_Get_History_Size");
        }

        // Static functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Debug_Delegate(int debugLevel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Logging_Callback_Delegate(CharArrayCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LLM_Logging_Stop_Delegate();

        public LLM_Debug_Delegate LLM_Debug;
        public LLM_Logging_Callback_Delegate LLM_Logging_Callback;
        public LLM_Logging_Stop_Delegate LLM_Logging_Stop;

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
            foreach (IntPtr dependencyHandle in dependencyHandles) LibraryLoader.FreeLibrary(dependencyHandle);
            dependencyHandles.Clear();

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
