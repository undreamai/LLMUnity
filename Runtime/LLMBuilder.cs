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
        static string movedCache = Path.Combine(LLMUnitySetup.BuildTempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Directory.CreateDirectory(LLMUnitySetup.BuildTempDir);
            Reset();
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
            else if (Directory.Exists(source)) moveCallback = LLMUnitySetup.MovePath;
            else return false;

            if (addEntry) AddMovedPair(source, target);
            moveCallback(source, target);
            return true;
        }

        static bool CopyAction(string source, string target, bool addEntry = true)
        {
            ActionCallback copyCallback;
            if (File.Exists(source)) copyCallback = File.Copy;
            else if (Directory.Exists(source)) copyCallback = LLMUnitySetup.CopyPath;
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

        static bool DeleteAction(string source)
        {
            return LLMUnitySetup.DeletePath(source);
        }

        public static void HideLibraryPlatforms(string platform)
        {
            List<string> platforms = new List<string>(){ "windows", "macos", "linux", "android", "ios" };
            platforms.Remove(platform);
            foreach (string source in Directory.GetDirectories(LLMUnitySetup.libraryPath))
            {
                foreach (string platformPrefix in platforms)
                {
                    if (Path.GetFileName(source).StartsWith(platformPrefix))
                    {
                        string target = Path.Combine(LLMUnitySetup.BuildTempDir, Path.GetFileName(source));
                        MoveAction(source, target);
                        MoveAction(source + ".meta", target + ".meta");
                    }
                }
            }
        }

        public static void BuildModels()
        {
            LLMManager.Build(CopyActionAddMeta);
            if (File.Exists(LLMUnitySetup.LLMManagerPath)) AddMovedPair("", LLMUnitySetup.LLMManagerPath);
        }

        public static void Reset()
        {
            if (!File.Exists(movedCache)) return;
            List<StringPair> movedPairs = JsonUtility.FromJson<ListStringPair>(File.ReadAllText(movedCache)).pairs;
            if (movedPairs == null) return;

            bool refresh = false;
            foreach (var pair in movedPairs)
            {
                if (pair.source == "") refresh |= DeleteAction(pair.target);
                else refresh |= MoveAction(pair.target, pair.source, false);
            }
            if (refresh) AssetDatabase.Refresh();
            LLMUnitySetup.DeletePath(movedCache);
        }
    }
}
#endif
