using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
namespace LLMUnity
{
    public class LLMBuilder
    {
        static List<MovedPair> movedPairs = new List<MovedPair>();
        static string movedCache = Path.Combine(LLMUnitySetup.buildTempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Directory.CreateDirectory(LLMUnitySetup.buildTempDir);
            Reset();
        }

        public delegate void ActionCallback(string source, string target);

        static void AddMovedPair(string source, string target)
        {
            movedPairs.Add(new MovedPair {source = source, target = target});
            File.WriteAllText(movedCache, JsonUtility.ToJson(new FoldersMovedWrapper { movedPairs = movedPairs }, true));
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

            if (addEntry) AddMovedPair("", target);
            copyCallback(source, target);
            return true;
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
                        string target = Path.Combine(LLMUnitySetup.buildTempDir, Path.GetFileName(source));
                        MoveAction(source, target);
                        MoveAction(source + ".meta", target + ".meta");
                    }
                }
            }
        }

        public static void CopyModels()
        {
            if (LLMManager.downloadOnStart) return;
            foreach (ModelEntry modelEntry in LLMManager.modelEntries)
            {
                string source = modelEntry.path;
                string target = LLMUnitySetup.GetAssetPath(modelEntry.filename);
                if (!modelEntry.includeInBuild || File.Exists(target)) continue;
                CopyAction(source, target);
                AddMovedPair("", target + ".meta");
            }
        }

        public static void Reset()
        {
            if (!File.Exists(movedCache)) return;
            List<MovedPair> movedPairs = JsonUtility.FromJson<FoldersMovedWrapper>(File.ReadAllText(movedCache)).movedPairs;
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

    [Serializable]
    public struct MovedPair
    {
        public string source;
        public string target;
    }

    [Serializable]
    public class FoldersMovedWrapper
    {
        public List<MovedPair> movedPairs;
    }
}
#endif
