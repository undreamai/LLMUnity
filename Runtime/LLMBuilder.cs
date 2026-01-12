/// @file
/// @brief File implementing the LLMUnity builder.
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
namespace LLMUnity
{
    /// @ingroup utils
    /// <summary>
    /// Class implementing the LLMUnity builder.
    /// </summary>
    public class LLMBuilder : AssetPostprocessor
    {
        static List<StringPair> movedPairs = new List<StringPair>();
        public static string BuildTempDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "LLMUnityBuild");
        static string movedCache = Path.Combine(BuildTempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Reset();
        }

        public static string PluginDir(string platform, bool relative = false)
        {
            string pluginDir = Path.Combine("Plugins", platform, "LLMUnity");
            if (!relative) pluginDir = Path.Combine(Application.dataPath, pluginDir);
            return pluginDir;
        }

        public static void Retry(System.Action action, int retries = 10, int delayMs = 100)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException e)
                {
                    if (i == retries - 1) LLMUnitySetup.LogError(e.Message, true);
                    System.Threading.Thread.Sleep(delayMs);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    if (i == retries - 1) LLMUnitySetup.LogError(e.Message, true);;
                    System.Threading.Thread.Sleep(delayMs);
                }
            }
        }

        /// <summary>
        /// Performs an action for a file or a directory recursively
        /// </summary>
        /// <param name="source">source file/directory</param>
        /// <param name="target">targer file/directory</param>
        /// <param name="actionCallback">action</param>
        public static void HandleActionFileRecursive(string source, string target, Action<string, string> actionCallback)
        {
            if (File.Exists(source))
            {
                string targetDir = Path.GetDirectoryName(target);
                if (targetDir != "" && !Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                Retry(() => actionCallback(source, target));
            }
            else if (Directory.Exists(source))
            {
                Directory.CreateDirectory(target);
                List<string> filesAndDirs = new List<string>();
                filesAndDirs.AddRange(Directory.GetFiles(source));
                filesAndDirs.AddRange(Directory.GetDirectories(source));
                foreach (string path in filesAndDirs)
                {
                    HandleActionFileRecursive(path, Path.Combine(target, Path.GetFileName(path)), actionCallback);
                }
            }
        }

        /// <summary>
        /// Overwrites a target file based on the source file
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="target">target file</param>
        public static void CopyWithOverwrite(string source, string target)
        {
            File.Copy(source, target, true);
        }

        /// <summary>
        /// Copies a source file to a target file
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="target">target file</param>
        public static void CopyPath(string source, string target)
        {
            HandleActionFileRecursive(source, target, CopyWithOverwrite);
        }

        /// <summary>
        /// Moves a source file to a target file
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="target">target file</param>
        public static void MovePath(string source, string target)
        {
            HandleActionFileRecursive(source, target, File.Move);
            DeletePath(source);
        }

        /// <summary>
        /// Deletes a path after checking if we are allowed to
        /// </summary>
        /// <param name="path">path</param>
        public static bool DeletePath(string path)
        {
            string[] allowedDirs = new string[] { LLMUnitySetup.GetAssetPath(), BuildTempDir, PluginDir("Android"), PluginDir("iOS"), PluginDir("VisionOS") };
            bool deleteOK = false;
            foreach (string allowedDir in allowedDirs) deleteOK = deleteOK || LLMUnitySetup.IsSubPath(path, allowedDir);
            if (!deleteOK)
            {
                LLMUnitySetup.LogError($"Safeguard: {path} will not be deleted because it may not be safe");
                return false;
            }
            if (File.Exists(path)) Retry(() => File.Delete(path));
            else if (Directory.Exists(path)) Retry(() => Directory.Delete(path, true));
            return true;
        }

        static void AddMovedPair(string source, string target)
        {
            movedPairs.Add(new StringPair { source = source, target = target });
            File.WriteAllText(movedCache, JsonUtility.ToJson(new ListStringPair { pairs = movedPairs }, true));
        }

        static void AddTargetPair(string target)
        {
            AddMovedPair("", target);
        }

        static bool MoveAction(string source, string target, bool addEntry = true)
        {
            Action<string, string> moveCallback;
            if (File.Exists(source) || Directory.Exists(source)) moveCallback = MovePath;
            else return false;

            if (addEntry) AddMovedPair(source, target);
            moveCallback(source, target);
            return true;
        }

        static bool CopyAction(string source, string target, bool addEntry = true)
        {
            Action<string, string> copyCallback;
            if (File.Exists(source)) copyCallback = File.Copy;
            else if (Directory.Exists(source)) copyCallback = CopyPath;
            else return false;

            if (addEntry) AddTargetPair(target);
            copyCallback(source, target);
            return true;
        }

        static void CopyActionAddMeta(string source, string target)
        {
            CopyAction(source, target);
            AddTargetPair(target + ".meta");
        }

        static void AddActionAddMeta(string target)
        {
            AddTargetPair(target);
            AddTargetPair(target + ".meta");
        }

        static string MobileSuffix(BuildTarget buildTarget)
        {
            return (buildTarget == BuildTarget.Android) ? "so" : "a";
        }

        static string MobilePluginPath(BuildTarget buildTarget, string arch, bool relative = false)
        {
            string os = buildTarget.ToString();
            return Path.Combine(PluginDir(os, relative), arch, $"libllamalib_{os.ToLower()}.{MobileSuffix(buildTarget)}");
        }

        /// <summary>
        /// Moves libraries in the correct place for building
        /// </summary>
        /// <param name="platform">target platform</param>
        public static void BuildLibraryPlatforms(BuildTarget buildTarget)
        {
            List<string> platforms = new List<string>();
            bool checkCUBLAS = false;
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    platforms.Add("win-x64");
                    checkCUBLAS = true;
                    break;
                case BuildTarget.StandaloneLinux64:
                    platforms.Add("linux-x64");
                    checkCUBLAS = true;
                    break;
                case BuildTarget.StandaloneOSX:
                    platforms.Add("osx-universal");
                    break;
                case BuildTarget.Android:
                    platforms.Add("android-arm64");
                    platforms.Add("android-x64");
                    break;
                case BuildTarget.iOS:
                    platforms.Add("ios-arm64");
                    break;
#if UNITY_2022_3_OR_NEWER
                case BuildTarget.VisionOS:
                    platforms.Add("visionos-arm64");
                    break;
#endif
            }

            foreach (string source in Directory.GetDirectories(LLMUnitySetup.libraryPath))
            {
                string sourceName = Path.GetFileName(source);
                if (!platforms.Contains(sourceName))
                {
                    string target = Path.Combine(BuildTempDir, sourceName);
                    MoveAction(source, target);
                    MoveAction(source + ".meta", target + ".meta");
                }
            }

            if (checkCUBLAS)
            {
                List<string> exclusionKeywords = LLMUnitySetup.CUBLAS ? new List<string>() { "tinyblas" } : new List<string>() { "cublas", "cudart" };
                foreach (string platform in platforms)
                {
                    string platformDir = Path.Combine(LLMUnitySetup.libraryPath, platform, "native");
                    foreach (string source in Directory.GetFiles(platformDir))
                    {
                        string sourceName = Path.GetFileName(source);
                        foreach (string exclusionKeyword in exclusionKeywords)
                        {
                            if (sourceName.Contains(exclusionKeyword))
                            {
                                string target = Path.Combine(BuildTempDir, platform, "native", sourceName);
                                MoveAction(source, target);
                                MoveAction(source + ".meta", target + ".meta");
                                break;
                            }
                        }
                    }
                }
            }

            bool isVisionOS = false;
#if UNITY_2022_3_OR_NEWER
            isVisionOS = buildTarget == BuildTarget.VisionOS;
#endif
            if (buildTarget == BuildTarget.Android || buildTarget == BuildTarget.iOS || isVisionOS)
            {
                foreach (string platform in platforms)
                {
                    string source = Path.Combine(LLMUnitySetup.libraryPath, platform, "native",  $"libllamalib_{platform}.{MobileSuffix(buildTarget)}");
                    string target = MobilePluginPath(buildTarget, platform.Split("-")[1].ToUpper());
                    string pluginDir = PluginDir(buildTarget.ToString());
                    MoveAction(source, target);
                    MoveAction(source + ".meta", target + ".meta");
                    AddActionAddMeta(pluginDir);
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            List<BuildTarget> buildTargets = new List<BuildTarget>() { BuildTarget.iOS, BuildTarget.Android };
#if UNITY_2022_3_OR_NEWER
            buildTargets.Add(BuildTarget.VisionOS);
#endif
            foreach (BuildTarget buildTarget in buildTargets)
            {
                string platformDir = Path.Combine("Assets", PluginDir(buildTarget.ToString(), true));
                if (!Directory.Exists(platformDir)) continue;
                foreach (string archDir in Directory.GetDirectories(platformDir))
                {
                    string arch = Path.GetFileName(archDir);
                    string pathToPlugin = Path.Combine("Assets", MobilePluginPath(buildTarget, arch, true));
                    for (int i = 0; i < movedAssets.Length; i++)
                    {
                        if (movedAssets[i] == pathToPlugin)
                        {
                            var importer = AssetImporter.GetAtPath(pathToPlugin) as PluginImporter;
                            if (importer != null && importer.isNativePlugin)
                            {
                                importer.SetCompatibleWithPlatform(buildTarget, true);
                                importer.SetPlatformData(buildTarget, "CPU", arch);
                                AssetDatabase.ImportAsset(pathToPlugin);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Bundles the model information
        /// </summary>
        public static void BuildModels()
        {
            LLMManager.Build(CopyActionAddMeta);
            if (File.Exists(LLMUnitySetup.LLMManagerPath)) AddActionAddMeta(LLMUnitySetup.LLMManagerPath);
        }

        /// <summary>
        /// Bundles the models and libraries
        /// </summary>
        public static void Build(BuildTarget buildTarget)
        {
            DeletePath(BuildTempDir);
            Directory.CreateDirectory(BuildTempDir);
            BuildLibraryPlatforms(buildTarget);
            BuildModels();
        }

        /// <summary>
        /// Resets the libraries back to their original state
        /// </summary>
        public static void Reset()
        {
            if (!File.Exists(movedCache)) return;
            List<StringPair> movedPairs = JsonUtility.FromJson<ListStringPair>(File.ReadAllText(movedCache)).pairs;
            if (movedPairs == null) return;

            bool refresh = false;
            foreach (var pair in movedPairs)
            {
                if (pair.source == "")
                {
                    refresh |= DeletePath(pair.target);
                }
                else
                {
                    if (File.Exists(pair.source) || Directory.Exists(pair.source))
                    {
                        refresh |= DeletePath(pair.target);
                    }
                    else
                    {
                        refresh |= MoveAction(pair.target, pair.source, false);
                    }
                }
            }
            if (refresh) AssetDatabase.Refresh();
            DeletePath(movedCache);
        }
    }
}
#endif
