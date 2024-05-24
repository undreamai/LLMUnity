using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LLMUnity
{
    public class StreamWrapper
    {
        LLMLib llmlib;
        Callback<string> callback;
        IntPtr stringWrapper;
        string previousString = "";
        string previousCalledString = "";
        int previousBufferSize = 0;
        bool clearOnUpdate;

        public StreamWrapper(LLMLib llmlib, Callback<string> callback, bool clearOnUpdate = false)
        {
            this.llmlib = llmlib;
            this.callback = callback;
            this.clearOnUpdate = clearOnUpdate;
            stringWrapper = (llmlib?.StringWrapper_Construct()).GetValueOrDefault();
        }

        public string GetString(bool clear = false)
        {
            string result;
            int bufferSize = (llmlib?.StringWrapper_GetStringSize(stringWrapper)).GetValueOrDefault();
            if (bufferSize <= 1)
            {
                result = "";
            }
            else if (previousBufferSize != bufferSize)
            {
                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    llmlib?.StringWrapper_GetString(stringWrapper, buffer, bufferSize, clear);
                    result = Marshal.PtrToStringAnsi(buffer);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                previousString = result;
            }
            else
            {
                result = previousString;
            }
            previousBufferSize = bufferSize;
            return result;
        }

        public void Update()
        {
            if (stringWrapper == IntPtr.Zero) return;
            string result = GetString(clearOnUpdate);
            if (result != "" && previousCalledString != result)
            {
                callback(result);
                previousCalledString = result;
            }
        }

        public IntPtr GetStringWrapper()
        {
            return stringWrapper;
        }

        public void Destroy()
        {
            if (stringWrapper != IntPtr.Zero) llmlib?.StringWrapper_Delete(stringWrapper);
        }
    }

    public class LLMLib
    {
        public static string Version = "1.1.0";
        public static string URL = $"https://github.com/undreamai/LlamaLib/releases/download/release/undreamai-{Version}-llamacpp.zip";

        public LLMLib(string arch)
        {
            LoadArchitecture(arch);
        }

        public void LoadArchitecture(string arch)
        {
            if (LibraryFunctions.TryGetValue(arch, out var delegates))
            {
                (
                    LoggingDelegate Logging_fn,
                    StopLoggingDelegate StopLogging_fn,
                    LLM_ConstructDelegate LLM_Construct_fn,
                    LLM_DeleteDelegate LLM_Delete_fn,
                    LLM_StartServerDelegate LLM_StartServer_fn,
                    LLM_StopServerDelegate LLM_StopServer_fn,
                    LLM_StartDelegate LLM_Start_fn,
                    LLM_StopDelegate LLM_Stop_fn,
                    LLM_SetTemplateDelegate LLM_SetTemplate_fn,
                    LLM_TokenizeDelegate LLM_Tokenize_fn,
                    LLM_DetokenizeDelegate LLM_Detokenize_fn,
                    LLM_CompletionDelegate LLM_Completion_fn,
                    LLM_SlotDelegate LLM_Slot_fn,
                    LLM_CancelDelegate LLM_Cancel_fn,
                    LLM_StatusDelegate LLM_Status_fn,
                    StringWrapper_ConstructDelegate StringWrapper_Construct_fn,
                    StringWrapper_DeleteDelegate StringWrapper_Delete_fn,
                    StringWrapper_GetStringSizeDelegate StringWrapper_GetStringSize_fn,
                    StringWrapper_GetStringDelegate StringWrapper_GetString_fn
                ) = (
                    delegates[0] as LoggingDelegate,
                    delegates[1] as StopLoggingDelegate,
                    delegates[2] as LLM_ConstructDelegate,
                    delegates[3] as LLM_DeleteDelegate,
                    delegates[4] as LLM_StartServerDelegate,
                    delegates[5] as LLM_StopServerDelegate,
                    delegates[6] as LLM_StartDelegate,
                    delegates[7] as LLM_StopDelegate,
                    delegates[8] as LLM_SetTemplateDelegate,
                    delegates[9] as LLM_TokenizeDelegate,
                    delegates[10] as LLM_DetokenizeDelegate,
                    delegates[11] as LLM_CompletionDelegate,
                    delegates[12] as LLM_SlotDelegate,
                    delegates[13] as LLM_CancelDelegate,
                    delegates[14] as LLM_StatusDelegate,
                    delegates[15] as StringWrapper_ConstructDelegate,
                    delegates[16] as StringWrapper_DeleteDelegate,
                    delegates[17] as StringWrapper_GetStringSizeDelegate,
                    delegates[18] as StringWrapper_GetStringDelegate
                );

                Logging = Logging_fn;
                StopLogging = StopLogging_fn;
                LLM_Construct = LLM_Construct_fn;
                LLM_Delete = LLM_Delete_fn;
                LLM_StartServer = LLM_StartServer_fn;
                LLM_StopServer = LLM_StopServer_fn;
                LLM_Start = LLM_Start_fn;
                LLM_Stop = LLM_Stop_fn;
                LLM_SetTemplate = LLM_SetTemplate_fn;
                LLM_Tokenize = LLM_Tokenize_fn;
                LLM_Detokenize = LLM_Detokenize_fn;
                LLM_Completion = LLM_Completion_fn;
                LLM_Slot = LLM_Slot_fn;
                LLM_Cancel = LLM_Cancel_fn;
                LLM_Status = LLM_Status_fn;
                StringWrapper_Construct = StringWrapper_Construct_fn;
                StringWrapper_Delete = StringWrapper_Delete_fn;
                StringWrapper_GetStringSize = StringWrapper_GetStringSize_fn;
                StringWrapper_GetString = StringWrapper_GetString_fn;
            }
            else
            {
                Debug.LogError($"Architecture {arch} not supported");
            }
        }

        public static List<string> PossibleArchitectures(bool gpu = false)
        {
            List<string> architectures = new List<string>();
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                string os = (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) ? "windows" : "linux";
                if (gpu)
                {
                    architectures.Add($"undreamai_{os}-cuda-cu12.2.0");
                    architectures.Add($"undreamai_{os}-cuda-cu11.7.1");
                    architectures.Add($"undreamai_{os}-clblast");
                }
                try
                {
                    if (has_avx512()) architectures.Add($"undreamai_{os}-avx512");
                    if (has_avx2()) architectures.Add($"undreamai_{os}-avx2");
                    if (has_avx()) architectures.Add($"undreamai_{os}-avx");
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.GetType()}: {e.Message}");
                }
                architectures.Add($"undreamai_{os}-noavx");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                string arch = LLMUnitySetup.RunProcess("uname", "-m");
                if (arch.Contains("arm64") || arch.Contains("aarch64"))
                {
                    architectures.Add("undreamai_macos-arm64");
                }
                else
                {
                    if (!arch.Contains("x86_64")) Debug.LogWarning($"Unknown architecture of processor {arch}! Falling back to x86_64");
                    architectures.Add("undreamai_macos-x64");
                }
            }
            else
            {
                Debug.LogError("Unknown OS");
            }
            return architectures;
        }

        const string linux_archchecker_dll = "archchecker";
        [DllImport(linux_archchecker_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool has_avx();
        [DllImport(linux_archchecker_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool has_avx2();
        [DllImport(linux_archchecker_dll)]
        public static extern bool has_avx512();

        public string GetStringWrapperResult(IntPtr stringWrapper)
        {
            string result = "";
            int bufferSize = StringWrapper_GetStringSize(stringWrapper);
            if (bufferSize > 1)
            {
                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    StringWrapper_GetString(stringWrapper, buffer, bufferSize);
                    result = Marshal.PtrToStringAnsi(buffer);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            return result;
        }

        public delegate void LoggingDelegate(IntPtr stringWrapper);
        public delegate void StopLoggingDelegate();
        public delegate IntPtr LLM_ConstructDelegate(string command);
        public delegate void LLM_DeleteDelegate(IntPtr LLMObject);
        public delegate void LLM_StartServerDelegate(IntPtr LLMObject);
        public delegate void LLM_StopServerDelegate(IntPtr LLMObject);
        public delegate void LLM_StartDelegate(IntPtr LLMObject);
        public delegate void LLM_StopDelegate(IntPtr LLMObject);
        public delegate void LLM_SetTemplateDelegate(IntPtr LLMObject, string chatTemplate);
        public delegate void LLM_TokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_DetokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_CompletionDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_SlotDelegate(IntPtr LLMObject, string jsonData);
        public delegate void LLM_CancelDelegate(IntPtr LLMObject, int idSlot);
        public delegate int LLM_StatusDelegate(IntPtr LLMObject, IntPtr stringWrapper);
        public delegate IntPtr StringWrapper_ConstructDelegate();
        public delegate void StringWrapper_DeleteDelegate(IntPtr instance);
        public delegate int StringWrapper_GetStringSizeDelegate(IntPtr instance);
        public delegate void StringWrapper_GetStringDelegate(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);

        public LoggingDelegate Logging;
        public StopLoggingDelegate StopLogging;
        public LLM_ConstructDelegate LLM_Construct;
        public LLM_DeleteDelegate LLM_Delete;
        public LLM_StartServerDelegate LLM_StartServer;
        public LLM_StopServerDelegate LLM_StopServer;
        public LLM_StartDelegate LLM_Start;
        public LLM_StopDelegate LLM_Stop;
        public LLM_SetTemplateDelegate LLM_SetTemplate;
        public LLM_TokenizeDelegate LLM_Tokenize;
        public LLM_DetokenizeDelegate LLM_Detokenize;
        public LLM_CompletionDelegate LLM_Completion;
        public LLM_SlotDelegate LLM_Slot;
        public LLM_CancelDelegate LLM_Cancel;
        public LLM_StatusDelegate LLM_Status;
        public StringWrapper_ConstructDelegate StringWrapper_Construct;
        public StringWrapper_DeleteDelegate StringWrapper_Delete;
        public StringWrapper_GetStringSizeDelegate StringWrapper_GetStringSize;
        public StringWrapper_GetStringDelegate StringWrapper_GetString;

        const string linux_linux_avx_dll = "undreamai_linux-avx";
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX_LLM_Construct(string command);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_AVX_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_AVX_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_AVX_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_AVX_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_AVX_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX_StringWrapper_Construct();
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX_StopLogging();

        const string linux_linux_avx2_dll = "undreamai_linux-avx2";
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX2_LLM_Construct(string command);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX2_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_AVX2_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_AVX2_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_AVX2_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_AVX2_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_AVX2_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX2_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX2_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX2_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX2_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX2_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX2_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX2_StringWrapper_Construct();
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX2_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX2_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX2_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX2_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX2_StopLogging();

        const string linux_linux_avx512_dll = "undreamai_linux-avx512";
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX512_LLM_Construct(string command);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX512_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_AVX512_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_AVX512_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_AVX512_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_AVX512_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_AVX512_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX512_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX512_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX512_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX512_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX512_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX512_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX512_StringWrapper_Construct();
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX512_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX512_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX512_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX512_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX512_StopLogging();

        const string linux_linux_clblast_dll = "undreamai_linux-clblast";
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CLBLAST_LLM_Construct(string command);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CLBLAST_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_CLBLAST_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_CLBLAST_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_CLBLAST_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_CLBLAST_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_CLBLAST_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CLBLAST_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CLBLAST_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CLBLAST_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CLBLAST_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CLBLAST_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CLBLAST_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CLBLAST_StringWrapper_Construct();
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CLBLAST_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CLBLAST_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CLBLAST_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CLBLAST_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CLBLAST_StopLogging();

        const string linux_linux_cuda_cu11_7_1_dll = "undreamai_linux-cuda-cu11.7.1";
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CUDA_CU11_7_1_LLM_Construct(string command);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CUDA_CU11_7_1_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CUDA_CU11_7_1_StringWrapper_Construct();
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CUDA_CU11_7_1_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CUDA_CU11_7_1_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CUDA_CU11_7_1_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CUDA_CU11_7_1_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CUDA_CU11_7_1_StopLogging();

        const string linux_linux_cuda_cu12_2_0_dll = "undreamai_linux-cuda-cu12.2.0";
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CUDA_CU12_2_0_LLM_Construct(string command);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CUDA_CU12_2_0_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CUDA_CU12_2_0_StringWrapper_Construct();
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CUDA_CU12_2_0_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CUDA_CU12_2_0_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CUDA_CU12_2_0_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CUDA_CU12_2_0_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CUDA_CU12_2_0_StopLogging();

        const string linux_linux_noavx_dll = "undreamai_linux-noavx";
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_NOAVX_LLM_Construct(string command);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_NOAVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void LINUX_NOAVX_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void LINUX_NOAVX_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void LINUX_NOAVX_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void LINUX_NOAVX_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void LINUX_NOAVX_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_NOAVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_NOAVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_NOAVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_NOAVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_NOAVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_NOAVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_NOAVX_StringWrapper_Construct();
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_NOAVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_NOAVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_NOAVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_NOAVX_Logging(IntPtr stringWrapper);
        [DllImport(linux_linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_NOAVX_StopLogging();

        const string linux_macos_arm64_dll = "undreamai_macos-arm64";
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr MACOS_ARM64_LLM_Construct(string command);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void MACOS_ARM64_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void MACOS_ARM64_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void MACOS_ARM64_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void MACOS_ARM64_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void MACOS_ARM64_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void MACOS_ARM64_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void MACOS_ARM64_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void MACOS_ARM64_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void MACOS_ARM64_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void MACOS_ARM64_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void MACOS_ARM64_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int MACOS_ARM64_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr MACOS_ARM64_StringWrapper_Construct();
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void MACOS_ARM64_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int MACOS_ARM64_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void MACOS_ARM64_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void MACOS_ARM64_Logging(IntPtr stringWrapper);
        [DllImport(linux_macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void MACOS_ARM64_StopLogging();

        const string linux_macos_x64_dll = "undreamai_macos-x64";
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr MACOS_X64_LLM_Construct(string command);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void MACOS_X64_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void MACOS_X64_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void MACOS_X64_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void MACOS_X64_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void MACOS_X64_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void MACOS_X64_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void MACOS_X64_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void MACOS_X64_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void MACOS_X64_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void MACOS_X64_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void MACOS_X64_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int MACOS_X64_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr MACOS_X64_StringWrapper_Construct();
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void MACOS_X64_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int MACOS_X64_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void MACOS_X64_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void MACOS_X64_Logging(IntPtr stringWrapper);
        [DllImport(linux_macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void MACOS_X64_StopLogging();

        const string linux_windows_arm64_dll = "undreamai_windows-arm64";
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_ARM64_LLM_Construct(string command);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_ARM64_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_ARM64_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_ARM64_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_ARM64_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_ARM64_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_ARM64_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_ARM64_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_ARM64_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_ARM64_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_ARM64_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_ARM64_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_ARM64_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_ARM64_StringWrapper_Construct();
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_ARM64_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_ARM64_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_ARM64_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_ARM64_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_ARM64_StopLogging();

        const string linux_windows_avx_dll = "undreamai_windows-avx";
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX_LLM_Construct(string command);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_AVX_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_AVX_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_AVX_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_AVX_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_AVX_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX_StringWrapper_Construct();
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX_StopLogging();

        const string linux_windows_avx2_dll = "undreamai_windows-avx2";
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX2_LLM_Construct(string command);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX2_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_AVX2_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_AVX2_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_AVX2_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_AVX2_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_AVX2_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX2_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX2_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX2_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX2_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX2_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX2_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX2_StringWrapper_Construct();
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX2_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX2_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX2_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX2_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX2_StopLogging();

        const string linux_windows_avx512_dll = "undreamai_windows-avx512";
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX512_LLM_Construct(string command);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX512_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_AVX512_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_AVX512_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_AVX512_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_AVX512_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_AVX512_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX512_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX512_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX512_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX512_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX512_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX512_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX512_StringWrapper_Construct();
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX512_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX512_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX512_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX512_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX512_StopLogging();

        const string linux_windows_clblast_dll = "undreamai_windows-clblast";
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_LLM_Construct(string command);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CLBLAST_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_CLBLAST_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_CLBLAST_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_CLBLAST_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_CLBLAST_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_CLBLAST_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CLBLAST_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CLBLAST_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CLBLAST_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CLBLAST_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CLBLAST_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CLBLAST_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_StringWrapper_Construct();
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CLBLAST_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CLBLAST_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CLBLAST_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CLBLAST_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CLBLAST_StopLogging();

        const string linux_windows_cuda_cu11_7_1_dll = "undreamai_windows-cuda-cu11.7.1";
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_LLM_Construct(string command);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU11_7_1_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_StringWrapper_Construct();
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU11_7_1_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU11_7_1_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_StopLogging();

        const string linux_windows_cuda_cu12_2_0_dll = "undreamai_windows-cuda-cu12.2.0";
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_LLM_Construct(string command);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU12_2_0_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_StringWrapper_Construct();
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU12_2_0_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU12_2_0_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_StopLogging();

        const string linux_windows_noavx_dll = "undreamai_windows-noavx";
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_LLM_Construct(string command);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_NOAVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StartServer")]
        public static extern void WINDOWS_NOAVX_LLM_StartServer(IntPtr LLMObject);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_StopServer")]
        public static extern void WINDOWS_NOAVX_LLM_StopServer(IntPtr LLMObject);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Start")]
        public static extern void WINDOWS_NOAVX_LLM_Start(IntPtr LLMObject);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Stop")]
        public static extern void WINDOWS_NOAVX_LLM_Stop(IntPtr LLMObject);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_SetTemplate")]
        public static extern void WINDOWS_NOAVX_LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_NOAVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_NOAVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_NOAVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_NOAVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_NOAVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_NOAVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_StringWrapper_Construct();
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_NOAVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_NOAVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_NOAVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_NOAVX_Logging(IntPtr stringWrapper);
        [DllImport(linux_windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_NOAVX_StopLogging();


        static Dictionary<string, List<Delegate>> LibraryFunctions = new Dictionary<string, List<Delegate>>
        {
            { "undreamai_linux-avx", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX_Logging,
                  (StopLoggingDelegate)LINUX_AVX_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_AVX_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_AVX_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_AVX_LLM_Start,
                  (LLM_StopDelegate)LINUX_AVX_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_AVX_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_AVX_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_AVX_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_AVX_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_AVX_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_AVX_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_AVX_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_AVX_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_AVX_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_AVX_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_AVX_StringWrapper_GetString,
              }},
            { "undreamai_linux-avx2", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX2_Logging,
                  (StopLoggingDelegate)LINUX_AVX2_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX2_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX2_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_AVX2_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_AVX2_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_AVX2_LLM_Start,
                  (LLM_StopDelegate)LINUX_AVX2_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_AVX2_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_AVX2_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_AVX2_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_AVX2_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_AVX2_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_AVX2_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_AVX2_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_AVX2_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_AVX2_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_AVX2_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_AVX2_StringWrapper_GetString,
              }},
            { "undreamai_linux-avx512", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX512_Logging,
                  (StopLoggingDelegate)LINUX_AVX512_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX512_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX512_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_AVX512_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_AVX512_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_AVX512_LLM_Start,
                  (LLM_StopDelegate)LINUX_AVX512_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_AVX512_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_AVX512_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_AVX512_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_AVX512_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_AVX512_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_AVX512_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_AVX512_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_AVX512_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_AVX512_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_AVX512_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_AVX512_StringWrapper_GetString,
              }},
            { "undreamai_linux-clblast", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CLBLAST_Logging,
                  (StopLoggingDelegate)LINUX_CLBLAST_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CLBLAST_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CLBLAST_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_CLBLAST_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_CLBLAST_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_CLBLAST_LLM_Start,
                  (LLM_StopDelegate)LINUX_CLBLAST_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_CLBLAST_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_CLBLAST_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_CLBLAST_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_CLBLAST_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_CLBLAST_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_CLBLAST_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_CLBLAST_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_CLBLAST_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_CLBLAST_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_CLBLAST_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_CLBLAST_StringWrapper_GetString,
              }},
            { "undreamai_linux-cuda-cu11.7.1", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CUDA_CU11_7_1_Logging,
                  (StopLoggingDelegate)LINUX_CUDA_CU11_7_1_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CUDA_CU11_7_1_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CUDA_CU11_7_1_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_CUDA_CU11_7_1_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_CUDA_CU11_7_1_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_CUDA_CU11_7_1_LLM_Start,
                  (LLM_StopDelegate)LINUX_CUDA_CU11_7_1_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_CUDA_CU11_7_1_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_CUDA_CU11_7_1_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_CUDA_CU11_7_1_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_CUDA_CU11_7_1_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_CUDA_CU11_7_1_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_CUDA_CU11_7_1_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_CUDA_CU11_7_1_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_CUDA_CU11_7_1_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_CUDA_CU11_7_1_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_CUDA_CU11_7_1_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_CUDA_CU11_7_1_StringWrapper_GetString,
              }},
            { "undreamai_linux-cuda-cu12.2.0", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CUDA_CU12_2_0_Logging,
                  (StopLoggingDelegate)LINUX_CUDA_CU12_2_0_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CUDA_CU12_2_0_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CUDA_CU12_2_0_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_CUDA_CU12_2_0_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_CUDA_CU12_2_0_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_CUDA_CU12_2_0_LLM_Start,
                  (LLM_StopDelegate)LINUX_CUDA_CU12_2_0_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_CUDA_CU12_2_0_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_CUDA_CU12_2_0_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_CUDA_CU12_2_0_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_CUDA_CU12_2_0_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_CUDA_CU12_2_0_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_CUDA_CU12_2_0_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_CUDA_CU12_2_0_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_CUDA_CU12_2_0_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_CUDA_CU12_2_0_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_CUDA_CU12_2_0_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_CUDA_CU12_2_0_StringWrapper_GetString,
              }},
            { "undreamai_linux-noavx", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_NOAVX_Logging,
                  (StopLoggingDelegate)LINUX_NOAVX_StopLogging,
                  (LLM_ConstructDelegate)LINUX_NOAVX_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_NOAVX_LLM_Delete,
                  (LLM_StartServerDelegate)LINUX_NOAVX_LLM_StartServer,
                  (LLM_StopServerDelegate)LINUX_NOAVX_LLM_StopServer,
                  (LLM_StartDelegate)LINUX_NOAVX_LLM_Start,
                  (LLM_StopDelegate)LINUX_NOAVX_LLM_Stop,
                  (LLM_SetTemplateDelegate)LINUX_NOAVX_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)LINUX_NOAVX_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_NOAVX_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_NOAVX_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_NOAVX_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_NOAVX_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_NOAVX_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_NOAVX_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_NOAVX_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_NOAVX_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_NOAVX_StringWrapper_GetString,
              }},
            { "undreamai_macos-arm64", new List<Delegate>()
              {
                  (LoggingDelegate)MACOS_ARM64_Logging,
                  (StopLoggingDelegate)MACOS_ARM64_StopLogging,
                  (LLM_ConstructDelegate)MACOS_ARM64_LLM_Construct,
                  (LLM_DeleteDelegate)MACOS_ARM64_LLM_Delete,
                  (LLM_StartServerDelegate)MACOS_ARM64_LLM_StartServer,
                  (LLM_StopServerDelegate)MACOS_ARM64_LLM_StopServer,
                  (LLM_StartDelegate)MACOS_ARM64_LLM_Start,
                  (LLM_StopDelegate)MACOS_ARM64_LLM_Stop,
                  (LLM_SetTemplateDelegate)MACOS_ARM64_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)MACOS_ARM64_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)MACOS_ARM64_LLM_Detokenize,
                  (LLM_CompletionDelegate)MACOS_ARM64_LLM_Completion,
                  (LLM_SlotDelegate)MACOS_ARM64_LLM_Slot,
                  (LLM_CancelDelegate)MACOS_ARM64_LLM_Cancel,
                  (LLM_StatusDelegate)MACOS_ARM64_LLM_Status,
                  (StringWrapper_ConstructDelegate)MACOS_ARM64_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)MACOS_ARM64_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)MACOS_ARM64_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)MACOS_ARM64_StringWrapper_GetString,
              }},
            { "undreamai_macos-x64", new List<Delegate>()
              {
                  (LoggingDelegate)MACOS_X64_Logging,
                  (StopLoggingDelegate)MACOS_X64_StopLogging,
                  (LLM_ConstructDelegate)MACOS_X64_LLM_Construct,
                  (LLM_DeleteDelegate)MACOS_X64_LLM_Delete,
                  (LLM_StartServerDelegate)MACOS_X64_LLM_StartServer,
                  (LLM_StopServerDelegate)MACOS_X64_LLM_StopServer,
                  (LLM_StartDelegate)MACOS_X64_LLM_Start,
                  (LLM_StopDelegate)MACOS_X64_LLM_Stop,
                  (LLM_SetTemplateDelegate)MACOS_X64_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)MACOS_X64_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)MACOS_X64_LLM_Detokenize,
                  (LLM_CompletionDelegate)MACOS_X64_LLM_Completion,
                  (LLM_SlotDelegate)MACOS_X64_LLM_Slot,
                  (LLM_CancelDelegate)MACOS_X64_LLM_Cancel,
                  (LLM_StatusDelegate)MACOS_X64_LLM_Status,
                  (StringWrapper_ConstructDelegate)MACOS_X64_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)MACOS_X64_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)MACOS_X64_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)MACOS_X64_StringWrapper_GetString,
              }},
            { "undreamai_windows-arm64", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_ARM64_Logging,
                  (StopLoggingDelegate)WINDOWS_ARM64_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_ARM64_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_ARM64_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_ARM64_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_ARM64_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_ARM64_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_ARM64_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_ARM64_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_ARM64_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_ARM64_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_ARM64_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_ARM64_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_ARM64_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_ARM64_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_ARM64_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_ARM64_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_ARM64_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_ARM64_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_AVX_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_AVX_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_AVX_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_AVX_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_AVX_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_AVX_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx2", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX2_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX2_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX2_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX2_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_AVX2_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_AVX2_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_AVX2_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_AVX2_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_AVX2_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_AVX2_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX2_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX2_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX2_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX2_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX2_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX2_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX2_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX2_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX2_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx512", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX512_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX512_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX512_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX512_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_AVX512_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_AVX512_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_AVX512_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_AVX512_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_AVX512_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_AVX512_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX512_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX512_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX512_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX512_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX512_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX512_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX512_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX512_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX512_StringWrapper_GetString,
              }},
            { "undreamai_windows-clblast", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CLBLAST_Logging,
                  (StopLoggingDelegate)WINDOWS_CLBLAST_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CLBLAST_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CLBLAST_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_CLBLAST_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_CLBLAST_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_CLBLAST_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_CLBLAST_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_CLBLAST_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_CLBLAST_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CLBLAST_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CLBLAST_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CLBLAST_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CLBLAST_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CLBLAST_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CLBLAST_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CLBLAST_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CLBLAST_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CLBLAST_StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu11.7.1", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU11_7_1_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU11_7_1_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_CUDA_CU11_7_1_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_CUDA_CU11_7_1_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_CUDA_CU11_7_1_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU11_7_1_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU11_7_1_StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu12.2.0", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU12_2_0_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU12_2_0_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_CUDA_CU12_2_0_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_CUDA_CU12_2_0_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_CUDA_CU12_2_0_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU12_2_0_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU12_2_0_StringWrapper_GetString,
              }},
            { "undreamai_windows-noavx", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_NOAVX_Logging,
                  (StopLoggingDelegate)WINDOWS_NOAVX_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_NOAVX_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_NOAVX_LLM_Delete,
                  (LLM_StartServerDelegate)WINDOWS_NOAVX_LLM_StartServer,
                  (LLM_StopServerDelegate)WINDOWS_NOAVX_LLM_StopServer,
                  (LLM_StartDelegate)WINDOWS_NOAVX_LLM_Start,
                  (LLM_StopDelegate)WINDOWS_NOAVX_LLM_Stop,
                  (LLM_SetTemplateDelegate)WINDOWS_NOAVX_LLM_SetTemplate,
                  (LLM_TokenizeDelegate)WINDOWS_NOAVX_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_NOAVX_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_NOAVX_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_NOAVX_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_NOAVX_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_NOAVX_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_NOAVX_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_NOAVX_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_NOAVX_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_NOAVX_StringWrapper_GetString,
              }}
        };
    }
}
