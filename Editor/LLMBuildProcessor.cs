using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_VISIONOS
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

#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_VISIONOS
        /// <summary>
        /// Postprocess the iOS Build
        /// </summary>
        public static void PostprocessIOSBuild(BuildTarget buildTarget, string outputPath)
        {
            List<string> libraryFileNames = new List<string>();
#if UNITY_IOS
            string projPath = PBXProject.GetPBXProjectPath(outputPath);
            libraryFileNames.Add("libllamalib_ios.a");
#elif UNITY_VISIONOS
            string projPath = PBXProject.GetPBXProjectPath(outputPath).Replace("Unity-iPhone", "Unity-VisionOS");
            libraryFileNames.Add("libllamalib_visionos.a");
#else
            string projPath = Path.Combine(outputPath, Path.GetFileName(outputPath) + ".xcodeproj", "project.pbxproj");
            if (!File.Exists(projPath)) return;
            libraryFileNames.Add("libllamalib_osx-universal_acc.dylib");
            libraryFileNames.Add("libllamalib_osx-universal_no-acc.dylib");
#endif

            PBXProject project = new PBXProject();
            project.ReadFromFile(projPath);

            string unityMainTargetGuid = project.GetUnityMainTargetGuid();
            string targetGuid = project.GetUnityFrameworkTargetGuid();

            // Add Accelerate framework
            project.AddFrameworkToProject(unityMainTargetGuid, "Accelerate.framework", false);
            if (targetGuid != null) project.AddFrameworkToProject(targetGuid, "Accelerate.framework", false);

            List<string> libraryFiles = new List<string>();
            foreach (string libraryFileName in libraryFileNames)
            {
                string lib = LLMUnitySetup.SearchDirectory(outputPath, libraryFileName);
                if (lib != null) libraryFiles.Add(lib);
            }

            if (libraryFiles.Count == 0)
            {
                Debug.LogError($"No library files found for the build");
            }
            else
            {
                foreach (string libraryFile in libraryFiles)
                {
                    string relLibraryFile = LLMUnitySetup.RelativePath(libraryFile, outputPath);
                    string fileGuid = project.FindFileGuidByProjectPath(relLibraryFile);
                    if (string.IsNullOrEmpty(fileGuid))
                    {
                        Debug.LogError($"Library file {relLibraryFile} not found in project");
                    }
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

                        string relLibraryDir = Path.GetDirectoryName(relLibraryFile);
                        project.AddFileToBuild(unityMainTargetGuid, fileGuid);
                        project.AddBuildProperty(unityMainTargetGuid, "LIBRARY_SEARCH_PATHS", "$(PROJECT_DIR)/" + relLibraryDir);
                        if (targetGuid != null)
                        {
                            project.AddFileToBuild(targetGuid, fileGuid);
                            project.AddBuildProperty(targetGuid, "LIBRARY_SEARCH_PATHS", "$(PROJECT_DIR)/" + relLibraryDir);
                        }
                    }
                }
            }

            project.WriteToFile(projPath);
        }

#endif

        // called after the build
        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_VISIONOS
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
