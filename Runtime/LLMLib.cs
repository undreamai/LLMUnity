using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LLMUnity
{
    public class LLMLib
    {
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
                    delegates[4] as LLM_TokenizeDelegate,
                    delegates[5] as LLM_DetokenizeDelegate,
                    delegates[6] as LLM_CompletionDelegate,
                    delegates[7] as LLM_SlotDelegate,
                    delegates[8] as LLM_CancelDelegate,
                    delegates[9] as LLM_StatusDelegate,
                    delegates[10] as StringWrapper_ConstructDelegate,
                    delegates[11] as StringWrapper_DeleteDelegate,
                    delegates[12] as StringWrapper_GetStringSizeDelegate,
                    delegates[13] as StringWrapper_GetStringDelegate
                );

                Logging = Logging_fn;
                StopLogging = StopLogging_fn;
                LLM_Construct = LLM_Construct_fn;
                LLM_Delete = LLM_Delete_fn;
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
            string result;
            int bufferSize = StringWrapper_GetStringSize(stringWrapper);
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
            return result;
        }

        public delegate void LoggingDelegate(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        public delegate void StopLoggingDelegate();
        public delegate IntPtr LLM_ConstructDelegate(string command, bool server_mode = false);
        public delegate void LLM_DeleteDelegate(IntPtr LLMObject);
        public delegate void LLM_TokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_DetokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_CompletionDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        public delegate void LLM_SlotDelegate(IntPtr LLMObject, string jsonData);
        public delegate void LLM_CancelDelegate(IntPtr LLMObject, int idSlot);
        public delegate int LLM_StatusDelegate(IntPtr LLMObject, IntPtr stringWrapper);
        public delegate IntPtr StringWrapper_ConstructDelegate();
        public delegate void StringWrapper_DeleteDelegate(IntPtr instance);
        public delegate int StringWrapper_GetStringSizeDelegate(IntPtr instance);
        public delegate void StringWrapper_GetStringDelegate(IntPtr instance, IntPtr buffer, int bufferSize);

        public LoggingDelegate Logging;
        public StopLoggingDelegate StopLogging;
        public LLM_ConstructDelegate LLM_Construct;
        public LLM_DeleteDelegate LLM_Delete;
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

        const string linux_avx_dll = "undreamai_linux-avx";
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX_StringWrapper_Construct();
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX_StopLogging();

        const string linux_avx2_dll = "undreamai_linux-avx2";
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX2_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX2_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX2_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX2_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX2_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX2_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX2_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX2_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX2_StringWrapper_Construct();
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX2_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX2_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX2_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX2_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX2_StopLogging();

        const string linux_avx2_so_dll = "undreamai_linux-avx2.so";
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX2_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX2_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX2_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX2_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX2_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX2_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX2_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX2_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX2_SO_StringWrapper_Construct();
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX2_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX2_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX2_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX2_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx2_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX2_SO_StopLogging();

        const string linux_avx512_dll = "undreamai_linux-avx512";
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX512_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX512_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX512_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX512_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX512_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX512_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX512_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX512_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX512_StringWrapper_Construct();
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX512_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX512_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX512_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX512_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX512_StopLogging();

        const string linux_avx512_so_dll = "undreamai_linux-avx512.so";
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX512_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX512_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX512_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX512_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX512_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX512_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX512_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX512_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX512_SO_StringWrapper_Construct();
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX512_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX512_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX512_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX512_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx512_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX512_SO_StopLogging();

        const string linux_avx_so_dll = "undreamai_linux-avx.so";
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX_SO_StringWrapper_Construct();
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_AVX_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_avx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_AVX_SO_StopLogging();

        const string linux_clblast_dll = "undreamai_linux-clblast";
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CLBLAST_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CLBLAST_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CLBLAST_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CLBLAST_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CLBLAST_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CLBLAST_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CLBLAST_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CLBLAST_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CLBLAST_StringWrapper_Construct();
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CLBLAST_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CLBLAST_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CLBLAST_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CLBLAST_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CLBLAST_StopLogging();

        const string linux_clblast_so_dll = "undreamai_linux-clblast.so";
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CLBLAST_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CLBLAST_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CLBLAST_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CLBLAST_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CLBLAST_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CLBLAST_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CLBLAST_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CLBLAST_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CLBLAST_SO_StringWrapper_Construct();
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CLBLAST_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CLBLAST_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CLBLAST_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CLBLAST_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_clblast_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CLBLAST_SO_StopLogging();

        const string linux_cuda_cu11_7_1_dll = "undreamai_linux-cuda-cu11.7.1";
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CUDA_CU11_7_1_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CUDA_CU11_7_1_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CUDA_CU11_7_1_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CUDA_CU11_7_1_StringWrapper_Construct();
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CUDA_CU11_7_1_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CUDA_CU11_7_1_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CUDA_CU11_7_1_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CUDA_CU11_7_1_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CUDA_CU11_7_1_StopLogging();

        const string linux_cuda_cu11_7_1_so_dll = "undreamai_linux-cuda-cu11.7.1.so";
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CUDA_CU11_7_1_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CUDA_CU11_7_1_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CUDA_CU11_7_1_SO_StringWrapper_Construct();
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CUDA_CU11_7_1_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu11_7_1_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CUDA_CU11_7_1_SO_StopLogging();

        const string linux_cuda_cu12_2_0_dll = "undreamai_linux-cuda-cu12.2.0";
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CUDA_CU12_2_0_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CUDA_CU12_2_0_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CUDA_CU12_2_0_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CUDA_CU12_2_0_StringWrapper_Construct();
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CUDA_CU12_2_0_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CUDA_CU12_2_0_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CUDA_CU12_2_0_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CUDA_CU12_2_0_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CUDA_CU12_2_0_StopLogging();

        const string linux_cuda_cu12_2_0_so_dll = "undreamai_linux-cuda-cu12.2.0.so";
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_CUDA_CU12_2_0_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_CUDA_CU12_2_0_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_CUDA_CU12_2_0_SO_StringWrapper_Construct();
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_CUDA_CU12_2_0_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_cuda_cu12_2_0_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_CUDA_CU12_2_0_SO_StopLogging();

        const string linux_noavx_dll = "undreamai_linux-noavx";
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_NOAVX_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_NOAVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_NOAVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_NOAVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_NOAVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_NOAVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_NOAVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_NOAVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_NOAVX_StringWrapper_Construct();
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_NOAVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_NOAVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_NOAVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_NOAVX_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_NOAVX_StopLogging();

        const string linux_noavx_so_dll = "undreamai_linux-noavx.so";
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_NOAVX_SO_LLM_Construct(string command, bool server_mode = false);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_NOAVX_SO_LLM_Delete(IntPtr LLMObject);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_NOAVX_SO_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_NOAVX_SO_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_NOAVX_SO_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_NOAVX_SO_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_NOAVX_SO_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_NOAVX_SO_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_NOAVX_SO_StringWrapper_Construct();
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_NOAVX_SO_StringWrapper_Delete(IntPtr instance);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_NOAVX_SO_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_NOAVX_SO_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void LINUX_NOAVX_SO_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(linux_noavx_so_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void LINUX_NOAVX_SO_StopLogging();

        const string macos_arm64_dll = "undreamai_macos-arm64";
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr MACOS_ARM64_LLM_Construct(string command, bool server_mode = false);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void MACOS_ARM64_LLM_Delete(IntPtr LLMObject);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void MACOS_ARM64_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void MACOS_ARM64_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void MACOS_ARM64_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void MACOS_ARM64_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void MACOS_ARM64_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int MACOS_ARM64_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr MACOS_ARM64_StringWrapper_Construct();
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void MACOS_ARM64_StringWrapper_Delete(IntPtr instance);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int MACOS_ARM64_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void MACOS_ARM64_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void MACOS_ARM64_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void MACOS_ARM64_StopLogging();

        const string macos_arm64_dy_dll = "undreamai_macos-arm64.dy";
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr MACOS_ARM64_DY_LLM_Construct(string command, bool server_mode = false);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void MACOS_ARM64_DY_LLM_Delete(IntPtr LLMObject);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void MACOS_ARM64_DY_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void MACOS_ARM64_DY_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void MACOS_ARM64_DY_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void MACOS_ARM64_DY_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void MACOS_ARM64_DY_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int MACOS_ARM64_DY_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr MACOS_ARM64_DY_StringWrapper_Construct();
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void MACOS_ARM64_DY_StringWrapper_Delete(IntPtr instance);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int MACOS_ARM64_DY_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void MACOS_ARM64_DY_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void MACOS_ARM64_DY_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_arm64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void MACOS_ARM64_DY_StopLogging();

        const string macos_x64_dll = "undreamai_macos-x64";
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr MACOS_X64_LLM_Construct(string command, bool server_mode = false);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void MACOS_X64_LLM_Delete(IntPtr LLMObject);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void MACOS_X64_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void MACOS_X64_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void MACOS_X64_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void MACOS_X64_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void MACOS_X64_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int MACOS_X64_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr MACOS_X64_StringWrapper_Construct();
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void MACOS_X64_StringWrapper_Delete(IntPtr instance);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int MACOS_X64_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void MACOS_X64_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void MACOS_X64_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_x64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void MACOS_X64_StopLogging();

        const string macos_x64_dy_dll = "undreamai_macos-x64.dy";
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr MACOS_X64_DY_LLM_Construct(string command, bool server_mode = false);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void MACOS_X64_DY_LLM_Delete(IntPtr LLMObject);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void MACOS_X64_DY_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void MACOS_X64_DY_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void MACOS_X64_DY_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void MACOS_X64_DY_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void MACOS_X64_DY_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int MACOS_X64_DY_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr MACOS_X64_DY_StringWrapper_Construct();
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void MACOS_X64_DY_StringWrapper_Delete(IntPtr instance);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int MACOS_X64_DY_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void MACOS_X64_DY_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void MACOS_X64_DY_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(macos_x64_dy_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void MACOS_X64_DY_StopLogging();

        const string windows_arm64_dll = "undreamai_windows-arm64";
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_ARM64_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_ARM64_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_ARM64_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_ARM64_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_ARM64_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_ARM64_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_ARM64_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_ARM64_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_ARM64_StringWrapper_Construct();
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_ARM64_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_ARM64_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_ARM64_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_ARM64_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_ARM64_StopLogging();

        const string windows_arm64_dll_dll = "undreamai_windows-arm64.dll";
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_ARM64_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_ARM64_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_ARM64_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_ARM64_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_ARM64_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_ARM64_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_ARM64_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_ARM64_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_ARM64_DLL_StringWrapper_Construct();
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_ARM64_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_ARM64_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_ARM64_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_ARM64_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_ARM64_DLL_StopLogging();

        const string windows_arm64_exp_dll = "undreamai_windows-arm64.exp";
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_ARM64_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_ARM64_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_ARM64_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_ARM64_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_ARM64_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_ARM64_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_ARM64_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_ARM64_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_ARM64_EXP_StringWrapper_Construct();
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_ARM64_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_ARM64_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_ARM64_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_ARM64_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_ARM64_EXP_StopLogging();

        const string windows_arm64__dll = "undreamai_windows-arm64.";
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_ARM64__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_ARM64__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_ARM64__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_ARM64__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_ARM64__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_ARM64__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_ARM64__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_ARM64__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_ARM64__StringWrapper_Construct();
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_ARM64__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_ARM64__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_ARM64__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_ARM64__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_arm64__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_ARM64__StopLogging();

        const string windows_avx_dll = "undreamai_windows-avx";
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX_StringWrapper_Construct();
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX_StopLogging();

        const string windows_avx2_dll = "undreamai_windows-avx2";
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX2_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX2_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX2_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX2_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX2_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX2_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX2_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX2_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX2_StringWrapper_Construct();
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX2_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX2_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX2_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX2_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX2_StopLogging();

        const string windows_avx2_dll_dll = "undreamai_windows-avx2.dll";
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX2_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX2_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX2_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX2_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX2_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX2_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX2_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX2_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX2_DLL_StringWrapper_Construct();
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX2_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX2_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX2_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX2_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX2_DLL_StopLogging();

        const string windows_avx2_exp_dll = "undreamai_windows-avx2.exp";
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX2_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX2_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX2_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX2_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX2_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX2_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX2_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX2_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX2_EXP_StringWrapper_Construct();
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX2_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX2_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX2_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX2_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX2_EXP_StopLogging();

        const string windows_avx2__dll = "undreamai_windows-avx2.";
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX2__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX2__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX2__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX2__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX2__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX2__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX2__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX2__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX2__StringWrapper_Construct();
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX2__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX2__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX2__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX2__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx2__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX2__StopLogging();

        const string windows_avx512_dll = "undreamai_windows-avx512";
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX512_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX512_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX512_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX512_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX512_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX512_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX512_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX512_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX512_StringWrapper_Construct();
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX512_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX512_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX512_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX512_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX512_StopLogging();

        const string windows_avx512_dll_dll = "undreamai_windows-avx512.dll";
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX512_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX512_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX512_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX512_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX512_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX512_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX512_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX512_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX512_DLL_StringWrapper_Construct();
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX512_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX512_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX512_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX512_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX512_DLL_StopLogging();

        const string windows_avx512_exp_dll = "undreamai_windows-avx512.exp";
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX512_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX512_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX512_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX512_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX512_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX512_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX512_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX512_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX512_EXP_StringWrapper_Construct();
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX512_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX512_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX512_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX512_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX512_EXP_StopLogging();

        const string windows_avx512__dll = "undreamai_windows-avx512.";
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX512__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX512__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX512__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX512__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX512__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX512__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX512__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX512__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX512__StringWrapper_Construct();
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX512__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX512__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX512__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX512__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx512__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX512__StopLogging();

        const string windows_avx_dll_dll = "undreamai_windows-avx.dll";
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX_DLL_StringWrapper_Construct();
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX_DLL_StopLogging();

        const string windows_avx_exp_dll = "undreamai_windows-avx.exp";
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX_EXP_StringWrapper_Construct();
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX_EXP_StopLogging();

        const string windows_avx__dll = "undreamai_windows-avx.";
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_AVX__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_AVX__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_AVX__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_AVX__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_AVX__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_AVX__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_AVX__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_AVX__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_AVX__StringWrapper_Construct();
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_AVX__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_AVX__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_AVX__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_AVX__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_avx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_AVX__StopLogging();

        const string windows_clblast_dll = "undreamai_windows-clblast";
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CLBLAST_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CLBLAST_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CLBLAST_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CLBLAST_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CLBLAST_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CLBLAST_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CLBLAST_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_StringWrapper_Construct();
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CLBLAST_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CLBLAST_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CLBLAST_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CLBLAST_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CLBLAST_StopLogging();

        const string windows_clblast_dll_dll = "undreamai_windows-clblast.dll";
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CLBLAST_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CLBLAST_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CLBLAST_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CLBLAST_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CLBLAST_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CLBLAST_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CLBLAST_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_DLL_StringWrapper_Construct();
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CLBLAST_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CLBLAST_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CLBLAST_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CLBLAST_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CLBLAST_DLL_StopLogging();

        const string windows_clblast_exp_dll = "undreamai_windows-clblast.exp";
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CLBLAST_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CLBLAST_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CLBLAST_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CLBLAST_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CLBLAST_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CLBLAST_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CLBLAST_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST_EXP_StringWrapper_Construct();
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CLBLAST_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CLBLAST_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CLBLAST_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CLBLAST_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CLBLAST_EXP_StopLogging();

        const string windows_clblast__dll = "undreamai_windows-clblast.";
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CLBLAST__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CLBLAST__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CLBLAST__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CLBLAST__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CLBLAST__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CLBLAST__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CLBLAST__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CLBLAST__StringWrapper_Construct();
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CLBLAST__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CLBLAST__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CLBLAST__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CLBLAST__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_clblast__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CLBLAST__StopLogging();

        const string windows_cuda_cu11_7_1_dll = "undreamai_windows-cuda-cu11.7.1";
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU11_7_1_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU11_7_1_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_StringWrapper_Construct();
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU11_7_1_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU11_7_1_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_StopLogging();

        const string windows_cuda_cu11_7_1_dll_dll = "undreamai_windows-cuda-cu11.7.1.dll";
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU11_7_1_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_Construct();
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_DLL_StopLogging();

        const string windows_cuda_cu11_7_1_exp_dll = "undreamai_windows-cuda-cu11.7.1.exp";
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU11_7_1_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_Construct();
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU11_7_1_EXP_StopLogging();

        const string windows_cuda_cu11_7_1__dll = "undreamai_windows-cuda-cu11.7.1.";
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU11_7_1__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU11_7_1__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU11_7_1__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU11_7_1__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU11_7_1__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU11_7_1__StringWrapper_Construct();
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU11_7_1__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU11_7_1__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU11_7_1__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU11_7_1__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu11_7_1__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU11_7_1__StopLogging();

        const string windows_cuda_cu12_2_0_dll = "undreamai_windows-cuda-cu12.2.0";
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU12_2_0_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU12_2_0_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_StringWrapper_Construct();
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU12_2_0_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU12_2_0_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_StopLogging();

        const string windows_cuda_cu12_2_0_dll_dll = "undreamai_windows-cuda-cu12.2.0.dll";
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU12_2_0_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_Construct();
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_DLL_StopLogging();

        const string windows_cuda_cu12_2_0_exp_dll = "undreamai_windows-cuda-cu12.2.0.exp";
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU12_2_0_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_Construct();
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU12_2_0_EXP_StopLogging();

        const string windows_cuda_cu12_2_0__dll = "undreamai_windows-cuda-cu12.2.0.";
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_CUDA_CU12_2_0__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_CUDA_CU12_2_0__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_CUDA_CU12_2_0__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_CUDA_CU12_2_0__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_CUDA_CU12_2_0__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_CUDA_CU12_2_0__StringWrapper_Construct();
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_CUDA_CU12_2_0__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_CUDA_CU12_2_0__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_CUDA_CU12_2_0__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_CUDA_CU12_2_0__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_cuda_cu12_2_0__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_CUDA_CU12_2_0__StopLogging();

        const string windows_noavx_dll = "undreamai_windows-noavx";
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_NOAVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_NOAVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_NOAVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_NOAVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_NOAVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_NOAVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_NOAVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_StringWrapper_Construct();
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_NOAVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_NOAVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_NOAVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_NOAVX_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_NOAVX_StopLogging();

        const string windows_noavx_dll_dll = "undreamai_windows-noavx.dll";
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_DLL_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_NOAVX_DLL_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_NOAVX_DLL_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_NOAVX_DLL_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_NOAVX_DLL_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_NOAVX_DLL_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_NOAVX_DLL_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_NOAVX_DLL_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_DLL_StringWrapper_Construct();
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_NOAVX_DLL_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_NOAVX_DLL_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_NOAVX_DLL_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_NOAVX_DLL_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx_dll_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_NOAVX_DLL_StopLogging();

        const string windows_noavx_exp_dll = "undreamai_windows-noavx.exp";
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_EXP_LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_NOAVX_EXP_LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_NOAVX_EXP_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_NOAVX_EXP_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_NOAVX_EXP_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_NOAVX_EXP_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_NOAVX_EXP_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_NOAVX_EXP_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_NOAVX_EXP_StringWrapper_Construct();
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_NOAVX_EXP_StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_NOAVX_EXP_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_NOAVX_EXP_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_NOAVX_EXP_Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx_exp_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_NOAVX_EXP_StopLogging();

        const string windows_noavx__dll = "undreamai_windows-noavx.";
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr WINDOWS_NOAVX__LLM_Construct(string command, bool server_mode = false);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void WINDOWS_NOAVX__LLM_Delete(IntPtr LLMObject);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void WINDOWS_NOAVX__LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void WINDOWS_NOAVX__LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void WINDOWS_NOAVX__LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void WINDOWS_NOAVX__LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void WINDOWS_NOAVX__LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int WINDOWS_NOAVX__LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr WINDOWS_NOAVX__StringWrapper_Construct();
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void WINDOWS_NOAVX__StringWrapper_Delete(IntPtr instance);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int WINDOWS_NOAVX__StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void WINDOWS_NOAVX__StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Logging")]
        public static extern void WINDOWS_NOAVX__Logging(IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(windows_noavx__dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopLogging")]
        public static extern void WINDOWS_NOAVX__StopLogging();

        static Dictionary<string, List<Delegate>> LibraryFunctions = new Dictionary<string, List<Delegate>>
        {
            { "undreamai_linux-avx", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX_Logging,
                  (StopLoggingDelegate)LINUX_AVX_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX_LLM_Delete,
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
            { "undreamai_linux-avx2.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX2_SO_Logging,
                  (StopLoggingDelegate)LINUX_AVX2_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX2_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX2_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_AVX2_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_AVX2_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_AVX2_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_AVX2_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_AVX2_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_AVX2_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_AVX2_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_AVX2_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_AVX2_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_AVX2_SO_StringWrapper_GetString,
              }},
            { "undreamai_linux-avx512", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX512_Logging,
                  (StopLoggingDelegate)LINUX_AVX512_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX512_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX512_LLM_Delete,
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
            { "undreamai_linux-avx512.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX512_SO_Logging,
                  (StopLoggingDelegate)LINUX_AVX512_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX512_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX512_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_AVX512_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_AVX512_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_AVX512_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_AVX512_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_AVX512_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_AVX512_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_AVX512_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_AVX512_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_AVX512_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_AVX512_SO_StringWrapper_GetString,
              }},
            { "undreamai_linux-avx.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_AVX_SO_Logging,
                  (StopLoggingDelegate)LINUX_AVX_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_AVX_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_AVX_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_AVX_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_AVX_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_AVX_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_AVX_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_AVX_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_AVX_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_AVX_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_AVX_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_AVX_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_AVX_SO_StringWrapper_GetString,
              }},
            { "undreamai_linux-clblast", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CLBLAST_Logging,
                  (StopLoggingDelegate)LINUX_CLBLAST_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CLBLAST_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CLBLAST_LLM_Delete,
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
            { "undreamai_linux-clblast.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CLBLAST_SO_Logging,
                  (StopLoggingDelegate)LINUX_CLBLAST_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CLBLAST_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CLBLAST_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_CLBLAST_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_CLBLAST_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_CLBLAST_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_CLBLAST_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_CLBLAST_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_CLBLAST_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_CLBLAST_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_CLBLAST_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_CLBLAST_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_CLBLAST_SO_StringWrapper_GetString,
              }},
            { "undreamai_linux-cuda-cu11.7.1", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CUDA_CU11_7_1_Logging,
                  (StopLoggingDelegate)LINUX_CUDA_CU11_7_1_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CUDA_CU11_7_1_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CUDA_CU11_7_1_LLM_Delete,
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
            { "undreamai_linux-cuda-cu11.7.1.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CUDA_CU11_7_1_SO_Logging,
                  (StopLoggingDelegate)LINUX_CUDA_CU11_7_1_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_CUDA_CU11_7_1_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_CUDA_CU11_7_1_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_CUDA_CU11_7_1_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_CUDA_CU11_7_1_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_CUDA_CU11_7_1_SO_StringWrapper_GetString,
              }},
            { "undreamai_linux-cuda-cu12.2.0", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CUDA_CU12_2_0_Logging,
                  (StopLoggingDelegate)LINUX_CUDA_CU12_2_0_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CUDA_CU12_2_0_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CUDA_CU12_2_0_LLM_Delete,
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
            { "undreamai_linux-cuda-cu12.2.0.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_CUDA_CU12_2_0_SO_Logging,
                  (StopLoggingDelegate)LINUX_CUDA_CU12_2_0_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_CUDA_CU12_2_0_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_CUDA_CU12_2_0_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_CUDA_CU12_2_0_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_CUDA_CU12_2_0_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_CUDA_CU12_2_0_SO_StringWrapper_GetString,
              }},
            { "undreamai_linux-noavx", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_NOAVX_Logging,
                  (StopLoggingDelegate)LINUX_NOAVX_StopLogging,
                  (LLM_ConstructDelegate)LINUX_NOAVX_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_NOAVX_LLM_Delete,
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
            { "undreamai_linux-noavx.so", new List<Delegate>()
              {
                  (LoggingDelegate)LINUX_NOAVX_SO_Logging,
                  (StopLoggingDelegate)LINUX_NOAVX_SO_StopLogging,
                  (LLM_ConstructDelegate)LINUX_NOAVX_SO_LLM_Construct,
                  (LLM_DeleteDelegate)LINUX_NOAVX_SO_LLM_Delete,
                  (LLM_TokenizeDelegate)LINUX_NOAVX_SO_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)LINUX_NOAVX_SO_LLM_Detokenize,
                  (LLM_CompletionDelegate)LINUX_NOAVX_SO_LLM_Completion,
                  (LLM_SlotDelegate)LINUX_NOAVX_SO_LLM_Slot,
                  (LLM_CancelDelegate)LINUX_NOAVX_SO_LLM_Cancel,
                  (LLM_StatusDelegate)LINUX_NOAVX_SO_LLM_Status,
                  (StringWrapper_ConstructDelegate)LINUX_NOAVX_SO_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)LINUX_NOAVX_SO_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)LINUX_NOAVX_SO_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)LINUX_NOAVX_SO_StringWrapper_GetString,
              }},
            { "undreamai_macos-arm64", new List<Delegate>()
              {
                  (LoggingDelegate)MACOS_ARM64_Logging,
                  (StopLoggingDelegate)MACOS_ARM64_StopLogging,
                  (LLM_ConstructDelegate)MACOS_ARM64_LLM_Construct,
                  (LLM_DeleteDelegate)MACOS_ARM64_LLM_Delete,
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
            { "undreamai_macos-arm64.dy", new List<Delegate>()
              {
                  (LoggingDelegate)MACOS_ARM64_DY_Logging,
                  (StopLoggingDelegate)MACOS_ARM64_DY_StopLogging,
                  (LLM_ConstructDelegate)MACOS_ARM64_DY_LLM_Construct,
                  (LLM_DeleteDelegate)MACOS_ARM64_DY_LLM_Delete,
                  (LLM_TokenizeDelegate)MACOS_ARM64_DY_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)MACOS_ARM64_DY_LLM_Detokenize,
                  (LLM_CompletionDelegate)MACOS_ARM64_DY_LLM_Completion,
                  (LLM_SlotDelegate)MACOS_ARM64_DY_LLM_Slot,
                  (LLM_CancelDelegate)MACOS_ARM64_DY_LLM_Cancel,
                  (LLM_StatusDelegate)MACOS_ARM64_DY_LLM_Status,
                  (StringWrapper_ConstructDelegate)MACOS_ARM64_DY_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)MACOS_ARM64_DY_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)MACOS_ARM64_DY_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)MACOS_ARM64_DY_StringWrapper_GetString,
              }},
            { "undreamai_macos-x64", new List<Delegate>()
              {
                  (LoggingDelegate)MACOS_X64_Logging,
                  (StopLoggingDelegate)MACOS_X64_StopLogging,
                  (LLM_ConstructDelegate)MACOS_X64_LLM_Construct,
                  (LLM_DeleteDelegate)MACOS_X64_LLM_Delete,
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
            { "undreamai_macos-x64.dy", new List<Delegate>()
              {
                  (LoggingDelegate)MACOS_X64_DY_Logging,
                  (StopLoggingDelegate)MACOS_X64_DY_StopLogging,
                  (LLM_ConstructDelegate)MACOS_X64_DY_LLM_Construct,
                  (LLM_DeleteDelegate)MACOS_X64_DY_LLM_Delete,
                  (LLM_TokenizeDelegate)MACOS_X64_DY_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)MACOS_X64_DY_LLM_Detokenize,
                  (LLM_CompletionDelegate)MACOS_X64_DY_LLM_Completion,
                  (LLM_SlotDelegate)MACOS_X64_DY_LLM_Slot,
                  (LLM_CancelDelegate)MACOS_X64_DY_LLM_Cancel,
                  (LLM_StatusDelegate)MACOS_X64_DY_LLM_Status,
                  (StringWrapper_ConstructDelegate)MACOS_X64_DY_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)MACOS_X64_DY_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)MACOS_X64_DY_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)MACOS_X64_DY_StringWrapper_GetString,
              }},
            { "undreamai_windows-arm64", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_ARM64_Logging,
                  (StopLoggingDelegate)WINDOWS_ARM64_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_ARM64_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_ARM64_LLM_Delete,
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
            { "undreamai_windows-arm64.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_ARM64_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_ARM64_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_ARM64_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_ARM64_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_ARM64_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_ARM64_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_ARM64_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_ARM64_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_ARM64_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_ARM64_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_ARM64_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_ARM64_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_ARM64_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_ARM64_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-arm64.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_ARM64_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_ARM64_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_ARM64_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_ARM64_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_ARM64_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_ARM64_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_ARM64_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_ARM64_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_ARM64_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_ARM64_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_ARM64_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_ARM64_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_ARM64_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_ARM64_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-arm64.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_ARM64__Logging,
                  (StopLoggingDelegate)WINDOWS_ARM64__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_ARM64__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_ARM64__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_ARM64__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_ARM64__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_ARM64__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_ARM64__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_ARM64__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_ARM64__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_ARM64__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_ARM64__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_ARM64__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_ARM64__StringWrapper_GetString,
              }},
            { "undreamai_windows-avx", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX_LLM_Delete,
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
            { "undreamai_windows-avx2.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX2_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX2_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX2_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX2_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX2_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX2_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX2_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX2_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX2_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX2_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX2_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX2_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX2_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX2_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx2.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX2_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX2_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX2_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX2_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX2_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX2_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX2_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX2_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX2_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX2_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX2_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX2_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX2_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX2_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx2.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX2__Logging,
                  (StopLoggingDelegate)WINDOWS_AVX2__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX2__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX2__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX2__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX2__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX2__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX2__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX2__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX2__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX2__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX2__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX2__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX2__StringWrapper_GetString,
              }},
            { "undreamai_windows-avx512", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX512_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX512_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX512_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX512_LLM_Delete,
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
            { "undreamai_windows-avx512.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX512_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX512_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX512_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX512_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX512_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX512_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX512_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX512_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX512_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX512_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX512_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX512_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX512_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX512_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx512.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX512_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX512_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX512_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX512_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX512_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX512_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX512_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX512_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX512_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX512_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX512_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX512_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX512_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX512_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx512.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX512__Logging,
                  (StopLoggingDelegate)WINDOWS_AVX512__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX512__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX512__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX512__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX512__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX512__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX512__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX512__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX512__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX512__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX512__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX512__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX512__StringWrapper_GetString,
              }},
            { "undreamai_windows-avx.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_AVX_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-avx.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_AVX__Logging,
                  (StopLoggingDelegate)WINDOWS_AVX__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_AVX__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_AVX__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_AVX__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_AVX__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_AVX__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_AVX__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_AVX__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_AVX__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_AVX__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_AVX__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_AVX__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_AVX__StringWrapper_GetString,
              }},
            { "undreamai_windows-clblast", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CLBLAST_Logging,
                  (StopLoggingDelegate)WINDOWS_CLBLAST_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CLBLAST_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CLBLAST_LLM_Delete,
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
            { "undreamai_windows-clblast.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CLBLAST_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_CLBLAST_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CLBLAST_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CLBLAST_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CLBLAST_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CLBLAST_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CLBLAST_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CLBLAST_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CLBLAST_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CLBLAST_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CLBLAST_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CLBLAST_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CLBLAST_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CLBLAST_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-clblast.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CLBLAST_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_CLBLAST_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CLBLAST_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CLBLAST_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CLBLAST_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CLBLAST_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CLBLAST_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CLBLAST_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CLBLAST_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CLBLAST_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CLBLAST_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CLBLAST_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CLBLAST_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CLBLAST_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-clblast.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CLBLAST__Logging,
                  (StopLoggingDelegate)WINDOWS_CLBLAST__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CLBLAST__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CLBLAST__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CLBLAST__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CLBLAST__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CLBLAST__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CLBLAST__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CLBLAST__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CLBLAST__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CLBLAST__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CLBLAST__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CLBLAST__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CLBLAST__StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu11.7.1", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU11_7_1_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU11_7_1_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_LLM_Delete,
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
            { "undreamai_windows-cuda-cu11.7.1.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU11_7_1_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU11_7_1_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU11_7_1_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU11_7_1_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu11.7.1.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU11_7_1_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU11_7_1_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU11_7_1_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU11_7_1_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu11.7.1.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU11_7_1__Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU11_7_1__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU11_7_1__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU11_7_1__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU11_7_1__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU11_7_1__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU11_7_1__StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu12.2.0", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU12_2_0_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU12_2_0_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_LLM_Delete,
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
            { "undreamai_windows-cuda-cu12.2.0.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU12_2_0_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU12_2_0_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU12_2_0_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU12_2_0_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu12.2.0.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU12_2_0_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU12_2_0_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU12_2_0_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU12_2_0_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-cuda-cu12.2.0.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_CUDA_CU12_2_0__Logging,
                  (StopLoggingDelegate)WINDOWS_CUDA_CU12_2_0__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_CUDA_CU12_2_0__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_CUDA_CU12_2_0__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_CUDA_CU12_2_0__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_CUDA_CU12_2_0__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_CUDA_CU12_2_0__StringWrapper_GetString,
              }},
            { "undreamai_windows-noavx", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_NOAVX_Logging,
                  (StopLoggingDelegate)WINDOWS_NOAVX_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_NOAVX_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_NOAVX_LLM_Delete,
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
              }},
            { "undreamai_windows-noavx.dll", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_NOAVX_DLL_Logging,
                  (StopLoggingDelegate)WINDOWS_NOAVX_DLL_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_NOAVX_DLL_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_NOAVX_DLL_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_NOAVX_DLL_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_NOAVX_DLL_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_NOAVX_DLL_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_NOAVX_DLL_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_NOAVX_DLL_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_NOAVX_DLL_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_NOAVX_DLL_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_NOAVX_DLL_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_NOAVX_DLL_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_NOAVX_DLL_StringWrapper_GetString,
              }},
            { "undreamai_windows-noavx.exp", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_NOAVX_EXP_Logging,
                  (StopLoggingDelegate)WINDOWS_NOAVX_EXP_StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_NOAVX_EXP_LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_NOAVX_EXP_LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_NOAVX_EXP_LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_NOAVX_EXP_LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_NOAVX_EXP_LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_NOAVX_EXP_LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_NOAVX_EXP_LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_NOAVX_EXP_LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_NOAVX_EXP_StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_NOAVX_EXP_StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_NOAVX_EXP_StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_NOAVX_EXP_StringWrapper_GetString,
              }},
            { "undreamai_windows-noavx.", new List<Delegate>()
              {
                  (LoggingDelegate)WINDOWS_NOAVX__Logging,
                  (StopLoggingDelegate)WINDOWS_NOAVX__StopLogging,
                  (LLM_ConstructDelegate)WINDOWS_NOAVX__LLM_Construct,
                  (LLM_DeleteDelegate)WINDOWS_NOAVX__LLM_Delete,
                  (LLM_TokenizeDelegate)WINDOWS_NOAVX__LLM_Tokenize,
                  (LLM_DetokenizeDelegate)WINDOWS_NOAVX__LLM_Detokenize,
                  (LLM_CompletionDelegate)WINDOWS_NOAVX__LLM_Completion,
                  (LLM_SlotDelegate)WINDOWS_NOAVX__LLM_Slot,
                  (LLM_CancelDelegate)WINDOWS_NOAVX__LLM_Cancel,
                  (LLM_StatusDelegate)WINDOWS_NOAVX__LLM_Status,
                  (StringWrapper_ConstructDelegate)WINDOWS_NOAVX__StringWrapper_Construct,
                  (StringWrapper_DeleteDelegate)WINDOWS_NOAVX__StringWrapper_Delete,
                  (StringWrapper_GetStringSizeDelegate)WINDOWS_NOAVX__StringWrapper_GetStringSize,
                  (StringWrapper_GetStringDelegate)WINDOWS_NOAVX__StringWrapper_GetString,
              }}
        };
    }
}
