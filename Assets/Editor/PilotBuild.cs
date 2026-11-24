using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-off pilot build entry point. Delete after the APK is produced.
/// </summary>
public static class PilotBuild
{
    /// <summary>Entry point for -executeMethod.</summary>
    public static void Run()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Debug.Log("[PILOT] Scene in build: " + string.Join(", ", scenes));
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Sicurezza-Immersiva-81_ver1.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[PILOT] Build {report.summary.result} size={report.summary.totalSize} errors={report.summary.totalErrors}");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Build fallita: " + report.summary.result);
    }
}
