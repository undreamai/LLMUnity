using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace LLMUnity
{
    public class LLMBuilder
    {
        static List<StringPair> movedPairs = new List<StringPair>();
        public static string BuildTempDir = Path.Combine(Application.temporaryCachePath, "LLMUnityBuild");
        static string movedCache = Path.Combine(BuildTempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Reset();
        }

        public static void CopyPath(string source, string target)
        {
            if (File.Exists(source))
            {
                File.Copy(source, target, true);
            }
            else if (Directory.Exists(source))
            {
                Directory.CreateDirectory(target);
                List<string> filesAndDirs = new List<string>();
                filesAndDirs.AddRange(Directory.GetFiles(source));
                filesAndDirs.AddRange(Directory.GetDirectories(source));
                foreach (string path in filesAndDirs)
                {
                    CopyPath(path, Path.Combine(target, Path.GetFileName(path)));
                }
            }
        }

        public static void MovePath(string source, string target)
        {
            CopyPath(source, target);
            DeletePath(source);
        }

        public static bool DeletePath(string path)
        {
            if (!LLMUnitySetup.IsSubPath(path, LLMUnitySetup.GetAssetPath()) && !LLMUnitySetup.IsSubPath(path, BuildTempDir))
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

        public static void HideLibraryPlatforms(string platform)
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
        }

        public static void BuildModels()
        {
            LLMManager.Build(CopyActionAddMeta);
            if (File.Exists(LLMUnitySetup.LLMManagerPath)) AddActionAddMeta(LLMUnitySetup.LLMManagerPath);
        }

        public static void Build(string platform)
        {
            Directory.CreateDirectory(BuildTempDir);
            HideLibraryPlatforms(platform);
            BuildModels();
        }

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
