using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

namespace LLMUnity
{
    public class LLMBuildProcessor : MonoBehaviour, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        static string tempDir = Path.Combine(Application.temporaryCachePath, "LLMBuildProcessor", Path.GetFileName(LLMUnitySetup.libraryPath));
        static List<MovedPair> movedPairs = new List<MovedPair>();
        static string movedCache = Path.Combine(tempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            else ResetMoves();
        }

        // CALLED BEFORE THE BUILD
        public void OnPreprocessBuild(BuildReport report)
        {
            // Start listening for errors when build starts
            Application.logMessageReceived += OnBuildError;
            HideLibraryPlatforms(report.summary.platform);
            HideModels();
            if (movedPairs.Count > 0) AssetDatabase.Refresh();
        }

        // CALLED DURING BUILD TO CHECK FOR ERRORS
        private void OnBuildError(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Error)
            {
                // FAILED TO BUILD, STOP LISTENING FOR ERRORS
                BuildCompleted();
            }
        }

        // CALLED AFTER THE BUILD
        public void OnPostprocessBuild(BuildReport report)
        {
            BuildCompleted();
        }

        public void BuildCompleted()
        {
            Application.logMessageReceived -= OnBuildError;
            ResetMoves();
        }

        static bool MovePath(string source, string target)
        {
            bool moved = false;
            if (File.Exists(source))
            {
                File.Move(source, target);
                moved = true;
            }
            else if (Directory.Exists(source))
            {
                Directory.Move(source, target);
                moved = true;
            }
            if (moved)
            {
                movedPairs.Add(new MovedPair {source = source, target = target});
                File.WriteAllText(movedCache, JsonUtility.ToJson(new FoldersMovedWrapper { movedPairs = movedPairs }));
            }
            return moved;
        }

        static void MoveAssetAndMeta(string source, string target)
        {
            MovePath(source + ".meta", target + ".meta");
            MovePath(source, target);
        }

        static void HideLibraryPlatforms(BuildTarget buildPlatform)
        {
            List<string> platforms = new List<string>(){ "windows", "macos", "linux", "android" };
            switch (buildPlatform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    platforms.Remove("windows");
                    break;
                case BuildTarget.StandaloneLinux64:
                    platforms.Remove("linux");
                    break;
                case BuildTarget.StandaloneOSX:
                    platforms.Remove("macos");
                    break;
                case BuildTarget.Android:
                    platforms.Remove("android");
                    break;
            }

            foreach (string dirname in Directory.GetDirectories(LLMUnitySetup.libraryPath))
            {
                foreach (string platform in platforms)
                {
                    if (Path.GetFileName(dirname).StartsWith(platform))
                    {
                        MoveAssetAndMeta(dirname, Path.Combine(tempDir, Path.GetFileName(dirname)));
                    }
                }
            }
        }

        static void HideModels()
        {
            foreach (LLM llm in FindObjectsOfType<LLM>())
            {
                // if (!llm.downloadOnBuild) continue;
                // if (llm.modelURL != "") MoveAssetAndMeta(LLMUnitySetup.GetAssetPath(llm.model), Path.Combine(tempDir, Path.GetFileName(llm.model)));
                if (llm.loraURL != "") MoveAssetAndMeta(LLMUnitySetup.GetAssetPath(llm.lora), Path.Combine(tempDir, Path.GetFileName(llm.lora)));
            }
        }

        static void ResetMoves()
        {
            if (!File.Exists(movedCache)) return;
            List<MovedPair> movedPairs = JsonUtility.FromJson<FoldersMovedWrapper>(File.ReadAllText(movedCache)).movedPairs;
            if (movedPairs == null) return;

            bool refresh = false;
            foreach (var pair in movedPairs) refresh |= MovePath(pair.target, pair.source);
            if (refresh) AssetDatabase.Refresh();
            File.Delete(movedCache);
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
