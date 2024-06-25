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
                callback?.Invoke(result);
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

    static class LibraryLoader
    {
        // LibraryLoader is adapted from SkiaForUnity:
        // https://github.com/ammariqais/SkiaForUnity/blob/f43322218c736d1c41f3a3df9355b90db4259a07/SkiaUnity/Assets/SkiaSharp/SkiaSharp-Bindings/SkiaSharp.HarfBuzz.Shared/HarfBuzzSharp.Shared/LibraryLoader.cs
        public static T GetSymbolDelegate<T>(IntPtr library, string name) where T : Delegate
        {
            var symbol = GetSymbol(library, name);
            if (symbol == IntPtr.Zero)
                throw new EntryPointNotFoundException($"Unable to load symbol '{name}'.");

            return Marshal.GetDelegateForFunctionPointer<T>(symbol);
        }

        public static IntPtr LoadLibrary(string libraryName)
        {
            if (string.IsNullOrEmpty(libraryName))
                throw new ArgumentNullException(nameof(libraryName));

            IntPtr handle;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                handle = Win32.LoadLibrary(libraryName);
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
                handle = Linux.dlopen(libraryName);
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                handle = Mac.dlopen(libraryName);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load library '{libraryName}'.");

            return handle;
        }

        public static IntPtr GetSymbol(IntPtr library, string symbolName)
        {
            if (string.IsNullOrEmpty(symbolName))
                throw new ArgumentNullException(nameof(symbolName));

            IntPtr handle;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                handle = Win32.GetProcAddress(library, symbolName);
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
                handle = Linux.dlsym(library, symbolName);
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                handle = Mac.dlsym(library, symbolName);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load symbol '{symbolName}' from library {library}.");

            return handle;
        }

        public static void FreeLibrary(IntPtr library)
        {
            if (library == IntPtr.Zero)
                return;

            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                Win32.FreeLibrary(library);
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
                Linux.dlclose(library);
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                Mac.dlclose(library);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to close library '{library}'.");
        }

        private static class Mac
        {
            private const string SystemLibrary = "/usr/lib/libSystem.dylib";

            private const int RTLD_LAZY = 1;
            private const int RTLD_NOW = 2;

            public static IntPtr dlopen(string path, bool lazy = true) =>
                dlopen(path, lazy ? RTLD_LAZY : RTLD_NOW);

            [DllImport(SystemLibrary)]
            public static extern IntPtr dlopen(string path, int mode);

            [DllImport(SystemLibrary)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport(SystemLibrary)]
            public static extern void dlclose(IntPtr handle);
        }

        private static class Linux
        {
            private const string SystemLibrary = "libdl.so";
            private const string SystemLibrary2 = "libdl.so.2"; // newer Linux distros use this

            private const int RTLD_LAZY = 1;
            private const int RTLD_NOW = 2;

            private static bool UseSystemLibrary2 = true;

            public static IntPtr dlopen(string path, bool lazy = true)
            {
                try
                {
                    return dlopen2(path, lazy ? RTLD_LAZY : RTLD_NOW);
                }
                catch (DllNotFoundException)
                {
                    UseSystemLibrary2 = false;
                    return dlopen1(path, lazy ? RTLD_LAZY : RTLD_NOW);
                }
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return UseSystemLibrary2 ? dlsym2(handle, symbol) : dlsym1(handle, symbol);
            }

            public static void dlclose(IntPtr handle)
            {
                if (UseSystemLibrary2)
                    dlclose2(handle);
                else
                    dlclose1(handle);
            }

            [DllImport(SystemLibrary, EntryPoint = "dlopen")]
            private static extern IntPtr dlopen1(string path, int mode);

            [DllImport(SystemLibrary, EntryPoint = "dlsym")]
            private static extern IntPtr dlsym1(IntPtr handle, string symbol);

            [DllImport(SystemLibrary, EntryPoint = "dlclose")]
            private static extern void dlclose1(IntPtr handle);

            [DllImport(SystemLibrary2, EntryPoint = "dlopen")]
            private static extern IntPtr dlopen2(string path, int mode);

            [DllImport(SystemLibrary2, EntryPoint = "dlsym")]
            private static extern IntPtr dlsym2(IntPtr handle, string symbol);

            [DllImport(SystemLibrary2, EntryPoint = "dlclose")]
            private static extern void dlclose2(IntPtr handle);
        }

        private static class Win32
        {
            private const string SystemLibrary = "Kernel32.dll";

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern void FreeLibrary(IntPtr hModule);
        }
    }

    public class LLMLib
    {
        static IntPtr archCheckerHandle = IntPtr.Zero;
        IntPtr libraryHandle = IntPtr.Zero;

        static LLMLib()
        {
            string archCheckerPath = GetArchitectureCheckerPath();
            if (archCheckerPath != null)
            {
                archCheckerHandle = LibraryLoader.LoadLibrary(archCheckerPath);
                if (archCheckerHandle == IntPtr.Zero)
                {
                    Debug.LogError($"Failed to load library {archCheckerPath}.");
                }
                else
                {
                    has_avx = LibraryLoader.GetSymbolDelegate<HasArchDelegate>(archCheckerHandle, "has_avx");
                    has_avx2 = LibraryLoader.GetSymbolDelegate<HasArchDelegate>(archCheckerHandle, "has_avx2");
                    has_avx512 = LibraryLoader.GetSymbolDelegate<HasArchDelegate>(archCheckerHandle, "has_avx512");
                }
            }
        }

        public LLMLib(string arch)
        {
            Debug.Log(GetArchitecturePath(arch));
            libraryHandle = LibraryLoader.LoadLibrary(GetArchitecturePath(arch));
            if (libraryHandle == IntPtr.Zero)
            {
                throw new Exception($"Failed to load library {arch}.");
            }

            LLM_Construct = LibraryLoader.GetSymbolDelegate<LLM_ConstructDelegate>(libraryHandle, "LLM_Construct");
            LLM_Delete = LibraryLoader.GetSymbolDelegate<LLM_DeleteDelegate>(libraryHandle, "LLM_Delete");
            LLM_StartServer = LibraryLoader.GetSymbolDelegate<LLM_StartServerDelegate>(libraryHandle, "LLM_StartServer");
            LLM_StopServer = LibraryLoader.GetSymbolDelegate<LLM_StopServerDelegate>(libraryHandle, "LLM_StopServer");
            LLM_Start = LibraryLoader.GetSymbolDelegate<LLM_StartDelegate>(libraryHandle, "LLM_Start");
            LLM_Started = LibraryLoader.GetSymbolDelegate<LLM_StartedDelegate>(libraryHandle, "LLM_Started");
            LLM_Stop = LibraryLoader.GetSymbolDelegate<LLM_StopDelegate>(libraryHandle, "LLM_Stop");
            LLM_SetTemplate = LibraryLoader.GetSymbolDelegate<LLM_SetTemplateDelegate>(libraryHandle, "LLM_SetTemplate");
            LLM_Tokenize = LibraryLoader.GetSymbolDelegate<LLM_TokenizeDelegate>(libraryHandle, "LLM_Tokenize");
            LLM_Detokenize = LibraryLoader.GetSymbolDelegate<LLM_DetokenizeDelegate>(libraryHandle, "LLM_Detokenize");
            LLM_Completion = LibraryLoader.GetSymbolDelegate<LLM_CompletionDelegate>(libraryHandle, "LLM_Completion");
            LLM_Slot = LibraryLoader.GetSymbolDelegate<LLM_SlotDelegate>(libraryHandle, "LLM_Slot");
            LLM_Cancel = LibraryLoader.GetSymbolDelegate<LLM_CancelDelegate>(libraryHandle, "LLM_Cancel");
            LLM_Status = LibraryLoader.GetSymbolDelegate<LLM_StatusDelegate>(libraryHandle, "LLM_Status");
            StringWrapper_Construct = LibraryLoader.GetSymbolDelegate<StringWrapper_ConstructDelegate>(libraryHandle, "StringWrapper_Construct");
            StringWrapper_Delete = LibraryLoader.GetSymbolDelegate<StringWrapper_DeleteDelegate>(libraryHandle, "StringWrapper_Delete");
            StringWrapper_GetStringSize = LibraryLoader.GetSymbolDelegate<StringWrapper_GetStringSizeDelegate>(libraryHandle, "StringWrapper_GetStringSize");
            StringWrapper_GetString = LibraryLoader.GetSymbolDelegate<StringWrapper_GetStringDelegate>(libraryHandle, "StringWrapper_GetString");
            Logging = LibraryLoader.GetSymbolDelegate<LoggingDelegate>(libraryHandle, "Logging");
            StopLogging = LibraryLoader.GetSymbolDelegate<StopLoggingDelegate>(libraryHandle, "StopLogging");
        }

        public void Destroy()
        {
            if (archCheckerHandle != IntPtr.Zero) LibraryLoader.FreeLibrary(libraryHandle);
            if (libraryHandle != IntPtr.Zero) LibraryLoader.FreeLibrary(libraryHandle);
        }

        public static List<string> PossibleArchitectures(bool gpu = false)
        {
            List<string> architectures = new List<string>();
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                if (gpu)
                {
                    architectures.Add("cuda-cu12.2.0");
                    architectures.Add("cuda-cu11.7.1");
                    architectures.Add("hip");
                    architectures.Add("vulkan");
                }
                try
                {
                    if (has_avx512()) architectures.Add("avx512");
                    if (has_avx2()) architectures.Add("avx2");
                    if (has_avx()) architectures.Add("avx");
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.GetType()}: {e.Message}");
                }
                architectures.Add("noavx");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                if (arch.Contains("arm"))
                {
                    architectures.Add("arm64-no_acc");
                    architectures.Add("arm64-acc");
                }
                else
                {
                    if (arch != "x86" && arch != "x64") Debug.LogWarning($"Unknown architecture of processor {arch}! Falling back to x86_64");
                    architectures.Add("x64-no_acc");
                    architectures.Add("x64-acc");
                }
            }
            else
            {
                string error = "Unknown OS";
                Debug.LogError(error);
                throw new Exception(error);
            }
            return architectures;
        }

        public static string GetArchitectureCheckerPath()
        {
            string filename;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                filename = $"windows-archchecker/archchecker.dll";
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                filename = $"linux-archchecker/libarchchecker.so";
            }
            else
            {
                return null;
            }
            return Path.Combine(LLMUnitySetup.libraryPath, filename);
        }

        public static string GetArchitecturePath(string arch)
        {
            string filename;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                filename = $"windows-{arch}/undreamai_windows-{arch}.dll";
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                filename = $"linux-{arch}/libundreamai_linux-{arch}.so";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                filename = $"macos-{arch}/libundreamai_macos-{arch}.dylib";
            }
            else
            {
                string error = "Unknown OS";
                Debug.LogError(error);
                throw new Exception(error);
            }
            return Path.Combine(LLMUnitySetup.libraryPath, filename);
        }

        public delegate bool HasArchDelegate();
        public static HasArchDelegate has_avx;
        public static HasArchDelegate has_avx2;
        public static HasArchDelegate has_avx512;

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
        public delegate bool LLM_StartedDelegate(IntPtr LLMObject);
        public delegate void LLM_StopDelegate(IntPtr LLMObject);
        public delegate void LLM_SetTemplateDelegate(IntPtr LLMObject, string chatTemplate);
        public delegate void LLM_TokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_DetokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_CompletionDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_SlotDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
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
        public LLM_StartedDelegate LLM_Started;
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
    }
}
/// \endcond
