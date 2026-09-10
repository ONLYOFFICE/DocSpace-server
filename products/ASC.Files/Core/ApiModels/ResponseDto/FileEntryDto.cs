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

using static ASC.Files.Core.Security.FileSecurity;

namespace ASC.Files.Core.ApiModels.ResponseDto;

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FileDto<int>[]))]
[JsonSerializable(typeof(FileDto<string>[]))]
[JsonSerializable(typeof(FolderDto<int>[]))]
[JsonSerializable(typeof(FolderDto<string>[]))]
public partial class FileEntryDtoContext : JsonSerializerContext;

/// <summary>
/// What every file and folder in an answer has in common; the concrete shape is a file or a folder, told apart by the
/// entry type.
/// </summary>
[JsonDerivedType(typeof(FileDto<int>))]
[JsonDerivedType(typeof(FileDto<string>))]
[JsonDerivedType(typeof(FolderDto<int>))]
[JsonDerivedType(typeof(FolderDto<string>))]
public abstract class FileEntryBaseDto
{
    /// <summary>
    /// The name shown for the entry. For a file it carries the extension, which is how the format is recognised, and
    /// for a room it is the room name.
    /// </summary>
    /// <example>Some title.txt</example>
    public string Title { get; set; }

    /// <summary>
    /// The level the calling account holds on this entry, resolved from its own rights, the groups it belongs to and
    /// any link it came in through. It is the level itself, not what the account may do with it - the action flags
    /// below answer that.
    /// </summary>
    /// <example>1</example>
    public FileShare Access { get; set; }

    /// <summary>
    /// Who gave the calling account the access it is using. It is filled in only while the entry is being read
    /// through a share, and never for a caller without an account.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto SharedBy { get; set; }

    /// <summary>
    /// Who owns the place the entry is shared from - the creator of the room it lies in, or of the personal section
    /// that holds it. It is filled in only while the entry is being read through a share, and never for a caller
    /// without an account.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto OwnedBy { get; set; }

    /// <summary>
    /// Whether at least one external link exists for the entry, whichever kind. It says nothing about accounts and
    /// groups - those are counted by the flag for members below.
    /// </summary>
    /// <example>false</example>
    public bool Shared { get; set; }

    /// <summary>
    /// Whether at least one account or group has been given rights on the entry directly, as opposed to reaching it
    /// through a link or through the room around it.
    /// </summary>
    /// <example>false</example>
    public bool SharedForUser { get; set; }

    /// <summary>
    /// Whether one of the entry's links is open to people outside the portal, as opposed to a link that only its own
    /// members can follow. This is the flag to watch when the concern is who can reach the content from outside.
    /// </summary>
    /// <example>false</example>
    public bool SharedExternal { get; set; }

    /// <summary>
    /// Whether the entry is reachable because the room or folder around it is shared, rather than through rights of
    /// its own. A copy or a move takes the entry out of that scope.
    /// </summary>
    /// <example>false</example>
    public bool ParentShared { get; set; }

    /// <summary>
    /// A shortened address that opens the entry through the link it is being read with. It is an empty string
    /// whenever no link applies, which is the usual case for a member browsing their own rooms.
    /// </summary>
    /// <example>http://localhost/s/abc123</example>
    [Url]
    public string ShortWebUrl { get; set; }

    /// <summary>
    /// When the entry was created, written with the offset of the portal's time zone. For a file restored from an
    /// older version this is still the moment the file first appeared.
    /// </summary>
    /// <example>2026-04-15T13:20:41.0000000+03:00</example>
    public ApiDateTime Created { get; set; }

    /// <summary>
    /// Who created the entry. It is null for a caller without an account, who is told nothing about the portal's
    /// members.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// When the entry last changed, written with the offset of the portal's time zone. It is never reported as
    /// earlier than the creation moment, so the two can be compared safely.
    /// </summary>
    /// <example>2026-04-15T13:20:41.0000000+03:00</example>
    public ApiDateTime Updated
    {
        get => field < Created ? Created : field;
        set;
    }

    /// <summary>
    /// When the entry will disappear on its own, written with the offset of the portal's time zone. It is filled in
    /// only where a removal is actually scheduled - something in the trash while the portal cleans it up
    /// automatically, or a guest's own documents - so a null means nothing is scheduled rather than that the entry is
    /// permanent.
    /// </summary>
    /// <example>2026-04-15T13:20:41.0000000+03:00</example>
    public ApiDateTime AutoDelete { get; set; }

    /// <summary>
    /// The section the entry ultimately belongs to, which is what tells a personal document from one inside a room,
    /// from a template and from something in the trash or the archive.
    /// </summary>
    /// <example>14</example>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The kind of room the entry lies in, which decides what the room allows - filling forms, public links,
    /// indexing. It is null for an entry that is not inside a room at all.
    /// </summary>
    /// <example>19</example>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// Who changed the entry last. It is null for a caller without an account.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto UpdatedBy { get; set; }

    /// <summary>
    /// Set when the entry is stored on a connected third-party account rather than on the portal, and null when it is
    /// stored on the portal. Such an entry is identified by a string rather than a number, and some operations skip
    /// it.
    /// </summary>
    /// <example>true</example>
    public bool? ProviderItem { get; set; }

    /// <summary>
    /// Which third-party service holds the entry, matching the keys accepted by the third-party operations. It is
    /// null for an entry stored on the portal.
    /// </summary>
    /// <example>google-drive</example>
    public string ProviderKey { get; set; }

    /// <summary>
    /// The connected account the entry comes from, for telling apart two connections to the same service. It is null
    /// for an entry stored on the portal.
    /// </summary>
    /// <example>1</example>
    public int? ProviderId { get; set; }

    /// <summary>
    /// The place of the entry in a room where the members arrange the content themselves, given as the position of
    /// the entry preceded by the positions of the folders leading to it, separated by dots. It is empty when nothing
    /// has been arranged.
    /// </summary>
    /// <example>1.3.2</example>
    public string Order { get; set; }

    /// <summary>
    /// Set when the calling account has marked the entry as a favorite, which is what puts it into the favorites
    /// listing. For a file that is not marked it is null rather than false.
    /// </summary>
    /// <example>true</example>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// Tells a folder from a file, and so which of the two shapes the rest of the object has. A room is reported as a
    /// folder here.
    /// </summary>
    /// <example>2</example>
    public abstract FileEntryType FileEntryType { get; }

    protected FileEntryBaseDto(FileEntry entry)
    {
        Title = entry.Title;
        Access = entry.Access;
        Shared = entry.Shared;
        SharedForUser = entry.SharedForUser;
        SharedExternal = entry.SharedExternal;
        ParentShared = entry.ParentShared;
        RootFolderType = entry.RootFolderType;
        ParentRoomType = entry.ParentRoomType;
        ProviderItem = entry.ProviderEntry.NullIfDefault();
        ProviderKey = entry.ProviderKey;
        ProviderId = entry.ProviderId.NullIfDefault();
    }

    protected FileEntryBaseDto() { }
}

/// <summary>
/// The part of a file or folder that depends on how the entry is identified: by a number on the portal, or by a
/// string on a connected third-party account.
/// </summary>
[DebuggerDisplay("{Title} ({Id})")]
public abstract class FileEntryDto<T> : FileEntryBaseDto
{
    /// <summary>
    /// The identifier to pass back to the other operations of this entry. It is a number for storage on the portal
    /// and a string for a connected third-party account, and it is unique only within its own kind, so files and
    /// folders may carry the same value.
    /// </summary>
    /// <example>10</example>
    public T Id { get; set; }

    /// <summary>
    /// The section the entry ultimately lies in, as an identifier that can be listed like any other folder. For an
    /// entry inside a room this is the rooms section, not the room.
    /// </summary>
    /// <example>1</example>
    public T RootFolderId { get; set; }

    /// <summary>
    /// The folder the entry was deleted from, which is where restoring it puts it back. It is left out of the answer
    /// unless the entry is in the trash.
    /// </summary>
    /// <example>12</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T OriginId { get; set; }

    /// <summary>
    /// The room the entry was deleted from, left out of the answer for anything that was not deleted out of a room.
    /// </summary>
    /// <example>22</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T OriginRoomId { get; set; }

    /// <summary>
    /// The name of the folder the entry was deleted from, for showing where it would be restored to. It is null for
    /// an entry that is not in the trash.
    /// </summary>
    /// <example>Contracts</example>
    public string OriginTitle { get; set; }

    /// <summary>
    /// The name of the room the entry was deleted from, null for anything that was not deleted out of a room.
    /// </summary>
    /// <example>Legal team</example>
    public string OriginRoomTitle { get; set; }

    /// <summary>
    /// Whether the calling account may change who has access to the entry, and so whether offering a sharing dialog
    /// for it makes sense. It is false in rooms whose access is fixed by the room itself, such as a private one, even
    /// for its manager.
    /// </summary>
    /// <example>true</example>
    public bool CanShare { get; set; }

    /// <summary>
    /// How many links of each kind currently exist for the entry, counted separately for the primary link and the
    /// additional ones. Kinds with no links are left out, and the whole field is null when the caller may not change
    /// the access or no link exists at all.
    /// </summary>
    /// <example>{"PrimaryExternalLink": 1, "ExternalLink": 2}</example>
    public IDictionary<SubjectType, int> ShareSettings { get; set; }

    /// <summary>
    /// What the calling account may do with this entry, one flag per action, and the cheapest way to decide which
    /// operations to offer without trying them. The flags already take the room's settings and the account's role
    /// into account.
    /// </summary>
    /// <example>{"Read": true, "Edit": false, "Delete": false}</example>
    public IDictionary<FilesSecurityActions, bool> Security { get; set; }

    /// <summary>
    /// Which access levels may be handed out on this entry, listed per kind of recipient, so that a client offers
    /// only levels the entry actually supports - a room for filling forms and a plain folder do not accept the same
    /// ones.
    /// </summary>
    /// <example>{"ExternalLink": ["Read", "Editing"]}</example>
    public IDictionary<SubjectType, IEnumerable<string>> AvailableShareRights { get; set; }

    /// <summary>
    /// The token of the link the entry is being read through, which is the value the external-share operations expect
    /// and which also has to be carried by the download and preview addresses. It is null whenever the entry is not
    /// being read through a link.
    /// </summary>
    /// <example>q7Ry8cQ1lZ0dP3sK2mXfA9tBnV6hJ4uE8wCz5oLg</example>
    public string RequestToken { get; set; }

    /// <summary>
    /// Set when the link being used was made for this very entry, and false when the entry is reached through a link
    /// to the room around it. It is null when no link is involved.
    /// </summary>
    /// <example>false</example>
    public bool? External { get; set; }

    /// <summary>
    /// When the link being used stops working, written with the offset of the portal's time zone. It is null for a
    /// link that never expires and whenever no link is involved.
    /// </summary>
    /// <example>2026-04-15T13:20:41.0000000+03:00</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// Set when the link being used has already passed its expiration date, which is why the entry cannot be opened
    /// even though it is described here. It is null when no link is involved.
    /// </summary>
    /// <example>false</example>
    public bool? IsLinkExpired { get; set; }

    protected FileEntryDto(FileEntry<T> entry)
        : base(entry)
    {
        Id = entry.Id;
        RootFolderId = entry.RootId;
    }

    protected FileEntryDto() { }
}

[Scope]
public class FileEntryDtoHelper(
    ApiDateTimeHelper apiDateTimeHelper,
    EmployeeDtoHelper employeeWrapperHelper,
    FileSharingHelper fileSharingHelper,
    FileSecurity fileSecurity,
    GlobalFolderHelper globalFolderHelper,
    FilesSettingsHelper filesSettingsHelper,
    FileDateTime fileDateTime,
    SecurityContext securityContext,
    UserManager userManager,
    IDaoFactory daoFactory,
    ExternalShare externalShare,
    FileSharing fileSharing,
    IUrlShortener urlShortener)
{
    protected readonly FileSecurity _fileSecurity = fileSecurity;
    protected readonly GlobalFolderHelper _globalFolderHelper = globalFolderHelper;
    protected readonly IDaoFactory _daoFactory = daoFactory;
    protected readonly ExternalShare _externalShare = externalShare;
    protected readonly IUrlShortener _urlShortener = urlShortener;
    protected readonly ApiDateTimeHelper _apiDateTimeHelper = apiDateTimeHelper;

    protected async Task<T> GetAsync<T, TId>(FileEntry<TId> entry) where T : FileEntryDto<TId>, new()
    {
        if (entry.Security == null)
        {
            entry = await _fileSecurity.SetSecurity(new[] { entry }.ToAsyncEnumerable()).FirstAsync();
        }

        if (entry.RootFolderType == FolderType.VirtualRooms && entry.ParentRoomType == null && entry is not Folder<TId> { IsRoom: true })
        {
            var room = await _daoFactory.GetCacheFolderDao<TId>().GetParentFoldersAsync(entry.ParentId).FirstOrDefaultAsync(r => r.IsRoom);
            if (room != null)
            {
                entry.ParentRoomType = room.FolderType;
                entry.ParentRoomCreatedBy = room.CreateBy;
            }
        }

        CorrectSecurityByLockedStatus(entry);

        var permanentlyDeletedOn = await GetDeletedPermanentlyOn(entry);

        if (entry.ProviderEntry)
        {
            entry.RootId = entry.RootFolderType switch
            {
                FolderType.VirtualRooms => IdConverter.Convert<TId>(await _globalFolderHelper.GetFolderVirtualRooms()),
                FolderType.Archive => IdConverter.Convert<TId>(await _globalFolderHelper.GetFolderArchive()),
                FolderType.RoomTemplates => IdConverter.Convert<TId>(await _globalFolderHelper.GetFolderRoomTemplatesAsync()),
                FolderType.DefaultTemplates => IdConverter.Convert<TId>(await _globalFolderHelper.GetFolderDefaultTemplatesAsync()),
                _ => entry.RootId
            };
        }

        var shortWebUrl = "";

        if (entry.FullShared)
        {
            var linkId = await _externalShare.GetLinkIdAsync();
            if (linkId != Guid.Empty)
            {
                var securityDao = _daoFactory.GetSecurityDao<string>();
                var record = await securityDao.GetSharesAsync([linkId]).FirstOrDefaultAsync();
                if (record != null)
                {
                    var linkData = await _externalShare.GetLinkDataAsync(entry, record.Subject);
                    shortWebUrl = await _urlShortener.GetShortenLinkAsync(linkData.Url);
                }
            }
            else if(entry.ParentRoomType == FolderType.PublicRoom)
            {
                var link = await fileSharing.GetPureSharesAsync(entry, ShareFilterType.PrimaryExternalLink, null, null, 0, 1).FirstOrDefaultAsync();
                if (link != null)
                {
                    shortWebUrl = link.Link;
                }
            }
        }

        var canSetAccess = await fileSharingHelper.CanSetAccessAsync(entry);
        if (entry is Folder<TId> { FolderType: FolderType.EditingRoom or FolderType.VirtualDataRoom} or Folder<TId> { FolderType: FolderType.CustomRoom, SettingsPrivate: true })
        {
            canSetAccess = false;
        }

        Dictionary<SubjectType, int> shareSettings = null;

        if (canSetAccess)
        {

            var primaryCount = await _fileSecurity.GetLinksSettings(entry, SubjectType.PrimaryExternalLink);
            var additionalCount = await _fileSecurity.GetLinksSettings(entry, SubjectType.ExternalLink);

            if (primaryCount > 0)
            {
                shareSettings = new Dictionary<SubjectType, int>
                {
                    {
                        SubjectType.PrimaryExternalLink, primaryCount
                    }
                };
            }

            if (additionalCount > 0)
            {
                shareSettings ??= new Dictionary<SubjectType, int>();
                shareSettings.Add(SubjectType.ExternalLink, additionalCount);
            }
        }

        var sharedBy = entry.SharedBy;

        if (sharedBy == null &&  entry.ShareRecord != null && Equals(entry.Id, entry.ShareRecord.EntryId))
        {
            sharedBy = entry.ShareRecord?.Owner;
        }

        Guid? ownedBy = null;
        if (entry.ShareRecord != null)
        {
            ownedBy = entry.ParentRoomCreatedBy ?? entry.RootCreateBy;
        }

        return new T
        {
            Id = entry.Id,
            Title = entry.Title,
            Access = entry.Access,
            Shared = entry.Shared,
            SharedBy = securityContext.IsAuthenticated && sharedBy.HasValue ? await employeeWrapperHelper.GetAsync(sharedBy.Value) : null,
            OwnedBy = securityContext.IsAuthenticated && ownedBy.HasValue ? await employeeWrapperHelper.GetAsync(ownedBy.Value) : null,
            SharedForUser = entry.SharedForUser,
            SharedExternal = entry.SharedExternal,
            ParentShared = entry.ParentShared,
            ShortWebUrl = shortWebUrl,
            Created = _apiDateTimeHelper.Get(entry.CreateOn),
            CreatedBy = securityContext.IsAuthenticated ? await employeeWrapperHelper.GetAsync(entry.CreateBy) : null,
            Updated = _apiDateTimeHelper.Get(entry.ModifiedOn),
            UpdatedBy = securityContext.IsAuthenticated ? await employeeWrapperHelper.GetAsync(entry.ModifiedBy) : null,
            RootFolderType = entry.RootFolderType,
            ParentRoomType = entry.ParentRoomType,
            RootFolderId = entry.RootId,
            ProviderItem = entry.ProviderEntry.NullIfDefault(),
            ProviderKey = entry.ProviderKey,
            ProviderId = entry.ProviderId.NullIfDefault(),
            CanShare = canSetAccess,
            ShareSettings = shareSettings,
            Security = entry.Security,
            OriginId = entry.OriginId,
            OriginTitle = entry.OriginTitle,
            OriginRoomId = entry.OriginRoomId,
            OriginRoomTitle = entry.OriginRoomTitle,
            AutoDelete = permanentlyDeletedOn != default ? _apiDateTimeHelper.Get(permanentlyDeletedOn) : null
        };
    }

    private async ValueTask<DateTime> GetDeletedPermanentlyOn<T>(FileEntry<T> entry)
    {
        var isGuest = await userManager.IsGuestAsync(securityContext.CurrentAccount.ID);
        if (isGuest)
        {
            var myId = await _globalFolderHelper.GetFolderMyAsync<int>();

            if (Equals(entry.FolderIdDisplay, myId) && myId != 0)
            {
                var folderDao = _daoFactory.GetFolderDao<int>();
                var my = await folderDao.GetFolderAsync(myId);

                return fileDateTime.GetModifiedOnWithAutoCleanUp(my.ModifiedOn, DateToAutoCleanUp.OneMonth);
            }
        }

        // The RootFolderType check must come first: FolderTrashAsync creates the user's trash root
        // when it does not exist yet, and only entries already in the trash can match it anyway.
        if (entry.ModifiedOn.Equals(default) || entry.RootFolderType != FolderType.TRASH || !Equals(entry.FolderIdDisplay, await _globalFolderHelper.FolderTrashAsync))
        {
            return default;
        }

        var settings = await filesSettingsHelper.GetAutomaticallyCleanUp();
        if (settings.IsAutoCleanUp)
        {
            return fileDateTime.GetModifiedOnWithAutoCleanUp(entry.ModifiedOn, settings.Gap);
        }

        return default;
    }
}
