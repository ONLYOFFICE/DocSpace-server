// (c) Copyright Ascensio System SIA 2009-2025
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

    /// <summary>
    /// Starts creating the room template.
    /// </summary>
    /// <short>Start creating room template</short>
    /// <path>api/2.0/files/roomtemplate</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomTemplateStatusDto))]
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
        return await GetTemplateCreatingStatus();
    }

    /// <summary>
    /// Returns the progress status of the room template creation process.
    /// </summary>
    /// <short>Get status of room template creation</short>
    /// <path>api/2.0/files/roomtemplate/status</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomTemplateStatusDto))]
    [HttpGet("status")]
    public async Task<RoomTemplateStatusDto> GetTemplateCreatingStatus()
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


    /// <summary>
    /// Returns the public settings of the room template with the ID specified in the request.
    /// </summary>
    /// <short>Get public settings</short>
    /// <path>api/2.0/files/roomtemplate/{id}/public</path>
    [Tags("Rooms")]
    [HttpGet("{id}/public")]
    public async Task<bool> IsPublic(PublicDto inDto)
    {
        return await fileStorageService.IsPublicAsync(inDto.Id);
    }


    /// <summary>
    /// Sets the public settings for the room template with the ID specified in the request.
    /// </summary>
    /// <short>Set public settings</short>
    /// <path>api/2.0/files/roomtemplate/public</path>
    [Tags("Rooms")]
    [HttpPut("public")]
    public async Task SetPublic(SetPublicDto inDto)
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
