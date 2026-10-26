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

/// <summary>What the server's docbuilder.run command answers (language-server/src/preview.mjs).</summary>
public sealed class PreviewResult
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("durationMs")]
    public long DurationMs { get; set; }

    [JsonProperty("builder")]
    public string? Builder { get; set; }

    [JsonProperty("sample")]
    public string? Sample { get; set; }

    [JsonProperty("scenario")]
    public string? Scenario { get; set; }

    [JsonProperty("script")]
    public string? Script { get; set; }

    [JsonProperty("document")]
    public string? Document { get; set; }

    [JsonProperty("pdf")]
    public string? Pdf { get; set; }

    [JsonProperty("stderr")]
    public string? Stderr { get; set; }

    [JsonProperty("error")]
    public PreviewError? Error { get; set; }
}

/// <summary>What the server's docbuilder.scenarios command answers: the scenarios of a template's sample.</summary>
public sealed class ScenarioList
{
    [JsonProperty("sample")]
    public string? Sample { get; set; }

    [JsonProperty("default")]
    public string? Default { get; set; }

    [JsonProperty("scenarios")]
    public List<ScenarioInfo> Scenarios { get; set; } = [];

    [JsonProperty("error")]
    public PreviewError? Error { get; set; }
}

public sealed class ScenarioInfo
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";
}

/// <summary>What the server's docbuilder.view command answers: the page that shows a document in the viewer.</summary>
public sealed class ViewerPage
{
    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("documentServer")]
    public string? DocumentServer { get; set; }

    /// <summary>The editable copy the page shows when it was opened for editing; the built document stays as it was.</summary>
    [JsonProperty("edited")]
    public string? Edited { get; set; }

    [JsonProperty("error")]
    public PreviewError? Error { get; set; }
}

/// <summary>What the server's docbuilder.edits command answers: the hand edits of an edited copy.</summary>
public sealed class EditsResult
{
    /// <summary>The changes as text, one line per property and area.</summary>
    [JsonProperty("text")]
    public string Text { get; set; } = "";

    /// <summary>Cells whose formatting changed.</summary>
    [JsonProperty("format")]
    public int Format { get; set; }

    /// <summary>Cells that hold another value or formula.</summary>
    [JsonProperty("data")]
    public int Data { get; set; }

    /// <summary>Column, row, merge, pane and table changes.</summary>
    [JsonProperty("layout")]
    public int Layout { get; set; }

    [JsonProperty("error")]
    public PreviewError? Error { get; set; }
}

/// <summary>What the server's docbuilder.assistant.ask command answers.</summary>
public sealed class AssistantAnswer
{
    [JsonProperty("reply")]
    public string? Reply { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    /// <summary>A changed template that passed the check and the run; null when the model only answered.</summary>
    [JsonProperty("proposal")]
    public AssistantProposal? Proposal { get; set; }

    [JsonProperty("error")]
    public PreviewError? Error { get; set; }
}

public sealed class AssistantProposal
{
    [JsonProperty("text")]
    public string Text { get; set; } = "";

    [JsonProperty("summary")]
    public string Summary { get; set; } = "";

    /// <summary>The run of the proposed text on the sample data: what the preview shows for it.</summary>
    [JsonProperty("run")]
    public PreviewResult? Run { get; set; }

    [JsonProperty("warnings")]
    public int Warnings { get; set; }

    /// <summary>When carrying hand edits: formatting differences the proposal leaves, with Unresolved saying why.</summary>
    [JsonProperty("remaining")]
    public int Remaining { get; set; }

    [JsonProperty("unresolved")]
    public string? Unresolved { get; set; }
}

public sealed class PreviewError
{
    [JsonProperty("message")]
    public string Message { get; set; } = "";

    /// <summary>The template the failing line came from; null when it cannot be told.</summary>
    [JsonProperty("file")]
    public string? File { get; set; }

    /// <summary>Zero-based.</summary>
    [JsonProperty("line")]
    public int? Line { get; set; }

    /// <summary>Zero-based.</summary>
    [JsonProperty("column")]
    public int? Column { get; set; }
}
