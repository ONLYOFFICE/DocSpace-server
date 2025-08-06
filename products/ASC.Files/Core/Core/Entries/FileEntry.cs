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
    /// The ID of the user who created the file entry.
    /// </summary>
    public Guid CreateBy { get; set; }

    /// <summary>
    /// The name of the user who created the file entry.
    /// </summary>
    [JsonIgnore]
    public string CreateByString
    {
        get => !CreateBy.Equals(Guid.Empty) ? Global.GetUserNameAsync(CreateBy).Result : _createByString;
        set => _createByString = value;
    }

    /// <summary>
    /// The ID of the user who modified the file entry.
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// The name of the user who modified the file entry.
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
    /// The access rights of the file entry.
    /// </summary>
    public FileShare Access { get; set; }

    /// <summary>
    /// Specifies if the file entry shared or not.
    /// </summary>
    public bool Shared { get; set; }

    /// <summary>
    /// Indicates whether the parent entity is shared.
    /// </summary>
    public bool ParentShared { get; set; }

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
    /// The date and time when the file entry was created.
    /// </summary>
    public DateTime CreateOn { get; set; }

    /// <summary>
    /// The date and time when the file entry was modified.
    /// </summary>
    public DateTime ModifiedOn { get; set; }

    /// <summary>
    /// The root folder type of the file entry.
    /// </summary>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The parent room type of the file entry.
    /// </summary>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// The ID of the user who created the root folder of the file entry.
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
    /// Converts the file entry object to the string.
    /// </summary>
    public override string ToString()
    {
        return Title;
    }

    /// <summary>
    /// Clones the file entry object.
    /// </summary>
    public object Clone()
    {
        return MemberwiseClone();
    }
}

/// <summary>
/// The file entry parameters.
/// </summary>
public abstract class FileEntry<T> : FileEntry, IEquatable<FileEntry<T>>
{
    /// <summary>
    /// The file entry ID.
    /// </summary>
    public T Id { get; set; }

    /// <summary>
    /// The file entry parent ID.
    /// </summary>
    public T ParentId { get; set; }

    /// <summary>
    /// The file entry origin ID.
    /// </summary>
    public T OriginId { get; set; }

    /// <summary>
    /// The file entry origin room ID.
    /// </summary>
    public T OriginRoomId { get; set; }

    /// <summary>
    /// Specifies if the file entry ID is mutable or not.
    /// </summary>
    public bool MutableId { get; set; }

    /// <summary>
    /// The record of the file entry sharing settings.
    /// </summary>
    public FileShareRecord<T> ShareRecord { get; set; }

    /// <summary>
    /// The actions that can be performed with the file entry.
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

    /// <summary>
    /// The folder ID display.
    /// </summary>
    public T FolderIdDisplay
    {
        get => !EqualityComparer<T>.Default.Equals(_folderIdDisplay, default) ? _folderIdDisplay : ParentId;
        set => _folderIdDisplay = value;
    }

    /// <summary>
    /// The root ID.
    /// </summary>
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
