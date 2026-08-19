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
/// Upper bounds for the persisted AI integration fields, enforced at the request-validation
/// layer so oversized payloads are rejected with a 400 instead of reaching the database.
/// <para>
/// The free-form profile/MCP fields (<c>DbProfile.Key</c>, <c>DbProfile.BaseUrl</c>,
/// <c>DbMcpServer.Config</c>) are validated on the plaintext value, before encryption; their
/// column type stays <c>text</c> on purpose, since encryption inflates the size.
/// </para>
/// <para>
/// The name/title fields mirror the <c>varchar(255)</c> columns behind them. Validating them
/// here is what makes the bulk-update paths (<c>ExecuteUpdateAsync</c>) behave like the
/// change-tracked ones, which get their check from <c>BaseDbContext.SaveChangesAsync</c>.
/// </para>
/// </summary>
internal static class AiIntegrationLimits
{
    /// <summary>Maximum length of a provider API key (plaintext, before encryption).</summary>
    public const int MaxKeyLength = 4096;

    /// <summary>Maximum length of a provider base URL.</summary>
    public const int MaxBaseUrlLength = 2048;

    /// <summary>Maximum length of an MCP server config payload (plaintext, before encryption).</summary>
    public const int MaxConfigLength = 32768;

    /// <summary>Maximum length of a prompt, prompt folder or thread name.</summary>
    public const int MaxNameLength = 255;
}
