using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Temporary build verification, removed after the folder migration.
[InitializeOnLoad]
public static class ProjectStructureMigration
{
    private const string Work = "Temp/AssetOrganization";
    static ProjectStructureMigration()
    {
        if (File.Exists(Work + "/build.request"))
            EditorApplication.delayCall += RunRequestedBuild;
    }

    private static void RunRequestedBuild()
    {
        if (!File.Exists(Work + "/build.request")) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedBuild;
            return;
        }
        File.Move(Work + "/build.request", Work + "/build.running");
        try { Build(); }
        catch (Exception error)
        {
            File.WriteAllText(Work + "/build-error.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    private static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 3 || scenes.Any(p => !File.Exists(p)))
            throw new InvalidOperationException("Expected three existing production scenes.");
        var settings = Directory.GetFiles("Assets/_Game/Settings/Rendering", "*.asset", SearchOption.AllDirectories)
            .Concat(new[] { "ProjectSettings/ProjectSettings.asset", "ProjectSettings/SceneTemplateSettings.json" })
            .ToDictionary(p => p, File.ReadAllBytes);
        try
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.GetFullPath(Work + "/Build/DemonsArcade.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            File.WriteAllText(Work + "/build-result.txt", report.summary.result + "\n" +
                "Errors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings +
                "\nBytes: " + report.summary.totalSize + "\nScenes:\n" + string.Join("\n", scenes));
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + report.summary.result);
            Debug.Log("[Project Organization] PASS: full Windows build succeeded.");
        }
        finally { foreach (var entry in settings) File.WriteAllBytes(entry.Key, entry.Value); }
    }
}
