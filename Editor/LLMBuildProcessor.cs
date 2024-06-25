using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

namespace LLMUnity
{
    public class LLMBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        static string tempDir = Path.Combine(Application.temporaryCachePath, "LLMBuildProcessor", Path.GetFileName(LLMUnitySetup.libraryPath));
        static string foldersMovedCache = Path.Combine(tempDir, "moved.json");

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            else ResetLibraryPlatforms();
        }

        // CALLED BEFORE THE BUILD
        public void OnPreprocessBuild(BuildReport report)
        {
            // Start listening for errors when build starts
            Application.logMessageReceived += OnBuildError;
            List<string> platforms = GetLibraryPlatformsToHide(report.summary.platform);
            HideLibraryPlatforms(platforms);
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
            ResetLibraryPlatforms();
        }

        static List<string> GetLibraryPlatformsToHide(BuildTarget platform)
        {
            List<string> platforms = new List<string>(){ "windows", "macos", "linux" };
            switch (platform)
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
            }
            return platforms;
        }

        static bool MovePath(string source, string target, List<FoldersMovedPair> foldersMoved = null)
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
            if (moved && foldersMoved != null) foldersMoved.Add(new FoldersMovedPair {source = source, target = target});
            return moved;
        }

        static void HideLibraryPlatforms(List<string> platforms)
        {
            List<FoldersMovedPair> foldersMoved = new List<FoldersMovedPair>();
            foreach (string dirname in Directory.GetDirectories(LLMUnitySetup.libraryPath))
            {
                foreach (string platform in platforms)
                {
                    if (Path.GetFileName(dirname).StartsWith(platform))
                    {
                        string movePath = Path.Combine(tempDir, Path.GetFileName(dirname));
                        MovePath(dirname + ".meta", movePath + ".meta", foldersMoved);
                        MovePath(dirname, movePath, foldersMoved);
                        File.WriteAllText(foldersMovedCache, JsonUtility.ToJson(new FoldersMovedWrapper { foldersMoved = foldersMoved }));
                    }
                }
            }
            if (foldersMoved.Count > 0) AssetDatabase.Refresh();
        }

        static void ResetLibraryPlatforms()
        {
            if (!File.Exists(foldersMovedCache)) return;
            List<FoldersMovedPair> foldersMoved = JsonUtility.FromJson<FoldersMovedWrapper>(File.ReadAllText(foldersMovedCache)).foldersMoved;
            if (foldersMoved == null) return;

            bool refresh = false;
            foreach (var pair in foldersMoved) refresh |= MovePath(pair.target, pair.source);
            if (refresh) AssetDatabase.Refresh();

            File.Delete(foldersMovedCache);
        }
    }

    [Serializable]
    public struct FoldersMovedPair
    {
        public string source;
        public string target;
    }

    [Serializable]
    public class FoldersMovedWrapper
    {
        public List<FoldersMovedPair> foldersMoved;
    }
}
