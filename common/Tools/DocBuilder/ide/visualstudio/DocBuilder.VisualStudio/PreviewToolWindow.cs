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

[Guid("e7b2c94d-8a13-4f6e-b05c-3d9a1e72f4b6")]
public sealed class PreviewToolWindow : ToolWindowPane
{
    public PreviewToolWindow() : base(null)
    {
        Caption = "DocBuilder Preview";
        View = new PreviewControl();
        Content = View;
    }

    public PreviewControl View { get; }
}

/// <summary>
/// The document a template built - as a PDF, or in the Document Server viewer - in WebView2, with the
/// run's status above it and the builder's error, clickable to the template line it came from, when
/// there is one.
/// </summary>
public sealed class PreviewControl : DockPanel
{
    private readonly TextBlock _status = new() { Margin = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Border _errorPanel = new() { Padding = new Thickness(8), Visibility = Visibility.Collapsed };
    private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _openDocument = new() { Content = "Open document", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2), IsEnabled = false };
    private readonly Button _openScript = new() { Content = "Script that ran", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2), IsEnabled = false };
    private readonly TextBox _text = new() { IsReadOnly = true, FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
    private readonly ComboBox _scenarios = new() { Margin = new Thickness(4), MinWidth = 140, IsEnabled = false, ToolTip = "Sample data scenario" };
    private readonly ComboBox _mode = new() { Margin = new Thickness(4), MinWidth = 80, ToolTip = "PDF: the document printed. Viewer: the document in a Document Server editor, read-only. Editor: the same, editable." };
    private readonly Button _changes = new() { Content = "Changes", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2), Visibility = Visibility.Collapsed, ToolTip = "What you changed in the editor, against the document the template built" };
    private readonly Button _carry = new() { Content = "Carry into template", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2), Visibility = Visibility.Collapsed, ToolTip = "The assistant changes the template so that it builds the document as you edited it" };
    private readonly WebView2 _browser = new();
    // The scenario picked for each template, so switching between templates keeps every choice.
    private readonly Dictionary<string, string> _chosen = new(StringComparer.OrdinalIgnoreCase);
    private readonly AssistantPanel _assistant;
    private PreviewResult? _result;
    private string? _template;
    // The editable copy the editor shows; null in the other modes.
    private string? _edited;
    private bool _fillingScenarios;
    private bool _browserReady;

    public PreviewControl()
    {
        var run = new Button { Content = "Run", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2) };
        run.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DocBuilderPackage.Instance?.RunActive();
        };
        _openDocument.Click += (_, _) => OpenExternally(_result?.Document);
        _openScript.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_result?.Script != null)
            {
                DocBuilderPackage.Instance?.Navigate(_result.Script, 0, 0);
            }
        };

        // Picking another scenario rebuilds the preview with it.
        _scenarios.SelectionChanged += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!_fillingScenarios && _template != null && Scenario != null)
            {
                _chosen[_template] = Scenario;
                DocBuilderPackage.Instance?.Run(_template);
            }
        };

        // Switching how to show a document shows the last one again; nothing is rebuilt.
        _mode.Items.Add(new ComboBoxItem { Content = "PDF", Tag = PreviewMode.Pdf });
        _mode.Items.Add(new ComboBoxItem { Content = "Viewer", Tag = PreviewMode.Viewer });
        _mode.Items.Add(new ComboBoxItem { Content = "Editor", Tag = PreviewMode.Editor });
        _mode.SelectedIndex = (int)(DocBuilderPackage.Instance?.PreviewMode ?? PreviewMode.Pdf);
        _mode.SelectionChanged += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (DocBuilderPackage.Instance is { } package)
            {
                package.PreviewMode = Mode;
                if (_template != null && _result != null)
                {
                    package.ShowAgain(this, _template, _result);
                }
            }
        };

        var toolbar = new DockPanel();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(run);
        buttons.Children.Add(_scenarios);
        buttons.Children.Add(_mode);
        buttons.Children.Add(_changes);
        buttons.Children.Add(_carry);
        buttons.Children.Add(_openDocument);
        buttons.Children.Add(_openScript);
        SetDock(buttons, Dock.Left);
        toolbar.Children.Add(buttons);
        toolbar.Children.Add(_status);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);

        _errorPanel.Background = new SolidColorBrush(Color.FromRgb(0xFD, 0xE7, 0xE9));
        _error.Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x0A, 0x14));
        _errorPanel.Child = _error;
        SetDock(_errorPanel, Dock.Top);
        Children.Add(_errorPanel);

        // The document on top, the assistant under it; the splitter moves the border between them.
        var document = new Grid();
        document.Children.Add(_browser);
        document.Children.Add(_text);
        _assistant = new AssistantPanel(this);
        _changes.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_edited != null)
            {
                _assistant.ShowEdits(_edited);
            }
        };
        _carry.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_edited != null)
            {
                _assistant.CarryEdits(_edited);
            }
        };

        var body = new Grid();
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 120 });
        var splitter = new GridSplitter { Height = 5, HorizontalAlignment = HorizontalAlignment.Stretch, ResizeDirection = GridResizeDirection.Rows, Background = Brushes.Gainsboro };
        Grid.SetRow(document, 0);
        Grid.SetRow(splitter, 1);
        Grid.SetRow(_assistant, 2);
        body.Children.Add(document);
        body.Children.Add(splitter);
        body.Children.Add(_assistant);
        Children.Add(body);

        _status.Text = "Save a .docbuilder file or press Run to build its document.";
    }

    private PreviewMode Mode => (_mode.SelectedItem as ComboBoxItem)?.Tag is PreviewMode mode ? mode : PreviewMode.Pdf;

    /// <summary>The template on show: what the assistant works on.</summary>
    public string? Template => _template;

    /// <summary>The scenario chosen for the template on show; null when its sample has none.</summary>
    public string? Scenario => (_scenarios.SelectedItem as ComboBoxItem)?.Tag as string;

    /// <summary>Fills the scenario list for a template, keeping the scenario picked for it before.</summary>
    public async Task ShowScenariosAsync(string template, ScenarioList list)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        _template = template;
        _fillingScenarios = true;
        try
        {
            _scenarios.Items.Clear();
            foreach (var scenario in list.Scenarios)
            {
                _scenarios.Items.Add(new ComboBoxItem
                {
                    Content = scenario.Name,
                    Tag = scenario.Name,
                    ToolTip = string.IsNullOrEmpty(scenario.Description) ? null : scenario.Description,
                });
            }

            var wanted = _chosen.TryGetValue(template, out var chosen) ? chosen : list.Default;
            _scenarios.SelectedItem = _scenarios.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == wanted)
                ?? _scenarios.Items.OfType<ComboBoxItem>().FirstOrDefault();
            _scenarios.IsEnabled = _scenarios.Items.Count > 1;
            _scenarios.ToolTip = (_scenarios.SelectedItem as ComboBoxItem)?.ToolTip ?? "Sample data scenario";
        }
        finally
        {
            _fillingScenarios = false;
        }
    }

    public void ShowRunning(string template, string? scenario)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _status.Text = scenario != null ? $"Running {Path.GetFileName(template)} · {scenario}…" : $"Running {Path.GetFileName(template)}…";
    }

    /// <param name="label">Who built it, when it is not the template in the editor ("assistant proposal").</param>
    public async Task ShowResultAsync(string template, PreviewResult result, string? label = null)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        _template ??= template;
        _result = result;
        _openDocument.IsEnabled = result.Document != null && File.Exists(result.Document);
        _openScript.IsEnabled = result.Script != null && File.Exists(result.Script);

        var sample = result.Sample == null ? " · no sample data"
            : result.Scenario is { } scenario && scenario != "default" ? $" · data: {Path.GetFileName(result.Sample)} / {scenario}"
            : $" · data: {Path.GetFileName(result.Sample)}";
        var who = label != null ? $" · {label}" : "";
        _status.Text = result.Ok
            ? $"{Path.GetFileName(template)}{who} · {result.DurationMs / 1000.0:0.0} s{sample}"
            : $"{Path.GetFileName(template)}{who} · failed{sample}";

        ShowError(result.Error);
        // A proposal is there to be compared, not edited: the editor shows it read-only, and the edited
        // copy of the template's own run stays the one the buttons work on.
        var edit = Mode == PreviewMode.Editor && label == null;
        if (label == null)
        {
            SetEdited(null);
        }

        // The viewer opens what the run built; when it cannot, the PDF is shown and the reason above it.
        if (Mode != PreviewMode.Pdf && result.Ok && result.Document != null && File.Exists(result.Document) && result.Pdf != null)
        {
            var (page, error) = await DocBuilderPackage.OpenViewerAsync(result.Document, edit);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (page?.Url != null)
            {
                if (edit)
                {
                    SetEdited(page.Edited);
                }

                _text.Visibility = Visibility.Collapsed;
                _browser.Visibility = Visibility.Visible;
                await NavigateAsync(page.Url);
                return;
            }

            ShowError(new PreviewError { Message = $"The viewer is not available, showing the PDF: {error}" });
        }

        if (result.Pdf != null && File.Exists(result.Pdf))
        {
            _text.Visibility = Visibility.Collapsed;
            _browser.Visibility = Visibility.Visible;

            // At 100 %: the server prints a sheet at its own size, so this reads like the spreadsheet itself;
            // the viewer's toolbar still fits it to the window on demand.
            await NavigateAsync(new Uri(result.Pdf).AbsoluteUri + "#zoom=100,0,0");
        }
        else if (result.Ok && result.Document != null && File.Exists(result.Document))
        {
            // A text result (ExtractFormFieldsData saves txt) has no PDF to show.
            _browser.Visibility = Visibility.Collapsed;
            _text.Visibility = Visibility.Visible;
            _text.Text = File.ReadAllText(result.Document);
        }
    }

    private void SetEdited(string? edited)
    {
        _edited = edited;
        var visibility = edited != null ? Visibility.Visible : Visibility.Collapsed;
        _changes.Visibility = visibility;
        _carry.Visibility = visibility;
    }

    private void ShowError(PreviewError? error)
    {
        _error.Inlines.Clear();
        if (error == null)
        {
            _errorPanel.Visibility = Visibility.Collapsed;
            return;
        }

        _error.Inlines.Add(new Run(error.Message));
        if (error.File != null && error.Line != null)
        {
            var link = new Hyperlink(new Run($"{Path.GetFileName(error.File)}, line {error.Line + 1}"));
            link.Click += (_, _) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                DocBuilderPackage.Instance?.Navigate(error.File, error.Line.Value, error.Column ?? 0);
            };
            _error.Inlines.Add(new Run("  "));
            _error.Inlines.Add(link);

            var fix = new Hyperlink(new Run("Fix with the assistant"));
            fix.Click += (_, _) => _assistant.Fix(error);
            _error.Inlines.Add(new Run("  "));
            _error.Inlines.Add(fix);
        }

        _errorPanel.Visibility = Visibility.Visible;
    }

    private async Task NavigateAsync(string url)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!_browserReady)
        {
            // The default user data folder would sit next to devenv.exe, where nothing may be written.
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DocBuilderPreview", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, data);
            await _browser.EnsureCoreWebView2Async(environment);
            _browserReady = true;
        }

        _browser.CoreWebView2.Navigate(url);
    }

    private static void OpenExternally(string? file)
    {
        if (file != null && File.Exists(file))
        {
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        }
    }
}
