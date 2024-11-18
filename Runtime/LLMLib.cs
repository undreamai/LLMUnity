/// @file
/// @brief File implementing the LLM library calls.
/// \cond HIDE
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LLMUnity
{
    /// @ingroup utils
    /// <summary>
    /// Class implementing a wrapper for a communication stream between Unity and the llama.cpp library (mainly for completion calls and logging).
    /// </summary>
    public class StreamWrapper
    {
        Callback<string> callback;
        IntPtr stringWrapper;
        string previousString = "";
        string previousCalledString = "";
        int previousBufferSize = 0;
        bool clearOnUpdate;

        public StreamWrapper(Callback<string> callback, bool clearOnUpdate = false)
        {
            this.callback = callback;
            this.clearOnUpdate = clearOnUpdate;
            stringWrapper = LLMLib.StringWrapper_Construct();
        }

        /// <summary>
        /// Retrieves the content of the stream
        /// </summary>
        /// <param name="clear">whether to clear the stream after retrieving the content</param>
        /// <returns>stream content</returns>
        public string GetString(bool clear = false)
        {
            string result;
            int bufferSize = LLMLib.StringWrapper_GetStringSize(stringWrapper);
            if (bufferSize <= 1)
            {
                result = "";
            }
            else if (previousBufferSize != bufferSize)
            {
                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    LLMLib.StringWrapper_GetString(stringWrapper, buffer, bufferSize, clear);
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

        /// <summary>
        /// Unity Update implementation that retrieves the content and calls the callback if it has changed.
        /// </summary>
        public void Update()
        {
            if (stringWrapper == IntPtr.Zero) return;
            string result = GetString(clearOnUpdate);
            if (result != "" && previousCalledString != result)
            {
                callback?.Invoke(result);
                previousCalledString = result;
            }
        }

        /// <summary>
        /// Gets the stringWrapper object to pass to the library.
        /// </summary>
        /// <returns>stringWrapper object</returns>
        public IntPtr GetStringWrapper()
        {
            return stringWrapper;
        }

        /// <summary>
        /// Deletes the stringWrapper object.
        /// </summary>
        public void Destroy()
        {
            if (stringWrapper != IntPtr.Zero) LLMLib.StringWrapper_Delete(stringWrapper);
        }
    }

    /// @ingroup utils
    /// <summary>
    /// Class implementing the LLM library handling
    /// </summary>
    public class LLMLib
    {
        /// <summary>
        /// Allows to retrieve a string from the library (Unity only allows marshalling of chars)
        /// </summary>
        /// <param name="stringWrapper">string wrapper pointer</param>
        /// <returns>retrieved string</returns>
        public static string GetStringWrapperResult(IntPtr stringWrapper)
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

        // const string libraryName = "libundreamai_avx2";
        const string libraryName = "libundreamai_iOS";

        [DllImport(libraryName)] public static extern void Logging(IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void StopLogging();
        [DllImport(libraryName)] public static extern IntPtr LLM_Construct(string command);
        [DllImport(libraryName)] public static extern void LLM_Delete(IntPtr LLMObject);
        [DllImport(libraryName)] public static extern void LLM_StartServer(IntPtr LLMObject);
        [DllImport(libraryName)] public static extern void LLM_StopServer(IntPtr LLMObject);
        [DllImport(libraryName)] public static extern void LLM_Start(IntPtr LLMObject);
        [DllImport(libraryName)] public static extern bool LLM_Started(IntPtr LLMObject);
        [DllImport(libraryName)] public static extern void LLM_Stop(IntPtr LLMObject);
        [DllImport(libraryName)] public static extern void LLM_SetTemplate(IntPtr LLMObject, string chatTemplate);
        [DllImport(libraryName)] public static extern void LLM_SetSSL(IntPtr LLMObject, string SSLCert, string SSLKey);
        [DllImport(libraryName)] public static extern void LLM_Tokenize(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_Detokenize(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_Embeddings(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_Lora_Weight(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_LoraList(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_Completion(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_Slot(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern void LLM_Cancel(IntPtr LLMObject, int idSlot);
        [DllImport(libraryName)] public static extern int LLM_Status(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(libraryName)] public static extern IntPtr StringWrapper_Construct();
        [DllImport(libraryName)] public static extern void StringWrapper_Delete(IntPtr instance);
        [DllImport(libraryName)] public static extern int StringWrapper_GetStringSize(IntPtr instance);
        [DllImport(libraryName)] public static extern void StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);
        [DllImport(libraryName)] public static extern int LLM_Test();
    }
}
/// \endcond
