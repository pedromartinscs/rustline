using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Rustline.Diagnostics.Benchmarking;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rustline.Editor
{
    public static class RustlineBenchmarkBuild
    {
        private const string MenuRoot = "Tools/Rustline/Performance Benchmark/";
        private const string BenchmarkScene = "Assets/Scenes/MovementLab.unity";
        private const string RelativeExecutablePath =
            "Builds/Performance/RustlineBenchmark.exe";
        private const string BenchmarkDefine = "RUSTLINE_BENCHMARK";
        private static readonly string[] BuildSideEffectPaths =
        {
            "Assets/Settings/UniversalRP.asset",
            "Assets/UniversalRenderPipelineGlobalSettings.asset",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/UnityConnectSettings.asset",
            "Assets/Resources.meta"
        };

        [MenuItem(MenuRoot + "Build Windows Benchmark")]
        public static void BuildWindowsBenchmark()
        {
            BuildBenchmarkPlayer();
        }

        [MenuItem(MenuRoot + "Build & Run Penumbra A/B")]
        public static void BuildAndRunPenumbraAb()
        {
            Launch(BuildBenchmarkPlayer(), "penumbra-ab");
        }

        [MenuItem(MenuRoot + "Build & Run A/A Control (Penumbra OFF)")]
        public static void BuildAndRunControlOff()
        {
            Launch(BuildBenchmarkPlayer(), "control-off");
        }

        [MenuItem(MenuRoot + "Build & Run Short Smoke")]
        public static void BuildAndRunShortSmoke()
        {
            string executablePath = BuildBenchmarkPlayer();
            Launch(
                executablePath,
                "control-off",
                "--benchmark-warmup-seconds 1 " +
                "--benchmark-settle-seconds 0.25 " +
                "--benchmark-block-seconds 1 " +
                "--benchmark-pairs 1 --benchmark-diagnostics --benchmark-auto-quit");
        }

        public static void BuildWindowsBenchmarkCommandLine()
        {
            BuildBenchmarkPlayer();
        }

        private static string BuildBenchmarkPlayer()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new BuildFailedException("Could not resolve the Rustline project root.");
            }

            string executablePath = Path.GetFullPath(
                Path.Combine(projectRoot, RelativeExecutablePath));
            string outputDirectory = Path.GetDirectoryName(executablePath);
            Directory.CreateDirectory(outputDirectory);
            BenchmarkBuildMetadata metadata = CaptureGitMetadata(projectRoot);
            BuildSourceSnapshot sourceSnapshot = BuildSourceSnapshot.Capture(projectRoot);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { BenchmarkScene },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { BenchmarkDefine }
            };

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                sourceSnapshot.Restore();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Rustline benchmark build failed: {report.summary.result}, " +
                    $"{report.summary.totalErrors} errors.");
            }

            string metadataPath = Path.Combine(
                outputDirectory,
                BenchmarkReportWriter.BuildMetadataFileName);
            File.WriteAllText(
                metadataPath,
                JsonUtility.ToJson(metadata, true) + Environment.NewLine);

            UnityEngine.Debug.Log(
                $"Rustline Windows benchmark built at {executablePath}\n" +
                $"Commit {metadata.gitCommit} ({metadata.gitDirtyState})\n" +
                "Build options: Development, no profiler auto-connect, no script debugging.");
            return executablePath;
        }

        private static BenchmarkBuildMetadata CaptureGitMetadata(string projectRoot)
        {
            string commit = RunGit(projectRoot, "rev-parse HEAD");
            string status = RunGit(projectRoot, "status --porcelain");
            return new BenchmarkBuildMetadata
            {
                gitCommit = string.IsNullOrWhiteSpace(commit) ? "unknown" : commit.Trim(),
                gitDirtyState = status == null
                    ? "unknown"
                    : (string.IsNullOrWhiteSpace(status) ? "clean" : "dirty")
            };
        }

        private static string RunGit(string workingDirectory, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return null;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(5000) || process.ExitCode != 0)
                    {
                        return null;
                    }

                    return output;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void Launch(
            string executablePath,
            string mode,
            string additionalArguments = null)
        {
            BenchmarkBuildMetadata metadata = CaptureGitMetadata(
                Directory.GetParent(Application.dataPath)?.FullName);
            string arguments =
                $"{BenchmarkOptions.ActivationFlag} --benchmark-mode {mode} " +
                $"--benchmark-git-commit {metadata.gitCommit} " +
                $"--benchmark-git-dirty {metadata.gitDirtyState}";
            if (!string.IsNullOrWhiteSpace(additionalArguments))
            {
                arguments += " " + additionalArguments;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false
            };
            Process.Start(startInfo);
        }

        private sealed class BuildSourceSnapshot
        {
            private readonly string _projectRoot;
            private readonly Dictionary<string, byte[]> _existingFiles;
            private readonly HashSet<string> _missingFiles;
            private readonly bool _resourcesDirectoryExisted;

            private BuildSourceSnapshot(
                string projectRoot,
                Dictionary<string, byte[]> existingFiles,
                HashSet<string> missingFiles,
                bool resourcesDirectoryExisted)
            {
                _projectRoot = projectRoot;
                _existingFiles = existingFiles;
                _missingFiles = missingFiles;
                _resourcesDirectoryExisted = resourcesDirectoryExisted;
            }

            public static BuildSourceSnapshot Capture(string projectRoot)
            {
                Dictionary<string, byte[]> existingFiles =
                    new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> missingFiles =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < BuildSideEffectPaths.Length; index++)
                {
                    string relativePath = BuildSideEffectPaths[index];
                    string absolutePath = Path.Combine(projectRoot, relativePath);
                    if (File.Exists(absolutePath))
                    {
                        existingFiles.Add(relativePath, File.ReadAllBytes(absolutePath));
                    }
                    else
                    {
                        missingFiles.Add(relativePath);
                    }
                }

                return new BuildSourceSnapshot(
                    projectRoot,
                    existingFiles,
                    missingFiles,
                    Directory.Exists(Path.Combine(projectRoot, "Assets/Resources")));
            }

            public void Restore()
            {
                foreach (KeyValuePair<string, byte[]> entry in _existingFiles)
                {
                    File.WriteAllBytes(Path.Combine(_projectRoot, entry.Key), entry.Value);
                }

                foreach (string relativePath in _missingFiles)
                {
                    string absolutePath = Path.Combine(_projectRoot, relativePath);
                    if (File.Exists(absolutePath))
                    {
                        File.Delete(absolutePath);
                    }
                }

                string resourcesDirectory = Path.Combine(_projectRoot, "Assets/Resources");
                if (!_resourcesDirectoryExisted &&
                    Directory.Exists(resourcesDirectory) &&
                    Directory.GetFileSystemEntries(resourcesDirectory).Length == 0)
                {
                    Directory.Delete(resourcesDirectory);
                }
            }
        }
    }
}
