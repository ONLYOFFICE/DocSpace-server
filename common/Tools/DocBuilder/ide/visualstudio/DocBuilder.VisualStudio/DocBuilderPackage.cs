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
/// The preview commands, the preview window, the options page and run-on-save. Loads in the
/// background as soon as a .docbuilder file is the active document.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(PreviewToolWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
[ProvideOptionPage(typeof(DocBuilderOptions), "DocBuilder Scripts", "General", 0, 0, true)]
[ProvideAutoLoad(DocBuilderActiveContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideUIContextRule(DocBuilderActiveContextGuid,
    name: "DocBuilder script active",
    expression: "DocBuilder",
    termNames: ["DocBuilder"],
    termValues: ["ActiveEditorContentType:" + DocBuilderContentDefinition.ContentTypeName])]
public sealed class DocBuilderPackage : AsyncPackage, IVsRunningDocTableEvents3
{
    public const string PackageGuidString = "6b6f0a4e-3c3b-4d7e-9b0c-2d6e7a1f5c41";
    private const string DocBuilderActiveContextGuid = "c4f1e9a7-5d2b-4e83-a6f0-91b7d3c28e54";

    private static readonly Guid _commandSet = new("a3c5d8e2-1f47-4b9a-8e36-7c0d5b2e9f18");
    private const int RunPreviewId = 0x0100;
    private const int ShowPreviewId = 0x0101;

    private IVsRunningDocumentTable? _documents;
    private uint _documentEventsCookie;
    private string? _lastTemplate;

    public static DocBuilderPackage? Instance { get; private set; }

    /// <summary>The options, loading the package first when the language client needs them before it is.</summary>
    public static async Task<DocBuilderOptions?> GetOptionsAsync()
    {
        if (Instance == null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsShell)) is IVsShell shell)
            {
                var guid = new Guid(PackageGuidString);
                shell.LoadPackage(ref guid, out _);
            }
        }

        return Instance?.GetDialogPage(typeof(DocBuilderOptions)) as DocBuilderOptions;
    }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        Instance = this;

        if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commands)
        {
            var run = new OleMenuCommand((_, _) => RunActive(), new CommandID(_commandSet, RunPreviewId));
            run.BeforeQueryStatus += (_, _) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                run.Visible = run.Enabled = IsTemplate(ActiveDocumentPath());
            };
            commands.AddCommand(run);
            commands.AddCommand(new OleMenuCommand((_, _) => ShowWindow(), new CommandID(_commandSet, ShowPreviewId)));
        }

        _documents = await GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        _documents?.AdviseRunningDocTableEvents(this, out _documentEventsCookie);
    }

    protected override void Dispose(bool disposing)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (disposing && _documents != null && _documentEventsCookie != 0)
        {
            _documents.UnadviseRunningDocTableEvents(_documentEventsCookie);
            _documentEventsCookie = 0;
        }

        base.Dispose(disposing);
    }

    /// <summary>Runs the active template, or the last one when the preview window has the focus.</summary>
    public void RunActive()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var path = ActiveDocumentPath();
        if (IsTemplate(path))
        {
            _lastTemplate = path;
        }

        if (_lastTemplate != null)
        {
            Run(_lastTemplate);
        }
    }

    /// <summary>Runs a template with the scenario the preview window has chosen for it.</summary>
    public void Run(string template)
    {
        _lastTemplate = template;
        JoinableTaskFactory.RunAsync(async () =>
        {
            var view = await ShowWindowAsync();
            if (view == null)
            {
                return;
            }

            // The sample may have changed since the last run, so the list is read again every time.
            var scenarios = await GetScenariosAsync(template);
            await view.ShowScenariosAsync(template, scenarios);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var scenario = view.Scenario;
            view.ShowRunning(template, scenario);
            var (result, error) = await ExecuteAsync<PreviewResult>("docbuilder.run", new { uri = new Uri(template).AbsoluteUri, scenario });
            await view.ShowResultAsync(template, result ?? new PreviewResult { Error = new PreviewError { Message = error ?? "" } });
        }).FileAndForget("docbuilder/preview/run");
    }

    // ---------- the assistant ----------

    /// <summary>Asks the assistant about a template; the open editor's text is what it works on.</summary>
    /// <param name="edits">The edited copy whose hand edits the template is to take over.</param>
    public static async Task<AssistantAnswer> AskAssistantAsync(string template, string prompt, string? scenario, string? lastError, string? edits = null)
    {
        var (answer, error) = await ExecuteAsync<AssistantAnswer>("docbuilder.assistant.ask",
            new { uri = new Uri(template).AbsoluteUri, prompt, scenario, lastError, edits });
        return answer ?? new AssistantAnswer { Error = new PreviewError { Message = error ?? "" } };
    }

    public static async Task ResetAssistantAsync(string template) =>
        await ExecuteAsync<JToken>("docbuilder.assistant.reset", new { uri = new Uri(template).AbsoluteUri });

    public static async Task CancelAssistantAsync(string template) =>
        await ExecuteAsync<JToken>("docbuilder.assistant.cancel", new { uri = new Uri(template).AbsoluteUri });

    /// <summary>Shows the template against a proposed text in the Visual Studio diff window.</summary>
    public void ShowDiff(string template, string proposed)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var folder = Path.Combine(Path.GetTempPath(), "docbuilder-assistant");
        Directory.CreateDirectory(folder);
        var name = Path.GetFileNameWithoutExtension(template);
        var current = Path.Combine(folder, $"{name}.current.docbuilder");
        var proposal = Path.Combine(folder, $"{name}.proposed.docbuilder");
        File.WriteAllText(current, CurrentText(template) ?? "");
        File.WriteAllText(proposal, proposed);

        if (GetService(typeof(SVsDifferenceService)) is IVsDifferenceService diff)
        {
            diff.OpenComparisonWindow2(current, proposal, $"{Path.GetFileName(template)}: assistant proposal", null,
                "Template", "Proposal", null, null, (uint)(__VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary | __VSDIFFSERVICEOPTIONS.VSDIFFOPT_RightFileIsTemporary));
        }
    }

    /// <summary>
    /// Puts a proposed text into the template's editor as one undoable change; nothing is saved, so
    /// Ctrl+Z takes it back and a save is still the user's call.
    /// </summary>
    public void ApplyTemplate(string template, string proposed)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (GetService(typeof(EnvDTE.DTE)) is not EnvDTE.DTE dte)
        {
            return;
        }

        VsShellUtilities.OpenDocument(this, template);
        if (FindDocument(dte, template)?.Object("TextDocument") is not EnvDTE.TextDocument text)
        {
            return;
        }

        var start = text.StartPoint.CreateEditPoint();
        var current = start.GetText(text.EndPoint);
        // The model writes \n; the file keeps its own line endings.
        var normalized = proposed.Replace("\r\n", "\n");
        if (current.Contains("\r\n"))
        {
            normalized = normalized.Replace("\n", "\r\n");
        }

        var undo = !dte.UndoContext.IsOpen;
        if (undo)
        {
            dte.UndoContext.Open("DocBuilder assistant");
        }

        try
        {
            start.ReplaceText(text.EndPoint, normalized, (int)EnvDTE.vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }
        finally
        {
            if (undo)
            {
                dte.UndoContext.Close();
            }
        }
    }

    private string? CurrentText(string template)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (GetService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte
            && FindDocument(dte, template)?.Object("TextDocument") is EnvDTE.TextDocument text)
        {
            return text.StartPoint.CreateEditPoint().GetText(text.EndPoint);
        }

        return File.Exists(template) ? File.ReadAllText(template) : null;
    }

    private static EnvDTE.Document? FindDocument(EnvDTE.DTE dte, string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (EnvDTE.Document document in dte.Documents)
        {
            if (string.Equals(document.FullName, path, StringComparison.OrdinalIgnoreCase))
            {
                return document;
            }
        }

        return null;
    }

    /// <summary>Shows a result once more, after the preview window switched how it shows documents.</summary>
    public void ShowAgain(PreviewControl view, string template, PreviewResult result) =>
        JoinableTaskFactory.RunAsync(() => view.ShowResultAsync(template, result)).FileAndForget("docbuilder/preview/mode");

    /// <summary>Runs an assistant action of the preview window off the click that started it.</summary>
    public void RunInBackground(Func<Task> action, string name) =>
        JoinableTaskFactory.RunAsync(action).FileAndForget($"docbuilder/assistant/{name}");

    /// <summary>The page that shows a document a run built in a Document Server editor, or why there is none.</summary>
    /// <param name="edit">Opens an editable copy instead; the page then names it.</param>
    public static async Task<(ViewerPage? Page, string? Error)> OpenViewerAsync(string document, bool edit)
    {
        var (page, error) = await ExecuteAsync<ViewerPage>("docbuilder.view", new { document, edit });
        return page?.Url != null ? (page, null) : (null, page?.Error?.Message ?? error);
    }

    /// <summary>What the user changed by hand in an edited copy; the editor saves its session first.</summary>
    public static async Task<EditsResult> GetEditsAsync(string edited)
    {
        var (edits, error) = await ExecuteAsync<EditsResult>("docbuilder.edits", new { document = edited });
        return edits ?? new EditsResult { Error = new PreviewError { Message = error ?? "" } };
    }

    /// <summary>How the preview window shows a document; kept in the options so it survives a restart.</summary>
    public PreviewMode PreviewMode
    {
        get => (GetDialogPage(typeof(DocBuilderOptions)) as DocBuilderOptions)?.PreviewMode ?? PreviewMode.Pdf;
        set
        {
            if (GetDialogPage(typeof(DocBuilderOptions)) is DocBuilderOptions options && options.PreviewMode != value)
            {
                options.PreviewMode = value;
                options.SaveSettingsToStorage();
            }
        }
    }

    private static async Task<ScenarioList> GetScenariosAsync(string template)
    {
        var (list, _) = await ExecuteAsync<ScenarioList>("docbuilder.scenarios", new { uri = new Uri(template).AbsoluteUri });
        return list ?? new ScenarioList();
    }

    /// <summary>A workspace/executeCommand of the language server: its answer, or why there is none.</summary>
    private static async Task<(T? Result, string? Error)> ExecuteAsync<T>(string command, object argument) where T : class
    {
        var rpc = DocBuilderLanguageClient.Current?.Rpc;
        if (rpc == null)
        {
            return (null, "The DocBuilder language server has not started yet: open a .docbuilder file and try again.");
        }

        try
        {
            var answer = await rpc.InvokeWithParameterObjectAsync<JToken>("workspace/executeCommand", new { command, arguments = new[] { argument } });
            var result = answer?.ToObject<T>();
            return (result, result == null ? "The language server returned nothing." : null);
        }
        catch (Exception e) when (e is RemoteInvocationException or ConnectionLostException or ObjectDisposedException)
        {
            return (null, $"The DocBuilder language server failed: {e.Message}");
        }
    }

    private void ShowWindow() => JoinableTaskFactory.RunAsync(ShowWindowAsync).FileAndForget("docbuilder/preview/show");

    private async Task<PreviewControl?> ShowWindowAsync()
    {
        var window = await ShowToolWindowAsync(typeof(PreviewToolWindow), 0, true, DisposalToken);
        return (window as PreviewToolWindow)?.View;
    }

    /// <summary>Opens a template at a zero-based line and column.</summary>
    public void Navigate(string file, int line, int column)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        VsShellUtilities.OpenDocument(this, file, Guid.Empty, out _, out _, out _, out var view);
        if (view != null)
        {
            view.SetCaretPos(line, Math.Max(column, 0));
            view.CenterLines(line, 1);
        }
    }

    private string? ActiveDocumentPath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return GetService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte ? dte.ActiveDocument?.FullName : null;
    }

    private static bool IsTemplate(string? path) => path != null && path.EndsWith(".docbuilder", StringComparison.OrdinalIgnoreCase);

    // ---------- run on save ----------

    public int OnAfterSave(uint docCookie)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_documents == null)
        {
            return VSConstants.S_OK;
        }

        _documents.GetDocumentInfo(docCookie, out _, out _, out _, out var path, out _, out _, out _);
        if (IsTemplate(path) && (GetDialogPage(typeof(DocBuilderOptions)) as DocBuilderOptions)?.RunOnSave == true)
        {
            _lastTemplate = path;
            Run(path);
        }

        return VSConstants.S_OK;
    }

    public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

    public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

    public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

    public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;

    public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

    public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;

    public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;
}
