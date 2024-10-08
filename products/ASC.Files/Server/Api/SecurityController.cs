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
    /// <category>Sharing</category>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">List of shared file information</returns>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
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
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <category>Sharing</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">List of shared folder information</returns>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
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
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SecurityInfoRequestDto, ASC.Files.Core" name="inDto">Security information request parameters</param>
    /// <short>Share a file</short>
    /// <category>Sharing</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">List of shared file information: sharing rights, a user who has the access to the specified file, the file is locked by this user or not, this user is an owner of the specified file or not, this user can edit the access to the specified file or not</returns>
    /// <path>api/2.0/files/file/{fileId}/share</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpPut("file/{fileId}/share")]
    public async IAsyncEnumerable<FileShareDto> SetFileSecurityInfoAsync(T fileId, SecurityInfoRequestDto inDto)
    {
        await foreach (var s in securityControllerHelper.SetSecurityInfoAsync(new List<T> { fileId }, new List<T>(), inDto.Share, inDto.Notify, inDto.SharingMessage))
        {
            yield return s;
        }
    }

    /// <summary>
    /// Sets the sharing settings to a folder with the ID specified in the request.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="folderId">Folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SecurityInfoRequestDto, ASC.Files.Core" name="inDto">Security information request parameters</param>
    /// <short>Share a folder</short>
    /// <category>Sharing</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto}, ASC.Files.Core">List of shared folder information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not</returns>
    /// <path>api/2.0/files/folder/{folderId}/share</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpPut("folder/{folderId}/share")]
    public async IAsyncEnumerable<FileShareDto> SetFolderSecurityInfoAsync(T folderId, SecurityInfoRequestDto inDto)
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
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <category>Sharing</category>
    /// <returns type="ASC.Web.Files.Core.Entries.EncryptionKeyPairDto, ASC.Files.Core">List of encryption key pairs: encrypted private key, public key, user ID</returns>
    /// <path>api/2.0/files/file/{fileId}/publickeys</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpGet("file/{fileId}/publickeys")]
    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccess(T fileId)
    {
        return await fileStorageService.GetEncryptionAccessAsync(fileId);
    }

    /// <summary>
    /// Sends a message to the users who are mentioned in the file with the ID specified in the request.
    /// </summary>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <param type="ASC.Web.Files.Services.WCFService.MentionMessageWrapper, ASC.Files.Core" name="mentionMessage">Mention message request parameters</param>
    /// <short>Send the mention message</short>
    /// <category>Sharing</category>
    /// <returns type="ASC.Web.Files.Services.WCFService.AceShortWrapper, ASC.Files.Core">List of access rights information</returns>
    /// <path>api/2.0/files/file/{fileId}/sendeditornotify</path>
    /// <httpMethod>POST</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpPost("file/{fileId}/sendeditornotify")]
    public async Task<List<AceShortWrapper>> SendEditorNotify(T fileId, MentionMessageWrapper mentionMessage)
    {
        return await fileStorageService.SendEditorNotifyAsync(fileId, mentionMessage);
    }
    
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
    /// <category>Sharing</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core">File entry information</returns>
    /// <path>api/2.0/files/owner</path>
    /// <httpMethod>POST</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
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
    /// <category>Sharing</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">List of shared files and folders information</returns>
    /// <path>api/2.0/files/share</path>
    /// <httpMethod>POST</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
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
    /// <category>Sharing</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/share</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <visible>false</visible>
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
    /// <category>Sharing</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SecurityInfoRequestDto, ASC.Files.Core" name="inDto">Security information request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">List of shared files and folders information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not</returns>
    /// <path>api/2.0/files/share</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
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
    /// <category>Sharing</category>
    /// <param type="System.String, System" name="key" method="url">The unique key</param>
    /// <param type="System.String, System" name="fileId">The unique document identifier</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.ExternalShareDto, ASC.Files.Core">External data</returns>
    /// <path>api/2.0/files/share/{key}</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    /// <requiresAuthorization>false</requiresAuthorization>
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
    /// <category>Sharing</category>
    /// <param type="System.String, System" name="key" method="url">The unique document identifier</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.ExternalShareRequestDto, ASC.Files.Core" name="inDto">External data request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.ExternalShareDto, ASC.Files.Core">External data</returns>
    /// <path>api/2.0/files/share/{key}/password</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    /// <requiresAuthorization>false</requiresAuthorization>
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