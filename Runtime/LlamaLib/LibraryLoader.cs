/// @file
/// @brief File implementing the LlamaLib library loader
/// \cond HIDE
using System;
using System.Runtime.InteropServices;

namespace UndreamAI.LlamaLib
{
    /// @ingroup utils
    /// <summary>
    /// Class implementing the LlamaLib library loader
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
        /// <param name="libraryPath">library path</param>
        /// <returns>library handle</returns>
        public static IntPtr LoadLibrary(string libraryPath)
        {
            if (string.IsNullOrEmpty(libraryPath))
                throw new ArgumentNullException(nameof(libraryPath));

#if (ANDROID || IOS || VISIONOS) || (UNITY_ANDROID || UNITY_IOS || UNITY_VISIONOS)
            return Mobile.dlopen(libraryPath);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Win32.LoadLibrary(libraryPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Linux.dlopen(libraryPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Mac.dlopen(libraryPath);
            else throw new PlatformNotSupportedException($"Current platform is unknown, unable to load library '{libraryPath}'.");
#endif
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

#if (ANDROID || IOS || VISIONOS) || (UNITY_ANDROID || UNITY_IOS || UNITY_VISIONOS)
            return Mobile.dlsym(library, symbolName);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Win32.GetProcAddress(library, symbolName);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Linux.dlsym(library, symbolName);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Mac.dlsym(library, symbolName);
            else throw new PlatformNotSupportedException($"Current platform is unknown, unable to load symbol '{symbolName}' from library {library}.");
#endif
        }

        /// <summary>
        /// Frees up the library
        /// </summary>
        /// <param name="library">library handle</param>
        public static void FreeLibrary(IntPtr library)
        {
            if (library == IntPtr.Zero)
                return;

#if (ANDROID || IOS || VISIONOS) || (UNITY_ANDROID || UNITY_IOS || UNITY_VISIONOS)
            Mobile.dlclose(library);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Win32.FreeLibrary(library);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Linux.dlclose(library);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Mac.dlclose(library);
            else throw new PlatformNotSupportedException($"Current platform is unknown, unable to close library '{library}'.");
#endif
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

            [DllImport(SystemLibrary, SetLastError = true)]
            public static extern bool FreeLibrary(IntPtr hModule);
        }

        private static class Mobile
        {
            public static IntPtr dlopen(string path) => dlopen(path, 1);

#if (ANDROID || IOS || VISIONOS) || (UNITY_ANDROID || UNITY_IOS || UNITY_VISIONOS)
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("__Internal")]
            public static extern int dlclose(IntPtr handle);
#else
            public static IntPtr dlopen(string filename, int flags)
            {
                return IntPtr.Zero;
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return IntPtr.Zero;
            }

            public static int dlclose(IntPtr handle)
            {
                return 0;
            }

#endif
        }
    }
}
