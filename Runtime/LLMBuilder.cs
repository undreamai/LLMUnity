/// @file
/// @brief File implementing the LLMUnity builder.
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace LLMUnity
{
    /// @ingroup utils
    /// <summary>
    /// Class implementing the LLMUnity builder.
    /// </summary>
    public class LLMBuilder
    {
        static List<StringPair> movedPairs = new List<StringPair>();
        public static string BuildTempDir = Path.Combine(Application.temporaryCachePath, "LLMUnityBuild");
        public static string androidPluginDir = Path.Combine(Application.dataPath, "Plugins", "Android", "LLMUnity");
        public static string iOSPluginDir = Path.Combine(Application.dataPath, "Plugins", "iOS", "LLMUnity");
        static string movedCache = Path.Combine(BuildTempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Reset();
        }

        /// <summary>
        /// Performs an action for a file or a directory recursively
        /// </summary>
        /// <param name="source">source file/directory</param>
        /// <param name="target">targer file/directory</param>
        /// <param name="actionCallback">action</param>
        public static void HandleActionFileRecursive(string source, string target, ActionCallback actionCallback)
        {
            if (File.Exists(source))
            {
                actionCallback(source, target);
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
            string[] allowedDirs = new string[] { LLMUnitySetup.GetAssetPath(), BuildTempDir, androidPluginDir, iOSPluginDir};
            bool deleteOK = false;
            foreach (string allowedDir in allowedDirs) deleteOK = deleteOK || LLMUnitySetup.IsSubPath(path, allowedDir);
            if (!deleteOK)
            {
                LLMUnitySetup.LogError($"Safeguard: {path} will not be deleted because it may not be safe");
                return false;
            }
            if (File.Exists(path)) File.Delete(path);
            else if (Directory.Exists(path)) Directory.Delete(path, true);
            return true;
        }

        static void AddMovedPair(string source, string target)
        {
            movedPairs.Add(new StringPair {source = source, target = target});
            File.WriteAllText(movedCache, JsonUtility.ToJson(new ListStringPair { pairs = movedPairs }, true));
        }

        static void AddTargetPair(string target)
        {
            AddMovedPair("", target);
        }

        static bool MoveAction(string source, string target, bool addEntry = true)
        {
            ActionCallback moveCallback;
            if (File.Exists(source)) moveCallback = File.Move;
            else if (Directory.Exists(source)) moveCallback = MovePath;
            else return false;

            if (addEntry) AddMovedPair(source, target);
            moveCallback(source, target);
            return true;
        }

        static bool CopyAction(string source, string target, bool addEntry = true)
        {
            ActionCallback copyCallback;
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

        /// <summary>
        /// Moves libraries in the correct place for building
        /// </summary>
        /// <param name="platform">target platform</param>
        public static void BuildLibraryPlatforms(string platform)
        {
            List<string> platforms = new List<string>(){ "windows", "macos", "linux", "android", "ios", "setup" };
            platforms.Remove(platform);
            foreach (string source in Directory.GetDirectories(LLMUnitySetup.libraryPath))
            {
                string sourceName = Path.GetFileName(source);
                foreach (string platformPrefix in platforms)
                {
                    bool move = sourceName.StartsWith(platformPrefix);
                    move = move || (sourceName.Contains("cuda") && !sourceName.Contains("full") && LLMUnitySetup.FullLlamaLib);
                    move = move || (sourceName.Contains("cuda") && sourceName.Contains("full") && !LLMUnitySetup.FullLlamaLib);
                    if (move)
                    {
                        string target = Path.Combine(BuildTempDir, sourceName);
                        MoveAction(source, target);
                        MoveAction(source + ".meta", target + ".meta");
                    }
                }
            }

            if (platform == "android" || platform == "ios")
            {
                string pluginDir = platform == "android"? androidPluginDir: iOSPluginDir;
                string source = Path.Combine(LLMUnitySetup.libraryPath, platform);
                string target = Path.Combine(pluginDir, LLMUnitySetup.libraryName);
                MoveAction(source, target);
                MoveAction(source + ".meta", target + ".meta");
                AddActionAddMeta(pluginDir);
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
        public static void Build(string platform)
        {
            DeletePath(BuildTempDir);
            Directory.CreateDirectory(BuildTempDir);
            BuildLibraryPlatforms(platform);
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
                if (pair.source == "") refresh |= DeletePath(pair.target);
                else refresh |= MoveAction(pair.target, pair.source, false);
            }
            if (refresh) AssetDatabase.Refresh();
            DeletePath(movedCache);
        }
    }
}
#endif
