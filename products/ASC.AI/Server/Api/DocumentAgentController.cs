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

namespace ASC.AI.Api;

[Scope]
[ApiEndpoint("ai", Internal = true)]
[AiFeature]
[ApiExplorerSettings(IgnoreApi = true)]
public class DocumentAgentController(DocumentAgentSettingsService documentAgentSettingsService) : ControllerBase
{
    /// <remarks>
    /// Returns the portal settings of the document agent. A deleted profile, or a task log folder that is deleted or
    /// in the trash, is returned as null. Available to DocSpace admins, room admins and users.
    /// </remarks>
    /// <summary>Get the document agent settings</summary>
    [HttpGet("document-agent/settings")]
    public async Task<DocumentAgentSettingsDto> ReadSettingsAsync()
    {
        var settings = await documentAgentSettingsService.ReadAsync();
        return settings.MapToDto();
    }

    /// <remarks>
    /// Replaces the portal settings of the document agent. Only DocSpace admins can change them. The profile must
    /// exist, and the admin must be able to create files in the task log folder.
    /// </remarks>
    /// <summary>Update the document agent settings</summary>
    [HttpPut("document-agent/settings")]
    public async Task<DocumentAgentSettingsDto> UpdateSettingsAsync(UpdateDocumentAgentSettingsRequestDto inDto)
    {
        var settings = await documentAgentSettingsService.UpdateAsync(inDto.Map());
        return settings.MapToDto();
    }
}
