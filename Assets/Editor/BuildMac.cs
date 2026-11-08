using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public static class BuildMac
    {
        // Usage: Unity -batchmode -quit -projectPath . -executeMethod Assets.Editor.BuildMac.Build -buildOutput /path/Open76.app
        public static void Build()
        {
            string output = "Build/Open76.app";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildOutput")
                {
                    output = args[i + 1];
                }
            }

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "arm64");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });

            Debug.Log("Build result: " + report.summary.result + ", errors: " + report.summary.totalErrors + ", output: " + output);
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
