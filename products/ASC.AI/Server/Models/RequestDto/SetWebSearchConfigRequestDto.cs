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
/// Request to configure the global web search settings for AI chat sessions.
/// </summary>
public class SetWebSearchConfigRequestDto
{
    /// <summary>
    /// The web search configuration parameters.
    /// </summary>
    /// <example>{"enabled": true, "type": 1, "key": "search-api-key-123"}</example>
    [FromBody]
    public required SetWebSearchSettingsRequestBody Body { get; init; }
}

/// <summary>
/// Parameters for configuring web search settings.
/// </summary>
public class SetWebSearchSettingsRequestBody
{
    /// <summary>
    /// Indicates whether web search is enabled for AI chat sessions.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; init; }

    /// <summary>
    /// The type of web search engine to use.
    /// </summary>
    /// <example>1</example>
    public EngineType Type { get; init; }

    /// <summary>
    /// The API key for the selected web search engine. Pass null to keep the existing key unchanged.
    /// </summary>
    /// <example>search-api-key-123</example>
    public string? Key { get; init; }
}