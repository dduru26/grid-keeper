using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GridKeeper.EditorTools
{
    public static class BuildScript
    {
        private const string IOSPath = "Builds/iOS";

        [MenuItem("Grid Keeper/Build/iOS Xcode Project")]
        public static void BuildIOS()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/GridKeeper/Scenes/Main.unity" },
                locationPathName = IOSPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[Grid Keeper] iOS build " + summary.result + " in " + summary.totalTime + ", " + summary.totalErrors + " errors, output " + summary.outputPath);
        }

        [MenuItem("Grid Keeper/Build/iOS Development Build")]
        public static void BuildIOSDevelopment()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/GridKeeper/Scenes/Main.unity" },
                locationPathName = IOSPath,
                target = BuildTarget.iOS,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[Grid Keeper] iOS development build " + report.summary.result + ", output " + report.summary.outputPath);
        }
    }
}
