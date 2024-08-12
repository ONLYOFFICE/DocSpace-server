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

using static ASC.Files.Core.Security.FileSecurity;

namespace ASC.Files.Core.ApiModels.ResponseDto;

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FileDto<int>[]))]
[JsonSerializable(typeof(FileDto<string>[]))]
[JsonSerializable(typeof(FolderDto<int>[]))]
[JsonSerializable(typeof(FolderDto<string>[]))]
public partial class FileEntryDtoContext : JsonSerializerContext;


[JsonDerivedType(typeof(FileDto<int>))]
[JsonDerivedType(typeof(FileDto<string>))]
[JsonDerivedType(typeof(FolderDto<int>))]
[JsonDerivedType(typeof(FolderDto<string>))]
public abstract class FileEntryDto
{
    [SwaggerSchemaCustom("Title")]
    public string Title { get; set; }

    [SwaggerSchemaCustomString("Access rights", Example = "None")]
    public FileShare Access { get; set; }

    [SwaggerSchemaCustom("Specifies if the file is shared or not")]
    public bool Shared { get; set; }

    [SwaggerSchemaCustom<ApiDateTime>("Creation time")]
    public ApiDateTime Created { get; set; }

    [SwaggerSchemaCustom<EmployeeDto>("Author")]
    public EmployeeDto CreatedBy { get; set; }

    private ApiDateTime _updated;

    [SwaggerSchemaCustom<ApiDateTime>("Time of the last file update")]
    public ApiDateTime Updated
    {
        get => _updated < Created ? Created : _updated;
        set => _updated = value;
    }

    [SwaggerSchemaCustom<ApiDateTime>("Time when the file will be automatically deleted")]
    public ApiDateTime AutoDelete { get; set; }

    [SwaggerSchemaCustomString("Root folder type", Example = "DEFAULT")]
    public FolderType RootFolderType { get; set; }

    [SwaggerSchemaCustomString("First parent folder type", Example = "DEFAULT", Nullable = true)]
    public FolderType? ParentRoomType { get; set; }

    [SwaggerSchemaCustom<EmployeeDto>("A user who updated a file")]
    public EmployeeDto UpdatedBy { get; set; }

    [SwaggerSchemaCustom("Provider is specified or not")]
    public bool? ProviderItem { get; set; }

    [SwaggerSchemaCustom("Provider key")]
    public string ProviderKey { get; set; }

    [SwaggerSchemaCustom("Provider ID")]
    public int? ProviderId { get; set; }

    [SwaggerSchemaCustomString("Order")]
    public string Order { get; set; }
    
    public abstract FileEntryType FileEntryType { get; }

    protected FileEntryDto(FileEntry entry)
    {
        Title = entry.Title;
        Access = entry.Access;
        Shared = entry.Shared;
        RootFolderType = entry.RootFolderType;
        ParentRoomType = entry.ParentRoomType;
        ProviderItem = entry.ProviderEntry.NullIfDefault();
        ProviderKey = entry.ProviderKey;
        ProviderId = entry.ProviderId.NullIfDefault();
    }

    protected FileEntryDto() { }
}

public abstract class FileEntryDto<T> : FileEntryDto
{
    [SwaggerSchemaCustomInt("Id")]
    public T Id { get; set; }

    [SwaggerSchemaCustomInt("Root folder id")]
    public T RootFolderId { get; set; }

    [SwaggerSchemaCustomInt("Origin id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T OriginId { get; set; }

    [SwaggerSchemaCustomInt("Origin room id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T OriginRoomId { get; set; }

    [SwaggerSchemaCustomString("Origin title")]
    public string OriginTitle { get; set; }

    [SwaggerSchemaCustomString("Origin room title")]
    public string OriginRoomTitle { get; set; }

    [SwaggerSchemaCustom("Can share")]
    public bool CanShare { get; set; }

    [SwaggerSchemaCustom<IDictionary<FilesSecurityActions, bool>>("Security")]
    public IDictionary<FilesSecurityActions, bool> Security { get; set; }

    protected FileEntryDto(FileEntry<T> entry)
        : base(entry)
    {
        Id = entry.Id;
        RootFolderId = entry.RootId;
    }

    protected FileEntryDto() { }
}

[Scope]
public class FileEntryDtoHelper(ApiDateTimeHelper apiDateTimeHelper,
    EmployeeDtoHelper employeeWrapperHelper,
    FileSharingHelper fileSharingHelper,
    FileSecurity fileSecurity,
    GlobalFolderHelper globalFolderHelper,
    FilesSettingsHelper filesSettingsHelper,
    FileDateTime fileDateTime)
{
    protected readonly FileSecurity _fileSecurity = fileSecurity;
    protected readonly GlobalFolderHelper _globalFolderHelper = globalFolderHelper;

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
                _ => entry.RootId
            };
        }
        
        return new T
        {
            Id = entry.Id,
            Title = entry.Title,
            Access = entry.Access,
            Shared = entry.Shared,
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
            CanShare = await fileSharingHelper.CanSetAccessAsync(entry),
            Security = entry.Security,
            OriginId = entry.OriginId,
            OriginTitle = entry.OriginTitle,
            OriginRoomId = entry.OriginRoomId,
            OriginRoomTitle = entry.OriginRoomTitle,
            AutoDelete = permanentlyDeletedOn != default ? apiDateTimeHelper.Get(permanentlyDeletedOn) : null
        };
    }

    private async Task<DateTime> GetDeletedPermanentlyOn<T>(FileEntry<T> entry)
    {
        if (!entry.ModifiedOn.Equals(default) && Equals(entry.FolderIdDisplay, await _globalFolderHelper.FolderTrashAsync))
        {
            var settings = await filesSettingsHelper.GetAutomaticallyCleanUp();
            if (settings.IsAutoCleanUp)
            {
                return fileDateTime.GetModifiedOnWithAutoCleanUp(entry.ModifiedOn, settings.Gap);
            }
        }

        return default;
    }
}
