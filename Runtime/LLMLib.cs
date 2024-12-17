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

        /// <summary>
        /// Retrieves the content of the stream
        /// </summary>
        /// <param name="clear">whether to clear the stream after retrieving the content</param>
        /// <returns>stream content</returns>
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
            if (stringWrapper != IntPtr.Zero) llmlib?.StringWrapper_Delete(stringWrapper);
        }
    }

    /// @ingroup utils
    /// <summary>
    /// Class implementing a library loader for Unity.
    /// Adapted from SkiaForUnity:
    /// https://github.com/ammariqais/SkiaForUnity/blob/f43322218c736d1c41f3a3df9355b90db4259a07/SkiaUnity/Assets/SkiaSharp/SkiaSharp-Bindings/SkiaSharp.HarfBuzz.Shared/HarfBuzzSharp.Shared/LibraryLoader.cs
    /// </summary>
    static class LibraryLoader
    {
        /// <summary>
        /// Allows to retrieve a function delegate for the library
        /// </summary>
        /// <typeparam name="T">type to cast the function</typeparam>
        /// <param name="library">library handle</param>
        /// <param name="name">function name</param>
        /// <returns>function delegate</returns>
        public static T GetSymbolDelegate<T>(IntPtr library, string name) where T : Delegate
        {
            var symbol = GetSymbol(library, name);
            if (symbol == IntPtr.Zero)
                throw new EntryPointNotFoundException($"Unable to load symbol '{name}'.");

            return Marshal.GetDelegateForFunctionPointer<T>(symbol);
        }

        /// <summary>
        /// Loads the provided library in a cross-platform manner
        /// </summary>
        /// <param name="libraryName">library path</param>
        /// <returns>library handle</returns>
        public static IntPtr LoadLibrary(string libraryName)
        {
            if (string.IsNullOrEmpty(libraryName))
                throw new ArgumentNullException(nameof(libraryName));

            IntPtr handle;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
                handle = Win32.LoadLibrary(libraryName);
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
                handle = Linux.dlopen(libraryName);
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
                handle = Mac.dlopen(libraryName);
            else if (Application.platform == RuntimePlatform.Android)
                handle = Android.dlopen(libraryName);
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
                handle = iOS.dlopen(libraryName);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load library '{libraryName}'.");

            return handle;
        }

        /// <summary>
        /// Retrieve a function delegate for the library in a cross-platform manner
        /// </summary>
        /// <param name="library">library handle</param>
        /// <param name="symbolName">function name</param>
        /// <returns>function handle</returns>
        public static IntPtr GetSymbol(IntPtr library, string symbolName)
        {
            if (string.IsNullOrEmpty(symbolName))
                throw new ArgumentNullException(nameof(symbolName));

            IntPtr handle;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
                handle = Win32.GetProcAddress(library, symbolName);
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
                handle = Linux.dlsym(library, symbolName);
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
                handle = Mac.dlsym(library, symbolName);
            else if (Application.platform == RuntimePlatform.Android)
                handle = Android.dlsym(library, symbolName);
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
                handle = iOS.dlsym(library, symbolName);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load symbol '{symbolName}' from library {library}.");

            return handle;
        }

        /// <summary>
        /// Frees up the library
        /// </summary>
        /// <param name="library">library handle</param>
        public static void FreeLibrary(IntPtr library)
        {
            if (library == IntPtr.Zero)
                return;

            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
                Win32.FreeLibrary(library);
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
                Linux.dlclose(library);
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
                Mac.dlclose(library);
            else if (Application.platform == RuntimePlatform.Android)
                Android.dlclose(library);
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
                iOS.dlclose(library);
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

        private static class Android
        {
            public static IntPtr dlopen(string path) => dlopen(path, 1);

#if UNITY_ANDROID
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("__Internal")]
            public static extern int dlclose(IntPtr handle);
#else
            public static IntPtr dlopen(string filename, int flags)
            {
                return default;
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return default;
            }

            public static int dlclose(IntPtr handle)
            {
                return default;
            }

#endif
        }

        private static class iOS
        {
            public static IntPtr dlopen(string path) => dlopen(path, 1);

#if UNITY_IOS
            // LoadLibrary for iOS
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string filename, int flags);

            // GetSymbol for iOS
            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            // FreeLibrary for iOS
            [DllImport("__Internal")]
            public static extern int dlclose(IntPtr handle);
#else
            public static IntPtr dlopen(string filename, int flags)
            {
                return default;
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return default;
            }

            public static int dlclose(IntPtr handle)
            {
                return default;
            }

#endif
        }
    }

    /// @ingroup utils
    /// <summary>
    /// Class implementing the LLM library handling
    /// </summary>
    public class LLMLib
    {
        IntPtr libraryHandle = IntPtr.Zero;
        static bool has_avx = false;
        static bool has_avx2 = false;
        static bool has_avx512 = false;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

        public LLMLib(string arch){}

#if UNITY_ANDROID
        public const string LibraryName = "libundreamai_android";
#else
        public const string LibraryName = "__Internal";
#endif

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="Logging")]
        public static extern void LoggingStatic(IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="StopLogging")]
        public static extern void StopLoggingStatic();
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Construct")]
        public static extern IntPtr LLM_ConstructStatic(string command);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Delete")]
        public static extern void LLM_DeleteStatic(IntPtr LLMObject);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_StartServer")]
        public static extern void LLM_StartServerStatic(IntPtr LLMObject);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_StopServer")]
        public static extern void LLM_StopServerStatic(IntPtr LLMObject);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Start")]
        public static extern void LLM_StartStatic(IntPtr LLMObject);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Started")]
        public static extern bool LLM_StartedStatic(IntPtr LLMObject);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Stop")]
        public static extern void LLM_StopStatic(IntPtr LLMObject);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_SetTemplate")]
        public static extern void LLM_SetTemplateStatic(IntPtr LLMObject, string chatTemplate);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_SetSSL")]
        public static extern void LLM_SetSSLStatic(IntPtr LLMObject, string SSLCert, string SSLKey);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Tokenize")]
        public static extern void LLM_TokenizeStatic(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Detokenize")]
        public static extern void LLM_DetokenizeStatic(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Embeddings")]
        public static extern void LLM_EmbeddingsStatic(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Lora_Weight")]
        public static extern void LLM_LoraWeightStatic(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Lora_List")]
        public static extern void LLM_LoraListStatic(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Completion")]
        public static extern void LLM_CompletionStatic(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Slot")]
        public static extern void LLM_SlotStatic(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Cancel")]
        public static extern void LLM_CancelStatic(IntPtr LLMObject, int idSlot);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="LLM_Status")]
        public static extern int LLM_StatusStatic(IntPtr LLMObject, IntPtr stringWrapper);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="StringWrapper_Construct")]
        public static extern IntPtr StringWrapper_ConstructStatic();
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="StringWrapper_Delete")]
        public static extern void StringWrapper_DeleteStatic(IntPtr instance);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="StringWrapper_GetStringSize")]
        public static extern int StringWrapper_GetStringSizeStatic(IntPtr instance);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint="StringWrapper_GetString")]
        public static extern void StringWrapper_GetStringStatic(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false);

        public void Logging(IntPtr stringWrapper){ LoggingStatic(stringWrapper); }
        public void StopLogging(){ StopLoggingStatic(); }
        public IntPtr LLM_Construct(string command){ return LLM_ConstructStatic(command); }
        public void LLM_Delete(IntPtr LLMObject){ LLM_DeleteStatic(LLMObject); }
        public void LLM_StartServer(IntPtr LLMObject){ LLM_StartServerStatic(LLMObject); }
        public void LLM_StopServer(IntPtr LLMObject){ LLM_StopServerStatic(LLMObject); }
        public void LLM_Start(IntPtr LLMObject){ LLM_StartStatic(LLMObject); }
        public bool LLM_Started(IntPtr LLMObject){ return LLM_StartedStatic(LLMObject); }
        public void LLM_Stop(IntPtr LLMObject){ LLM_StopStatic(LLMObject); }
        public void LLM_SetTemplate(IntPtr LLMObject, string chatTemplate){ LLM_SetTemplateStatic(LLMObject, chatTemplate); }
        public void LLM_SetSSL(IntPtr LLMObject, string SSLCert, string SSLKey){ LLM_SetSSLStatic(LLMObject, SSLCert, SSLKey); }
        public void LLM_Tokenize(IntPtr LLMObject, string jsonData, IntPtr stringWrapper){ LLM_TokenizeStatic(LLMObject, jsonData, stringWrapper); }
        public void LLM_Detokenize(IntPtr LLMObject, string jsonData, IntPtr stringWrapper){ LLM_DetokenizeStatic(LLMObject, jsonData, stringWrapper); }
        public void LLM_Embeddings(IntPtr LLMObject, string jsonData, IntPtr stringWrapper){ LLM_EmbeddingsStatic(LLMObject, jsonData, stringWrapper); }
        public void LLM_LoraWeight(IntPtr LLMObject, string jsonData, IntPtr stringWrapper){ LLM_LoraWeightStatic(LLMObject, jsonData, stringWrapper); }
        public void LLM_LoraList(IntPtr LLMObject, IntPtr stringWrapper){ LLM_LoraListStatic(LLMObject, stringWrapper); }
        public void LLM_Completion(IntPtr LLMObject, string jsonData, IntPtr stringWrapper){ LLM_CompletionStatic(LLMObject, jsonData, stringWrapper); }
        public void LLM_Slot(IntPtr LLMObject, string jsonData, IntPtr stringWrapper){ LLM_SlotStatic(LLMObject, jsonData, stringWrapper); }
        public void LLM_Cancel(IntPtr LLMObject, int idSlot){ LLM_CancelStatic(LLMObject, idSlot); }
        public int LLM_Status(IntPtr LLMObject, IntPtr stringWrapper){ return LLM_StatusStatic(LLMObject, stringWrapper); }
        public IntPtr StringWrapper_Construct(){ return StringWrapper_ConstructStatic(); }
        public void StringWrapper_Delete(IntPtr instance){ StringWrapper_DeleteStatic(instance); }
        public int StringWrapper_GetStringSize(IntPtr instance){ return StringWrapper_GetStringSizeStatic(instance); }
        public void StringWrapper_GetString(IntPtr instance, IntPtr buffer, int bufferSize, bool clear = false){ StringWrapper_GetStringStatic(instance, buffer, bufferSize, clear); }

#else

        static bool has_avx_set = false;
        static readonly object staticLock = new object();

        static LLMLib()
        {
            lock (staticLock)
            {
                if (has_avx_set) return;
                string archCheckerPath = GetArchitectureCheckerPath();
                if (archCheckerPath != null)
                {
                    IntPtr archCheckerHandle = LibraryLoader.LoadLibrary(archCheckerPath);
                    if (archCheckerHandle == IntPtr.Zero)
                    {
                        LLMUnitySetup.LogError($"Failed to load library {archCheckerPath}.");
                    }
                    else
                    {
                        try
                        {
                            has_avx = LibraryLoader.GetSymbolDelegate<HasArchDelegate>(archCheckerHandle, "has_avx")();
                            has_avx2 = LibraryLoader.GetSymbolDelegate<HasArchDelegate>(archCheckerHandle, "has_avx2")();
                            has_avx512 = LibraryLoader.GetSymbolDelegate<HasArchDelegate>(archCheckerHandle, "has_avx512")();
                            LibraryLoader.FreeLibrary(archCheckerHandle);
                        }
                        catch (Exception e)
                        {
                            LLMUnitySetup.LogError($"{e.GetType()}: {e.Message}");
                        }
                    }
                }
                has_avx_set = true;
            }
        }

        /// <summary>
        /// Loads the library and function handles for the defined architecture
        /// </summary>
        /// <param name="arch">archtecture</param>
        /// <exception cref="Exception"></exception>
        public LLMLib(string arch)
        {
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
            LLM_SetSSL = LibraryLoader.GetSymbolDelegate<LLM_SetSSLDelegate>(libraryHandle, "LLM_SetSSL");
            LLM_Tokenize = LibraryLoader.GetSymbolDelegate<LLM_TokenizeDelegate>(libraryHandle, "LLM_Tokenize");
            LLM_Detokenize = LibraryLoader.GetSymbolDelegate<LLM_DetokenizeDelegate>(libraryHandle, "LLM_Detokenize");
            LLM_Embeddings = LibraryLoader.GetSymbolDelegate<LLM_EmbeddingsDelegate>(libraryHandle, "LLM_Embeddings");
            LLM_LoraWeight = LibraryLoader.GetSymbolDelegate<LLM_LoraWeightDelegate>(libraryHandle, "LLM_Lora_Weight");
            LLM_LoraList = LibraryLoader.GetSymbolDelegate<LLM_LoraListDelegate>(libraryHandle, "LLM_Lora_List");
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

        /// <summary>
        /// Gets the path of a library that allows to detect the underlying CPU (Windows / Linux).
        /// </summary>
        /// <returns>architecture checker library path</returns>
        public static string GetArchitectureCheckerPath()
        {
            string filename;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
            {
                filename = $"windows-archchecker/archchecker.dll";
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
            {
                filename = $"linux-archchecker/libarchchecker.so";
            }
            else
            {
                return null;
            }
            return Path.Combine(LLMUnitySetup.libraryPath, filename);
        }

        /// <summary>
        /// Gets the path of the llama.cpp library for the specified architecture.
        /// </summary>
        /// <param name="arch">architecture</param>
        /// <returns>llama.cpp library path</returns>
        public static string GetArchitecturePath(string arch)
        {
            string filename;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
            {
                filename = $"windows-{arch}/undreamai_windows-{arch}.dll";
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
            {
                filename = $"linux-{arch}/libundreamai_linux-{arch}.so";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
            {
                filename = $"macos-{arch}/libundreamai_macos-{arch}.dylib";
            }
            else
            {
                string error = "Unknown OS";
                LLMUnitySetup.LogError(error);
                throw new Exception(error);
            }
            return Path.Combine(LLMUnitySetup.libraryPath, filename);
        }

        public delegate bool HasArchDelegate();
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
        public delegate void LLM_SetSSLDelegate(IntPtr LLMObject, string SSLCert, string SSLKey);
        public delegate void LLM_TokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_DetokenizeDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_EmbeddingsDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_LoraWeightDelegate(IntPtr LLMObject, string jsonData, IntPtr stringWrapper);
        public delegate void LLM_LoraListDelegate(IntPtr LLMObject, IntPtr stringWrapper);
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
        public LLM_SetSSLDelegate LLM_SetSSL;
        public LLM_TokenizeDelegate LLM_Tokenize;
        public LLM_DetokenizeDelegate LLM_Detokenize;
        public LLM_CompletionDelegate LLM_Completion;
        public LLM_EmbeddingsDelegate LLM_Embeddings;
        public LLM_LoraWeightDelegate LLM_LoraWeight;
        public LLM_LoraListDelegate LLM_LoraList;
        public LLM_SlotDelegate LLM_Slot;
        public LLM_CancelDelegate LLM_Cancel;
        public LLM_StatusDelegate LLM_Status;
        public StringWrapper_ConstructDelegate StringWrapper_Construct;
        public StringWrapper_DeleteDelegate StringWrapper_Delete;
        public StringWrapper_GetStringSizeDelegate StringWrapper_GetStringSize;
        public StringWrapper_GetStringDelegate StringWrapper_GetString;

#endif

        /// <summary>
        /// Identifies the possible architectures that we can use based on the OS and GPU usage
        /// </summary>
        /// <param name="gpu">whether to allow GPU architectures</param>
        /// <returns>possible architectures</returns>
        public static List<string> PossibleArchitectures(bool gpu = false)
        {
            List<string> architectures = new List<string>();
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer ||
                Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
            {
                if (gpu)
                {
                    if (LLMUnitySetup.FullLlamaLib)
                    {
                        architectures.Add("cuda-cu12.2.0-full");
                        architectures.Add("cuda-cu11.7.1-full");
                    }
                    else
                    {
                        architectures.Add("cuda-cu12.2.0");
                        architectures.Add("cuda-cu11.7.1");
                    }
                    architectures.Add("hip");
                    architectures.Add("vulkan");
                }
                if (has_avx512) architectures.Add("avx512");
                if (has_avx2) architectures.Add("avx2");
                if (has_avx) architectures.Add("avx");
                architectures.Add("noavx");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                if (arch.Contains("arm"))
                {
                    architectures.Add("arm64-acc");
                    architectures.Add("arm64-no_acc");
                }
                else
                {
                    if (arch != "x86" && arch != "x64") LLMUnitySetup.LogWarning($"Unknown architecture of processor {arch}! Falling back to x86_64");
                    architectures.Add("x64-acc");
                    architectures.Add("x64-no_acc");
                }
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                architectures.Add("android");
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                architectures.Add("ios");
            }
            else
            {
                string error = "Unknown OS";
                LLMUnitySetup.LogError(error);
                throw new Exception(error);
            }
            return architectures;
        }
    
        /// <summary>
        /// Allows to retrieve a string from the library (Unity only allows marshalling of chars)
        /// </summary>
        /// <param name="stringWrapper">string wrapper pointer</param>
        /// <returns>retrieved string</returns>
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

        /// <summary>
        /// Destroys the LLM library
        /// </summary>
        public void Destroy()
        {
            if (libraryHandle != IntPtr.Zero) LibraryLoader.FreeLibrary(libraryHandle);
        }
    }

}
/// \endcond
