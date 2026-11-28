using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindowsPlayer
{
    [MenuItem("Tools/Build DanToPhong Windows x64 Release")]
    public static void BuildWindows64Release()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string configuredOutput = Environment.GetEnvironmentVariable("DANTOPHONG_PLAYER_OUTPUT");
        string outputPath = string.IsNullOrWhiteSpace(configuredOutput)
            ? Path.Combine(projectRoot, "BuildGameRelease", "DanToPhong.exe")
            : Path.GetFullPath(configuredOutput);
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");

        if (!string.IsNullOrWhiteSpace(configuredOutput) && File.Exists(outputPath))
            throw new InvalidOperationException($"Refusing to overwrite existing player output: {outputPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("The player output must include a directory."));

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Windows x64 build failed: {report.summary.result}.");
        if (!File.Exists(outputPath))
            throw new InvalidOperationException($"The build reported success but the player executable was not created: {outputPath}.");

        Debug.Log($"Windows x64 player built: {outputPath}");
    }
}
