using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Shared `gh` CLI process runner, used by every helper that talks to GitHub via `gh api ...`
    internal static class GitHubCliRunner
    {
        // Runs `gh` with the given arguments, quoting each one; never throws (missing gh surfaces as a non-zero exit code)
        internal static string Run(string[] arguments, out int exitCode, out string stdError)
        {
            try
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = string.Join(" ", QuoteAll(arguments)),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    return output;
                }
            }
            catch (Win32Exception)
            {
                exitCode = -1;
                stdError = "GitHub CLI ('gh') was not found on PATH. Install it, then run gh auth login.";
                return null;
            }
        }

        private static IEnumerable<string> QuoteAll(string[] arguments)
        {
            foreach (var argument in arguments)
            {
                yield return QuoteArgument(argument);
            }
        }

        private static string QuoteArgument(string argument)
        {
            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '"', '\t', '\n' }) < 0)
            {
                return argument;
            }

            var builder = new StringBuilder("\"");
            builder.Append(argument.Replace("\"", "\\\""));
            builder.Append('"');
            return builder.ToString();
        }
    }
}
