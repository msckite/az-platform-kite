using System.ComponentModel;
using System.Diagnostics;

namespace MSCKite.Azure.Platform.Internal.Common
{
    internal static class GitRepositoryHelper
    {
        // Runs `git clone --depth 1 --branch <branch> <url> <destinationPath>`; never throws
        internal static bool Clone(string repositoryUrl, string branch, string destinationPath, out string error)
        {
            error = null;

            try
            {
                var arguments = $"clone --depth 1 --branch \"{branch}\" \"{repositoryUrl}\" \"{destinationPath}\"";

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
                    var standardOutput = process.StandardOutput.ReadToEnd();
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
