using System;
using System.Runtime.InteropServices;

namespace LLMUnity
{
    public class LLMLib
    {
        const string dllName = "undreamai";

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LLM_Construct_Default();
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LLM_Construct(string command);
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LLM_Delete(IntPtr LLMObject);
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LLM_Tokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LLM_Detokenize(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LLM_Completion(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LLM_Slot(IntPtr LLMObject, string json_data);

        [DllImport(dllName)]
        public static extern IntPtr StringWrapper_Construct();
        [DllImport(dllName)]
        public static extern void StringWrapper_Delete(IntPtr instance);
        [DllImport(dllName)]
        public static extern int StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(dllName)]
        public static extern void StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize);
    }
}
