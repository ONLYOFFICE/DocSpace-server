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

namespace ASC.Files.Api;

[ApiEndpoint(Template = "roomtemplate")]
public class RoomTemplatesController(IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FolderDtoHelper folderDtoHelper,
    FileStorageService fileStorageService,
    FileDtoHelper fileDtoHelper,
    RoomTemplatesWorker roomTemplatesWorker) : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <remarks>
    /// Starts a job that creates a room template from a room, and returns the job state. The portal saves the template
    /// in the Templates section. The template takes the type, indexing, storage limit, lifetime, download, and
    /// watermark settings of the room. The job copies the files and ordinary subfolders of the room, but not its
    /// service subfolders. Requires room manager access to the source room. Other callers get 403, even a DocSpace
    /// admin. The call queues work. Poll `GET api/2.0/files/roomtemplate/status` until `isCompleted` is true, then read
    /// `templateId`. While a job of the caller runs, a repeated call starts nothing and returns its state. After the
    /// job ends, a repeated call creates another template. If `public` is true, the call ignores `share` and `groups`
    /// and shares the template with everyone. If `copyLogo` is true, the call ignores `logo`. If `roomId` is not a
    /// room of the Rooms section, the call returns 404. The call returns 403 if the room is archived or private. It
    /// also returns 403 if `quota` is set while room quotas are turned off. The call returns 400 if `roomId` is
    /// missing, `title` is empty, or a value breaks its limits.
    /// </remarks>
    /// <summary>Create a room template</summary>
    /// <path>api/2.0/files/roomtemplate</path>
    [RaisesWebhook(WebhookTrigger.RoomCreated, When = "for the new template, once the queued operation builds it")]
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "after it when `quota` is given")]
    [RaisesWebhook(WebhookTrigger.AgentCreated, When = "instead for an AI agent template")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead for an AI agent template with `quota`")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The state of the template creation just queued: `isCompleted` is still false, so the job has to be polled for its result", typeof(RoomTemplateStatusDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `roomId`, `title` is empty or longer than 400 characters, `color` is longer than 6 or `cover` longer than 50 characters, or `logo` has no `tmpFile` or a position or size outside 0-1280")]
    [SwaggerResponse(403, "The source room is archived or private, the caller cannot edit it, or `quota` is set while the storage quota for rooms (for an AI agent, for agents) is turned off")]
    [SwaggerResponse(404, "No room with `roomId` exists in the Rooms section")]
    [HttpPost("")]
    public async Task<RoomTemplateStatusDto> CreateRoomTemplate(RoomTemplateDto dto)
    {
        LogoSettings logo = null;
        if (dto.Logo != null)
        {
            logo = new LogoSettings
            {
                Height = dto.Logo.Height,
                Width = dto.Logo.Width,
                TmpFile = dto.Logo.TmpFile,
                X = dto.Logo.X,
                Y = dto.Logo.Y
            };
        }
        if (dto.Public)
        {
            dto.Share = null;
            dto.Groups = [Constants.GroupEveryone.ID];
        }

        // The template is built by a background operation, so the access to the source room has to
        // be verified here — otherwise the caller is told the request succeeded and only finds out
        // later, from the operation status, that it could not.
        await fileStorageService.CheckCanCreateRoomTemplateAsync(dto.RoomId, dto.Quota);

        var taskId = await roomTemplatesWorker.StartCreateTemplateAsync(tenantManager.GetCurrentTenantId(), authContext.CurrentAccount.ID,
            dto.RoomId,
            dto.Title,
            dto.Share,
            logo,
            dto.CopyLogo,
            dto.Tags,
            dto.Groups,
            dto.Cover,
            dto.Color,
            dto.Quota,
            false);

        await eventBus.PublishAsync(new CreateRoomTemplateIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
        {
            RoomId = dto.RoomId,
            Title = dto.Title,
            Logo = logo,
            Emails = dto.Share,
            Tags = dto.Tags,
            Groups = dto.Groups,
            TaskId = taskId,
            CopyLogo = dto.CopyLogo,
            Cover = dto.Cover,
            Color = dto.Color,
            Quota = dto.Quota
        });
        return await GetRoomTemplateCreatingStatus();
    }

    /// <remarks>
    /// Returns the state of the room template job that the caller started with `POST api/2.0/files/roomtemplate`. Any
    /// signed-in user can call it. The call is read-only and reports only the jobs of the caller. If the caller has
    /// started none, the call returns no status object. This is the call to poll. Repeat it until `isCompleted` is
    /// true, then read `templateId`. Use `isCompleted`, not `progress`, as the end signal. After the job ends, the call
    /// returns the same state until the caller starts another job.
    /// </remarks>
    /// <summary>Get room template creation status</summary>
    /// <path>api/2.0/files/roomtemplate/status</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The state of the caller's latest template creation, or an empty body when this account has started none", typeof(RoomTemplateStatusDto))]
    [HttpGet("status")]
    public async Task<RoomTemplateStatusDto> GetRoomTemplateCreatingStatus()
    {
        try
        {
            var status = await roomTemplatesWorker.GetStatusTemplateCreatingAsync(tenantManager.GetCurrentTenantId(), authContext.CurrentAccount.ID);
            if (status != null)
            {
                var result = new RoomTemplateStatusDto
                {
                    Progress = status.Percentage,
                    Error = status.Exception != null ? status.Exception.Message : "",
                    IsCompleted = status.IsCompleted,
                    TemplateId = status.TemplateId
                };
                return result;
            }
        }
        catch
        {

        }
        return null;
    }


    /// <remarks>
    /// Returns whether a room template is shared with everyone. If `true`, the Everyone group has read access. Then any
    /// user who can create rooms can use the template with `POST api/2.0/files/rooms/fromtemplate`. If `false`, only
    /// the owner of the template and the users it is shared with can use it. Requires read access to the template.
    /// Other callers get 403, even a DocSpace admin. The user types user and guest always get 403. The call is
    /// read-only. It returns 404 if `id` is an ordinary room, a deleted template, or unknown. If `id` is less than 1,
    /// the call returns 400. To change the setting, use `PUT api/2.0/files/roomtemplate/public`.
    /// </remarks>
    /// <summary>Get room template public access</summary>
    /// <path>api/2.0/files/roomtemplate/{id}/public</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "True when the template is shared with everyone, false when only its owner and the accounts it was shared with can reach it", typeof(bool))]
    [SwaggerResponse(400, "A parameter has the wrong type, or `id` is less than 1")]
    [SwaggerResponse(403, "The caller has no read access to the template")]
    [SwaggerResponse(404, "`id` does not identify a room template")]
    [HttpGet("{id}/public")]
    public async Task<bool> GetPublicSettings(PublicDto inDto)
    {
        await fileStorageService.CheckIsRoomTemplateAsync(inDto.Id);

        return await fileStorageService.IsPublicAsync(inDto.Id);
    }


    /// <remarks>
    /// Changes whether a room template is shared with everyone. If `public` is true, the Everyone group gets read
    /// access. Then any user who can create rooms can use the template with `POST api/2.0/files/rooms/fromtemplate`.
    /// If `public` is false, the Everyone group loses access. In both cases, the call removes access for all other
    /// users and groups of the template. This includes the users from `share` at creation. So you cannot add one
    /// recipient with this call. Only the owner of the template can call it. Other callers get 403, even the portal
    /// owner. The call changes data and returns nothing. You can repeat it with the same body. The call returns 404 if
    /// `id` is an ordinary room or unknown. It returns 400 if the body has no `id` or `id` is less than 1. To read the
    /// setting, use `GET api/2.0/files/roomtemplate/{id}/public`.
    /// </remarks>
    /// <summary>Set room template public access</summary>
    /// <path>api/2.0/files/roomtemplate/public</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The recipient list of the template has been rewritten to match the requested access; nothing is returned")]
    [SwaggerResponse(400, "The request body cannot be read or has no `id`, or `id` is less than 1")]
    [SwaggerResponse(403, "The caller is not allowed to change who the template is shared with")]
    [SwaggerResponse(404, "`id` does not identify a room template")]
    [HttpPut("public")]
    public async Task SetPublicSettings(SetPublicDto inDto)
    {
        await fileStorageService.CheckIsRoomTemplateAsync(inDto.Id);

        var shared = fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "", 0, -1);

        var wrappers = new List<AceWrapper> { new() { Id = Constants.GroupEveryone.ID, Access = inDto.Public ? FileShare.Read : FileShare.None, SubjectType = SubjectType.Group } };

        await foreach (var share in shared)
        {
            if (share.Id != authContext.CurrentAccount.ID && share.Id != Constants.GroupEveryone.ID)
            {
                wrappers.Add(new AceWrapper { Id = share.Id, Access = FileShare.None, SubjectType = share.SubjectType });
            }
        }

        var aceCollection = new AceCollection<int>
        {
            Files = [],
            Folders = [inDto.Id],
            Aces = wrappers,
            Message = string.Empty
        };

        await fileStorageService.SetAceObjectAsync(aceCollection, false);
    }
}