﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Files.Core.RoomTemplates;
using ASC.Files.Core.RoomTemplates.Events;

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
    [HttpPost("")]
    public async Task<RoomTemplateStatusDto> CreateTemplateAsync(RoomTemplateDto dto)
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
            dto.Groups = new List<Guid> { Constants.GroupEveryone.ID };
        }

        await eventBus.PublishAsync(new CreateRoomTemplateIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
        {
            RoomId = dto.RoomId,
            Title = dto.Title,
            Logo = logo,
            Emails = dto.Share,
            Tags = dto.Tags,
            Groups = dto.Groups
        });
        var status = await Status();
        if (status == null || status.IsCompleted == true)
        {
            return null;
        }
        return status;
    }

    [HttpGet("status")]
    public async Task<RoomTemplateStatusDto> Status()
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

    [HttpGet("{id}/public")]
    public async Task<bool> IsPublic(PublicDto inDto)
    {
        return await fileStorageService.IsPublicAsync(inDto.Id);
    }

    [HttpPut("public")]
    public async Task SetPublic(SetPublicDto inDto)
    {
        var wrappers = new List<AceWrapper>() { new AceWrapper() { Id = Constants.GroupEveryone.ID, Access = inDto.Public ? FileShare.Read : FileShare.None, SubjectType = SubjectType.Group } };
        var aceCollection = new AceCollection<int>
        {
            Files = Array.Empty<int>(),
            Folders = [inDto.Id],
            Aces = wrappers,
            Message = string.Empty
        };

        var warning = await fileStorageService.SetAceObjectAsync(aceCollection, false);
    }
}
