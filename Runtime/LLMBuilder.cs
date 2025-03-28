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
    public class LLMBuilder : AssetPostprocessor
    {
        static List<StringPair> movedPairs = new List<StringPair>();
        public static string BuildTempDir = Path.Combine(Application.temporaryCachePath, "LLMUnityBuild");
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

        public static string PluginLibraryDir(string platform, bool relative = false)
        {
            return Path.Combine(PluginDir(platform, relative), LLMUnitySetup.libraryName);
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
            string[] allowedDirs = new string[] { LLMUnitySetup.GetAssetPath(), BuildTempDir, PluginDir("Android"), PluginDir("iOS"), PluginDir("VisionOS")};
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
        public static void BuildLibraryPlatforms(BuildTarget buildTarget)
        {
            string platform = "";
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    platform = "windows";
                    break;
                case BuildTarget.StandaloneLinux64:
                    platform = "linux";
                    break;
                case BuildTarget.StandaloneOSX:
                    platform = "macos";
                    break;
                case BuildTarget.Android:
                    platform = "android";
                    break;
                case BuildTarget.iOS:
                    platform = "ios";
                    break;
                case BuildTarget.VisionOS:
                    platform = "visionos";
                    break;
            }

            foreach (string source in Directory.GetDirectories(LLMUnitySetup.libraryPath))
            {
                string sourceName = Path.GetFileName(source);
                bool move = !sourceName.StartsWith(platform);
                move = move || (sourceName.Contains("cuda") && !sourceName.Contains("full") && LLMUnitySetup.FullLlamaLib);
                move = move || (sourceName.Contains("cuda") && sourceName.Contains("full") && !LLMUnitySetup.FullLlamaLib);
                if (move)
                {
                    string target = Path.Combine(BuildTempDir, sourceName);
                    MoveAction(source, target);
                    MoveAction(source + ".meta", target + ".meta");
                }
            }

            if (buildTarget == BuildTarget.Android || buildTarget == BuildTarget.iOS || buildTarget == BuildTarget.VisionOS)
            {
                string source = Path.Combine(LLMUnitySetup.libraryPath, platform);
                string target = PluginLibraryDir(buildTarget.ToString());
                string pluginDir = PluginDir(buildTarget.ToString());
                MoveAction(source, target);
                MoveAction(source + ".meta", target + ".meta");
                AddActionAddMeta(pluginDir);
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (BuildTarget buildTarget in new BuildTarget[]{BuildTarget.iOS, BuildTarget.VisionOS})
            {
                string pathToPlugin = Path.Combine("Assets", PluginLibraryDir(buildTarget.ToString(), true), $"libundreamai_{buildTarget.ToString().ToLower()}.a");
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    if (movedAssets[i] == pathToPlugin)
                    {
                        var importer = AssetImporter.GetAtPath(pathToPlugin) as PluginImporter;
                        if (importer != null && importer.isNativePlugin)
                        {
                            importer.SetCompatibleWithPlatform(buildTarget, true);
                            importer.SetPlatformData(buildTarget, "CPU", "ARM64");
                            AssetDatabase.ImportAsset(pathToPlugin);
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
                if (pair.source == "") refresh |= DeletePath(pair.target);
                else refresh |= MoveAction(pair.target, pair.source, false);
            }
            if (refresh) AssetDatabase.Refresh();
            DeletePath(movedCache);
        }
    }
}
#endif
