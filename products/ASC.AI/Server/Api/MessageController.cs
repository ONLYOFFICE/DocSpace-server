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
[DefaultRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
public class MessageController(MessageExporter exporter) : ControllerBase
{
    /// <summary>
    /// Export a single AI message to a document
    /// </summary>
    /// <remarks>
    /// Exports a specific AI chat message as a document into the specified folder. The system verifies that the message exists
    /// and belongs to a chat accessible by the current user, then publishes an asynchronous export task to the event bus.
    /// The exported document will be created in the target folder with the given title once the background task completes.
    /// </remarks>
    /// <path>api/2.0/ai/messages/{messageId}/export</path>
    [Tags("AI / Messages")]
    [SwaggerResponse(200, "The message export task has been successfully queued for background processing")]
    [SwaggerResponse(400, "The message identifier is invalid (must be greater than 0)")]
    [SwaggerResponse(404, "The specified message was not found or the current user does not have access to it")]
    [HttpPost("messages/{messageId}/export")]
    public async Task ExportMessageAsync(ExportMessageRequestDto inDto)
    {
        if (inDto.Body.FolderId.ValueKind == JsonValueKind.Number)
        {
            await exporter.ExportMessageAsync(inDto.Body.FolderId.GetInt32(), inDto.Body.Title, inDto.MessageId);
        }
        else
        {
            await exporter.ExportMessageAsync(inDto.Body.FolderId.GetString(), inDto.Body.Title, inDto.MessageId);
        }
    }
}