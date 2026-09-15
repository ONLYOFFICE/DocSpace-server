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
    /// Queues a background job that turns an existing room into a reusable room template, and returns the state of
    /// that job right away. The template lands in the portal's Templates section, inherits the source room's type,
    /// privacy, indexing, storage limit, lifetime, download and watermark settings, and receives copies of the room's
    /// files together with its ordinary subfolders and everything inside them; the service subfolders a room keeps
    /// for its own workflows are left out. The caller needs room-manager rights on the source room, and the room must
    /// not be archived: a room that cannot be found under Rooms is answered as missing, and every other refusal comes
    /// back as a rejection. The template is not ready when the response arrives, so poll
    /// `GET api/2.0/files/roomtemplate/status` until `isCompleted` is true, then read `templateId`; a non-empty
    /// `error` there means the job failed and the half-built template was removed. Only one template creation is
    /// tracked per caller, and starting another replaces the previous record. Setting `public` to true discards
    /// `share` and `groups` and shares the finished template with everyone instead, while `copyLogo` reuses the
    /// source room's own picture and makes `logo` irrelevant.
    /// </remarks>
    /// <summary>Create a room template</summary>
    /// <path>api/2.0/files/roomtemplate</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomTemplateStatusDto))]
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
    /// Reports the state of the room template creation the caller started with `POST api/2.0/files/roomtemplate`. The
    /// record is private to the account that started the job: work started by another member is never reported, and a
    /// caller who has started none gets an empty response instead of an object. Poll until `isCompleted` turns true,
    /// then take the identifier of the finished template from `templateId`; a non-empty `error` means the job failed
    /// and no template was kept. Treat `isCompleted` as the completion signal rather than `progress`, which the
    /// background job only sets to 100 once the work is over. The record outlives the job, so a finished operation
    /// can be read again and keeps returning the same identifier until the caller starts another template creation,
    /// which replaces it. The call only reads state and needs no access to the source room or to the template, but it
    /// does require an authenticated caller.
    /// </remarks>
    /// <summary>Get room template creation status</summary>
    /// <path>api/2.0/files/roomtemplate/status</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomTemplateStatusDto))]
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
    /// Reports whether the room template addressed by `id` is shared with everyone or is reachable only by the
    /// accounts it was explicitly shared with. True means the Everyone group holds read access, so any member allowed
    /// to create rooms can build one from the template with `POST api/2.0/files/rooms/fromtemplate`; false means only
    /// the owner and the named recipients can. The identifier has to belong to a room template — take it from
    /// `templateId` of `GET api/2.0/files/roomtemplate/status`, or from the folder list of `GET api/2.0/files/rooms`
    /// called with `searchArea` set to 4 — while an ordinary room, a deleted template or an unknown value is answered
    /// as missing. The caller needs read access to the template, so somebody else's private template is refused even
    /// for a portal administrator, and members who cannot reach the Templates section at all are refused whatever the
    /// template's state. The call only reads state; use `PUT api/2.0/files/roomtemplate/public` to change it.
    /// </remarks>
    /// <summary>Get room template public access</summary>
    /// <path>api/2.0/files/roomtemplate/{id}/public</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok", typeof(bool))]
    [HttpGet("{id}/public")]
    public async Task<bool> GetPublicSettings(PublicDto inDto)
    {
        await fileStorageService.CheckIsRoomTemplateAsync(inDto.Id);

        return await fileStorageService.IsPublicAsync(inDto.Id);
    }


    /// <remarks>
    /// Switches the room template named by `id` between shared with everyone and private, rewriting its whole
    /// recipient list in the process. With `public` true the Everyone group is granted read access, so every member
    /// allowed to create rooms can build one from the template with `POST api/2.0/files/rooms/fromtemplate`; with
    /// false that access is taken away. In both cases every other account and group the template was shared with —
    /// including the addresses passed as `share` when it was created — loses access, so this is not a way to add a
    /// single recipient to an existing list. Only the account that owns the template may call it: a portal
    /// administrator who does not own it is refused, and so is a member invited to the source room. The identifier
    /// has to resolve to a room template; an ordinary room or an unknown value is answered as missing, and an
    /// identifier below 1 is rejected as an invalid request. Repeating the call with the same value changes nothing,
    /// and nothing is returned; read the current state with `GET api/2.0/files/roomtemplate/{id}/public`.
    /// </remarks>
    /// <summary>Set room template public access</summary>
    /// <path>api/2.0/files/roomtemplate/public</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok")]
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