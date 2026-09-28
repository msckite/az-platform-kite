using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MSCKite.Azure.Platform.Internal.Common
{
    internal static class GitRepositoryHelper
    {
        // Stable release tags only (e.g. "v1.1.0"); prereleases such as "v1.1.0-prev1" are never picked as "latest"
        private static readonly Regex StableReleaseTagPattern = new Regex(@"^v(\d+)\.(\d+)\.(\d+)$");

        // Runs `git clone --depth 1 --branch <branch> <url> <destinationPath>`; <branch> may also be a tag. Never throws
        internal static bool Clone(string repositoryUrl, string branch, string destinationPath, out string error)
        {
            return Run($"clone --depth 1 --branch \"{branch}\" \"{repositoryUrl}\" \"{destinationPath}\"", out _, out error);
        }

        // Returns the highest stable release tag (e.g. "v1.1.0") of the remote repository, or null with an error; never throws
        internal static string GetLatestReleaseTag(string repositoryUrl, out string error)
        {
            if (!Run($"ls-remote --tags --refs \"{repositoryUrl}\"", out var output, out error))
            {
                return null;
            }

            string latestTag = null;
            Version latestVersion = null;
            foreach (var line in output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Each line is "<sha>\trefs/tags/<tag>"
                var tag = line.Trim().Substring(line.Trim().LastIndexOf('/') + 1);
                var match = StableReleaseTagPattern.Match(tag);
                if (!match.Success)
                {
                    continue;
                }

                var version = new Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
                if (latestVersion == null || version > latestVersion)
                {
                    latestVersion = version;
                    latestTag = tag;
                }
            }

            if (latestTag == null)
            {
                error = $"No stable release tag (vX.Y.Z) found in '{repositoryUrl}'.";
            }

            return latestTag;
        }

        internal static bool IsReleaseTag(string reference)
        {
            return reference != null && Regex.IsMatch(reference, @"^v\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$");
        }

        private static bool Run(string arguments, out string standardOutput, out string error)
        {
            standardOutput = null;
            error = null;

            try
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    standardOutput = process.StandardOutput.ReadToEnd();
                    var standardError = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        error = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
                        return false;
                    }

                    return true;
                }
            }
            catch (Win32Exception)
            {
                // git executable not found on PATH
                error = "Git executable not found on PATH. Install Git and ensure 'git' is available.";
                return false;
            }
        }
    }
}
