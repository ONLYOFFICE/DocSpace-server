// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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