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

[DefaultRoute("roomtemplate")]
public class RoomTemplatesController(IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    FolderDtoHelper folderDtoHelper,
    FileStorageService fileStorageService,
    FileDtoHelper fileDtoHelper,
    RoomTemplatesWorker roomTemplatesWorker) : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <remarks>
    /// Starts creating the room template.
    /// </remarks>
    /// <summary>Start creating room template</summary>
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
    /// Returns the progress status of the room template creation process.
    /// </remarks>
    /// <summary>Get status of room template creation</summary>
    /// <path>api/2.0/files/roomtemplate/status</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomTemplateStatusDto))]
    [HttpGet("status")]
    public async Task<RoomTemplateStatusDto> GetRoomTemplateCreatingStatus()
    {
        try
        {
            var status = await roomTemplatesWorker.GetStatusTemplateCreatingAsync(tenantManager.GetCurrentTenantId());
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
    /// Returns the public settings of the room template with the ID specified in the request.
    /// </remarks>
    /// <summary>Get public settings</summary>
    /// <path>api/2.0/files/roomtemplate/{id}/public</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok", typeof(bool))]
    [HttpGet("{id}/public")]
    public async Task<bool> GetPublicSettings(PublicDto inDto)
    {
        return await fileStorageService.IsPublicAsync(inDto.Id);
    }


    /// <remarks>
    /// Sets the public settings for the room template with the ID specified in the request.
    /// </remarks>
    /// <summary>Set public settings</summary>
    /// <path>api/2.0/files/roomtemplate/public</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok")]
    [HttpPut("public")]
    public async Task SetPublicSettings(SetPublicDto inDto)
    {
        var shared = fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "", 0, -1);

        var wrappers = new List<AceWrapper> { new() { Id = Constants.GroupEveryone.ID, Access = inDto.Public ? FileShare.Read : FileShare.None, SubjectType = SubjectType.Group } };

        await foreach (var share in shared)
        {
            if (share.Id != authContext.CurrentAccount.ID)
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