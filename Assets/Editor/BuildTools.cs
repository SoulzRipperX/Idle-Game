using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildTools
{
    private const string RootBuildFolder = "Builds";
    private static string currentBatchBuildFolder;

    [MenuItem("Tools/Build/Preflight Check")]
    public static void PreflightCheck()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Preflight", "No enabled scenes in Build Settings.", "OK");
            return;
        }

        string message =
            $"Scenes: {scenes.Length}\n" +
            $"Product Name: {PlayerSettings.productName}\n" +
            $"Company Name: {PlayerSettings.companyName}\n" +
            $"Bundle Version: {PlayerSettings.bundleVersion}\n\n" +
            "If Android build fails, check:\n" +
            "- Package Name\n" +
            "- Keystore\n" +
            "- Target API Level";

        EditorUtility.DisplayDialog("Preflight Check", message, "OK");
    }

    [MenuItem("Tools/Build/Build Windows (x64)")]
    public static void BuildWindows()
    {
        string outDir = Path.Combine(GetBuildRootFolder(), "Windows");
        Directory.CreateDirectory(outDir);
        string exePath = Path.Combine(outDir, SanitizeName(PlayerSettings.productName) + ".exe");
        BuildPlayer(BuildTarget.StandaloneWindows64, exePath, BuildOptions.None);
    }

    [MenuItem("Tools/Build/Build WebGL")]
    public static void BuildWebGL()
    {
        string outDir = Path.Combine(GetBuildRootFolder(), "WebGL");
        Directory.CreateDirectory(outDir);
        BuildPlayer(BuildTarget.WebGL, outDir, BuildOptions.None);
    }

    [MenuItem("Tools/Build/Build Android (AAB)")]
    public static void BuildAndroidAab()
    {
        string outDir = Path.Combine(GetBuildRootFolder(), "Android");
        Directory.CreateDirectory(outDir);

        bool oldAab = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = true;
            string aabPath = Path.Combine(outDir, SanitizeName(PlayerSettings.productName) + ".aab");
            BuildPlayer(BuildTarget.Android, aabPath, BuildOptions.None);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = oldAab;
        }
    }

    [MenuItem("Tools/Build/Build All (Windows + WebGL + Android AAB)")]
    public static void BuildAll()
    {
        currentBatchBuildFolder = GetStampedBuildFolder();
        try
        {
            BuildWindows();
            BuildWebGL();
            BuildAndroidAab();
        }
        finally
        {
            currentBatchBuildFolder = null;
        }
    }

    private static void BuildPlayer(BuildTarget target, string outputPath, BuildOptions options)
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes in Build Settings.");
        }

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            target = target,
            locationPathName = outputPath,
            options = options
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build Success",
                $"Target: {target}\nOutput: {outputPath}\nSize: {report.summary.totalSize / (1024f * 1024f):F2} MB",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Build Failed",
                $"Target: {target}\nOutput: {outputPath}\nResult: {report.summary.result}",
                "OK");
        }
    }

    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
    }

    private static string GetStampedBuildFolder()
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        return Path.Combine(RootBuildFolder, stamp);
    }

    private static string GetBuildRootFolder()
    {
        return string.IsNullOrWhiteSpace(currentBatchBuildFolder)
            ? GetStampedBuildFolder()
            : currentBatchBuildFolder;
    }

    private static string SanitizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Game";
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(c, '_');
        }

        return raw.Trim();
    }
}
