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
    /// Lists the accounts and groups that hold rights on one file, one entry per subject, with the level each of them
    /// has, whether the caller may still change that level, and which of them owns the file. The owner comes first,
    /// then room managers, groups, ordinary members, guests, and last the accounts that have not accepted their
    /// invitation yet, each of those ranked by access level and by name. External links are left out and are listed
    /// by `GET api/2.0/files/file/{id}/links` instead, while a PDF form kept in a form-filling room also reports the
    /// link of that room, because the form is filled out through it. `startIndex` and `count` page through the
    /// subjects, and their total number is reported in the response headers rather than in the body. Listing takes
    /// the right to change the sharing of the file, which its creator, the manager of its room and a portal
    /// administrator acting as room manager have, while inside a public room reading the file is enough; a member who
    /// may read but not share is answered with an empty list although the header still counts the subjects, and a
    /// caller with no access, a guest included, is refused. A file that does not exist, or was deleted permanently,
    /// is answered as missing. The call is read-only; for several entries at once use `POST api/2.0/files/share`.
    /// </remarks>
    /// <summary>Get file sharing rights</summary>
    /// <path>api/2.0/files/file/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The accounts and groups that hold rights on the file, the owner first", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Lists the accounts and groups that hold rights on one folder or room, one entry per subject, with the level
    /// each of them has, whether the caller may still change that level, and which of them owns the entry. The owner
    /// comes first, then room managers, groups, ordinary members, guests, and last the accounts that have not
    /// accepted their invitation yet, each of those ranked by access level and by name. External links are left out
    /// and are listed by `GET api/2.0/files/folder/{id}/links` instead. `startIndex` and `count` page through the
    /// subjects, and their total number is reported in the response headers rather than in the body. For a room, and
    /// for a folder inside a public room, read access is enough; any other folder is listed only to a caller who may
    /// change its sharing, which the manager of its room and a portal administrator acting as room manager may, and a
    /// member who may only read such a folder is answered with an empty list although the header still counts the
    /// subjects. A caller with no access, a guest included, is refused, and a folder that does not exist is answered
    /// as missing. The call is read-only. For a room prefer `GET api/2.0/files/rooms/{id}/share`, which filters the
    /// same subjects by kind and by name.
    /// </remarks>
    /// <summary>Get folder sharing rights</summary>
    /// <path>api/2.0/files/folder/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The accounts and groups that hold rights on the folder, the owner first", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Grants, changes or withdraws the rights of the listed accounts and groups on one file, and answers with the
    /// rights those subjects hold afterwards. Every element of `share` names a subject and the level it is to get,
    /// and the level that denies everything takes the access away instead; an empty `share` changes nothing and is
    /// answered with an empty list. A subject the caller is not allowed to share with, such as a guest who belongs to
    /// another member, is dropped without an error, so compare the answer with what was sent. With `notify` set, each
    /// account named is emailed about the access it received and `sharingMessage` is put into that mail with its
    /// markup stripped, while a message longer than the field allows is rejected as an invalid request. The caller
    /// has to be allowed to change the sharing of the file, which its creator, the manager of the room it lies in and
    /// a portal administrator acting as room manager are; anyone else, a guest and a member with read access
    /// included, is refused. The call is mutating and safe to repeat. For several files and folders in one request
    /// use `PUT api/2.0/files/share`.
    /// </remarks>
    /// <summary>Share a file</summary>
    /// <path>api/2.0/files/file/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rights the listed subjects hold on the file after the change", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Grants, changes or withdraws the rights of the listed accounts and groups on one folder, and answers with the
    /// rights those subjects hold afterwards. Every element of `share` names a subject and the level it is to get,
    /// and the level that denies everything takes the access away instead; an empty `share` changes nothing and is
    /// answered with an empty list. A subject the caller is not allowed to share with, such as a guest who belongs to
    /// another member, is dropped without an error. With `notify` set, each account named is emailed about the access
    /// it received and `sharingMessage` is put into that mail with its markup stripped, while a message longer than
    /// the field allows is rejected as an invalid request. The caller has to be allowed to change the sharing of the
    /// folder, which the manager of the room it belongs to and a portal administrator acting as room manager are;
    /// anyone else, a guest and a member with read access included, is refused. The call is mutating and safe to
    /// repeat. For a room use `PUT api/2.0/files/rooms/{id}/share`, which invites people by email as well.
    /// </remarks>
    /// <summary>Share a folder</summary>
    /// <path>api/2.0/files/folder/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rights the listed subjects hold on the folder after the change", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Answers with the encryption keys that open one file kept in a private room: one entry per member who holds
    /// rights on the file and has published keys, each carrying that member's public key, and the caller's own entry
    /// carrying the encrypted private half as well. The private half of another member is never handed out. A member
    /// who has not published keys yet is left out of the answer altogether, which is how a client tells that this
    /// member cannot open the file until keys are published through `POST api/2.0/privacyroom/keys`; a member who
    /// holds the file only through a group is not reported either, because group entries are skipped. The file has to
    /// lie in a private room or in the encrypted section - a file kept anywhere else carries no keys and is rejected
    /// as an unsupported request. The caller needs read access to the file and is answered with 403 otherwise, and a
    /// file that does not exist is answered as missing. The call is read-only, and the answer changes as soon as a
    /// member publishes or rotates keys, so read it again rather than caching it for a later session.
    /// </remarks>
    /// <summary>Get file encryption keys</summary>
    /// <path>api/2.0/files/file/{fileId}/publickeys</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The keys of the members who can open the file, the private half only for the caller", typeof(List<EncryptionKeyDto>))]
    [SwaggerResponse(403, "The caller may not read the file")]
    [HttpGet("file/{fileId}/publickeys")]
    public async Task<List<EncryptionKeyDto>> GetEncryptionAccess(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.GetEncryptionAccessAsync(inDto.FileId);
    }

    /// <remarks>
    /// Emails the people named in `emails` that they were mentioned in a file, with a link that opens the file at the
    /// place the mention sits when `actionLink` carries the anchor the editor produced. Only addresses that belong to
    /// portal accounts are notified: an address that belongs to nobody is skipped, and the note is cut to its first
    /// 200 characters in the mail, while a `message` longer than the field allows is refused with 400. The answer is
    /// usually empty: the access list of the file comes back when the file is encrypted, or when one of the addresses
    /// belongs to nobody and the caller may share the file - that is then the cue to invite that person with
    /// `PUT api/2.0/files/file/{id}/share`. The caller needs comment rights, which the creator of the file, the
    /// manager of its room and a member invited to comment, review or edit have, while a guest or a member without
    /// access is refused with 403; a file that does not exist answers with 404 and a file in the trash is refused.
    /// The operation is rate-limited and answers 429 once the caller sends too many notifications. A delivery failure
    /// is swallowed, so 200 does not prove that the mail left the portal.
    /// </remarks>
    /// <summary>Notify mentioned users</summary>
    /// <path>api/2.0/files/file/{fileId}/sendeditornotify</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The people who currently have access to the file, when the caller still has to invite someone; empty otherwise", typeof(List<AceShortWrapper>))]
    [SwaggerResponse(400, "The address list is missing, or the message is longer than the field allows")]
    [SwaggerResponse(403, "The caller may not comment on the file")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpPost("file/{fileId}/sendeditornotify")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<List<AceShortWrapper>> SendEditorNotify(MentionMessageWrapperRequestDto<T> inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        return await fileStorageService.SendEditorNotifyAsync(inDto.FileId, inDto.MentionMessage);
    }

    /// <remarks>
    /// Lists the members of one portal group together with the access each of them has on a folder or room that group
    /// was granted rights to: `groupAccess` is the level the group itself carries, `userAccess` is the level set on
    /// that member alone, `overridden` says which of the two applies, `owner` marks the member who created the entry,
    /// and `canEditAccess` says whether the caller may still change that member's level. Take the group identifier
    /// from the group entries of `GET api/2.0/files/folder/{id}/share`. `startIndex` and `count` page through the
    /// members, `filterValue` keeps only those whose first name, last name or email contains the value - the
    /// comparison is made in lower case, so an uppercase value matches nothing - and the number of members is
    /// reported in the response headers. Members come back ordered by first name. A group that holds no rights on
    /// this folder, a folder the caller cannot read and a folder that does not exist are all answered with an empty
    /// list rather than an error, so an empty answer does not mean that the group has no members. A guest is refused.
    /// The call is read-only.
    /// </remarks>
    /// <summary>Get folder access of group members</summary>
    /// <path>api/2.0/files/folder/{folderId}/group/{groupId}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The members of the group with the access each of them has on the folder", typeof(IAsyncEnumerable<GroupMemberSecurityRequestDto>))]
    [HttpGet("folder/{folderId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityRequestDto> GetGroupsMembersWithFolderSecurity(GroupMemberSecurityFolderRequestDto<T> inDto)
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
    /// Lists the members of one portal group together with the access each of them has on a file that group was
    /// granted rights to: `groupAccess` is the level the group itself carries, `userAccess` is the level set on that
    /// member alone, `overridden` says which of the two applies, `owner` marks the member who created the file, and
    /// `canEditAccess` says whether the caller may still change that member's level. Take the group identifier from
    /// the group entries of `GET api/2.0/files/file/{id}/share`. `startIndex` and `count` page through the members,
    /// `filterValue` keeps only those whose first name, last name or email contains the value - the comparison is
    /// made in lower case, so an uppercase value matches nothing - and the number of members is reported in the
    /// response headers. Members come back ordered by first name. A group that holds no rights on this file, a file
    /// the caller cannot read and a file that does not exist are all answered with an empty list rather than an
    /// error, so an empty answer does not mean that the group has no members. A guest is refused. The call is
    /// read-only.
    /// </remarks>
    /// <summary>Get file access of group members</summary>
    /// <path>api/2.0/files/file/{fileId}/group/{groupId}/share</path>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The members of the group with the access each of them has on the file", typeof(IAsyncEnumerable<GroupMemberSecurityRequestDto>))]
    [HttpGet("file/{fileId}/group/{groupId:guid}/share")]
    public async IAsyncEnumerable<GroupMemberSecurityRequestDto> GetGroupsMembersWithFileSecurity(GroupMemberSecurityFileRequestDto<T> inDto)
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
    /// Hands the ownership of the listed rooms and files over to a single account, and returns the entries as they
    /// look afterwards. Among folders only rooms are accepted - take their identifiers from
    /// `GET api/2.0/files/rooms`; a plain folder is refused. A file is accepted only while it lies in the portal's
    /// common section, so a file kept inside a room or in a personal section is refused as well, and so is a file
    /// that is locked or currently open in the editor. The new owner has to be an active account that is allowed to
    /// manage rooms, and a private room additionally requires that this account has already set up its encryption
    /// keys; a deactivated account, a guest or a plain member is rejected. The caller must be the creator of every
    /// listed room, or a portal administrator. The call mutates the entries one at a time and stops at the first item
    /// it may not touch, leaving the entries already processed changed, so a partial answer is possible; an item
    /// whose owner is already the target account is returned untouched, which makes a repeat safe. The previous owner
    /// keeps access to a transferred room as its manager, while a transferred file is saved as a new version authored
    /// by the new owner. An entry that lives on a connected third-party account is quietly left out.
    /// </remarks>
    /// <summary>Change the room or file owner</summary>
    /// <path>api/2.0/files/owner</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rooms and files whose owner has been changed, as folder and file objects", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
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
    /// Returns who has access to the files and folders listed in the request, merged into one list of subjects, and
    /// is the batch counterpart of `GET api/2.0/files/file/{id}/share` and `GET api/2.0/files/rooms/{id}/share`.
    /// Identifiers come from any listing operation, such as `GET api/2.0/files/{folderId}`. The caller needs read
    /// access to every listed entry: a single entry it cannot read makes the whole call fail instead of dropping that
    /// entry, so the list has to be filtered beforehand. Identifiers that match nothing are skipped without an error,
    /// and an empty list of identifiers gives an empty answer. The call is read-only. Each account or group appears
    /// once: the caller's own record comes first, the owner's record second, and the rest are ordered by display
    /// name. When the same subject holds different rights on the listed entries, its access is reported as the
    /// `Varies` value instead of a real level, which means the entries have to be inspected one by one to see the
    /// difference. Records that describe external links are included only for a caller that is allowed to read the
    /// links of the entry.
    /// </remarks>
    /// <summary>Get sharing rights in batch</summary>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The merged sharing rights of the listed files and folders, one record per account or group", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Revokes the access of every account and group on the files and folders listed in the request, and clears the
    /// entries from the caller's own favorites, recent and unread marks. The owner's own record is kept, since
    /// removing it would take the entry away from the account that owns it, and external links survive untouched -
    /// remove those through the link operations of the entry. The caller must be allowed to change the access of each
    /// entry, which means the creator of the room, a portal administrator, or a member with the rights to manage it;
    /// a caller whose only access came through an external link may use this call to drop the entry from its own
    /// list, while a directly invited member or an unrelated account is refused. The answer is always `true` and
    /// identifiers that match nothing are skipped silently, so a successful answer is not proof that anything was
    /// revoked - read the rights back with `POST api/2.0/files/share`. The call is destructive and safe to repeat. To
    /// take the rights of one account away instead of all of them, call `PUT api/2.0/files/share` with that account's
    /// access set to `None`.
    /// </remarks>
    /// <summary>Remove sharing rights in batch</summary>
    /// <path>api/2.0/files/share</path>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "Always true: the accounts and groups that had access to the listed entries no longer have it", typeof(bool))]
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
    /// Grants, changes or withdraws the access of the listed accounts and groups on every file and folder named in
    /// the request at once, and returns the resulting rights. Entry identifiers come from a listing operation, and
    /// the accounts and groups come from the portal's own account and group lists; an access of `None` withdraws the
    /// rights instead of granting them. The caller must be allowed to change the access of every listed entry - the
    /// creator of the room, a member with the rights to manage it, or a portal administrator - and a read-only member
    /// or a guest is refused even when the payload changes nothing. A subject the caller is not allowed to share
    /// with, such as a guest that belongs to another member, is skipped without an error, and an empty `share`
    /// collection makes the call do nothing and answer with an empty list. Repeating the same request leaves the same
    /// rights in place. The answer holds one record per listed subject for each entry that was actually processed, so
    /// it is shorter than the request when something was skipped and worth comparing against it. For a single room
    /// prefer `PUT api/2.0/files/rooms/{id}/share`, which also invites members by email.
    /// </remarks>
    /// <summary>Set sharing rights in batch</summary>
    /// <path>api/2.0/files/share</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The rights of the listed accounts and groups on every entry that was processed", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Resolves the token of an external share link into the room or file it points at, and reports the outcome of
    /// validating the link. The token is the `requestToken` of a link returned by the link operations of an entry,
    /// such as `GET api/2.0/files/file/{id}/link` or `GET api/2.0/files/rooms/{id}/link`. The call needs no
    /// authentication and answers a refused link in the `status` field rather than with an HTTP error, so that field
    /// has to be read before anything else: a token that matches no link, and a link whose entry has been archived or
    /// moved to the trash, both resolve as invalid; a link past its expiration date resolves as expired; a
    /// password-protected link resolves as requiring a password, which is then submitted through
    /// `POST api/2.0/files/share/{key}/password`; and a public link resolves as denied when the portal forbids
    /// sharing with people outside it. The call is not read-only: for a signed-in caller the first successful
    /// resolution puts the entry into the account's own lists, and for a visitor without an account it opens an
    /// anonymous session that later requests with the same token reuse. Pass `fileId` or `folderId` to have an entry
    /// inside the link's target echoed back.
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
    /// Submits the password of a protected external share link and answers with the same resolved link data as
    /// `GET api/2.0/files/share/{key}`, so this operation is called only after that one reported that a password is
    /// required. The token in the path is the `requestToken` of the link, and the password is the one chosen by the
    /// member who shared the entry. The call needs no authentication; a signed-in caller that may already read the
    /// room is let through by the resolve operation itself and does not need the password at all. A correct password
    /// is remembered for the caller, so later requests with the same token resolve without repeating it, and a wrong
    /// one is reported in the `status` field as an invalid password rather than as an HTTP error, while the
    /// remembered password is dropped. Attempts are counted per link and per calling address: once the portal's limit
    /// is reached, further attempts are rejected until the block expires, which makes the operation unsuitable for
    /// trying passwords in a loop. Nothing about the entry is changed by the call itself.
    /// </remarks>
    /// <summary>Unlock a password-protected link</summary>
    /// <path>api/2.0/files/share/{key}/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The entry the token points at, with the status the link reached after the password was checked", typeof(ExternalShareDto))]
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
