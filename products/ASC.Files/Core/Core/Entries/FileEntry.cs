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

namespace ASC.Files.Core;

/// <summary>
/// The file entry parameters.
/// </summary>
public abstract class FileEntry : ICloneable
{
    /// <summary>
    /// The file entry helper.
    /// </summary>
    [JsonIgnore]
    public FileHelper FileHelper { get; set; }

    /// <summary>
    /// The global file entry.
    /// </summary>
    [JsonIgnore] 
    private Global Global { get; }

    protected FileEntry() { }

    protected FileEntry(FileHelper fileHelper, Global global)
    {
        FileHelper = fileHelper;
        Global = global;
    }

    /// <summary>
    /// The file entry title.
    /// </summary>
    public virtual string Title { get; set; }

    /// <summary>
    /// The file entry creat by ID.
    /// </summary>
    public Guid CreateBy { get; set; }

    /// <summary>
    /// The file entry create by string.
    /// </summary>
    [JsonIgnore]
    public string CreateByString
    {
        get => !CreateBy.Equals(Guid.Empty) ? Global.GetUserNameAsync(CreateBy).Result : _createByString;
        set => _createByString = value;
    }

    /// <summary>
    /// The ID who modified the file entry.
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// The file entry modified by string.
    /// </summary>
    [JsonIgnore]
    public string ModifiedByString
    {
        get => !ModifiedBy.Equals(Guid.Empty) ? Global.GetUserNameAsync(ModifiedBy).Result : _modifiedByString;
        set => _modifiedByString = value;
    }

    /// <summary>
    /// The time when the file entry was created on.
    /// </summary>
    [JsonIgnore]
    public string CreateOnString => CreateOn.Equals(default) ? null : CreateOn.ConvertNumerals("g");

    /// <summary>
    /// The time when the file entry was modified on.
    /// </summary>
    [JsonIgnore]
    public string ModifiedOnString => ModifiedOn.Equals(default) ? null : ModifiedOn.ConvertNumerals("g");

    /// <summary>
    /// The error message of the file entry.
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    /// The access to the file entry.
    /// </summary>
    public FileShare Access { get; set; }

    /// <summary>
    /// Specifies if the file entry shared or not.
    /// </summary>
    public bool Shared { get; set; }

    /// <summary>
    /// The provider ID.
    /// </summary>
    public int ProviderId { get; set; }

    /// <summary>
    /// The provider key.
    /// </summary>
    public string ProviderKey { get; set; }

    /// <summary>
    /// Specifies if the file is the provider entry or not.
    /// </summary>
    [JsonIgnore]
    public bool ProviderEntry => !string.IsNullOrEmpty(ProviderKey);

    /// <summary>
    /// The date and time of the file entry creation.
    /// </summary>
    public DateTime CreateOn { get; set; }

    /// <summary>
    /// The date and time of the file entry modification.
    /// </summary>
    public DateTime ModifiedOn { get; set; }

    /// <summary>
    /// The file entry folder type.
    /// </summary>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The file entry parent folder type.
    /// </summary>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// The root create by of the file entry.
    /// </summary>
    public Guid RootCreateBy { get; set; }

    /// <summary>
    /// Specifies whether the file entry is new or not.
    /// </summary>
    public abstract bool IsNew { get; set; }

    /// <summary>
    /// The file entry type.
    /// </summary>
    public FileEntryType FileEntryType { get; set; }

    /// <summary>
    /// The list of the file entry tags.
    /// </summary>
    public IEnumerable<Tag> Tags { get; set; }

    /// <summary>
    /// The origin title of the file entry.
    /// </summary>
    public string OriginTitle { get; set; }

    /// <summary>
    /// The origin room title of the file entry.
    /// </summary>
    public string OriginRoomTitle { get; set; }

    /// <summary>
    /// The order of the file entry.
    /// </summary>
    public int Order { get; set; }

    private string _modifiedByString;
    private string _createByString;

    /// <summary>
    /// Convert the file entry object to string.
    /// </summary>
    public override string ToString()
    {
        return Title;
    }

    /// <summary>
    /// Clone the file entry object.
    /// </summary>
    public object Clone()
    {
        return MemberwiseClone();
    }
}

/// <summary>
/// The generic file entry parameters.
/// </summary>
public abstract class FileEntry<T> : FileEntry, IEquatable<FileEntry<T>>
{
    /// <summary>
    /// The generic file entry ID.
    /// </summary>
    public T Id { get; set; }

    /// <summary>
    /// The generic file entry parent ID.
    /// </summary>
    public T ParentId { get; set; }

    /// <summary>
    /// The generic file entry origin ID.
    /// </summary>
    public T OriginId { get; set; }

    /// <summary>
    /// The generic file entry origin room ID.
    /// </summary>
    public T OriginRoomId { get; set; }

    /// <summary>
    /// Specifies if the generic file entry ID is mutable or not.
    /// </summary>
    public bool MutableId { get; set; }

    /// <summary>
    /// The file share record.
    /// </summary>
    public FileShareRecord<T> ShareRecord { get; set; }

    /// <summary>
    /// The file entry security actions by boolean result.
    /// </summary>
    public IDictionary<FilesSecurityActions, bool> Security { get; set; }

    private T _folderIdDisplay;
    private readonly SecurityContext _securityContext;

    protected FileEntry() { }

    protected FileEntry(
        FileHelper fileHelper,
        Global global,
        SecurityContext securityContext) : base(fileHelper, global)
    {
        _securityContext = securityContext;
    }

    public T FolderIdDisplay
    {
        get => !EqualityComparer<T>.Default.Equals(_folderIdDisplay, default) ? _folderIdDisplay : ParentId;
        set => _folderIdDisplay = value;
    }

    public T RootId { get; set; }

    [JsonIgnore]
    public virtual string UniqID => $"{GetType().Name.ToLower()}_{Id}";

    public override bool Equals(object obj)
    {
        return obj is FileEntry<T> f && f.FileEntryType == FileEntryType && Equals(f.Id, Id);
    }

    public bool Equals(FileEntry<T> obj)
    {
        return obj != null && Equals(obj.Id, Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, FileEntryType);
    }

    public override string ToString()
    {
        return Title;
    }

    public Guid GetFileQuotaOwner()
    {
        return
            RootFolderType == FolderType.VirtualRooms ?
                ASC.Core.Configuration.Constants.CoreSystem.ID :

                RootFolderType is FolderType.USER or FolderType.DEFAULT or FolderType.TRASH ?
                    RootCreateBy :

                    RootFolderType == FolderType.Privacy && CreateBy == _securityContext.CurrentAccount.ID ?
                        CreateBy :
                        ASC.Core.Configuration.Constants.CoreSystem.ID;

    }
}
