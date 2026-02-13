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

using HtmlAgilityPack;

namespace ASC.Files.Api;

[ConstraintRoute("int")]
public class SecurityControllerInternal(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        IDaoFactory daoFactory,
        FileSharing fileSharing,
        EmployeeFullDtoHelper employeeFullDtoHelper,
        FileShareDtoHelper fileShareDtoHelper)
    : SecurityController<int>(fileStorageService, securityControllerHelper, folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper, fileShareDtoHelper);

public class SecurityControllerThirdparty(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        IDaoFactory daoFactory,
        FileSharing fileSharing,
        EmployeeFullDtoHelper employeeFullDtoHelper,
        FileShareDtoHelper fileShareDtoHelper)
    : SecurityController<string>(fileStorageService, securityControllerHelper, folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper, fileShareDtoHelper);

public abstract class SecurityController<T>(
    FileStorageService fileStorageService,
    SecurityControllerHelper securityControllerHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileShareDtoHelper fileShareDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns the detailed information about the shared file with the ID specified in the request.
    /// </remarks>
    /// <summary>Get the shared file information</summary>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("file/{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFileSecurityInfo(FilePrimaryIdRequestDto<T> inDto)
    {
        var counter = 0;

        await foreach (var ace in fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.File, ShareFilterType.UserOrGroup, null, inDto.StartIndex, inDto.Count))
        {
            counter++;

            yield return await fileShareDtoHelper.Get(ace);
        }

        apiContext.SetCount(counter);
        apiContext.SetTotalCount(await fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.File, ShareFilterType.UserOrGroup, null));
    }

    /// <remarks>
    /// Returns the detailed information about the shared folder with the ID specified in the request.
    /// </remarks>
    /// <summary>Get the shared folder information</summary>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("folder/{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFolderSecurityInfo(FolderPrimaryIdRequestDto<T> inDto)
    {
        var counter = 0;

        await foreach (var ace in fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, null, inDto.StartIndex, inDto.Count))
        {
            counter++;

            yield return await fileShareDtoHelper.Get(ace);
        }

        apiContext.SetCount(counter);
        apiContext.SetTotalCount(await fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, null));
    }

    /// <remarks>
    /// Sets the sharing settings to a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Share a file</summary>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared file information: sharing rights, a user who has the access to the specified file, the file is locked by this user or not, this user is an owner of the specified file or not, this user can edit the access to the specified file or not", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPut("file/{fileId}/share")]
    public IAsyncEnumerable<FileShareDto> SetFileSecurityInfo(FileSecurityInfoSimpleRequestDto<T> inDto)
    {                
        string text = null;

        if (inDto.SecurityInfoSimple.SharingMessage != null)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(inDto.SecurityInfoSimple.SharingMessage);
            text = htmlDoc.DocumentNode.InnerText;
        }

        return securityControllerHelper.SetSecurityInfoAsync([inDto.FileId], [], inDto.SecurityInfoSimple.Share, inDto.SecurityInfoSimple.Notify, text);
    }

    /// <remarks>
    /// Sets the sharing settings to a folder with the ID specified in the request.
    /// </remarks>
    /// <summary>Share a folder</summary>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared folder information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPut("folder/{folderId}/share")]
    public IAsyncEnumerable<FileShareDto> SetFolderSecurityInfo(FolderSecurityInfoSimpleRequestDto<T> inDto)
    {        
        string text = null;

        if (inDto.SecurityInfoSimple.SharingMessage != null)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(inDto.SecurityInfoSimple.SharingMessage);
            text = htmlDoc.DocumentNode.InnerText;
        }
        
        return securityControllerHelper.SetSecurityInfoAsync([], [inDto.FolderId], inDto.SecurityInfoSimple.Share, inDto.SecurityInfoSimple.Notify, text);
    }

    /// <remarks>
    /// Returns the encryption keys to access a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Get file encryption keys</summary>
    /// <path>api/2.0/files/file/{fileId}/publickeys</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of encryption key pairs: encrypted private key, public key, user ID", typeof(List<EncryptionKeyPairDto>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpGet("file/{fileId}/publickeys")]
    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccess(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.GetEncryptionAccessAsync(inDto.FileId);
    }

    /// <remarks>
    /// Sends a message to the users who are mentioned in the file with the ID specified in the request.
    /// </remarks>
    /// <summary>Send the mention message</summary>
    /// <path>api/2.0/files/file/{fileId}/sendeditornotify</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of access rights information", typeof(List<AceShortWrapper>))]
    [HttpPost("file/{fileId}/sendeditornotify")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<List<AceShortWrapper>> SendEditorNotify(MentionMessageWrapperRequestDto<T> inDto)
    {
        return await fileStorageService.SendEditorNotifyAsync(inDto.FileId, inDto.MentionMessage);
    }

    /// <remarks>
    /// Returns the group members with their folder security information.
    /// </remarks>
    /// <summary>Get folder group members with security information</summary>
    /// <path>api/2.0/files/folder/{folderId}/group/{groupId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<GroupMemberSecurityRequestDto>))]
    [HttpGet("folder/{folderId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityRequestDto> GetGroupsMembersWithFolderSecurity(GroupMemberSecurityFolderRequestDto<T> inDto)
    {
        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;

        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.FolderId);
        var totalCount = await fileSharing.GetGroupMembersCountAsync(folder, inDto.GroupId, text);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var memberSecurity in fileSharing.GetGroupMembersAsync(folder, inDto.GroupId, text, offset, count))
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

    /// <remarks>
    /// Returns the group members with their file security information.
    /// </remarks>
    /// <summary>Get file group members with security information</summary>
    /// <path>api/2.0/files/file/{fileId}/group/{groupId}/share</path>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<GroupMemberSecurityRequestDto>))]
    [HttpGet("file/{fileId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityRequestDto> GetGroupsMembersWithFileSecurity(GroupMemberSecurityFileRequestDto<T> inDto)
    {
        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;

        var file = await daoFactory.GetFileDao<T>().GetFileAsync(inDto.FileId);
        var totalCount = await fileSharing.GetGroupMembersCountAsync(file, inDto.GroupId, text);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var memberSecurity in fileSharing.GetGroupMembersAsync(file, inDto.GroupId, text, offset, count))
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

public class SecurityControllerCommon(FileStorageService fileStorageService,
        SecurityControllerHelper securityControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        BruteForceLoginManager bruteForceLoginManager,
        ExternalLinkHelper externalLinkHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Changes the owner of the file with the ID specified in the request.
    /// </remarks>
    /// <summary>Change the file owner</summary>
    /// <path>api/2.0/files/owner</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "File entry information", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [HttpPost("owner")]
    public async IAsyncEnumerable<FileEntryBaseDto> ChangeFileOwner(ChangeOwnerRequestDto inDto)
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

    /// <remarks>
    /// Returns the sharing rights for all the files and folders specified in the request.
    /// </remarks>
    /// <summary>Get the sharing rights</summary>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared files and folders information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPost("share")]
    public async IAsyncEnumerable<FileShareDto> GetSecurityInfo(BaseBatchRequestDto inDto)
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

    /// <remarks>
    /// Removes the sharing rights from all the files and folders specified in the request.
    /// </remarks>
    /// <summary>Remove the sharing rights</summary>
    /// <path>api/2.0/files/share</path>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpDelete("share")]
    public async Task<bool> RemoveSecurityInfo(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        await fileStorageService.RemoveAceAsync(fileIntIds, folderIntIds);
        await fileStorageService.RemoveAceAsync(fileStringIds, folderStringIds);

        return true;
    }


    /// <remarks>
    /// Sets the sharing rights to all the files and folders specified in the request.
    /// </remarks>
    /// <summary>Set the sharing rights</summary>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of shared files and folders information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpPut("share")]
    public async IAsyncEnumerable<FileShareDto> SetSecurityInfo(SecurityInfoRequestDto inDto)
    {
        string text = null;

        if (inDto.SharingMessage != null)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(inDto.SharingMessage);
            text = htmlDoc.DocumentNode.InnerText;
        }
        
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var internalIds = securityControllerHelper.SetSecurityInfoAsync(fileIntIds, folderIntIds, inDto.Share, inDto.Notify, text);
        var thirdpartyIds = securityControllerHelper.SetSecurityInfoAsync(fileStringIds, folderStringIds, inDto.Share, inDto.Notify, text);

        await foreach (var s in internalIds.Concat(thirdpartyIds))
        {
            yield return s;
        }
    }

    /// <remarks>
    /// Returns the external data by the key specified in the request.
    /// </remarks>
    /// <summary>Get the external data</summary>
    /// <path>api/2.0/files/share/{key}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "External data", typeof(ExternalShareDto))]
    [AllowAnonymous]
    [HttpGet("share/{key}")]
    public async Task<ExternalShareDto> GetExternalShareData(ExternalShareDataRequestDto inDto)
    {
        var validationInfo = await externalLinkHelper.ValidateAsync(inDto.Key, fileId: inDto.FileId, folderId: inDto.FolderId);

        return validationInfo.Map();
    }

    /// <remarks>
    /// Applies a password specified in the request to get the external data.
    /// </remarks>
    /// <summary>Apply external data password</summary>
    /// <path>api/2.0/files/share/{key}/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "External data", typeof(ExternalShareDto))]
    [SwaggerResponse(429, "Too many requests")]
    [AllowAnonymous]
    [HttpPost("share/{key}/password")]
    public async Task<ExternalShareDto> ApplyExternalSharePassword(ExternalShareRequestDto inDto)
    {
        var ip = MessageSettings.GetIP(Request);

        await bruteForceLoginManager.IncrementAsync(inDto.Key, ip, true, FilesCommonResource.ErrorMessage_SharePasswordManyAttempts);

        var validationInfo = await externalLinkHelper.ValidateAsync(inDto.Key, inDto.RequestParam.Password);

        if (validationInfo.Status != Status.InvalidPassword)
        {
            await bruteForceLoginManager.DecrementAsync(inDto.Key, ip);
        }

        return validationInfo.Map();
    }
}