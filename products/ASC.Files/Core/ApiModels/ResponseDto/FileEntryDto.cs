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

using static ASC.Files.Core.Security.FileSecurity;

namespace ASC.Files.Core.ApiModels.ResponseDto;

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FileDto<int>[]))]
[JsonSerializable(typeof(FileDto<string>[]))]
[JsonSerializable(typeof(FolderDto<int>[]))]
[JsonSerializable(typeof(FolderDto<string>[]))]
public partial class FileEntryDtoContext : JsonSerializerContext;

/// <summary>
/// The file entry information.
/// </summary>
[JsonDerivedType(typeof(FileDto<int>))]
[JsonDerivedType(typeof(FileDto<string>))]
[JsonDerivedType(typeof(FolderDto<int>))]
[JsonDerivedType(typeof(FolderDto<string>))]
public abstract class FileEntryBaseDto
{
    /// <summary>
    /// The file entry title.
    /// </summary>
    /// <example>Some title.txt</example>
    public string Title { get; set; }

    /// <summary>
    /// The access rights to the file entry.
    /// </summary>
    /// <example>1</example>
    public FileShare Access { get; set; }

    /// <summary>
    /// Provides information about the employee who shared the file or folder.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto SharedBy { get; set; }

    /// <summary>
    /// The information about the employee who owns the file entry.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto OwnedBy { get; set; }

    /// <summary>
    /// Specifies if the file entry is shared via link or not.
    /// </summary>
    /// <example>false</example>
    public bool Shared { get; set; }

    /// <summary>
    /// Specifies if the file entry is shared for user or not.
    /// </summary>
    /// <example>false</example>
    public bool SharedForUser { get; set; }

    /// <summary>
    /// Indicates whether the parent entity is shared.
    /// </summary>
    /// <example>false</example>
    public bool ParentShared { get; set; }

    /// <summary>
    /// The short Web URL.
    /// </summary>
    /// <example>http://localhost/s/abc123</example>
    [Url]
    public string ShortWebUrl { get; set; }

    /// <summary>
    /// The creation date and time of the file entry.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime Created { get; set; }

    /// <summary>
    /// The file entry author.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// The last date and time when the file entry was updated.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime Updated
    {
        get => field < Created ? Created : field;
        set;
    }

    /// <summary>
    /// The date and time when the file entry will be automatically deleted.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime AutoDelete { get; set; }

    /// <summary>
    /// The root folder type of the file entry.
    /// </summary>
    /// <example>0</example>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The parent room type of the file entry.
    /// </summary>
    /// <example>0</example>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// The user who updated the file entry.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto UpdatedBy { get; set; }

    /// <summary>
    /// Specifies if the file entry provider is specified or not.
    /// </summary>
    /// <example>false</example>
    public bool? ProviderItem { get; set; }

    /// <summary>
    /// The provider key of the file entry.
    /// </summary>
    /// <example>google-drive</example>
    public string ProviderKey { get; set; }

    /// <summary>
    /// The provider ID of the file entry.
    /// </summary>
    /// <example>1</example>
    public int? ProviderId { get; set; }

    /// <summary>
    /// The order of the file entry.
    /// </summary>
    /// <example>1</example>
    public string Order { get; set; }

    /// <summary>
    /// Specifies if the file is a favorite or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// The file entry type.
    /// </summary>
    /// <example>0</example>
    public abstract FileEntryType FileEntryType { get; }

    protected FileEntryBaseDto(FileEntry entry)
    {
        Title = entry.Title;
        Access = entry.Access;
        Shared = entry.Shared;
        SharedForUser = entry.SharedForUser;
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
/// The generic file entry information.
/// </summary>
[DebuggerDisplay("{Title} ({Id})")]
public abstract class FileEntryDto<T> : FileEntryBaseDto
{
    /// <summary>
    /// The file entry ID.
    /// </summary>
    /// <example>10</example>
    public T Id { get; set; }

    /// <summary>
    /// The root folder ID of the file entry.
    /// </summary>
    /// <example>1</example>
    public T RootFolderId { get; set; }

    /// <summary>
    /// The origin ID of the file entry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T OriginId { get; set; }

    /// <summary>
    /// The origin room ID of the file entry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T OriginRoomId { get; set; }

    /// <summary>
    /// The origin title of the file entry.
    /// </summary>
    /// <example>Original Title</example>
    public string OriginTitle { get; set; }

    /// <summary>
    /// The origin room title of the file entry.
    /// </summary>
    /// <example>Original Room</example>
    public string OriginRoomTitle { get; set; }

    /// <summary>
    /// Specifies if the file entry can be shared or not.
    /// </summary>
    /// <example>true</example>
    public bool CanShare { get; set; }

    /// <summary>
    /// A dictionary representing the sharing settings for the file entry.
    /// </summary>
    /// <example>{"ExternalLink": 1, "InvitationLink": 2}</example>
    public IDictionary<SubjectType, int> ShareSettings { get; set; }

    /// <summary>
    /// The actions that can be performed with the file entry.
    /// </summary>
    /// <example>{"Read": true, "Edit": false, "Delete": false}</example>
    public IDictionary<FilesSecurityActions, bool> Security { get; set; }

    /// <summary>
    /// The available external rights of the file entry.
    /// </summary>
    /// <example>{"ExternalLink": ["Read", "Edit"]}</example>
    public IDictionary<SubjectType, IEnumerable<string>> AvailableShareRights { get; set; }

    /// <summary>
    /// The request token of the file entry.
    /// </summary>
    /// <example>token-abc-123</example>
    public string RequestToken { get; set; }

    /// <summary>
    /// Specifies if the folder can be accessed via an external link or not.
    /// </summary>
    /// <example>false</example>
    public bool? External { get; set; }

    /// <summary>
    /// Represents the expiration date of the file entry.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// Indicates whether the shareable link associated with the file or folder has expired.
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
        }

        var canSetAccess = await fileSharingHelper.CanSetAccessAsync(entry);
        if (entry is Folder<TId> { FolderType: FolderType.EditingRoom or FolderType.VirtualDataRoom })
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
            SharedBy = sharedBy.HasValue ? await employeeWrapperHelper.GetAsync(sharedBy.Value) : null,
            OwnedBy = ownedBy.HasValue ? await employeeWrapperHelper.GetAsync(ownedBy.Value) : null,
            SharedForUser = entry.SharedForUser,
            ParentShared = entry.ParentShared,
            ShortWebUrl = shortWebUrl,
            Created = _apiDateTimeHelper.Get(entry.CreateOn),
            CreatedBy = await employeeWrapperHelper.GetAsync(entry.CreateBy),
            Updated = _apiDateTimeHelper.Get(entry.ModifiedOn),
            UpdatedBy = await employeeWrapperHelper.GetAsync(entry.ModifiedBy),
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

        if (entry.ModifiedOn.Equals(default) || !Equals(entry.FolderIdDisplay, await _globalFolderHelper.FolderTrashAsync))
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