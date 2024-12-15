using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
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

        /// <summary>
        /// Adds the Accelerate framework (for ios)
        /// </summary>
        public static void AddAccelerate(string outputPath)
        {
            string projPath = PBXProject.GetPBXProjectPath(outputPath);
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);
            proj.AddFrameworkToProject(proj.GetUnityMainTargetGuid(), "Accelerate.framework", false);
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "Accelerate.framework", false);
            proj.WriteToFile(projPath);
        }

        // called after the build
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS) AddAccelerate(report.summary.outputPath);
            BuildCompleted();
        }

        public void BuildCompleted()
        {
            // Delay the reset operation to ensure Unity is no longer in the build process
            EditorApplication.delayCall += () =>
            {
                Application.logMessageReceived -= OnBuildError;
                LLMBuilder.Reset();
            };
        }
    }
}
