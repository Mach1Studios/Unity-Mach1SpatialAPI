#if UNITY_EDITOR || UNITY_IOS || UNITY_VISIONOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class VisionOSBuildPostProcessor
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        // Proceed only if building for visionOS
        if (target == BuildTarget.VisionOS)
        {
            // Path to the Xcode project
            string projectPath = PBXProject.GetPBXProjectPath(buildPath);

            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Get the target GUIDs
            string targetGUID = project.GetUnityMainTargetGuid();
            string frameworkTargetGUID = project.GetUnityFrameworkTargetGuid();

            // Determine the SDK being used
            string sdk = project.GetBuildPropertyForAnyConfig(targetGUID, "SDKROOT");

            // Define paths to your libraries
            string pluginsPath = Path.Combine("$(PROJECT_DIR)", "Libraries");

            if (sdk.Contains("visionossimulator"))
            {
                // Include simulator libraries
                string simLibPath = Path.Combine(pluginsPath, "visionSimulator");
                AddStaticLibraries(project, frameworkTargetGUID, simLibPath);
            }
            else if (sdk.Contains("visionos"))
            {
                // Include device libraries
                string deviceLibPath = Path.Combine(pluginsPath, "visionOS");
                AddStaticLibraries(project, frameworkTargetGUID, deviceLibPath);
            }

            // Save the modified project
            project.WriteToFile(projectPath);
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
