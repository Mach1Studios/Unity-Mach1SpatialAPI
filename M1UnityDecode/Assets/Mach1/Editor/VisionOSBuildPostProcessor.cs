#if UNITY_EDITOR || UNITY_VISIONOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

// This BuildPostProcessor script is for swapping between visionOS and visionSimulator libraries
// based on the target platform being built for. This script is only executed when building for visionOS.
// Ideally instead of this script you should have FAT libs of both SDKs, however there are issues with 
// making the appropriate FAT libraries for visionOS and this script is to be used instead

public class VisionOSBuildPostProcessor
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        // Log the buildPath for debugging
        UnityEngine.Debug.Log($"[VisionOSBuildPostProcessor] Build path: {buildPath}");

        // Proceed only if building for visionOS
        if (target == BuildTarget.VisionOS)
        {
            // Path to the Xcode project
            string projectPath = FindXcodeProjectPath(buildPath);
            UnityEngine.Debug.Log($"[VisionOSBuildPostProcessor] Project path: {projectPath}");

            // Check if the project file exists
            if (!File.Exists(projectPath))
            {
                UnityEngine.Debug.LogError($"[VisionOSBuildPostProcessor] Project file not found at path: {projectPath}");
                return;
            }

            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Get the target GUIDs
            string targetGUID = project.GetUnityMainTargetGuid();
            string frameworkTargetGUID = project.GetUnityFrameworkTargetGuid();

            // Determine the SDK being used
            string sdk = project.GetBuildPropertyForAnyConfig(targetGUID, "SDKROOT");
            UnityEngine.Debug.Log($"[VisionOSBuildPostProcessor] SDKROOT: {sdk}");

            string sdk_platform = project.GetBuildPropertyForAnyConfig(targetGUID, "SUPPORTED_PLATFORMS");
            UnityEngine.Debug.Log($"[VisionOSBuildPostProcessor] SUPPORTED_PLATFORMS: {sdk_platform}");

            // Define paths to your libraries
            string pluginsPath = Path.Combine("$(PROJECT_DIR)", "Libraries");

            // Unity seems to only build one or the other, so far now we will search for any mention of xrsimulator
            // otherwise we will assume it is building for the device
            string libPath = "";
            if (sdk_platform.Contains("xrsimulator"))
            {
                // Include simulator libraries
                libPath = Path.Combine(pluginsPath, "visionSimulator");
            }
            else if (sdk.Contains("xros"))
            {
                // Include device libraries
                libPath = Path.Combine(pluginsPath, "visionOS");
            }

            UnityEngine.Debug.Log($"[VisionOSBuildPostProcessor] Using libraries from: {libPath}");
            //AddStaticLibraries(project, frameworkTargetGUID, libPath, buildPath);

            // Update Library Search Paths
            //UpdateLibrarySearchPaths(project, frameworkTargetGUID, libPath, buildPath);

            // Save the modified project
            //project.WriteToFile(projectPath);
        }
    }

    private static string FindXcodeProjectPath(string buildPath)
    {
        // Look for any .xcodeproj files in the build directory
        string[] xcodeprojFiles = Directory.GetDirectories(buildPath, "*.xcodeproj");
        if (xcodeprojFiles.Length > 0)
        {
            return Path.Combine(xcodeprojFiles[0], "project.pbxproj");
        }
        else
        {
            // Default to the iOS project path
            return PBXProject.GetPBXProjectPath(buildPath);
        }
    }

    private static void AddStaticLibraries(PBXProject project, string targetGUID, string libPath, string buildPath)
    {
        if (Directory.Exists(libPath))
        {
            string[] files = Directory.GetFiles(libPath, "*.a", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                // Get the relative path of the file to the Xcode project
                string relativePath = "Libraries" + file.Replace(buildPath + "/Libraries", "").Replace("\\", "/");

                // Remove existing references if any
                string existingFileGuid = project.FindFileGuidByProjectPath(relativePath);
                if (!string.IsNullOrEmpty(existingFileGuid))
                {
                    project.RemoveFileFromBuild(targetGUID, existingFileGuid);
                    project.RemoveFile(existingFileGuid);
                }

                // Add the library file
                string fileGuid = project.AddFile(relativePath, relativePath, PBXSourceTree.Source);

                // Add the library to the "Link Binary With Libraries" build phase
                project.AddFileToBuild(targetGUID, fileGuid);

                // Add linker flags
                //string libName = Path.GetFileNameWithoutExtension(file).Substring(3); // Remove 'lib' prefix
                //project.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", $"-l{libName}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Library path not found: {libPath}");
        }
    }

    private static void UpdateLibrarySearchPaths(PBXProject project, string targetGUID, string libPath, string buildPath)
    {
        // Remove existing library search paths that reference visionOS and visionSimulator
        string[] existingSearchPaths = project.GetBuildPropertyForAnyConfig(targetGUID, "LIBRARY_SEARCH_PATHS")?.Split(' ');

        if (existingSearchPaths != null)
        {
            var updatedSearchPaths = new System.Collections.Generic.List<string>();
            foreach (var path in existingSearchPaths)
            {
                if (!path.Contains("visionOS") && !path.Contains("visionSimulator"))
                {
                    updatedSearchPaths.Add(path);
                }
            }

            // Set the updated search paths
            project.SetBuildProperty(targetGUID, "LIBRARY_SEARCH_PATHS", string.Join(" ", updatedSearchPaths));
        }

        // Add the new library search path
        string relativeLibPath = libPath.Replace(buildPath + "/", "");
        string libSearchPath = "$(PROJECT_DIR)/" + relativeLibPath;

        // Enclose the path in quotes to handle spaces
        libSearchPath = $"\"{libSearchPath}\"";

        project.AddBuildProperty(targetGUID, "LIBRARY_SEARCH_PATHS", libSearchPath);
    }

}
#endif
