using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Automated build script for WebGL with proper configuration
/// </summary>
public class WebGLBuildScript
{
    private const string BUILD_PATH = "Builds/WebGL";

    [MenuItem("Build/Build WebGL Production")]
    public static void BuildProduction()
    {
        Debug.Log("🚀 Starting WebGL Production Build...");

        // Configure WebGL settings for production
        ConfigureWebGLSettings(true);

        // Build
        string outputPath = Path.Combine(BUILD_PATH, "Production");
        BuildWebGL(outputPath, BuildOptions.None);

        Debug.Log($"✅ Production build complete: {outputPath}");
    }

    [MenuItem("Build/Build WebGL Development")]
    public static void BuildDevelopment()
    {
        Debug.Log("🔧 Starting WebGL Development Build...");

        // Configure WebGL settings for development
        ConfigureWebGLSettings(false);

        // Build
        string outputPath = Path.Combine(BUILD_PATH, "Development");
        BuildWebGL(outputPath, BuildOptions.Development);

        Debug.Log($"✅ Development build complete: {outputPath}");
    }

    private static void ConfigureWebGLSettings(bool isProduction)
    {
        Debug.Log("⚙️ Configuring WebGL Player Settings...");

        // Compression
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;

        // Memory
        PlayerSettings.WebGL.memorySize = 512; // 512 MB

        // Code optimization
        if (isProduction)
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Master);
            EditorUserBuildSettings.il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSize;
        }
        else
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Debug);
            EditorUserBuildSettings.il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSpeed;
        }

        // Exception handling (smaller build size)
        PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);

        // Code stripping
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Medium);

        // WebGL-specific settings
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;

        Debug.Log("✅ WebGL settings configured");
    }

    private static void BuildWebGL(string outputPath, BuildOptions options)
    {
        // Get all scenes in build settings
        string[] scenes = GetScenesInBuild();

        if (scenes.Length == 0)
        {
            Debug.LogError("❌ No scenes found in Build Settings!");
            return;
        }

        Debug.Log($"📦 Building {scenes.Length} scenes to: {outputPath}");

        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputPath);

        // Build
        BuildReport report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.WebGL, options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✅ Build succeeded: {summary.totalSize / (1024 * 1024)} MB");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"❌ Build failed!");
        }
    }

    private static string[] GetScenesInBuild()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }
        return scenes.ToArray();
    }
}
