#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class VisionOSPostProcessBuild
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target == BuildTarget.VisionOS || target.ToString() == "VisionOS")
        {
            string projectPath = GetProjectPath(buildPath);
            if (string.IsNullOrEmpty(projectPath))
            {
                Debug.LogError("[VisionOSPostProcessBuild] Failed to find the Xcode project file.");
                return;
            }

            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Get target GUIDs
            string unityMainTargetGuid = project.GetUnityMainTargetGuid();
            string unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

            // Path to the xcframeworks in the Unity project
            string xcframeworksPathInUnity = Path.Combine(Application.dataPath, "Mach1/Plugins/visionOS");

            // Destination path in the Xcode project
            string xcframeworksDestinationPath = Path.Combine(buildPath, "Frameworks/Mach1/Plugins/visionOS");

            // Copy xcframeworks to the Xcode project
            CopyXCFrameworks(xcframeworksPathInUnity, xcframeworksDestinationPath);

            // Add xcframeworks to the Xcode project
            AddXCFrameworksToProject(project, unityFrameworkTargetGuid, xcframeworksDestinationPath, buildPath);

            // Save the modified Xcode project
            project.WriteToFile(projectPath);

            // Log completion
            Debug.Log("[VisionOSPostProcessBuild] Successfully added xcframeworks to the Xcode project.");
        }
    }

    private static string GetProjectPath(string buildPath)
    {
        // Look for any .xcodeproj files in the build directory
        string[] xcodeprojFiles = Directory.GetDirectories(buildPath, "*.xcodeproj");
        if (xcodeprojFiles.Length > 0)
        {
            // Use the first .xcodeproj found
            string xcodeprojPath = xcodeprojFiles[0];
            return Path.Combine(xcodeprojPath, "project.pbxproj");
        }
        else
        {
            Debug.LogError("[VisionOSPostProcessBuild] No Xcode project found in the build path.");
            return null;
        }
    }

    private static void CopyXCFrameworks(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning($"[VisionOSPostProcessBuild] Source directory does not exist: {sourceDir}");
            return;
        }

        // Ensure the destination directory exists
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy all xcframeworks
        string[] xcframeworks = Directory.GetDirectories(sourceDir, "*.xcframework", SearchOption.AllDirectories);
        foreach (string xcframework in xcframeworks)
        {
            string relativePath = xcframework.Substring(sourceDir.Length + 1);
            string destPath = Path.Combine(destDir, relativePath);

            // If the xcframework already exists at the destination, delete it
            if (Directory.Exists(destPath))
            {
                try
                {
                    Directory.Delete(destPath, true);
                    Debug.Log($"[VisionOSPostProcessBuild] Deleted existing xcframework at: {destPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VisionOSPostProcessBuild] Failed to delete existing xcframework at: {destPath}. Error: {e.Message}");
                    continue; // Skip copying this framework to prevent further errors
                }
            }

            // Copy the xcframework directory
            try
            {
                FileUtil.CopyFileOrDirectory(xcframework, destPath);
                Debug.Log($"[VisionOSPostProcessBuild] Copied xcframework: {relativePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VisionOSPostProcessBuild] Failed to copy xcframework: {relativePath}. Error: {e.Message}");
            }
        }
    }

    private static void AddXCFrameworksToProject(PBXProject project, string targetGuid, string xcframeworksPath, string buildPath)
    {
        // Get all xcframeworks in the destination directory
        string[] xcframeworks = Directory.GetDirectories(xcframeworksPath, "*.xcframework", SearchOption.AllDirectories);
        foreach (string xcframework in xcframeworks)
        {
            // Get the relative path from the build directory
            string relativePath = xcframework.Substring(buildPath.Length + 1);
            string fileGuid = project.AddFile(relativePath, relativePath, PBXSourceTree.Source);

            // Add to the UnityFramework target
            project.AddFileToBuild(targetGuid, fileGuid);

            // Set build properties
            project.AddFileToEmbedFrameworks(targetGuid, fileGuid);

            // Set "Code Sign On Copy" to true
            // var settings = new PBXProjectExtensions.PBXFrameworksBuildPhaseEntry
            // {
            //     fileGuid = fileGuid,
            //     codeSignOnCopy = true,
            //     removeHeadersOnCopy = false
            // };
            //PBXProjectExtensions.AddFileToEmbedFrameworksWithOptions(project, targetGuid, settings);
            PBXProjectExtensions.AddFileToEmbedFrameworks(project, targetGuid, fileGuid);

            // Update framework search paths
            string frameworkPath = Path.GetDirectoryName(relativePath);
            project.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/" + frameworkPath);

            Debug.Log($"[VisionOSPostProcessBuild] Added xcframework to project: {relativePath}");
        }
    }
}
#endif
