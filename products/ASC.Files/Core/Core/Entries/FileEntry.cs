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

public abstract class FileEntry : ICloneable
{
    [JsonIgnore]
    public FileHelper FileHelper { get; set; }

    [JsonIgnore]
    public Global Global { get; set; }

    protected FileEntry() { }

    protected FileEntry(FileHelper fileHelper, Global global)
    {
        FileHelper = fileHelper;
        Global = global;
    }

    public virtual string Title { get; set; }
    public Guid CreateBy { get; set; }

    [JsonIgnore]
    public string CreateByString
    {
        get => !CreateBy.Equals(Guid.Empty) ? Global.GetUserNameAsync(CreateBy).Result : _createByString;
        set => _createByString = value;
    }

    public Guid ModifiedBy { get; set; }

    [JsonIgnore]
    public string ModifiedByString
    {
        get => !ModifiedBy.Equals(Guid.Empty) ? Global.GetUserNameAsync(ModifiedBy).Result : _modifiedByString;
        set => _modifiedByString = value;
    }

    [JsonIgnore]
    public string CreateOnString => CreateOn.Equals(default) ? null : CreateOn.ConvertNumerals("g");

    [JsonIgnore]
    public string ModifiedOnString => ModifiedOn.Equals(default) ? null : ModifiedOn.ConvertNumerals("g");

    public string Error { get; set; }
    public FileShare Access { get; set; }
    public bool Shared { get; set; }
    public int ProviderId { get; set; }
    public string ProviderKey { get; set; }

    [JsonIgnore]
    public bool ProviderEntry => !string.IsNullOrEmpty(ProviderKey);

    public DateTime CreateOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public FolderType RootFolderType { get; set; }
    public Guid RootCreateBy { get; set; }
    public abstract bool IsNew { get; set; }
    public FileEntryType FileEntryType { get; set; }
    public IEnumerable<Tag> Tags { get; set; }
    public string OriginTitle { get; set; }
    public string OriginRoomTitle { get; set; }
    public FileShareRecord ShareRecord { get; set; }
    public int Order { get; set; }

    private string _modifiedByString;
    private string _createByString;

    public override string ToString()
    {
        return Title;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public abstract class FileEntry<T> : FileEntry, IEquatable<FileEntry<T>>
{
    public T Id { get; set; }
    public T ParentId { get; set; }
    public T OriginId { get; set; }
    public T OriginRoomId { get; set; }
    public bool MutableId { get; set; }

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

    public bool DenyDownload { get; set; }

    public bool DenySharing { get; set; }

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
            DocSpaceHelper.IsRoom(RootFolderType) ?
                ASC.Core.Configuration.Constants.CoreSystem.ID :

                RootFolderType == FolderType.USER || RootFolderType == FolderType.DEFAULT || RootFolderType == FolderType.TRASH ?
                    RootCreateBy :

                    RootFolderType == FolderType.Privacy && CreateBy == _securityContext.CurrentAccount.ID ?
                        CreateBy :
                        ASC.Core.Configuration.Constants.CoreSystem.ID;

    }
}
