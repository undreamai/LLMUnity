using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LLMUnity
{
    public class LLMBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // called before the build
        public void OnPreprocessBuild(BuildReport report)
        {
            Application.logMessageReceived += OnBuildError;
            string platform = null;
            switch (report.summary.platform)
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
            }
            LLMBuilder.Build(platform);
            AssetDatabase.Refresh();
        }

        // called during build to check for errors
        private void OnBuildError(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Error) BuildCompleted();
        }

        // called after the build
        public void OnPostprocessBuild(BuildReport report)
        {
            BuildCompleted();
        }

        public void BuildCompleted()
        {
            Application.logMessageReceived -= OnBuildError;
            LLMBuilder.Reset();
        }
    }
}
