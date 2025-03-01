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

namespace ASC.Files.Core;

[Flags]
public enum FileStatus
{
    [SwaggerEnum(Description = "None")]
    None = 0x0,

    [SwaggerEnum(Description = "Is editing")]
    IsEditing = 0x1,

    [SwaggerEnum(Description = "Is new")]
    IsNew = 0x2,

    [SwaggerEnum(Description = "Is converting")]
    IsConverting = 0x4,

    [SwaggerEnum(Description = "Is original")]
    IsOriginal = 0x8,

    [SwaggerEnum(Description = "Is editing alone")]
    IsEditingAlone = 0x10,

    [SwaggerEnum(Description = "Is favorite")]
    IsFavorite = 0x20,

    [SwaggerEnum(Description = "Is template")]
    IsTemplate = 0x40,

    [SwaggerEnum(Description = "Is fill form draft")]
    IsFillFormDraft = 0x80
}

[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
[DebuggerDisplay("{Title} ({Id} v{Version})")]
public class File<T> : FileEntry<T>
{
    private FileStatus _status;

    public File()
    {
        Version = 1;
        VersionGroup = 1;
        FileEntryType = FileEntryType.File;
    }

    public File(
        FileHelper fileHelper,
        Global global, SecurityContext securityContext) : base(fileHelper, global, securityContext)
    {
        Version = 1;
        VersionGroup = 1;
        FileEntryType = FileEntryType.File;
    }

    public int Version { get; set; }
    public int VersionGroup { get; set; }
    public string Comment { get; set; }
    public string PureTitle
    {
        get => base.Title;
        set => base.Title = value;
    }
    public long ContentLength { get; set; }

    [JsonIgnore]
    public string ContentLengthString => FileSizeComment.FilesSizeToString(ContentLength);

    [JsonIgnore]
    public FilterType FilterType
    {
        get
        {
            switch (FileUtility.GetFileTypeByFileName(Title))
            {
                case FileType.Image:
                    return FilterType.ImagesOnly;
                case FileType.Document:
                    return FilterType.DocumentsOnly;
                case FileType.Presentation:
                    return FilterType.PresentationsOnly;
                case FileType.Spreadsheet:
                    return FilterType.SpreadsheetsOnly;
                case FileType.Archive:
                    return FilterType.ArchiveOnly;
                case FileType.Audio:
                case FileType.Video:
                    return FilterType.MediaOnly;
                case FileType.Pdf:
                    if (this.IsForm)
                    {
                        return FilterType.PdfForm;
                    }
                    else
                    {
                        return FilterType.Pdf;
                    }
                   
            }

            return FilterType.None;
        }
    }

    public async Task<FileStatus> GetFileStatus()
    {
        _status = await FileHelper.GetFileStatus(this, _status);
        return _status;
    }

    public void SetFileStatus(FileStatus value) => _status = value;

    public override string UniqID => $"file_{Id}";

    [JsonIgnore]
    public override string Title => FileHelper.GetTitle(this);


    [JsonIgnore]
    public string DownloadUrl => FileHelper.GetDownloadUrl(this);

    public bool Locked { get; set; }
    public bool IsForm {
        get
        {
            return (FilterType)Category == FilterType.PdfForm;
        }
    }

    public int Category { get; set; }
    public string LockedBy { get; set; }

    [JsonIgnore]
    public override bool IsNew
    {
        get => (_status & FileStatus.IsNew) == FileStatus.IsNew;
        set
        {
            if (value)
            {
                _status |= FileStatus.IsNew;
            }
            else
            {
                _status &= ~FileStatus.IsNew;
            }
        }
    }

    [JsonIgnore]
    public bool IsFavorite
    {
        get => (_status & FileStatus.IsFavorite) == FileStatus.IsFavorite;
        set
        {
            if (value)
            {
                _status |= FileStatus.IsFavorite;
            }
            else
            {
                _status &= ~FileStatus.IsFavorite;
            }
        }
    }

    [JsonIgnore]
    public bool IsTemplate
    {
        get => (_status & FileStatus.IsTemplate) == FileStatus.IsTemplate;
        set
        {
            if (value)
            {
                _status |= FileStatus.IsTemplate;
            }
            else
            {
                _status &= ~FileStatus.IsTemplate;
            }
        }
    }

    public bool Encrypted { get; set; }
    public Thumbnail ThumbnailStatus { get; set; }
    public ForcesaveType Forcesave { get; set; }
    public string ConvertedType { get; set; }

    [JsonIgnore]
    public string ConvertedExtension
    {
        get
        {
            if (string.IsNullOrEmpty(ConvertedType))
            {
                return FileUtility.GetFileExtension(Title);
            }

            var curFileType = FileUtility.GetFileTypeByFileName(Title);

            return curFileType switch
            {
                FileType.Image => ConvertedType.Trim('.') == "zip" ? ".pptt" : ConvertedType,
                FileType.Spreadsheet => ConvertedType.Trim('.') != "xlsx" ? ".xlst" : ConvertedType,
                FileType.Document => ConvertedType.Trim('.') == "zip" ? ".doct" : ConvertedType,
                _ => ConvertedType
            };
        }
    }
    
    public DateTime? LastOpened { get; set; }
    public FormInfo<T> FormInfo { get; set; }
}

public record FormInfo<T>
{
    public T LinkedId { get; init; }
    public EntryProperties<T> Properties { get; init; }
    public static  FormInfo<T> Empty => new();
}
