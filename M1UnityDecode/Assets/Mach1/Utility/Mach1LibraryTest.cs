using UnityEngine;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Test script to diagnose Mach1 library loading issues
/// Add this to a GameObject in your scene to run diagnostics
/// </summary>
public class Mach1LibraryTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTestOnStart = true;
    public bool enableDebugOutput = true;

    void Start()
    {
        if (runTestOnStart)
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("Run Mach1 Library Diagnostics")]
    public void RunDiagnostics()
    {
        Debug.Log("=== Mach1 Library Diagnostics ===");
        
        // Test 1: Platform and Architecture
        TestPlatformInfo();
        
        // Test 2: Basic Mach1Decode
        TestMach1Decode();
        
        // Test 3: Mach1DecodePositional
        TestMach1DecodePositional();
        
        // Test 4: Direct P/Invoke test
        TestDirectPInvoke();
        
        Debug.Log("=== Diagnostics Complete ===");
    }

    void TestPlatformInfo()
    {
        Debug.Log("--- Platform Information ---");
        Debug.Log($"Unity Platform: {Application.platform}");
        Debug.Log($"Unity Editor: {Application.isEditor}");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        
#if UNITY_EDITOR
        Debug.Log("Running in Unity Editor");
#if UNITY_EDITOR_WIN
        Debug.Log("Windows Editor");
#elif UNITY_EDITOR_OSX
        Debug.Log("macOS Editor");
#elif UNITY_EDITOR_LINUX
        Debug.Log("Linux Editor");
#endif
#endif

#if UNITY_STANDALONE_WIN
        Debug.Log("Windows Standalone Build");
#elif UNITY_STANDALONE_OSX
        Debug.Log("macOS Standalone Build");
#elif UNITY_STANDALONE_LINUX
        Debug.Log("Linux Standalone Build");
#endif
    }

    void TestMach1Decode()
    {
        Debug.Log("--- Testing Mach1Decode ---");
        try
        {
            var decode = new Mach1.Mach1Decode();
            Debug.Log("✅ Mach1Decode created successfully");
            
            // Test basic functionality
            decode.setPlatformType(Mach1.Mach1PlatformType.Mach1PlatformUnity);
            decode.setDecodeMode(Mach1.Mach1DecodeMode.M1DecodeSpatial_8);
            
            Debug.Log("✅ Mach1Decode basic methods work");
            
            decode.Dispose();
            Debug.Log("✅ Mach1Decode disposed successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Mach1Decode failed: {e.GetType().Name}: {e.Message}");
            if (e.InnerException != null)
            {
                Debug.LogError($"Inner Exception: {e.InnerException.Message}");
            }
        }
    }

    void TestMach1DecodePositional()
    {
        Debug.Log("--- Testing Mach1DecodePositional ---");
        try
        {
            var positional = new Mach1.Mach1DecodePositional();
            Debug.Log("✅ Mach1DecodePositional created successfully");
            
            // Test basic functionality
            positional.setPlatformType(Mach1.Mach1PlatformType.Mach1PlatformUnity);
            positional.setDecodeMode(Mach1.Mach1DecodeMode.M1DecodeSpatial_8);
            
            int channelCount = positional.getFormatChannelCount();
            int coeffCount = positional.getFormatCoeffCount();
            
            Debug.Log($"✅ Channel Count: {channelCount}, Coeff Count: {coeffCount}");
            
            positional.Dispose();
            Debug.Log("✅ Mach1DecodePositional disposed successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Mach1DecodePositional failed: {e.GetType().Name}: {e.Message}");
            if (e.InnerException != null)
            {
                Debug.LogError($"Inner Exception: {e.InnerException.Message}");
            }
        }
    }

    void TestDirectPInvoke()
    {
        Debug.Log("--- Testing Direct P/Invoke ---");
        
        // Test if we can call the create function directly
        try
        {
            IntPtr ptr = Mach1DecodePositionalCAPI_create();
            if (ptr != IntPtr.Zero)
            {
                Debug.Log("✅ Direct P/Invoke create succeeded");
                Mach1DecodePositionalCAPI_delete(ptr);
                Debug.Log("✅ Direct P/Invoke delete succeeded");
            }
            else
            {
                Debug.LogError("❌ Direct P/Invoke create returned null pointer");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Direct P/Invoke failed: {e.GetType().Name}: {e.Message}");
        }
    }

    // Direct P/Invoke declarations for testing
#if (UNITY_IOS || UNITY_VISIONOS || UNITY_TVOS) && !UNITY_EDITOR
    private const string libname = "__Internal";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID
    private const string libname = "Mach1DecodePositionalCAPI";
#else
    private const string libname = "libMach1DecodePositionalCAPI";
#endif

    [DllImport(libname)]
    private static extern IntPtr Mach1DecodePositionalCAPI_create();

    [DllImport(libname)]
    private static extern void Mach1DecodePositionalCAPI_delete(IntPtr M1obj);
}