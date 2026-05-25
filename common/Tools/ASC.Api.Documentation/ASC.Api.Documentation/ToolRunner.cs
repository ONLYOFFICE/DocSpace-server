// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Api.Documentation;

internal static class ToolRunner
{
    public static ValidationResult ValidateAvailable(string toolName, params string[] arguments)
    {
        try
        {
            var result = RunAndCapture(toolName, arguments);

            if (result.ExitCode == 0)
            {
                return ValidationResult.Success();
            }

            return ValidationResult.Error(BuildExitCodeMessage(toolName, result));
        }
        catch (ToolExecutionException ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }

    private static ToolRunResult RunAndCapture(
        string toolName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null)
    {
        var resolvedTool = ResolveTool(toolName);
        var startInfo = CreateStartInfo(resolvedTool, arguments, workingDirectory);

        try
        {
            using var process = new Process();
            process.StartInfo = startInfo;

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();

            using var outputCompleted = new ManualResetEventSlim(false);
            using var errorCompleted = new ManualResetEventSlim(false);

            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    outputCompleted.Set();
                    return;
                }

                standardOutput.AppendLine(eventArgs.Data);
            };

            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    errorCompleted.Set();
                    return;
                }

                standardError.AppendLine(eventArgs.Data);
            };

            if (!process.Start())
            {
                throw new ToolExecutionException(
                    $"Resolved '{toolName}' to '{resolvedTool.Path}', but the process was not created.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            outputCompleted.Wait();
            errorCompleted.Wait();

            return new ToolRunResult(
                process.ExitCode,
                standardOutput.ToString(),
                standardError.ToString(),
                resolvedTool.Path);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            throw new ToolExecutionException(
                $"Resolved '{toolName}' to '{resolvedTool.Path}', but it could not be started: {ex.Message}",
                ex);
        }
    }

    public static async Task<int> RunAndWriteAsync(
        string toolName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken,
        string? startFailureMessage = null)
    {
        try
        {
            var resolvedTool = ResolveTool(toolName);
            var startInfo = CreateStartInfo(resolvedTool, arguments, workingDirectory);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new ToolExecutionException(
                    $"Resolved '{toolName}' to '{resolvedTool.Path}', but the process was not created.");
            }

            var outputTask = PumpOutputAsync(
                process.StandardOutput,
                line => AnsiConsole.WriteLine(line),
                cancellationToken);

            var errorTask = PumpOutputAsync(
                process.StandardError,
                WriteStandardErrorLine,
                cancellationToken);

            await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync(cancellationToken));
            return process.ExitCode;
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            var message = startFailureMessage ?? $"Failed to start '{toolName}'.";
            AnsiConsole.MarkupLine($"[red]{Markup.Escape($"{message} {ex.Message}")}[/]");
            return -1;
        }
        catch (ToolExecutionException ex)
        {
            var message = startFailureMessage ?? $"Failed to start '{toolName}'.";
            AnsiConsole.MarkupLine($"[red]{Markup.Escape($"{message} {ex.Message}")}[/]");
            return -1;
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        ResolvedTool resolvedTool,
        IReadOnlyList<string> arguments,
        string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedTool.IsCommandShellRequired ? "cmd.exe" : resolvedTool.Path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        if (resolvedTool.IsCommandShellRequired)
        {
            startInfo.Arguments = $"/d /s /c {BuildCommandShellInvocation(resolvedTool.Path, arguments)}";
            return startInfo;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static ResolvedTool ResolveTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ToolExecutionException("Tool name cannot be empty.");
        }

        if (HasPathSeparators(toolName) || Path.IsPathRooted(toolName))
        {
            var fullPath = Path.GetFullPath(toolName);
            if (!File.Exists(fullPath))
            {
                throw new ToolExecutionException($"Tool path '{fullPath}' does not exist.");
            }

            return new ResolvedTool(fullPath, CheckRequiresCommandShell(fullPath));
        }

        var pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawPathEntry in pathEntries)
        {
            var pathEntry = rawPathEntry.Trim('"');
            if (string.IsNullOrWhiteSpace(pathEntry))
            {
                continue;
            }

            foreach (var suffix in GetCandidateSuffixes(toolName))
            {
                var candidate = Path.Combine(pathEntry, toolName + suffix);
                if (!File.Exists(candidate))
                {
                    continue;
                }

                return new ResolvedTool(candidate, CheckRequiresCommandShell(candidate));
            }
        }

        throw new ToolExecutionException($"Tool '{toolName}' was not found in PATH.");
    }

    private static IEnumerable<string> GetCandidateSuffixes(string toolName)
    {
        if (!OperatingSystem.IsWindows())
        {
            yield return string.Empty;
            yield break;
        }

        if (Path.HasExtension(toolName))
        {
            yield return string.Empty;
            yield break;
        }

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        var suffixes = string.IsNullOrWhiteSpace(pathExt)
            ? [".com", ".exe", ".bat", ".cmd"]
            : pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var suffix in suffixes)
        {
            yield return suffix;
        }
    }

    private static bool HasPathSeparators(string value)
    {
        return value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar);
    }

    private static bool CheckRequiresCommandShell(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCommandShellInvocation(string executablePath, IReadOnlyList<string> arguments)
    {
        var quotedExecutable = QuoteForCommandShell(executablePath);
        var quotedArguments = string.Join(" ", arguments.Select(QuoteForCommandShell));

        return string.IsNullOrWhiteSpace(quotedArguments)
            ? $"\"{quotedExecutable}\""
            : $"\"{quotedExecutable} {quotedArguments}\"";
    }

    private static string QuoteForCommandShell(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var escaped = value
            .Replace("%", "%%")
            .Replace("\"", "\"\"");

        var needsQuotes = escaped.Any(ch => char.IsWhiteSpace(ch) || "&()[]{}^=;!'+,`~|<>".Contains(ch));
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private static string BuildExitCodeMessage(string toolName, ToolRunResult result)
    {
        var details = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;

        details = details.Trim();

        return string.IsNullOrWhiteSpace(details)
            ? $"'{toolName}' exited with code {result.ExitCode} (resolved to '{result.ResolvedPath}')."
            : $"'{toolName}' exited with code {result.ExitCode} (resolved to '{result.ResolvedPath}'): {details}";
    }

    private static async Task PumpOutputAsync(
        StreamReader reader,
        Action<string> writeLine,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            writeLine(line);
        }
    }

    private static void WriteStandardErrorLine(string line)
    {
        var style = GetStandardErrorStyle(line);
        if (style is null)
        {
            AnsiConsole.WriteLine(line);
            return;
        }

        AnsiConsole.MarkupLine($"[{style}]{Markup.Escape(line)}[/]");
    }

    private static string? GetStandardErrorStyle(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        if (ContainsAny(line, "error", "fatal", "err!", "exception", "failed", "failure"))
        {
            return "red";
        }

        if (ContainsAny(line, "warn", "warning", "notice"))
        {
            return "yellow";
        }

        return null;
    }

    private static bool ContainsAny(string line, params string[] markers)
    {
        return markers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record ResolvedTool(string Path, bool IsCommandShellRequired);

    private sealed record ToolRunResult(int ExitCode, string StandardOutput, string StandardError, string ResolvedPath);

    private sealed class ToolExecutionException : Exception
    {
        public ToolExecutionException(string message)
            : base(message)
        {
        }

        public ToolExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
