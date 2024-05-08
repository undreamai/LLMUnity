using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LLMUnity
{
    public class LLMLib
    {
        static LLMLib()
        {
            string arch = DetermineArchitecture();
            if (LibraryFunctions.TryGetValue(arch, out var delegates))
            {
                (
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
                    delegates[0] as LLM_ConstructDelegate,
                    delegates[1] as LLM_DeleteDelegate,
                    delegates[2] as LLM_TokenizeDelegate,
                    delegates[3] as LLM_DetokenizeDelegate,
                    delegates[4] as LLM_CompletionDelegate,
                    delegates[5] as LLM_SlotDelegate,
                    delegates[6] as LLM_CancelDelegate,
                    delegates[7] as LLM_StatusDelegate,
                    delegates[8] as StringWrapper_ConstructDelegate,
                    delegates[9] as StringWrapper_DeleteDelegate,
                    delegates[10] as StringWrapper_GetStringSizeDelegate,
                    delegates[11] as StringWrapper_GetStringDelegate
                );

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

        private static string DetermineArchitecture()
        {
            return "undreamai_linux-avx";
        }

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


        static Dictionary<string, List<Delegate>> LibraryFunctions = new Dictionary<string, List<Delegate>>
        {
            { "undreamai_linux-avx", new List<Delegate>()
              {
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
            { "undreamai_linux-avx512", new List<Delegate>()
              {
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
            { "undreamai_linux-clblast", new List<Delegate>()
              {
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
            { "undreamai_linux-cuda-cu11.7.1", new List<Delegate>()
              {
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
            { "undreamai_linux-cuda-cu12.2.0", new List<Delegate>()
              {
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
            { "undreamai_linux-noavx", new List<Delegate>()
              {
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
            { "undreamai_macos-arm64", new List<Delegate>()
              {
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
            { "undreamai_macos-x64", new List<Delegate>()
              {
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
            { "undreamai_windows-arm64", new List<Delegate>()
              {
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
            { "undreamai_windows-avx", new List<Delegate>()
              {
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
            { "undreamai_windows-avx512", new List<Delegate>()
              {
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
            { "undreamai_windows-clblast", new List<Delegate>()
              {
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
            { "undreamai_windows-cuda-cu11.7.1", new List<Delegate>()
              {
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
            { "undreamai_windows-cuda-cu12.2.0", new List<Delegate>()
              {
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
            { "undreamai_windows-noavx", new List<Delegate>()
              {
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
              }}
        };
    }
}
