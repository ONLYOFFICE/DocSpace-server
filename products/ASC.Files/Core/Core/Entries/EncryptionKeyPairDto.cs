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

namespace ASC.Web.Files.Core.Entries;

[EnumExtensions]
[JsonConverter(typeof(EncryptionKeyTypeConverter))]
public enum EncryptionKeyType
{
    Crypt,
    Sign
}

public class EncryptionKeyTypeConverter : JsonConverter<EncryptionKeyType>
{
    public override EncryptionKeyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var result))
        {
            return (EncryptionKeyType)result;
        }

        if (reader.TokenType == JsonTokenType.String && EncryptionKeyTypeExtensions.TryParse(reader.GetString(), true, out var share))
        {
            return share;
        }

        return EncryptionKeyType.Sign;
    }

    public override void Write(Utf8JsonWriter writer, EncryptionKeyType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToStringFast());
    }
}

[Scope]
public class EncryptionKeyPairDtoHelper(
    UserManager userManager,
    AuthContext authContext,
    EncryptionLoginProvider encryptionLoginProvider,
    FileSecurity fileSecurity,
    FileSharing fileSharing,
    IDaoFactory daoFactory,
    ShareLinkKeyDao shareLinkKeyDao,
    ExternalShare externalShare)
{

    public async Task<List<EncryptionKeyDto>> CreateKeyPairAsync(EncryptionKeyDto keyPair)
    {
        var userId = await DemandKeyOwnerAsync();
        ValidateKeyPair(keyPair);

        var currentKeyPairs = await GetKeyPairAsync() ?? [];

        if (currentKeyPairs.Exists(r => r.Id == keyPair.Id))
        {
            throw new CustomHttpException(HttpStatusCode.Conflict, $"The encryption key {keyPair.Id} already exists");
        }

        keyPair.UserId = userId;
        currentKeyPairs.Add(keyPair);

        return await Save(currentKeyPairs);
    }

    public async Task<List<EncryptionKeyDto>> ReplaceKeyPairAsync(EncryptionKeyDto keyPair)
    {
        var userId = await DemandKeyOwnerAsync();
        ValidateKeyPair(keyPair);

        var currentKeyPairs = await GetKeyPairAsync() ?? [];

        var index = currentKeyPairs.FindIndex(r => r.Id == keyPair.Id);
        if (index < 0)
        {
            throw new ItemNotFoundException($"The encryption key {keyPair.Id} is not found");
        }

        keyPair.UserId = userId;
        currentKeyPairs[index] = keyPair;

        return await Save(currentKeyPairs);
    }

    /// <summary>
    /// Checks that the current account may own encryption keys. Guests never can, which is also why
    /// they cannot be invited to a private room.
    /// </summary>
    private async Task<Guid> DemandKeyOwnerAsync()
    {
        var userId = authContext.CurrentAccount.ID;

        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(userId))
        {
            throw new SecurityException();
        }

        return userId;
    }

    /// <summary>
    /// Checks that the submitted key material is usable. Both halves are mandatory: a request that
    /// omits or blanks one of them would otherwise be stored as is and leave the user with a key
    /// that can no longer decrypt anything.
    /// </summary>
    private static void ValidateKeyPair(EncryptionKeyDto keyPair)
    {
        if (keyPair == null)
        {
            throw new ArgumentException("The encryption key is not specified");
        }

        if (string.IsNullOrWhiteSpace(keyPair.PublicKey))
        {
            throw new ArgumentException("The public key is not specified");
        }

        if (string.IsNullOrWhiteSpace(keyPair.PrivateKeyEnc))
        {
            throw new ArgumentException("The encrypted private key is not specified");
        }
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairAsync()
    {
        var currentAddressString = await encryptionLoginProvider.GetKeysAsync();
        if (string.IsNullOrEmpty(currentAddressString))
        {
            return null;
        }

        return ParseKeys(currentAddressString, authContext.CurrentAccount.ID);
    }

    /// <summary>The ids of the keys in a stored key set, read as <see cref="ParseKeys"/> reads them.</summary>
    public static List<Guid> ParseKeyIds(string json, Guid ownerId)
    {
        return [.. ParseKeys(json, ownerId).Select(k => k.Id)];
    }

    /// <summary>
    /// Reads a user's stored key set. Before key sets existed a user had exactly one pair, stored as
    /// a single <c>{PrivateKeyEnc, PublicKey, UserId}</c> object rather than an array; such a pair is
    /// read as a one-element set with the all-zero identifier and the minimal date, since it was
    /// stored without either, and ignored when it names another user, as the single-pair code did.
    /// </summary>
    private static List<EncryptionKeyDto> ParseKeys(string json, Guid ownerId)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        switch (root.ValueKind)
        {
            case JsonValueKind.Array:
                return root.Deserialize<List<EncryptionKeyDto>>(JsonSerializerOptions.Web);
            case JsonValueKind.Object:
                var legacyKeyPair = root.Deserialize<EncryptionKeyDto>(JsonSerializerOptions.Web);
                if (legacyKeyPair.UserId != ownerId)
                {
                    return [];
                }

                legacyKeyPair.Id = Guid.Empty;
                legacyKeyPair.Date = DateTime.MinValue;
                return [legacyKeyPair];
            default:
                return [];
        }
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        await fileDao.InvalidateCacheAsync(fileId);

        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        var locatedInPrivateRoom = file.RootFolderType is FolderType.VirtualRooms or FolderType.Archive
            && await DocSpaceHelper.LocatedInPrivateRoomAsync(file, folderDao);

        if (file.RootFolderType != FolderType.Privacy && !locatedInPrivateRoom)
        {
            throw new NotSupportedException();
        }

        // A visitor without an account, or a signed-in one who is not a member of the room, reads the file only through
        // a link session, which the read check above has already required: they get the wrap made for the link instead
        // of the members' key set, which holds nothing they can open.
        if (!authContext.IsAuthenticated || await ReadsThroughLinkOnlyAsync(file, folderDao))
        {
            return await GetLinkSessionKeysAsync(file, folderDao);
        }

        var tmpFiles = await fileSharing.GetSharedInfoAsync([fileId], []);
        var fileShares = tmpFiles.ToList();
        var recipientIds = fileShares.Where(share => !share.SubjectGroup).Select(share => share.Id).ToList();

        if (locatedInPrivateRoom)
        {
            recipientIds = await GetPrivateRoomRecipientsAsync(file, folderDao, recipientIds);
        }

        var tasks = recipientIds.Distinct().Select(async userId =>
        {
            var fileKeyPairString = await encryptionLoginProvider.GetKeysAsync(userId);
            if (string.IsNullOrEmpty(fileKeyPairString))
            {
                return null;
            }

            var fileKeyPair = ParseKeys(fileKeyPairString, userId)
                .ToList();

            return fileKeyPair;
        });

        var fileKeysPair = (await Task.WhenAll(tasks))
            .Where(keyPair => keyPair != null)
            .SelectMany(keyPair => keyPair);

        return SanitizeAccessKeys(fileKeysPair);
    }

    /// <summary>
    /// The people a key of a file in a private room is wrapped for: the room's owner always, also after a change of
    /// owner took away their own access record, and the file's creator only while they still belong to the room.
    /// </summary>
    private async Task<List<Guid>> GetPrivateRoomRecipientsAsync<T>(File<T> file, IFolderDao<T> folderDao, List<Guid> recipientIds)
    {
        var room = await folderDao.GetParentFoldersAsync(file.ParentId).FirstOrDefaultAsync(f => f.IsRoom);
        if (room == null)
        {
            return recipientIds;
        }

        var result = new List<Guid>(recipientIds);

        if (file.CreateBy != room.CreateBy && result.Contains(file.CreateBy))
        {
            var creatorIsMember = await fileSecurity.GetPureSharesAsync(room, [file.CreateBy])
                .AnyAsync(r => r.Share is not FileShare.None and not FileShare.Restrict);

            if (!creatorIsMember)
            {
                result.RemoveAll(id => id == file.CreateBy);
            }
        }

        if (!result.Contains(room.CreateBy))
        {
            result.Add(room.CreateBy);
        }

        return result;
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairForRoomAsync<T>(T roomId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(roomId);
        if (room == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        var locatedInPrivateRoom = room.RootFolderType is FolderType.VirtualRooms or FolderType.Archive && DocSpaceHelper.LocatedInPrivateRoom(room);

        if (room.RootFolderType != FolderType.Privacy && !locatedInPrivateRoom)
        {
            throw new ArgumentException(FilesCommonResource.ErrorMessage_PrivateRoomOnly);
        }

        var tmpFiles = await fileSharing.GetSharedInfoAsync(room);
        var fileShares = tmpFiles.ToList();
        fileShares = fileShares.Where(share => !share.SubjectGroup).ToList();

        var tasks = fileShares.Select(async share =>
        {
            var fileKeyPairString = await encryptionLoginProvider.GetKeysAsync(share.Id);
            if (string.IsNullOrEmpty(fileKeyPairString))
            {
                return null;
            }

            var fileKeyPair = ParseKeys(fileKeyPairString, share.Id)
                .ToList();

            return fileKeyPair;
        });

        var fileKeysPair = (await Task.WhenAll(tasks))
            .Where(keyPair => keyPair != null)
            .SelectMany(keyPair => keyPair)
            .ToList();

        if(fileKeysPair.All(r => r.UserId != authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var keys = SanitizeAccessKeys(fileKeysPair);

        // Each working link of the room or of an item in it is a recipient of file keys: a client wraps the key of a
        // file it uploads for the links that cover the file, so it needs their public keys.
        if (room is Folder<int> internalRoom)
        {
            keys.AddRange((await shareLinkKeyDao.GetWorkingLinksInRoomAsync(internalRoom.Id)).Select(l => new EncryptionKeyDto
            {
                Id = l.LinkId,
                UserId = l.LinkId,
                PublicKey = l.PublicKey,
                IsLink = true
            }));
        }

        return keys;
    }

    /// <summary>
    /// Reads the public keys of the people about to be invited to a private room, so that a client can
    /// encrypt the file keys for them before it grants the access. Only somebody who may change the
    /// room's access gets an answer. A user who could not become a member is left out: the key rows
    /// are not partitioned by tenant, so an id of another portal's user must not reach them.
    /// </summary>
    public async Task<List<EncryptionKeyDto>> GetKeyPairForRoomCandidatesAsync<T>(T roomId, IEnumerable<Guid> userIds)
    {
        await DemandRoomAccessManagerAsync(roomId);

        var keys = new List<EncryptionKeyDto>();

        foreach (var userId in userIds.Distinct())
        {
            var user = await userManager.GetUsersAsync(userId);
            if (!userManager.UserExists(user) || user.Status == EmployeeStatus.Terminated || await userManager.IsGuestAsync(user))
            {
                continue;
            }

            var userKeys = await encryptionLoginProvider.GetKeysAsync(userId);
            if (string.IsNullOrEmpty(userKeys))
            {
                continue;
            }

            keys.AddRange(ParseKeys(userKeys, userId).Where(r => !string.IsNullOrEmpty(r.PublicKey)));
        }

        // Unlike the room's own key set, this answer never carries a private half, not even the caller's.
        keys.ForEach(r => r.PrivateKeyEnc = null);

        return keys;
    }

    /// <summary>
    /// Says which encrypted files of a private room each user cannot open yet, so that a client can
    /// write the missing keys without trying every file. Without <paramref name="userIds"/> it covers
    /// the members of the room; a user who has everything is left out of the answer.
    /// </summary>
    public async Task<List<RoomMissingFileKeysDto>> GetMissingFileKeysAsync(int roomId, IReadOnlyCollection<Guid> userIds)
    {
        var room = await DemandRoomAccessManagerAsync(roomId);

        var links = (await shareLinkKeyDao.GetWorkingLinksInRoomAsync(roomId)).ToLookup(l => l.LinkId);

        IEnumerable<Guid> subjects = userIds;
        if (userIds.Count == 0)
        {
            var shares = await fileSharing.GetSharedInfoAsync(room);
            subjects = shares.Where(share => !share.SubjectGroup && !share.IsLink).Select(share => share.Id).Append(room.CreateBy)
                .Concat(links.Select(l => l.Key));
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var result = new List<RoomMissingFileKeysDto>();

        foreach (var subjectId in subjects.Distinct())
        {
            if (links.Contains(subjectId))
            {
                var linkFileIds = await GetLinkFilesWithoutKeyAsync(fileDao, links[subjectId], subjectId);
                if (linkFileIds.Count != 0)
                {
                    result.Add(new RoomMissingFileKeysDto { UserId = subjectId, FileIds = linkFileIds, IsLink = true });
                }

                continue;
            }

            if (!userManager.UserExists(await userManager.GetUsersAsync(subjectId)))
            {
                continue;
            }

            var fileIds = await DocSpaceHelper.GetEncryptedFilesWithoutKeyAsync(fileDao, roomId, subjectId);
            if (fileIds.Count != 0)
            {
                result.Add(new RoomMissingFileKeysDto { UserId = subjectId, FileIds = fileIds });
            }
        }

        return result;
    }

    /// <summary>Finds a private room and checks that the current user may change who has access to it.</summary>
    private async Task<Folder<T>> DemandRoomAccessManagerAsync<T>(T roomId)
    {
        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);
        if (room == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (!room.IsRoom || !DocSpaceHelper.LocatedInPrivateRoom(room))
        {
            throw new ArgumentException(FilesCommonResource.ErrorMessage_PrivateRoomOnly);
        }

        if (!await fileSharing.CanSetAccessAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        return room;
    }

    /// <summary>
    /// Prepares somebody else's keys for the current user: an entry without a public key is of no
    /// use to the caller and must not be reported as access at all, and the encrypted private half
    /// never leaves its owner — only the caller's own entry keeps it.
    /// </summary>
    /// <summary>
    /// What a visitor of a link of a private item needs to open a file: one entry of the link with its public key and the
    /// file key wrapped for it (empty while no member has wrapped it), and the public keys of the room's members, among
    /// which the visitor finds the member who made the wrap by the id the wrap carries. The server does not record that
    /// member. A file the link does not cover is refused.
    /// </summary>
    /// <summary>
    /// Whether the signed-in caller came on a link and is neither the room's owner nor holds an access record of their
    /// own in the room. A member who sends a link token keeps the member answer.
    /// </summary>
    private async Task<bool> ReadsThroughLinkOnlyAsync<T>(File<T> file, IFolderDao<T> folderDao)
    {
        if (await externalShare.GetLinkIdAsync() == Guid.Empty)
        {
            return false;
        }

        var room = await DocSpaceHelper.GetParentRoom(file, folderDao);
        var userId = authContext.CurrentAccount.ID;
        if (room == null || room.CreateBy == userId)
        {
            return false;
        }

        return !await fileSecurity.GetPureSharesAsync(room, [userId])
            .AnyAsync(r => r.Share is not (FileShare.Restrict or FileShare.None));
    }

    private async Task<List<EncryptionKeyDto>> GetLinkSessionKeysAsync<T>(File<T> file, IFolderDao<T> folderDao)
    {
        var linkId = await externalShare.GetLinkIdAsync();
        var room = await DocSpaceHelper.GetParentRoom(file, folderDao);

        if (linkId == Guid.Empty || file is not File<int> internalFile || room is not Folder<int> internalRoom)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var link = (await shareLinkKeyDao.GetWorkingLinksInRoomAsync(internalRoom.Id)).FirstOrDefault(l => l.LinkId == linkId);
        var folderIds = await daoFactory.GetCacheFolderDao<int>().GetParentFoldersAsync(internalFile.ParentId)
            .Select(f => f.Id)
            .ToHashSetAsync();

        if (link == null || !link.Covers(internalFile.Id, folderIds))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var wrap = (await daoFactory.GetFileDao<int>().GetFileKeys(internalFile.Id, linkId)).FirstOrDefault();

        var memberIds = await fileSecurity.GetPureSharesAsync(internalRoom, ShareFilterType.User, null, null)
            .Where(r => r.Share is not (FileShare.Restrict or FileShare.None))
            .Select(r => r.Subject)
            .ToListAsync();
        memberIds.Add(internalRoom.CreateBy);

        var keys = new List<EncryptionKeyDto>();
        foreach (var memberId in memberIds.Distinct())
        {
            var memberKeys = await encryptionLoginProvider.GetKeysAsync(memberId);
            if (!string.IsNullOrEmpty(memberKeys))
            {
                keys.AddRange(ParseKeys(memberKeys, memberId).Where(r => !string.IsNullOrEmpty(r.PublicKey)));
            }
        }

        keys.ForEach(r => r.PrivateKeyEnc = null);

        keys.Add(new EncryptionKeyDto
        {
            Id = linkId,
            UserId = linkId,
            PublicKey = link.PublicKey,
            IsLink = true,
            FileKeyEnc = wrap?.PrivateKeyEnc
        });

        return keys;
    }

    /// <summary>
    /// The encrypted files a link covers and holds no wrap for: those under the folders or the room it is set on, and the
    /// file it is set on. One link id has one share record, but the lookup takes any number.
    /// </summary>
    private static async Task<List<int>> GetLinkFilesWithoutKeyAsync(IFileDao<int> fileDao, IEnumerable<ShareLinkInRoom> link, Guid linkId)
    {
        var fileIds = new HashSet<int>();

        foreach (var record in link)
        {
            if (record.EntryType == FileEntryType.Folder)
            {
                fileIds.UnionWith(await DocSpaceHelper.GetEncryptedFilesWithoutKeyAsync(fileDao, record.EntryId, linkId));
                continue;
            }

            var file = await fileDao.GetFileAsync(record.EntryId);
            if (file is { Encrypted: true } && !await fileDao.GetFileIdsWithKeyAsync([file.Id], linkId).AnyAsync())
            {
                fileIds.Add(file.Id);
            }
        }

        return [.. fileIds.Order()];
    }

    private List<EncryptionKeyDto> SanitizeAccessKeys(IEnumerable<EncryptionKeyDto> keyPairs)
    {
        var userId = authContext.CurrentAccount.ID;

        return keyPairs
            .Where(r => !string.IsNullOrEmpty(r.PublicKey))
            .Select(r =>
            {
                if (r.UserId != userId)
                {
                    r.PrivateKeyEnc = null;
                }

                return r;
            })
            .ToList();
    }

    public async Task DeleteAsync(Guid id)
    {
        await DemandKeyOwnerAsync();

        var currentSettings = await GetKeyPairAsync();

        if (currentSettings == null || currentSettings.RemoveAll(r => r.Id == id) == 0)
        {
            throw new ItemNotFoundException($"The encryption key {id} is not found");
        }

        await Save(currentSettings);
    }

    private async Task<List<EncryptionKeyDto>> Save(List<EncryptionKeyDto> currentSettings)
    {
        var keyPairString = JsonSerializer.Serialize(currentSettings, JsonSerializerOptions.Web);
        await encryptionLoginProvider.SetKeysAsync(authContext.CurrentAccount.ID, keyPairString);
        return currentSettings;
    }
}
