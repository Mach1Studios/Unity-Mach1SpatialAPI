#if UNITY_2023_3_OR_NEWER || UNITY_2022_3
#define VISION_OS_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildPreProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("MyCustomBuildProcessor.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
        Debug.Log($"[BuildPreProcessor] report.summary.platform: {report.summary.platform}");
        SetRuntimePluginCopyDelegate(report.summary.platform);
    }

    static void SetRuntimePluginCopyDelegate(BuildTarget platform)
    {
        var allPlugins = PluginImporter.GetImporters(platform);
        Debug.Log($"[BuildPreProcessor] allPlugins: {allPlugins}");
        foreach (var plugin in allPlugins)
        {
            bool isMach1 = plugin.assetPath.Contains("Mach1/Plugins");
            Debug.Log($"[BuildPreProcessor] plugin.assetPath: {plugin.assetPath}, isNative? {plugin.isNativePlugin}, isMach1? {isMach1}");

            if (plugin.isNativePlugin
                && plugin.assetPath.Contains("Mach1/Plugins")
                )
            {
                switch (platform)
                {
#if VISION_OS_SUPPORTED
                    case BuildTarget.VisionOS:
#endif
                        Debug.Log($"[BuildPreProcessor] plugin.assetPath: {plugin.assetPath}");
                        plugin.SetIncludeInBuildDelegate(IncludeAppleLibraryInBuild);
                        break;
                }
            }
        }
    }

    static bool IsSimulatorBuild(BuildTarget platformGroup)
    {
        Debug.Log($"[BuildPreProcessor] PlatformGroup: {platformGroup}");
        switch (platformGroup)
        {
#if VISION_OS_SUPPORTED
            case BuildTarget.VisionOS:
                return PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Simulator;
#endif
        }

        return false;
    }

    static bool IncludeAppleLibraryInBuild(string path)
    {
        var isSimulatorLibrary = IsAppleSimulatorLibrary(path);
        var isSimulatorBuild = IsSimulatorBuild(EditorUserBuildSettings.activeBuildTarget);
        Debug.Log($"[BuildPreProcessor] isSimulatorLibrary: {isSimulatorBuild}");
        return isSimulatorLibrary == isSimulatorBuild;
    }

    public static bool IsAppleSimulatorLibrary(string assetPath)
    {
        var parent = new DirectoryInfo(assetPath).Parent;
        Debug.Log($"[BuildPreProcessor] Library SDK Platform: {parent?.Name}");

        switch (parent?.Name)
        {
            case "Simulator":
                return true;
            case "Device":
                return false;
            default:
                throw new InvalidDataException(
                    $@"Could not determine SDK type of library ""{assetPath}"". " +
                    @"Apple visionOS native libraries have to be placed in a folder named ""Device"" " +
                    @"or ""Simulator"" for implicit SDK type detection."
                );
        }
    }
}