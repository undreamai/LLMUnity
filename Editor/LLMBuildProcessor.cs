using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS || UNITY_VISIONOS
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

namespace LLMUnity
{
    public class LLMBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // called before the build
        public void OnPreprocessBuild(BuildReport report)
        {
            Application.logMessageReceived += OnBuildError;
            LLMBuilder.Build(report.summary.platform);
            AssetDatabase.Refresh();
        }

        // called during build to check for errors
        private void OnBuildError(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Error) BuildCompleted();
        }

#if UNITY_IOS || UNITY_VISIONOS
        /// <summary>
        /// Postprocess the iOS Build
        /// </summary>
        public static void PostprocessIOSBuild(BuildTarget buildTarget, string outputPath)
        {
            string projPath = PBXProject.GetPBXProjectPath(outputPath);
#if UNITY_VISIONOS
            projPath = projPath.Replace("Unity-iPhone", "Unity-VisionOS");
#endif
            PBXProject project = new PBXProject();
            project.ReadFromFile(projPath);

            string targetGuid = project.GetUnityFrameworkTargetGuid();
            string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            string unityMainTargetGuid = project.GetUnityMainTargetGuid();
            string embedFrameworksGuid = project.GetResourcesBuildPhaseByTarget(frameworkTargetGuid);

            // Add Accelerate framework
            project.AddFrameworkToProject(unityMainTargetGuid, "Accelerate.framework", false);
            project.AddFrameworkToProject(targetGuid, "Accelerate.framework", false);

            string libraryFile = LLMUnitySetup.RelativePath(LLMUnitySetup.SearchDirectory(outputPath, $"libundreamai_{buildTarget.ToString().ToLower()}.a"), outputPath);
            string fileGuid = project.FindFileGuidByProjectPath(libraryFile);
            if (string.IsNullOrEmpty(fileGuid)) Debug.LogError($"Library file {libraryFile} not found in project");
            else
            {
                foreach (var phaseGuid in project.GetAllBuildPhasesForTarget(unityMainTargetGuid))
                {
                    if (project.GetBuildPhaseName(phaseGuid) == "Embed Frameworks")
                    {
                        project.RemoveFileFromBuild(phaseGuid, fileGuid);
                        break;
                    }
                }

                project.AddFileToBuild(unityMainTargetGuid, fileGuid);
                project.AddFileToBuild(targetGuid, fileGuid);
            }

            project.WriteToFile(projPath);
            AssetDatabase.ImportAsset(projPath);
        }

#endif

        // called after the build
        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS || UNITY_VISIONOS
            PostprocessIOSBuild(report.summary.platform, report.summary.outputPath);
#endif
            EditorApplication.delayCall += () =>
            {
                BuildCompleted();
            };
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
