using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Separate from ReleaseUIBuild: preparing options never starts a build.
public static class WindowsReleaseBuild
{
    public static BuildPlayerOptions CreateOptions()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || Path.GetFileNameWithoutExtension(scenes[0]) != "TitleScene")
            throw new InvalidOperationException("The release must start with TitleScene.");
        if (scenes.Any(scene => !File.Exists(scene))) throw new FileNotFoundException("A build scene is missing.");
        string name = PlayerSettings.productName;
        foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        if (string.IsNullOrWhiteSpace(name)) name = "2DActionGame";
        return new BuildPlayerOptions {
            scenes = scenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Builds/Windows/" + name + ".exe",
            options = BuildOptions.None
        };
    }
    // Call only when release building is explicitly requested.
    public static void Run()
    {
        var options = CreateOptions();
        Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName));
        var report = BuildPipeline.BuildPlayer(options);
        Directory.CreateDirectory("StageAuthoringReports/WindowsRelease");
        File.WriteAllText("StageAuthoringReports/WindowsRelease/build.txt",
            report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nSize: " + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows release build failed.");
        string folder = Path.GetDirectoryName(options.locationPathName);
        File.Copy("Distribution/Windows/README.txt", Path.Combine(folder,"README.txt"), true);
        File.Copy("Assets/Resources/Credits.txt", Path.Combine(folder,"CREDITS.txt"), true);
    }
}
