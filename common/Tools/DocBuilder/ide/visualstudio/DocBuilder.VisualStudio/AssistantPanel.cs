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
/// The assistant under the preview: a conversation about the template on show, with the portal's AI.
/// A change comes back as a proposal that already passed the check and a run on the sample data; the
/// preview shows what it builds, and nothing reaches the editor until Apply.
/// </summary>
public sealed class AssistantPanel : DockPanel
{
    private readonly PreviewControl _owner;
    private readonly StackPanel _transcript = new() { Margin = new Thickness(4) };
    private readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly TextBox _input = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 44, MaxHeight = 120, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(4) };
    private readonly Button _send = new() { Content = "Send", Margin = new Thickness(4), Padding = new Thickness(10, 2, 10, 2), ToolTip = "Ctrl+Enter" };
    private readonly Button _stop = new() { Content = "Stop", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2), IsEnabled = false };
    private readonly TextBlock _progress = new() { Margin = new Thickness(8, 0, 8, 2), Foreground = Brushes.Gray, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Border _proposalBar = new() { Padding = new Thickness(6), Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xEA)) };
    private readonly TextBlock _proposalText = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
    private AssistantProposal? _proposal;
    private string? _proposalTemplate;
    private string? _asking;

    public AssistantPanel(PreviewControl owner)
    {
        _owner = owner;

        var newChat = new Button { Content = "New chat", Margin = new Thickness(4), Padding = new Thickness(8, 2, 8, 2) };
        newChat.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_owner.Template is { } template)
            {
                DocBuilderPackage.Instance?.RunInBackground(() => DocBuilderPackage.ResetAssistantAsync(template), "reset");
            }

            _transcript.Children.Clear();
            HideProposal();
        };
        _stop.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_asking != null)
            {
                var template = _asking;
                DocBuilderPackage.Instance?.RunInBackground(() => DocBuilderPackage.CancelAssistantAsync(template), "cancel");
            }
        };
        _send.Click += (_, _) => Send(_input.Text, null, null);
        _input.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                e.Handled = true;
                Send(_input.Text, null, null);
            }
        };

        var header = new DockPanel { Background = new SolidColorBrush(Color.FromArgb(0x10, 0, 0, 0)) };
        var title = new TextBlock { Text = "Assistant · DocSpace AI", FontWeight = FontWeights.SemiBold, Margin = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center };
        var headerButtons = new StackPanel { Orientation = Orientation.Horizontal };
        headerButtons.Children.Add(_stop);
        headerButtons.Children.Add(newChat);
        SetDock(headerButtons, Dock.Right);
        header.Children.Add(headerButtons);
        header.Children.Add(title);
        SetDock(header, Dock.Top);
        Children.Add(header);

        var diff = new Button { Content = "Diff", Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 2, 8, 2) };
        var apply = new Button { Content = "Apply", Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 2, 8, 2), FontWeight = FontWeights.SemiBold };
        var discard = new Button { Content = "Discard", Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 2, 8, 2) };
        diff.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_proposal != null && _proposalTemplate != null)
            {
                DocBuilderPackage.Instance?.ShowDiff(_proposalTemplate, _proposal.Text);
            }
        };
        apply.Click += (_, _) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_proposal != null && _proposalTemplate != null)
            {
                DocBuilderPackage.Instance?.ApplyTemplate(_proposalTemplate, _proposal.Text);
                Add("Applied to the editor (not saved; Ctrl+Z takes it back).", Brushes.Gray);
                HideProposal();
            }
        };
        discard.Click += (_, _) =>
        {
            Add("Discarded.", Brushes.Gray);
            HideProposal();
        };
        var proposalButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        proposalButtons.Children.Add(diff);
        proposalButtons.Children.Add(apply);
        proposalButtons.Children.Add(discard);
        var proposalRow = new DockPanel();
        SetDock(proposalButtons, Dock.Right);
        proposalRow.Children.Add(proposalButtons);
        proposalRow.Children.Add(_proposalText);
        _proposalBar.Child = proposalRow;

        var inputRow = new DockPanel();
        SetDock(_send, Dock.Right);
        inputRow.Children.Add(_send);
        inputRow.Children.Add(_input);

        var bottom = new StackPanel();
        bottom.Children.Add(_proposalBar);
        bottom.Children.Add(_progress);
        bottom.Children.Add(inputRow);
        SetDock(bottom, Dock.Bottom);
        Children.Add(bottom);

        _scroll.Content = _transcript;
        Children.Add(_scroll);

        ServerNotifications.AssistantProgress += (template, text) =>
            DocBuilderPackage.Instance?.RunInBackground(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (string.Equals(template, _asking, StringComparison.OrdinalIgnoreCase))
                {
                    _progress.Text = text;
                }
            }, "progress");
    }

    /// <summary>Lists what the user changed by hand in the edited copy, without the model.</summary>
    public void ShowEdits(string edited)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Add("Changes made in the editor:", Brushes.SteelBlue, bold: true);
        DocBuilderPackage.Instance?.RunInBackground(async () =>
        {
            var edits = await DocBuilderPackage.GetEditsAsync(edited);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (edits.Error != null)
            {
                Add(edits.Error.Message, Brushes.Firebrick);
                return;
            }

            Add(edits.Text, null);
        }, "edits");
    }

    /// <summary>Asks the assistant to change the template so that it builds the edited copy.</summary>
    public void CarryEdits(string edited)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Send("Change the template so that it builds the document as I edited it.", null, edited);
    }

    /// <summary>Asks to fix the error the last preview run reported.</summary>
    public void Fix(PreviewError error)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var where = error.Line != null ? $" (line {error.Line + 1})" : "";
        Send($"Fix the error of the last preview run{where}: {error.Message}", error.Message + where, null);
    }

    private void Send(string prompt, string? lastError, string? edits)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        prompt = prompt.Trim();
        var template = _owner.Template;
        if (prompt.Length == 0 || _asking != null)
        {
            return;
        }

        if (template == null)
        {
            Add("Open a .docbuilder file and run its preview first: the assistant works on the template on show.", Brushes.Gray);
            return;
        }

        var scenario = _owner.Scenario;
        _input.Clear();
        Add(prompt, Brushes.SteelBlue, bold: true);
        HideProposal();
        SetBusy(template);

        DocBuilderPackage.Instance?.RunInBackground(async () =>
        {
            var answer = await DocBuilderPackage.AskAssistantAsync(template, prompt, scenario, lastError, edits);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            SetBusy(null);
            if (answer.Error != null)
            {
                Add(answer.Error.Message, Brushes.Firebrick);
                return;
            }

            if (!string.IsNullOrWhiteSpace(answer.Reply))
            {
                Add(answer.Reply!, null);
            }

            if (answer.Proposal is { } proposal)
            {
                ShowProposal(template, proposal);
                if (proposal.Run != null)
                {
                    await _owner.ShowResultAsync(template, proposal.Run, "assistant proposal");
                }
            }
        }, "ask");
    }

    private void ShowProposal(string template, AssistantProposal proposal)
    {
        _proposal = proposal;
        _proposalTemplate = template;
        var warnings = proposal.Warnings > 0 ? $" · {proposal.Warnings} warning(s)" : "";
        var remaining = proposal.Remaining > 0 ? $" · {proposal.Remaining} hand edit(s) not carried: {proposal.Unresolved}" : "";
        _proposalText.Text = $"Proposal: {proposal.Summary} — checked and built on the sample data{warnings}{remaining}; the preview shows it.";
        _proposalBar.Visibility = Visibility.Visible;
    }

    private void HideProposal()
    {
        _proposal = null;
        _proposalTemplate = null;
        _proposalBar.Visibility = Visibility.Collapsed;
    }

    private void SetBusy(string? template)
    {
        _asking = template;
        _send.IsEnabled = template == null;
        _stop.IsEnabled = template != null;
        _progress.Text = template != null ? "Working…" : "";
    }

    // A null color is the tool window's own text color, so the model's answers read in any theme.
    private void Add(string text, Brush? color, bool bold = false)
    {
        var block = new TextBox
        {
            Text = text,
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Margin = new Thickness(0, 2, 0, 6),
        };
        if (color != null)
        {
            block.Foreground = color;
        }
        else
        {
            block.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
        }

        _transcript.Children.Add(block);
        _scroll.ScrollToEnd();
    }
}
