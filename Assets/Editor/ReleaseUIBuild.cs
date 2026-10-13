using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class ReleaseUIBuild
{
    public static void Run()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = ".repair-checkpoints/ReleaseUIPlayer/2DActionGame.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText("StageAuthoringReports/ReleaseUI/build.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nSize: " + report.summary.totalSize + " bytes\nTime: " + report.summary.totalTime);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
