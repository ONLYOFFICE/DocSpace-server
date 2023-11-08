// (c) Copyright Ascensio System SIA 2010-2022
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
        get => !CreateBy.Equals(Guid.Empty) ? Global.GetUserName(CreateBy) : _createByString;
        set => _createByString = value;
    }

    public Guid ModifiedBy { get; set; }

    [JsonIgnore]
    public string ModifiedByString
    {
        get => !ModifiedBy.Equals(Guid.Empty) ? Global.GetUserName(ModifiedBy) : _modifiedByString;
        set => _modifiedByString = value;
    }

    [JsonIgnore]
    public string CreateOnString => CreateOn.Equals(default) ? null : CreateOn.ConvertNumerals();

    [JsonIgnore]
    public string ModifiedOnString => ModifiedOn.Equals(default) ? null : ModifiedOn.ConvertNumerals();

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

static file class ArabicNumeralHelper
{
    public static string ConvertNumerals(this DateTime input)
    {
        if (!new[] { "ar-lb", "ar-SA" }.Contains(Thread.CurrentThread.CurrentCulture.Name))
        {
            return input.ToString("g", CultureInfo.InvariantCulture);
        }

        var currentDateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
        var result = input.ToString($"{currentDateTimeFormat.ShortDatePattern} {currentDateTimeFormat.LongTimePattern}", CultureInfo.InvariantCulture);
        return result
            .Replace('0', '\u06f0')
            .Replace('1', '\u06f1')
            .Replace('2', '\u06f2')
            .Replace('3', '\u06f3')
            .Replace('4', '\u06f4')
            .Replace('5', '\u06f5')
            .Replace('6', '\u06f6')
            .Replace('7', '\u06f7')
            .Replace('8', '\u06f8')
            .Replace('9', '\u06f9');

    }
}

public interface IFileEntry
{
    string UniqID { get; }
}

public abstract class FileEntry<T> : FileEntry, IFileEntry, IEquatable<FileEntry<T>>
{
    public T Id { get; set; }
    public T ParentId { get; set; }
    public T OriginId { get; set; }
    public T OriginRoomId { get; set; }

    public IDictionary<FilesSecurityActions, bool> Security { get; set; }

    private T _folderIdDisplay;


    protected FileEntry() { }

    protected FileEntry(
        FileHelper fileHelper,
        Global global) : base(fileHelper, global)
    {
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
}
