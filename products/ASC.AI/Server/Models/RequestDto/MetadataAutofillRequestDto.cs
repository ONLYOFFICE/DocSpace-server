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

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// The request parameters for autofilling the metadata of a single document.
/// </summary>
public class MetadataAutofillRequestDto
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    public required int FileId { get; set; }

    /// <summary>
    /// The ID of the metadata template whose fields are filled. When omitted, all the templates assigned to the file and the system template are used.
    /// </summary>
    /// <example>1</example>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Specifies whether the existing values are overwritten. By default only the empty fields are filled.
    /// </summary>
    public bool Overwrite { get; set; }

    /// <summary>
    /// Specifies whether the values are only proposed without saving.
    /// </summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// The request parameters for autofilling the metadata of a batch of documents.
/// </summary>
public class MetadataAutofillBatchRequestDto
{
    /// <summary>
    /// The file IDs.
    /// </summary>
    public required List<int> FileIds { get; set; }

    /// <summary>
    /// The ID of the metadata template whose fields are filled. When omitted, all the templates assigned to each file and the system template are used.
    /// </summary>
    /// <example>1</example>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Specifies whether the existing values are overwritten. By default only the empty fields are filled.
    /// </summary>
    public bool Overwrite { get; set; }
}

/// <summary>
/// The request parameters for suggesting new metadata fields for a document.
/// </summary>
public class MetadataSuggestFieldsRequestDto
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    public required int FileId { get; set; }
}
