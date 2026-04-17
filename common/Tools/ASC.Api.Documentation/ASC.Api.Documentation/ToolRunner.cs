// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.ComponentModel;
using System.Diagnostics;

namespace ASC.Api.Documentation.Commands;

internal static class ToolRunner
{
    public static ValidationResult ValidateAvailable(string toolName, params string[] arguments)
    {
        try
        {
            var result = RunAndCaptureAsync(toolName, arguments, cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();

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

    private static async Task<ToolRunResult> RunAndCaptureAsync(
        string toolName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedTool = ResolveTool(toolName);
        var startInfo = CreateStartInfo(resolvedTool, arguments, workingDirectory);

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new ToolExecutionException(
                    $"Resolved '{toolName}' to '{resolvedTool.Path}', but the process was not created.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            return new ToolRunResult(
                process.ExitCode,
                await outputTask,
                await errorTask,
                resolvedTool.Path);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            throw new ToolExecutionException(
                $"Resolved '{toolName}' to '{resolvedTool.Path}', but it could not be started: {ex.Message}",
                ex);
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        ResolvedTool resolvedTool,
        IReadOnlyList<string> arguments,
        string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedTool.RequiresCommandShell ? "cmd.exe" : resolvedTool.Path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        if (resolvedTool.RequiresCommandShell)
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

            return new ResolvedTool(fullPath, RequiresCommandShell(fullPath));
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

                return new ResolvedTool(candidate, RequiresCommandShell(candidate));
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

    private static bool RequiresCommandShell(string path)
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

    private sealed record ResolvedTool(string Path, bool RequiresCommandShell);

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
