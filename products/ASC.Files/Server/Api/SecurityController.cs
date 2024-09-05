// (c) Copyright Ascensio System SIA 2009-2024
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
public class SecurityControllerInternal(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        IDaoFactory daoFactory,
        FileSharing fileSharing,
        EmployeeFullDtoHelper employeeFullDtoHelper)
    : SecurityController<int>(fileStorageService, securityControllerHelper, folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper);

public class SecurityControllerThirdparty(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        IDaoFactory daoFactory,
        FileSharing fileSharing,
        EmployeeFullDtoHelper employeeFullDtoHelper)
    : SecurityController<string>(fileStorageService, securityControllerHelper, folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper);

public abstract class SecurityController<T>(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        IDaoFactory daoFactory,
        FileSharing fileSharing,
        EmployeeFullDtoHelper employeeFullDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the detailed information about the shared file with the ID specified in the request.
    /// </summary>
    /// <short>Get the shared file information</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information", typeof(FileShareDto))]
    [HttpGet("file/{fileId}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFileSecurityInfoAsync(T fileId)
    {
        await foreach (var s in securityControllerHelper.GetFileSecurityInfoAsync(fileId))
        {
            yield return s;
        }
    }

    /// <summary>
    /// Returns the detailed information about the shared folder with the ID specified in the request.
    /// </summary>
    /// <short>Get the shared folder information</short>
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information", typeof(FileShareDto))]
    [HttpGet("folder/{folderId}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFolderSecurityInfoAsync(T folderId)
    {
        await foreach (var s in securityControllerHelper.GetFolderSecurityInfoAsync(folderId))
        {
            yield return s;
        }
    }

    /// <summary>
    /// Sets the sharing settings to a file with the ID specified in the request.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <short>Share a file</short>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information: sharing rights, a user who has the access to the specified file, the file is locked by this user or not, this user is an owner of the specified file or not, this user can edit the access to the specified file or not", typeof(FileShareDto))]
    [HttpPut("file/{fileId}/share")]
    public async IAsyncEnumerable<FileShareDto> SetFileSecurityInfoAsync(T fileId, SecurityInfoSimpeRequestDto inDto)
    {
        await foreach (var s in securityControllerHelper.SetSecurityInfoAsync(new List<T> { fileId }, new List<T>(), inDto.Share, inDto.Notify, inDto.SharingMessage))
        {
            yield return s;
        }
    }

    /// <summary>
    /// Sets the sharing settings to a folder with the ID specified in the request.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <short>Share a folder</short>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared folder information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not", typeof(FileShareDto))]
    [HttpPut("folder/{folderId}/share")]
    public async IAsyncEnumerable<FileShareDto> SetFolderSecurityInfoAsync(T folderId, SecurityInfoSimpeRequestDto inDto)
    {
        await foreach (var s in securityControllerHelper.SetSecurityInfoAsync(new List<T>(), new List<T> { folderId }, inDto.Share, inDto.Notify, inDto.SharingMessage))
        {
            yield return s;
        }
    }

    /// <summary>
    /// Returns the encryption keys to access a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file encryption keys</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/publickeys</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of encryption key pairs: encrypted private key, public key, user ID", typeof(List<EncryptionKeyPairDto>))]
    [HttpGet("file/{fileId}/publickeys")]
    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccess(T fileId)
    {
        return await fileStorageService.GetEncryptionAccessAsync(fileId);
    }

    /// <summary>
    /// Sends a message to the users who are mentioned in the file with the ID specified in the request.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <short>Send the mention message</short>
    /// <path>api/2.0/files/file/{fileId}/sendeditornotify</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of access rights information", typeof(List<AceShortWrapper>))]
    [HttpPost("file/{fileId}/sendeditornotify")]
    public async Task<List<AceShortWrapper>> SendEditorNotify(T fileId, MentionMessageWrapper mentionMessage)
    {
        return await fileStorageService.SendEditorNotifyAsync(fileId, mentionMessage);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [HttpGet("folder/{folderId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityDto> GetGroupsMembersWithFolderSecurityAsync(T folderId, Guid groupId)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId);
        var totalCount = await fileSharing.GetGroupMembersCountAsync(folder, groupId, apiContext.FilterValue);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var memberSecurity in fileSharing.GetGroupMembersAsync(folder, groupId, apiContext.FilterValue, offset, count))
        {
            yield return new GroupMemberSecurityDto
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

public class SecurityControllerCommon(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        BruteForceLoginManager bruteForceLoginManager,
        IHttpContextAccessor httpContextAccessor,
        ExternalLinkHelper externalLinkHelper,
        IMapper mapper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Changes the owner of the file with the ID specified in the request.
    /// </summary>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.ChangeOwnerRequestDto, ASC.Files.Core" name="inDto">Request parameters for changing the file owner</param>
    /// <short>Change the file owner</short>
    /// <path>api/2.0/files/owner</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "File entry information", typeof(FileEntryDto))]
    [HttpPost("owner")]
    public async IAsyncEnumerable<FileEntryDto> ChangeOwnerAsync(ChangeOwnerRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var data = AsyncEnumerable.Empty<FileEntry>();
        data = data.Concat(fileStorageService.ChangeOwnerAsync(folderIntIds, fileIntIds, inDto.UserId));
        data = data.Concat(fileStorageService.ChangeOwnerAsync(folderStringIds, fileStringIds, inDto.UserId));

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
    [SwaggerResponse(200, "List of shared files and folders information", typeof(FileShareDto))]
    [HttpPost("share")]
    public async IAsyncEnumerable<FileShareDto> GetSecurityInfoAsync(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var internalIds = securityControllerHelper.GetSecurityInfoAsync(fileIntIds, folderIntIds);
        var thirdpartyIds = securityControllerHelper.GetSecurityInfoAsync(fileStringIds, folderStringIds);

        await foreach (var r in internalIds.Concat(thirdpartyIds))
        {
            yield return r;
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

        await securityControllerHelper.RemoveSecurityInfoAsync(fileIntIds, folderIntIds);
        await securityControllerHelper.RemoveSecurityInfoAsync(fileStringIds, folderStringIds);

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
    [SwaggerResponse(200, "List of shared files and folders information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not", typeof(FileShareDto))]
    [HttpPut("share")]
    public async IAsyncEnumerable<FileShareDto> SetSecurityInfoAsync(SecurityInfoRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var internalIds = securityControllerHelper.SetSecurityInfoAsync(fileIntIds, folderIntIds, inDto.Share, inDto.Notify, inDto.SharingMessage);
        var thirdpartyIds = securityControllerHelper.SetSecurityInfoAsync(fileStringIds, folderStringIds, inDto.Share, inDto.Notify, inDto.SharingMessage);

        await foreach (var s in internalIds.Concat(thirdpartyIds))
        {
            yield return s;
        }
    }

    /// <summary>
    /// Returns the external data by the key specified in the request.
    /// </summary>
    /// <short>Get the external data</short>
    /// <param type="System.String, System" name="key" method="url" example="some text">The unique document identifier</param>
    /// <param type="System.String, System" name="fileId">The unique document identifier</param>    /// <path>api/2.0/files/share/{key}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "External data", typeof(ExternalShareDto))]
    [AllowAnonymous]
    [HttpGet("share/{key}")]
    public async Task<ExternalShareDto> GetExternalShareDataAsync(string key, string fileId = null)
    {
        var validationInfo = await externalLinkHelper.ValidateAsync(key, fileId: fileId);

        return mapper.Map<ValidationInfo, ExternalShareDto>(validationInfo);
    }

    /// <summary>
    /// Applies a password specified in the request to get the external data.
    /// </summary>
    /// <short>Apply external data password</short>
    /// <param type="System.String, System" name="key" method="url" example="some text">The unique document identifier</param>
    /// <path>api/2.0/files/share/{key}/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "External data", typeof(ExternalShareDto))]
    [AllowAnonymous]
    [HttpPost("share/{key}/password")]
    public async Task<ExternalShareDto> ApplyExternalSharePasswordAsync(string key, ExternalShareRequestDto inDto)
    {
        var ip = MessageSettings.GetIP(httpContextAccessor.HttpContext?.Request);
        
        await bruteForceLoginManager.IncrementAsync(key, ip, true, FilesCommonResource.ErrorMessage_SharePasswordManyAttempts);
        
        var validationInfo =  await externalLinkHelper.ValidateAsync(key, inDto.Password);

        if (validationInfo.Status != Status.InvalidPassword)
        {
            await bruteForceLoginManager.DecrementAsync(key, ip);
        }

        return mapper.Map<ValidationInfo, ExternalShareDto>(validationInfo);
    }
}