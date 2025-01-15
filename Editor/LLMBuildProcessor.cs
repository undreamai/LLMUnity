using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS || UNITY_VISIONOS
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
        public static void PostprocessIOSBuild(string outputPath)
        {
            string projPath = PBXProject.GetPBXProjectPath(outputPath);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projPath);

            string targetGuid = project.GetUnityFrameworkTargetGuid();
            string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            string unityMainTargetGuid = project.GetUnityMainTargetGuid();
            string embedFrameworksGuid = project.GetResourcesBuildPhaseByTarget(frameworkTargetGuid);

            // Add Accelerate framework
            project.AddFrameworkToProject(unityMainTargetGuid, "Accelerate.framework", false);
            project.AddFrameworkToProject(targetGuid, "Accelerate.framework", false);

            // Remove libundreamai_ios.a from Embed Frameworks
            string libraryFile = Path.Combine("Libraries", LLMBuilder.PluginLibraryDir("iOS", true), "libundreamai_ios.a");
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
                project.RemoveFileFromBuild(unityMainTargetGuid, fileGuid);
            }

            project.WriteToFile(projPath);
        }

#endif

        // called after the build
        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS || UNITY_VISIONOS
            AddAccelerate(report.summary.outputPath);
#endif
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
