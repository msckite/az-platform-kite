using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    internal static class GitHubCliHelper
    {
        // Runs `gh auth status` and parses its output into a GitHubContext; never throws
        internal static GitHubContext GetAuthStatus(out string message)
        {
            message = null;
            string output;
            int exitCode;

            try
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = "auth status",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            catch (Win32Exception)
            {
                // gh executable not found on PATH
                message = "Not signed in to GitHub. Install the GitHub CLI ('gh'), then run gh auth login.";
                return new GitHubContext();
            }

            // Initialize context w/order of precedence
            var context = new GitHubContext
            {
                Account = null,
                Host = null,
                Protocol = null,
                TokenScopes = null,
                IsSignedIn = false
            };

            context.IsSignedIn = exitCode == 0;

            var loginMatch = Regex.Match(output, @"Logged in to (\S+) account (\S+)");
            if (loginMatch.Success)
            {
                context.Host = loginMatch.Groups[1].Value;
                context.Account = loginMatch.Groups[2].Value;
            }

            var protocolMatch = Regex.Match(output, @"Git operations protocol:\s*(\S+)");
            if (protocolMatch.Success)
            {
                context.Protocol = protocolMatch.Groups[1].Value;
            }

            var scopesMatch = Regex.Match(output, @"Token scopes:\s*(.+)");
            if (scopesMatch.Success)
            {
                context.TokenScopes = scopesMatch.Groups[1].Value.Trim();
            }

            if (!context.IsSignedIn)
            {
                message = "Not signed in to GitHub. Run gh auth login.";
            }

            return context;
        }

        // Runs `gh auth logout` for the given host/user to drop the cached GitHub credentials for that specific account; never throws
        internal static void SignOut(string host, string user)
        {
            try
            {
                var arguments = "auth logout";
                if (!string.IsNullOrEmpty(host))
                {
                    arguments += $" --hostname {host}";
                }
                if (!string.IsNullOrEmpty(user))
                {
                    arguments += $" --user {user}";
                }

                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit();
                }
            }
            catch (Win32Exception)
            {
                // gh executable not found on PATH; nothing to sign out of
            }
        }
    }
}
