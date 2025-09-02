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
    [SwaggerSchemaCustom(Example = "Some titile.txt/ Some title")]
    public string Title { get; set; }

    /// <summary>
    /// The access rights to the file entry.
    /// </summary>
    public FileShare Access { get; set; }

    /// <summary>
    /// Specifies if the file entry is shared or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool Shared { get; set; }

    /// <summary>
    /// Indicates whether the parent entity is shared.
    /// </summary>
    public bool ParentShared { get; set; }

    /// <summary>
    /// The short Web URL.
    /// </summary>
    [Url]
    public string ShortWebUrl { get; set; }

    /// <summary>
    /// The creation date and time of the file entry.
    /// </summary>
    public ApiDateTime Created { get; set; }

    /// <summary>
    /// The file entry author.
    /// </summary>
    public EmployeeDto CreatedBy { get; set; }

    private ApiDateTime _updated;

    /// <summary>
    /// The last date and time when the file entry was updated.
    /// </summary>
    public ApiDateTime Updated
    {
        get => _updated < Created ? Created : _updated;
        set => _updated = value;
    }

    /// <summary>
    /// The date and time when the file entry will be automatically deleted.
    /// </summary>
    public ApiDateTime AutoDelete { get; set; }

    /// <summary>
    /// The root folder type of the file entry.
    /// </summary>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The parent room type of the file entry.
    /// </summary>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// The user who updated the file entry.
    /// </summary>
    public EmployeeDto UpdatedBy { get; set; }

    /// <summary>
    /// Specifies if the file entry provider is specified or not.
    /// </summary>
    public bool? ProviderItem { get; set; }

    /// <summary>
    /// The provider key of the file entry.
    /// </summary>
    public string ProviderKey { get; set; }

    /// <summary>
    /// The provider ID of the file entry.
    /// </summary>
    public int? ProviderId { get; set; }

    /// <summary>
    /// The order of the file entry.
    /// </summary>
    public string Order { get; set; }

    /// <summary>
    /// The file entry type.
    /// </summary>
    public abstract FileEntryType FileEntryType { get; }

    protected FileEntryBaseDto(FileEntry entry)
    {
        Title = entry.Title;
        Access = entry.Access;
        Shared = entry.Shared;
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
public abstract class FileEntryDto<T> : FileEntryBaseDto
{
    /// <summary>
    /// The file entry ID.
    /// </summary>
    [SwaggerSchemaCustom(Example = 10)]
    public T Id { get; set; }

    /// <summary>
    /// The root folder ID of the file entry.
    /// </summary>
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
    public string OriginTitle { get; set; }

    /// <summary>
    /// The origin room title of the file entry.
    /// </summary>
    public string OriginRoomTitle { get; set; }

    /// <summary>
    /// Specifies if the file entry can be shared or not.
    /// </summary>
    public bool CanShare { get; set; }


    /// <summary>
    /// A dictionary representing the sharing settings for the file entry.
    /// </summary>
    public IDictionary<SubjectType, int> ShareSettings { get; set; }

    /// <summary>
    /// The actions that can be perforrmed with the file entry.
    /// </summary>
    public IDictionary<FilesSecurityActions, bool> Security { get; set; }

    /// <summary>
    /// The available external rights of the file entry.
    /// </summary>
    public IDictionary<string, bool> AvailableExternalRights { get; set; }

    /// <summary>
    /// The request token of the file entry.
    /// </summary>
    public string RequestToken { get; set; }

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
                _ => entry.RootId
            };
        }

        var shortWebUrl = "";

        if (entry.FullShared)
        {
            var linkId = await _externalShare.GetLinkIdAsync();
            var securityDao = _daoFactory.GetSecurityDao<string>();
            var record = await securityDao.GetSharesAsync([linkId]).FirstOrDefaultAsync();
            if (record != null)
            {
                var linkData = await _externalShare.GetLinkDataAsync(entry, record.Subject);
                shortWebUrl = await _urlShortener.GetShortenLinkAsync(linkData.Url);
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
        
        return new T
        {
            Id = entry.Id,
            Title = entry.Title,
            Access = entry.Access,
            Shared = entry.Shared,
            ParentShared = entry.ParentShared,
            ShortWebUrl = shortWebUrl,
            Created = apiDateTimeHelper.Get(entry.CreateOn),
            CreatedBy = await employeeWrapperHelper.GetAsync(entry.CreateBy),
            Updated = apiDateTimeHelper.Get(entry.ModifiedOn),
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
            AutoDelete = permanentlyDeletedOn != default ? apiDateTimeHelper.Get(permanentlyDeletedOn) : null
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