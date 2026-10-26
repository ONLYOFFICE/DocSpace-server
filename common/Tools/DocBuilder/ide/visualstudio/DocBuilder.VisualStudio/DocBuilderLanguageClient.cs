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

namespace DocBuilder.VisualStudio;

/// <summary>
/// Starts the bundled language server (server\docbuilder-language-server.cjs) under Node.js when the
/// first .docbuilder file opens, and keeps its connection for the preview command.
/// </summary>
[Export(typeof(ILanguageClient))]
[ContentType(DocBuilderContentDefinition.ContentTypeName)]
[RunOnContext(RunningContext.RunOnHost)]
public sealed class DocBuilderLanguageClient : ILanguageClient, ILanguageClientCustomMessage2
{
    public static DocBuilderLanguageClient? Current { get; private set; }

    /// <summary>The connection to the server, set once it is initialized.</summary>
    public JsonRpc? Rpc { get; private set; }

    public string Name => "DocBuilder Scripts";

    public IEnumerable<string>? ConfigurationSections => null;

    public object? InitializationOptions { get; private set; }

    public IEnumerable<string>? FilesToWatch => ["**/*.docbuilder"];

    public bool ShowNotificationOnInitializeFailed => true;

    public object? MiddleLayer => null;

    /// <summary>Receives the server's own notifications (the assistant's progress).</summary>
    public object? CustomMessageTarget { get; } = new ServerNotifications();

    public event AsyncEventHandler<EventArgs>? StartAsync;

    public event AsyncEventHandler<EventArgs>? StopAsync;

    public async Task<Connection?> ActivateAsync(CancellationToken token)
    {
        Current = this;
        var options = await DocBuilderPackage.GetOptionsAsync();
        InitializationOptions = new
        {
            builderPath = NullIfEmpty(options?.BuilderPath),
            samplesFolder = NullIfEmpty(options?.SamplesFolder),
            documentServerUrl = NullIfEmpty(options?.DocumentServerUrl),
            documentServerSecret = NullIfEmpty(options?.DocumentServerSecret),
            fileHost = NullIfEmpty(options?.FileHost),
            portalUrl = NullIfEmpty(options?.PortalUrl),
            portalApiKey = NullIfEmpty(options?.PortalApiKey),
            profileId = NullIfEmpty(options?.ProfileId),
        };

        var server = Path.Combine(Path.GetDirectoryName(typeof(DocBuilderLanguageClient).Assembly.Location)!, "server", "docbuilder-language-server.cjs");
        var node = FindNode(options?.NodePath);
        if (node == null)
        {
            throw new InvalidOperationException("Node.js not found: install it or set its path in Tools > Options > DocBuilder Scripts.");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(node, $"\"{server}\" --stdio")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                Debug.WriteLine($"[docbuilder-language-server] {e.Data}");
            }
        };

        if (!process.Start())
        {
            return null;
        }

        process.BeginErrorReadLine();
        return new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
    }

    public async Task OnLoadedAsync()
    {
        if (StartAsync != null)
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }
    }

    public Task OnServerInitializedAsync() => Task.CompletedTask;

    public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState) =>
        Task.FromResult<InitializationFailureContext?>(new InitializationFailureContext
        {
            FailureMessage = $"The DocBuilder language server did not start: {initializationState.InitializationException?.Message}",
        });

    public Task AttachForCustomMessageAsync(JsonRpc rpc)
    {
        Rpc = rpc;
        return Task.CompletedTask;
    }

    /// <summary>Stops the server; Visual Studio starts it again with the next .docbuilder file.</summary>
    public async Task RestartAsync()
    {
        if (StopAsync != null)
        {
            await StopAsync.InvokeAsync(this, EventArgs.Empty);
        }

        if (StartAsync != null)
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }
    }

    // The configured one, node on PATH, then the Node.js Visual Studio installs for its web tooling.
    private static string? FindNode(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        var onPath = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator)
            .Where(dir => !string.IsNullOrWhiteSpace(dir))
            .Select(dir => Path.Combine(dir.Trim('"'), "node.exe"))
            .FirstOrDefault(File.Exists);
        if (onPath != null)
        {
            return onPath;
        }

        var ide = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
        var bundled = Path.GetFullPath(Path.Combine(ide, "..", "..", "MSBuild", "Microsoft", "VisualStudio", "NodeJs", "node.exe"));
        return File.Exists(bundled) ? bundled : null;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
