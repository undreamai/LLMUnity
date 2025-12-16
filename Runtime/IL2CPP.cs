#if ENABLE_IL2CPP
using System;
using System.Runtime.InteropServices;
using AOT;
using UndreamAI.LlamaLib;

namespace LLMUnity
{
    public class IL2CPP_Logging
    {
        private static Action<string> onLogging;
        private static LlamaLib.CharArrayCallback nativeLoggingThunk;

        public static void LoggingCallback(Action<string> callback)
        {
            onLogging = callback;
            if (nativeLoggingThunk == null)
            {
                nativeLoggingThunk = LoggingThunkImpl;
                LlamaLib.LoggingCallback(nativeLoggingThunk);
            }
        }

        [MonoPInvokeCallback(typeof(LlamaLib.CharArrayCallback))]
        private static void LoggingThunkImpl(IntPtr msg)
        {
            if (onLogging == null || msg == IntPtr.Zero)
                return;
            try
            {
                onLogging(Marshal.PtrToStringUTF8(msg));
            }
            catch {}
        }
    }

    public class IL2CPP_Completion
    {
        private static Action<string> onCompletion;
        private static LlamaLib.CharArrayCallback nativeCompletionThunk;

        public static LlamaLib.CharArrayCallback CreateCallback(Action<string> callback)
        {
            onCompletion = callback;
            if (nativeCompletionThunk == null)
            {
                nativeCompletionThunk = CompletionThunkImpl;
            }
            return nativeCompletionThunk;
        }

        [MonoPInvokeCallback(typeof(LlamaLib.CharArrayCallback))]
        private static void CompletionThunkImpl(IntPtr msg)
        {
            if (onCompletion == null || msg == IntPtr.Zero)
                return;
            try
            {
                onCompletion(Marshal.PtrToStringUTF8(msg));
            }
            catch {}
        }
    }
}
#endif
