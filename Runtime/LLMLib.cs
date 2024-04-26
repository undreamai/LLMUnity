using System;
using System.Runtime.InteropServices;

namespace LLMUnity
{
    public class LLMLib
    {
        public delegate IntPtr LLM_ConstructDelegate(string command);
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

        // Function pointer instances for each native function
        public static LLM_ConstructDelegate LLM_Construct;
        public static LLM_DeleteDelegate LLM_Delete;
        public static LLM_TokenizeDelegate LLM_Tokenize;
        public static LLM_DetokenizeDelegate LLM_Detokenize;
        public static LLM_CompletionDelegate LLM_Completion;
        public static LLM_SlotDelegate LLM_Slot;
        public static LLM_CancelDelegate LLM_Cancel;
        public static LLM_StatusDelegate LLM_Status;
        public static StringWrapper_ConstructDelegate StringWrapper_Construct;
        public static StringWrapper_DeleteDelegate StringWrapper_Delete;
        public static StringWrapper_GetStringSizeDelegate StringWrapper_GetStringSize;
        public static StringWrapper_GetStringDelegate StringWrapper_GetString;

        const string avx_dll = "undreamai_linux-avx";
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Construct")]
        public static extern IntPtr LINUX_AVX_LLM_Construct(string command);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Delete")]
        public static extern void LINUX_AVX_LLM_Delete(IntPtr LLMObject);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Tokenize")]
        public static extern void LINUX_AVX_LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Detokenize")]
        public static extern void LINUX_AVX_LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Completion")]
        public static extern void LINUX_AVX_LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Slot")]
        public static extern void LINUX_AVX_LLM_Slot(IntPtr LLMObject, string json_data);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Cancel")]
        public static extern void LINUX_AVX_LLM_Cancel(IntPtr LLMObject, int id_slot);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLM_Status")]
        public static extern int LINUX_AVX_LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Construct")]
        public static extern IntPtr LINUX_AVX_StringWrapper_Construct();
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_Delete")]
        public static extern void LINUX_AVX_StringWrapper_Delete(IntPtr instance);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetStringSize")]
        public static extern int LINUX_AVX_StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(avx_dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StringWrapper_GetString")]
        public static extern void LINUX_AVX_StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);

        static LLMLib()
        {
            string arch = DeterminePluginName();
            if (arch == "linux-avx")
            {
                LLM_Construct = LINUX_AVX_LLM_Construct;
                LLM_Delete = LINUX_AVX_LLM_Delete;
                LLM_Tokenize = LINUX_AVX_LLM_Tokenize;
                LLM_Detokenize = LINUX_AVX_LLM_Detokenize;
                LLM_Completion = LINUX_AVX_LLM_Completion;
                LLM_Slot = LINUX_AVX_LLM_Slot;
                LLM_Cancel = LINUX_AVX_LLM_Cancel;
                LLM_Status = LINUX_AVX_LLM_Status;
                StringWrapper_Construct = LINUX_AVX_StringWrapper_Construct;
                StringWrapper_Delete = LINUX_AVX_StringWrapper_Delete;
                StringWrapper_GetStringSize = LINUX_AVX_StringWrapper_GetStringSize;
                StringWrapper_GetString = LINUX_AVX_StringWrapper_GetString;
            }
        }

        private static string DeterminePluginName()
        {
            return "linux-avx";
        }
    }
}
