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

[ConstraintRoute("int")]
public class SecurityControllerInternal(
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileService fileService,
    SharingService sharingService,
    FileShareDtoHelper fileShareDtoHelper)
    : SecurityController<int>(folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper, fileService, sharingService, fileShareDtoHelper);

public class SecurityControllerThirdparty(
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileService fileService,
    SharingService sharingService,
    FileShareDtoHelper fileShareDtoHelper)
    : SecurityController<string>(folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper, fileService, sharingService, fileShareDtoHelper);

public abstract class SecurityController<T>(
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileService fileService,
    SharingService sharingService,
    FileShareDtoHelper fileShareDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the detailed information about the shared file with the ID specified in the request.
    /// </summary>
    /// <short>Get the shared file information</short>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("file/{fileId}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFileSecurityInfoAsync(FileIdRequestDto<T> inDto)
    {        
        var fileShares = await sharingService.GetSharedInfoAsync([inDto.FileId], []);

        foreach (var fileShareDto in fileShares)
        {
            yield return await fileShareDtoHelper.Get(fileShareDto);
        }
    }

    /// <summary>
    /// Returns the detailed information about the shared folder with the ID specified in the request.
    /// </summary>
    /// <short>Get the shared folder information</short>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("folder/{folderId}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFolderSecurityInfoAsync(FolderIdRequestDto<T> inDto)
    {
        var fileShares = await sharingService.GetSharedInfoAsync([], [inDto.FolderId ]);

        foreach (var fileShareDto in fileShares)
        {
            yield return await fileShareDtoHelper.Get(fileShareDto);
        }
    }

    /// <summary>
    /// Sets the sharing settings to a file with the ID specified in the request.
    /// </summary>
    /// <short>Share a file</short>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information: sharing rights, a user who has the access to the specified file, the file is locked by this user or not, this user is an owner of the specified file or not, this user can edit the access to the specified file or not", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPut("file/{fileId}/share")]
    public async IAsyncEnumerable<FileShareDto> SetFileSecurityInfoAsync(FileSecurityInfoSimpleRequestDto<T> inDto)
    {        
        var ids = await sharingService.SetSecurityInfoAsync([inDto.FileId], [], inDto.SecurityInfoSimpe.Share, inDto.SecurityInfoSimpe.Notify, inDto.SecurityInfoSimpe.SharingMessage);
        
        foreach (var r in ids)
        {
            yield return await fileShareDtoHelper.Get(r);
        }
    }

    /// <summary>
    /// Sets the sharing settings to a folder with the ID specified in the request.
    /// </summary>
    /// <short>Share a folder</short>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared folder information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPut("folder/{folderId}/share")]
    public async IAsyncEnumerable<FileShareDto> SetFolderSecurityInfoAsync(FolderSecurityInfoSimpleRequestDto<T> inDto)
    {
        var ids = await sharingService.SetSecurityInfoAsync([], [inDto.FolderId], inDto.SecurityInfoSimpe.Share, inDto.SecurityInfoSimpe.Notify, inDto.SecurityInfoSimpe.SharingMessage);
        
        foreach (var r in ids)
        {
            yield return await fileShareDtoHelper.Get(r);
        }
    }

    /// <summary>
    /// Returns the encryption keys to access a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file encryption keys</short>
    /// <path>api/2.0/files/file/{fileId}/publickeys</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of encryption key pairs: encrypted private key, public key, user ID", typeof(List<EncryptionKeyPairDto>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpGet("file/{fileId}/publickeys")]
    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccess(FileIdRequestDto<T> inDto)
    {
        return await fileService.GetEncryptionAccessAsync(inDto.FileId);
    }

    /// <summary>
    /// Sends a message to the users who are mentioned in the file with the ID specified in the request.
    /// </summary>
    /// <short>Send the mention message</short>
    /// <path>api/2.0/files/file/{fileId}/sendeditornotify</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of access rights information", typeof(List<AceShortWrapper>))]
    [HttpPost("file/{fileId}/sendeditornotify")]
    public async Task<List<AceShortWrapper>> SendEditorNotify(MentionMessageWrapperRequestDto<T> inDto)
    {
        return await fileService.SendEditorNotifyAsync(inDto.FileId, inDto.MentionMessage);
    }

    /// <summary>
    /// Returns the group memebers with their folder security information.
    /// </summary>
    /// <short>Get group members with security information</short>
    /// <path>api/2.0/files/folder/{folderId}/group/{groupId}/share</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<GroupMemberSecurityRequestDto>))]
    [HttpGet("folder/{folderId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityRequestDto> GetGroupsMembersWithFolderSecurityAsync(GroupMemberSecurityRequestDto<T> inDto)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.FolderId);
        var totalCount = await fileSharing.GetGroupMembersCountAsync(folder, inDto.GroupId, apiContext.FilterValue);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var memberSecurity in fileSharing.GetGroupMembersAsync(folder, inDto.GroupId, apiContext.FilterValue, offset, count))
        {
            yield return new GroupMemberSecurityRequestDto
            {
                User = await employeeFullDtoHelper.GetFullAsync(memberSecurity.User),
                GroupAccess = memberSecurity.GroupShare,
                CanEditAccess = memberSecurity.CanEditAccess,
                UserAccess = memberSecurity.UserShare,
                Overridden = memberSecurity.UserShare.HasValue,
                Owner = memberSecurity.Owner
            };
        }
    }
}

public class SecurityControllerCommon(
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    BruteForceLoginManager bruteForceLoginManager,
    ExternalLinkHelper externalLinkHelper,
    IMapper mapper,
    SharingService sharingService,
    FileShareDtoHelper fileShareDtoHelper,
    FileShareParamsHelper fileShareParamsHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Changes the owner of the file with the ID specified in the request.
    /// </summary>
    /// <short>Change the file owner</short>
    /// <path>api/2.0/files/owner</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "File entry information", typeof(IAsyncEnumerable<FileEntryDto>))]
    [HttpPost("owner")]
    public async IAsyncEnumerable<FileEntryDto> ChangeOwnerAsync(ChangeOwnerRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var data = AsyncEnumerable.Empty<FileEntry>();
        data = data.Concat(sharingService.ChangeOwnerAsync(folderIntIds, fileIntIds, inDto.UserId));
        data = data.Concat(sharingService.ChangeOwnerAsync(folderStringIds, fileStringIds, inDto.UserId));

        await foreach (var e in data)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <summary>
    /// Returns the sharing rights for all the files and folders specified in the request.
    /// </summary>
    /// <short>Get the sharing rights</short>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared files and folders information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPost("share")]
    public async IAsyncEnumerable<FileShareDto> GetSecurityInfoAsync(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);
        
        var internalIds = await sharingService.GetSharedInfoAsync(fileIntIds, folderIntIds);
        var thirdpartyIds = await sharingService.GetSharedInfoAsync(fileStringIds, folderStringIds);
        
        foreach (var r in internalIds.Concat(thirdpartyIds))
        {
            yield return await fileShareDtoHelper.Get(r);
        }
    }

    /// <summary>
    /// Removes the sharing rights from all the files and folders specified in the request.
    /// </summary>
    /// <short>Remove the sharing rights</short>
    /// <path>api/2.0/files/share</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpDelete("share")]
    public async Task<bool> RemoveSecurityInfoAsync(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);
        
        await sharingService.RemoveAceAsync(fileIntIds, folderIntIds);
        await sharingService.RemoveAceAsync(fileStringIds, folderStringIds);

        return true;
    }


    /// <summary>
    /// Sets the sharing rights to all the files and folders specified in the request.
    /// </summary>
    /// <short>Set the sharing rights</short>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared files and folders information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPut("share")]
    public async IAsyncEnumerable<FileShareDto> SetSecurityInfoAsync(SecurityInfoRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var internalIds = await sharingService.SetSecurityInfoAsync(fileIntIds, folderIntIds, inDto.Share, inDto.Notify, inDto.SharingMessage);
        var thirdpartyIds = await sharingService.SetSecurityInfoAsync(fileStringIds, folderStringIds, inDto.Share, inDto.Notify, inDto.SharingMessage);
        
        foreach (var r in internalIds.Concat(thirdpartyIds))
        {
            yield return await fileShareDtoHelper.Get(r);
        }
    }

    /// <summary>
    /// Returns the external data by the key specified in the request.
    /// </summary>
    /// <short>Get the external data</short>
    /// <path>api/2.0/files/share/{key}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "External data", typeof(ExternalShareDto))]
    [AllowAnonymous]
    [HttpGet("share/{key}")]
    public async Task<ExternalShareDto> GetExternalShareDataAsync(ExternalShareDataRequestDto inDto)
    {
        var validationInfo = await externalLinkHelper.ValidateAsync(inDto.Key, fileId: inDto.FileId);

        return mapper.Map<ValidationInfo, ExternalShareDto>(validationInfo);
    }

    /// <summary>
    /// Applies a password specified in the request to get the external data.
    /// </summary>
    /// <short>Apply external data password</short>
    /// <path>api/2.0/files/share/{key}/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "External data", typeof(ExternalShareDto))]
    [SwaggerResponse(429, "Too many requests")]
    [AllowAnonymous]
    [HttpPost("share/{key}/password")]
    public async Task<ExternalShareDto> ApplyExternalSharePasswordAsync(ExternalShareRequestDto inDto)
    {
        var ip = MessageSettings.GetIP(Request);
        
        await bruteForceLoginManager.IncrementAsync(inDto.Key, ip, true, FilesCommonResource.ErrorMessage_SharePasswordManyAttempts);
        
        var validationInfo =  await externalLinkHelper.ValidateAsync(inDto.Key, inDto.RequestParam.Password);

        if (validationInfo.Status != Status.InvalidPassword)
        {
            await bruteForceLoginManager.DecrementAsync(inDto.Key, ip);
        }

        return mapper.Map<ValidationInfo, ExternalShareDto>(validationInfo);
    }
}