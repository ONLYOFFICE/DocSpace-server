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

namespace ASC.AI.Tools.Core;

public class ToolContext
{
    public JsonElement? FolderId { get; init; }
    public int FormId { get; init; }

    /// <summary>The attachment the form was attached under; keys the per-attachment analyze intent.</summary>
    public string? AttachmentId { get; init; }

    /// <summary>Set by the ASC.AI.Chat form-analysis sub-agent so the form-data tools are emitted for it
    /// only, and never for the main chat agent.</summary>
    public bool FormSubAgent { get; init; }
}

public class ResolvedToolContext
{
    public IFolder? Folder { get; init; }
    public FileEntry? Form { get; init; }

    /// <summary>
    /// True when the user launched form-response analysis for <see cref="Form"/> (resolved server-side
    /// from the attach intent). Gates the form-data tools.
    /// </summary>
    public bool Analyze { get; init; }

    /// <summary>True when the request comes from the form-analysis sub-agent (which runs the form-data
    /// tools on the FormAnalysis model), so the tools are withheld from the main chat agent.</summary>
    public bool FormSubAgent { get; init; }
}
