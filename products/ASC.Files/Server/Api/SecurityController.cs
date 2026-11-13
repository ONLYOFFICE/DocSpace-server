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

using HtmlAgilityPack;

namespace ASC.Files.Api;

[ConstraintRoute("int")]
public class SecurityControllerInternal(
    UserManager userManager,
    AuthContext authContext,
    FileStorageService fileStorageService,
    SecurityControllerHelper securityControllerHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileShareDtoHelper fileShareDtoHelper)
    : SecurityController<int>(userManager, authContext, fileStorageService, securityControllerHelper, folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper, fileShareDtoHelper);

public class SecurityControllerThirdparty(
    UserManager userManager,
    AuthContext authContext,
    FileStorageService fileStorageService,
    SecurityControllerHelper securityControllerHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileShareDtoHelper fileShareDtoHelper)
    : SecurityController<string>(userManager, authContext, fileStorageService, securityControllerHelper, folderDtoHelper, fileDtoHelper, apiContext, daoFactory, fileSharing, employeeFullDtoHelper, fileShareDtoHelper);

public abstract class SecurityController<T>(
    UserManager userManager,
    AuthContext authContext,
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
    /// Returns the accounts and groups that have access to a file, with the access level of each. The owner comes
    /// first, and accounts that have not accepted their invitation come last. The list has no external links of the
    /// file. To read them, use `GET api/2.0/files/file/{id}/links`. The list of a PDF form in a form-filling room
    /// ends with the room's external link, if any. Any signed-in user can call it, except a guest. A guest gets 403.
    /// A caller who cannot read the file also gets 403. A caller who can read the file but not change its access
    /// gets an empty list. The total in the response headers still counts the hidden entries. Inside a public room,
    /// read access is enough. The call is read-only. Use `startIndex` and `count` to page through the list. The
    /// response headers carry the total. A `count` out of range returns 400. If the file does not exist, the call
    /// returns 404. To read several entries in one call, use `POST api/2.0/files/share`.
    /// </remarks>
    /// <summary>Get file sharing rights</summary>
    /// <path>api/2.0/files/file/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The accounts and groups that hold rights on the file, the owner first", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest or may not read the file")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpGet("file/{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFileSecurityInfo(FilePrimaryIdRequestDto<T> inDto)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

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
    /// Returns the accounts and groups that have access to a folder or room, with the access level of each. The
    /// owner comes first, and accounts that have not accepted their invitation come last. The list has no external
    /// links. To read them, use `GET api/2.0/files/folder/{id}/links`. Any signed-in user can call it, except a
    /// guest. A guest gets 403. A caller who cannot read the folder also gets 403. For a room or a folder in a public
    /// room, read access is enough. Any other folder requires the right to change its access. A caller who can only
    /// read such a folder gets an empty list. The total in the response headers still counts the hidden entries.
    /// The call is read-only. Use `startIndex` and `count` to page through the list. The response headers carry the
    /// total. A `count` out of range returns 400. If the folder does not exist, the call returns 404. For a room, use
    /// `GET api/2.0/files/rooms/{id}/share`, which also filters by kind and name.
    /// </remarks>
    /// <summary>Get folder sharing rights</summary>
    /// <path>api/2.0/files/folder/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The accounts and groups that hold rights on the folder, the owner first", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest or may not read the folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpGet("folder/{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetFolderSecurityInfo(FolderPrimaryIdRequestDto<T> inDto)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

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
    /// Changes the access of accounts and groups to a file. The access `None` deletes the rights. Requires the right to
    /// manage access to the file. Other callers get 403. The call changes data, and you can repeat it with the same
    /// body. It skips subjects the caller may not share with. It also skips deactivated accounts, deleted groups, and
    /// files outside My documents and rooms. If `notify` is true, the portal emails the accounts about the change. An
    /// account that got an email about the same access to this file recently gets no new one. A new `sharingMessage`
    /// does not change this. The call returns the new rights of the listed subjects. A repeat skips subjects that
    /// already have the listed access, so it sends no email. A repeated `None` changes nothing but returns the rights
    /// of the other listed subjects. An empty `share` or an unknown file changes nothing and returns an empty list. To
    /// change several entries in one call, use `PUT api/2.0/files/share`. The call returns 400 for an invalid or too
    /// long `email`, too many email invitations, or a too long `sharingMessage`. It also returns 403 if a subject
    /// cannot get the access.
    /// </remarks>
    /// <summary>Share a file</summary>
    /// <path>api/2.0/files/file/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rights the listed subjects hold on the file after the change", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "The request body cannot be read, an `email` in `share` is malformed or longer than 255 characters, `share` invites more addresses by email than the portal allows at once, or `sharingMessage` is longer than 255 characters")]
    [SwaggerResponse(403, "The caller may not change the sharing of the file, or a listed subject cannot be given the requested access on it")]
    [HttpPut("file/{id}/share")]
    public IAsyncEnumerable<FileShareDto> SetFileSecurityInfo(FileSecurityInfoSimpleRequestDto<T> inDto)
    {
        string text = null;

        if (inDto.SecurityInfoSimple.SharingMessage != null)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(inDto.SecurityInfoSimple.SharingMessage);
            text = htmlDoc.DocumentNode.InnerText;
        }

        return securityControllerHelper.SetSecurityInfoAsync([inDto.Id], [], inDto.SecurityInfoSimple.Share, inDto.SecurityInfoSimple.Notify, text);
    }

    /// <remarks>
    /// Changes the access of accounts and groups to a folder. The access `None` deletes the rights. Requires the right
    /// to manage access to the folder. Other callers get 403. The call changes data, and you can repeat it with the
    /// same body. It skips subjects the caller may not share with. It also skips deactivated accounts and deleted
    /// groups. If `notify` is true, the portal emails the accounts about the change. An account that got an email about
    /// the same access to this folder recently gets no new one. A new `sharingMessage` does not change this. The call
    /// returns the new rights of the listed subjects. A repeat skips subjects that already have the listed access, so
    /// it sends no email. A repeated `None` changes nothing but returns the rights of the other listed subjects. In a
    /// room, a repeated `None` for an account emails a room invitation if `notify` is true. An empty `share` or an
    /// unknown folder changes nothing and returns an empty list. For a room, use `PUT api/2.0/files/rooms/{id}/share`,
    /// which also invites people by email. The call returns 400 for an invalid or too long `email`, too many email
    /// invitations, or a too long `sharingMessage`. It also returns 403 if a subject cannot get the access, or lacks
    /// encryption keys for a private room.
    /// </remarks>
    /// <summary>Share a folder</summary>
    /// <path>api/2.0/files/folder/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rights the listed subjects hold on the folder after the change", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "The request body cannot be read, an `email` in `share` is malformed or longer than 255 characters, `share` invites more addresses by email than the portal allows at once, or `sharingMessage` is longer than 255 characters")]
    [SwaggerResponse(403, "The caller may not change the sharing of the folder, a listed subject cannot be given the requested access on it, or the folder is a private room and a listed account has not set up its encryption keys")]
    [HttpPut("folder/{id}/share")]
    public IAsyncEnumerable<FileShareDto> SetFolderSecurityInfo(FolderSecurityInfoSimpleRequestDto<T> inDto)
    {
        string text = null;

        if (inDto.SecurityInfoSimple.SharingMessage != null)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(inDto.SecurityInfoSimple.SharingMessage);
            text = htmlDoc.DocumentNode.InnerText;
        }

        return securityControllerHelper.SetSecurityInfoAsync([], [inDto.Id], inDto.SecurityInfoSimple.Share, inDto.SecurityInfoSimple.Notify, text);
    }

    /// <remarks>
    /// Returns the encryption keys that open a file in a private room. The list has the public keys of each member
    /// who has access to the file and has keys. The caller's own entries also have the encrypted private key.
    /// The entries of other members have no private key.
    /// The call skips members without keys and members who have access only through a group. A member without keys
    /// cannot open the file until they add keys with `POST api/2.0/privacyroom/keys`. Requires read access to the
    /// file. Other callers get 403. The call is read-only. Members can add or change keys at any time. So request the
    /// keys again before each use. If the file does not exist, the call returns 404. If the file is outside a private
    /// room and the encrypted section, the call returns 415.
    /// </remarks>
    /// <summary>Get file encryption keys</summary>
    /// <path>api/2.0/files/file/{fileId}/publickeys</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The keys of the members who can open the file, the private half only for the caller", typeof(List<EncryptionKeyDto>))]
    [SwaggerResponse(403, "The caller may not read the file")]
    [SwaggerResponse(404, "The file does not exist")]
    [SwaggerResponse(415, "The file lies neither in a private room nor in the encrypted section")]
    [HttpGet("file/{fileId}/publickeys")]
    public async Task<List<EncryptionKeyDto>> GetEncryptionAccess(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.GetEncryptionAccessAsync(inDto.FileId);
    }

    /// <remarks>
    /// Sends an email to the accounts mentioned in a file, with a link to the file. The call skips addresses that
    /// belong to no account. Requires the right to comment on the file. Other callers get 403. The call queues emails.
    /// Do not repeat it. Each call sends the emails again. It returns one entry per mentioned account, with the
    /// access of that account to the file. An account without access is in the list too. To give access, use
    /// `PUT api/2.0/files/file/{id}/share`. If sending fails, the call still returns 200. The call is rate-limited.
    /// The call returns 400 if `emails` is missing or `message` is too long. If the file does not exist, the call
    /// returns 404.
    /// </remarks>
    /// <summary>Notify mentioned users</summary>
    /// <path>api/2.0/files/file/{fileId}/sendeditornotify</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The people who currently have access to the file, when the caller still has to invite someone; empty otherwise", typeof(List<AceShortDto>))]
    [SwaggerResponse(400, "The address list is missing, or the message is longer than the field allows")]
    [SwaggerResponse(403, "The caller may not comment on the file")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpPost("file/{fileId}/sendeditornotify")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<List<AceShortDto>> SendEditorNotify(MentionMessageRequestDto<T> inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        return await fileStorageService.SendEditorNotifyAsync(inDto.FileId, inDto.MentionMessage);
    }

    /// <remarks>
    /// Returns the members of a group with their access to a folder or room that the group has access to. Any
    /// signed-in user can call it, except a guest. A guest gets 403. The call is read-only. It sorts the members by
    /// first name. Use `startIndex` and `count` to page through the list. The response headers carry the total. A
    /// `count` out of range returns 400. The call returns an empty list for a group without access to the folder,
    /// or an unknown folder. It also returns an empty list if the caller cannot read the folder. So an empty
    /// list does not mean that the group has no members.
    /// </remarks>
    /// <summary>Get folder access of group members</summary>
    /// <path>api/2.0/files/folder/{folderId}/group/{groupId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The members of the group with the access each of them has on the folder", typeof(IAsyncEnumerable<GroupMemberSecurityDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest")]
    [HttpGet("folder/{folderId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityDto> GetGroupsMembersWithFolderSecurity(GroupMemberSecurityFolderRequestDto<T> inDto)
    {

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;

        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.FolderId);
        var totalCount = await fileSharing.GetGroupMembersCountAsync(folder, inDto.GroupId, text);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var memberSecurity in fileSharing.GetGroupMembersAsync(folder, inDto.GroupId, text, offset, count))
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

    /// <remarks>
    /// Returns the members of a group with their access to a file that the group has access to. Any signed-in user
    /// can call it, except a guest. A guest gets 403. The call is read-only. It sorts the members by first name. Use
    /// `startIndex` and `count` to page through the list. The response headers carry the total. A `count` out of
    /// range returns 400. The call returns an empty list for a group without access to the file, or an
    /// unknown file. It also returns an empty list if the caller cannot read the file. So an empty list does not mean
    /// that the group has no members.
    /// </remarks>
    /// <summary>Get file access of group members</summary>
    /// <path>api/2.0/files/file/{fileId}/group/{groupId}/share</path>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The members of the group with the access each of them has on the file", typeof(IAsyncEnumerable<GroupMemberSecurityDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest")]
    [HttpGet("file/{fileId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityDto> GetGroupsMembersWithFileSecurity(GroupMemberSecurityFileRequestDto<T> inDto)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;

        var file = await daoFactory.GetFileDao<T>().GetFileAsync(inDto.FileId);
        var totalCount = await fileSharing.GetGroupMembersCountAsync(file, inDto.GroupId, text);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var memberSecurity in fileSharing.GetGroupMembersAsync(file, inDto.GroupId, text, offset, count))
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
        ExternalLinkHelper externalLinkHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Changes the owner of the listed rooms and files to one account. Requires the owner of each listed entry, or a
    /// DocSpace admin. Other callers get 403. The call changes data, one entry at a time. It stops at the first entry
    /// it may not change, and earlier entries stay changed. An entry that the account already owns stays unchanged,
    /// so you can repeat the call. The previous owner stays in a transferred room as its room manager. A transferred
    /// file gets a new version by the new owner. The call skips third-party files. The call returns 403 if the
    /// new owner is not an active DocSpace admin or room admin. It also returns 403 for a private room if the new
    /// owner has no encryption keys. It also returns 403 for a folder that is not a room, or a file outside the
    /// common section. A file that is locked or being edited also returns 403. If the new owner lacks keys to some
    /// encrypted files of a private room, the call returns 409. To find these files, use
    /// `GET api/2.0/privacyroom/{roomId}/access/missing`. After their keys are added, repeat the call. Without
    /// `userId`, the call returns 400. It also returns 400 if a string file id points to a storage account that is
    /// not connected.
    /// </remarks>
    /// <summary>Change the room or file owner</summary>
    /// <path>api/2.0/files/owner</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "for every listed room whose owner changes")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead for a listed AI agent whose owner changes")]
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "for every file whose owner changes")]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rooms and files whose owner has been changed, as folder and file objects", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `userId`, or a third-party file identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The new owner is not an active account allowed to manage rooms or has not set up encryption keys for a listed private room, the caller may not change the owner of a listed entry, a listed folder lies outside the rooms and common sections, or a listed file is locked, being edited or lies outside the common section")]
    [SwaggerResponse(409, "The new owner holds no key for some encrypted files of a listed private room: `GET api/2.0/privacyroom/{roomId}/access/missing` names them, and the call can be repeated once their keys are written")]
    [HttpPost("owner")]
    public async IAsyncEnumerable<FileEntryBaseDto> ChangeFileOwner(ChangeOwnerRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var data = AsyncEnumerable.Empty<FileEntry>();
        data = data.Concat(fileStorageService.ChangeOwnerAsync(folderIntIds, fileIntIds, inDto.UserId, demandFileKeys: true));
        data = data.Concat(fileStorageService.ChangeOwnerAsync(folderStringIds, fileStringIds, inDto.UserId, demandFileKeys: true));

        await foreach (var e in data)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <remarks>
    /// Returns who has access to the listed files and folders, merged into one list. Each account or group appears
    /// once. To read the rights of one entry, use `GET api/2.0/files/file/{id}/share` or
    /// `GET api/2.0/files/rooms/{id}/share`. Requires read access to every listed entry. If the caller cannot read
    /// one entry, the whole call returns 403, so filter the list first. The call is read-only. It skips IDs that
    /// match nothing, and an empty list gives an empty answer. The caller's own record comes first, then the owner's
    /// record, then the rest by display name. If a subject has different rights on the entries, or none on some, its
    /// access is `Varies`. To see the difference, read the entries one by one. It shows external links only to a
    /// caller who may read them.
    /// </remarks>
    /// <summary>Get sharing rights in batch</summary>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The merged sharing rights of the listed files and folders, one record per account or group", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(403, "The caller may not read one of the listed files or folders")]
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
    /// Deletes all sharing rights on the listed files and folders, or removes them from the caller's own list. The
    /// owner's record and external links stay. To delete external links, use the link operations of the entry.
    /// Requires read access to each entry. Other callers get 403. Only a caller who may manage access, or a DocSpace
    /// admin outside a private room, deletes the rights. Any other caller only removes the entry from their own list.
    /// For a room member, this means leaving the room. The call changes data, and you can repeat it. It stops at the
    /// first entry it may not change, and earlier entries stay changed. In both modes, it clears the caller's
    /// favorite, recent, and new marks on the entry. In a private room, every account
    /// that loses access also loses its file keys. The call always returns `true` and skips IDs that match nothing.
    /// To check the result, use `POST api/2.0/files/share`. To delete the rights of one account, use
    /// `PUT api/2.0/files/share` with the access `None`.
    /// </remarks>
    /// <summary>Remove sharing rights in batch</summary>
    /// <path>api/2.0/files/share</path>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Always true: the accounts and groups that had access to the listed entries no longer have it", typeof(bool))]
    [SwaggerResponse(403, "The caller may not change the access of a listed entry and cannot read it either, so it cannot be dropped from their own list. A portal administrator outside a private room gets this as well")]
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
    /// Changes the access of accounts and groups to the listed files and folders. The access `None` deletes the rights.
    /// Requires the right to manage access to each entry. Other callers get 403. The call changes data, one entry at a
    /// time. You can repeat it with the same body. It stops at the first entry it may not change. Earlier entries stay
    /// changed. It skips subjects the caller may not share with, such as another user's guest. It also skips
    /// deactivated accounts, deleted groups, and files outside My documents and rooms. If `notify` is true, the portal
    /// emails the accounts about the change. It returns the new rights of these subjects on each changed entry. A
    /// skipped subject or entry is not in the answer. An empty `share` changes nothing and returns an empty list. A
    /// repeat skips subjects that already have the listed access, so it sends no email. A repeated `None` changes
    /// nothing but returns the rights of the other listed subjects on that entry. In a listed room, a repeated `None`
    /// for an account emails a room invitation if `notify` is true. For one room, use
    /// `PUT api/2.0/files/rooms/{id}/share`. The call returns 400 for an invalid or too long `email`, too many email
    /// invitations, or a too long `sharingMessage`. It also returns 403 if a subject cannot get the access, or lacks
    /// encryption keys for a private room.
    /// </remarks>
    /// <summary>Set sharing rights in batch</summary>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rights of the listed accounts and groups on every entry that was processed", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "The request body cannot be read, an `email` in `share` is malformed or longer than 255 characters, `share` invites more addresses by email than the portal allows at once, or `sharingMessage` is longer than 255 characters")]
    [SwaggerResponse(403, "The caller may not change the sharing of a listed entry, a listed subject cannot be given the requested access on it, or a listed private room has an account in `share` that has not set up its encryption keys")]
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
    /// Returns the room or file that an external share link points at, and the link status. Anyone can call it
    /// without signing in. The call changes data, and you can repeat it. For a signed-in caller, the first valid call
    /// adds the entry to their lists. For a visitor, it opens an anonymous session that later requests with
    /// the same token reuse. The call reports a refused link in `status`, not with an HTTP error code. So read
    /// `status` first. The status is invalid for an unknown token, and for a link to an archived or trashed entry. It
    /// is expired for a link past its expiration date. For a protected link, the status asks for a password. To send
    /// it, use `POST api/2.0/files/share/{key}/password`. A signed-in caller who can read the room gets no password
    /// request. If the portal forbids sharing outside it, a public link is denied to an anonymous visitor.
    /// </remarks>
    /// <summary>Resolve an external share link</summary>
    /// <path>api/2.0/files/share/{key}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The entry the token points at, with the validation status of the link", typeof(ExternalShareDto))]
    [AllowAnonymous]
    [HttpGet("share/{key}")]
    public async Task<ExternalShareDto> GetExternalShareData(ExternalShareDataRequestDto inDto)
    {
        var validationInfo = await externalLinkHelper.ValidateAsync(inDto.Key, fileId: inDto.FileId, folderId: inDto.FolderId);

        return validationInfo.Map();
    }

    /// <remarks>
    /// Checks the password of a protected external share link, and returns the same data as
    /// `GET api/2.0/files/share/{key}`. Call it after that operation reports that the link requires a password.
    /// Anyone can call it without signing in. A signed-in caller who can read the room does not need the password.
    /// The call changes data but not the entry. The portal remembers a correct password for the caller, so later
    /// requests with the token skip it. The portal counts only wrong passwords for each link and calling address. So
    /// you can repeat the call with the correct password. A wrong password gives an invalid-password `status`, not an
    /// HTTP error. The portal then forgets the remembered password. After too many attempts, the call returns 403 until
    /// the block expires, even with the correct password.
    /// </remarks>
    /// <summary>Unlock a password-protected link</summary>
    /// <path>api/2.0/files/share/{key}/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The entry the token points at, with the status the link reached after the password was checked", typeof(ExternalShareDto))]
    [SwaggerResponse(403, "Too many attempts were made for this link from the calling address, and the block has not expired yet")]
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
