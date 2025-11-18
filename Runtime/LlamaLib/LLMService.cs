using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UndreamAI.LlamaLib
{
    public class LLMService : LLMProvider
    {
        public LLMService(string modelPath, int numSlots = 1,
                          int numThreads = -1, int numGpuLayers = 0,
                          bool flashAttention = false, int contextSize = 4096,
                          int batchSize = 2048, bool embeddingOnly = false, string[] loraPaths = null)
        {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentNullException(nameof(modelPath));
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model file not found: {modelPath}");

            try
            {
                llamaLib = new LlamaLib(numGpuLayers > 0);
                llm = CreateLLM(llamaLib, modelPath, numSlots, numThreads, numGpuLayers,
                    flashAttention, contextSize, batchSize, embeddingOnly, loraPaths);
            }
            catch
            {
                llamaLib?.Dispose();
                throw;
            }
        }

        public LLMService(LlamaLib llamaLibInstance, IntPtr llmInstance)
        {
            if (llamaLibInstance == null) throw new ArgumentNullException(nameof(llamaLibInstance));
            if (llmInstance == IntPtr.Zero) throw new ArgumentNullException(nameof(llmInstance));
            llamaLib = llamaLibInstance;
            llm = llmInstance;
        }

        public static LLMService FromCommand(string paramsString)
        {
            if (string.IsNullOrEmpty(paramsString))
                throw new ArgumentNullException(nameof(paramsString));

            LlamaLib llamaLibInstance = null;
            IntPtr llmInstance = IntPtr.Zero;
            try
            {
                llamaLibInstance = new LlamaLib(LlamaLib.Has_GPU_Layers(paramsString ?? string.Empty));
                llmInstance = llamaLibInstance.LLMService_From_Command(paramsString ?? string.Empty);
            }
            catch
            {
                llamaLibInstance?.Dispose();
                throw;
            }
            return new LLMService(llamaLibInstance, llmInstance);
        }

        public static IntPtr CreateLLM(LlamaLib llamaLib, string modelPath, int numSlots, int numThreads,
            int numGpuLayers, bool flashAttention, int contextSize, int batchSize,
            bool embeddingOnly, string[] loraPaths)
        {
            IntPtr loraPathsPtr = IntPtr.Zero;
            int loraPathCount = 0;

            if (loraPaths != null && loraPaths.Length > 0)
            {
                loraPathCount = loraPaths.Length;
                // Allocate array of string pointers
                loraPathsPtr = Marshal.AllocHGlobal(IntPtr.Size * loraPathCount);

                try
                {
                    for (int i = 0; i < loraPathCount; i++)
                    {
                        if (string.IsNullOrEmpty(loraPaths[i]))
                            throw new ArgumentException($"Lora path at index {i} is null or empty");

                        IntPtr stringPtr = Marshal.StringToHGlobalAnsi(loraPaths[i] ?? string.Empty);
                        Marshal.WriteIntPtr(loraPathsPtr, i * IntPtr.Size, stringPtr);
                    }
                }
                catch
                {
                    // Clean up if allocation failed
                    for (int i = 0; i < loraPathCount; i++)
                    {
                        IntPtr stringPtr = Marshal.ReadIntPtr(loraPathsPtr, i * IntPtr.Size);
                        if (stringPtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(stringPtr);
                    }
                    Marshal.FreeHGlobal(loraPathsPtr);
                    throw;
                }
            }

            try
            {
                var llm = llamaLib.LLMService_Construct(
                    modelPath ?? string.Empty, numSlots, numThreads, numGpuLayers,
                    flashAttention, contextSize, batchSize, embeddingOnly,
                    loraPathCount, loraPathsPtr);

                if (llm == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create LLMService");

                return llm;
            }
            finally
            {
                // Clean up allocated strings
                if (loraPathsPtr != IntPtr.Zero)
                {
                    for (int i = 0; i < loraPathCount; i++)
                    {
                        IntPtr stringPtr = Marshal.ReadIntPtr(loraPathsPtr, i * IntPtr.Size);
                        if (stringPtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(stringPtr);
                    }
                    Marshal.FreeHGlobal(loraPathsPtr);
                }
            }
        }

        public string Command
        {
            get
            {
                CheckLlamaLib();
                return Marshal.PtrToStringAnsi(llamaLib.LLMService_Command(llm)) ?? "";
            }

            set
            {
            }
        }
    }
}
