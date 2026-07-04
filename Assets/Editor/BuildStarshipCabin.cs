using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace StarshipCabin.EditorTools
{
    public static class BuildStarshipCabin
    {
        public static void BuildAndroidApk()
        {
            StarshipCabinProjectSetup.SetupMvpScene();

            Directory.CreateDirectory("Builds");
            var buildPath = "Builds/StarshipCabin-MVP.apk";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Cabin_Seated_MVP.unity" },
                locationPathName = buildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Console.WriteLine($"Build result: {summary.result}");
            Console.WriteLine($"Build output: {Path.GetFullPath(buildPath)}");
            Console.WriteLine($"Build size: {summary.totalSize}");
            Console.WriteLine($"Build time: {summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {summary.result}");
            }
        }
    }
}

