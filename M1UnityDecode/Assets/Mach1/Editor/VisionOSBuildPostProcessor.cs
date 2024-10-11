#if UNITY_EDITOR || UNITY_VISIONOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

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
            if (sdk_platform.Contains("xrsimulator"))
            {
                // Include simulator libraries
                string simLibPath = Path.Combine(pluginsPath, "visionSimulator");
                AddStaticLibraries(project, frameworkTargetGUID, simLibPath);
            }
            else if (sdk.Contains("xros"))
            {
                // Include device libraries
                string deviceLibPath = Path.Combine(pluginsPath, "visionOS");
                AddStaticLibraries(project, frameworkTargetGUID, deviceLibPath);
            }

            // Save the modified project
            project.WriteToFile(projectPath);
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

    private static void AddStaticLibraries(PBXProject project, string targetGUID, string libPath)
    {
        if (Directory.Exists(libPath))
        {
            string[] files = Directory.GetFiles(libPath, "*.a", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string fileGuid = project.AddFile(file, file);
                project.AddFileToBuild(targetGUID, fileGuid);
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Library path not found: {libPath}");
        }
    }
}
#endif
