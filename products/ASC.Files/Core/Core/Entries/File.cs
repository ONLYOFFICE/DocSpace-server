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

/// <summary>
/// The file parameters.
/// </summary>
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

    /// <summary>
    /// The file version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The file version group.
    /// </summary>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The file comment.
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// The file pure title.
    /// </summary>
    public string PureTitle
    {
        get => base.Title;
        set => base.Title = value;
    }

    /// <summary>
    /// The file content length.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    /// The file content length in the string format.
    /// </summary>
    [JsonIgnore]
    public string ContentLengthString => FileSizeComment.FilesSizeToString(ContentLength);

    /// <summary>
    /// The file filter type.
    /// </summary>
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
                    return this.IsForm ? FilterType.PdfForm : FilterType.Pdf;
                   
            }

            return FilterType.None;
        }
    }
    /// <summary>
    /// Returns the file status.
    /// </summary>
    public async Task<FileStatus> GetFileStatus()
    {
        _status = await FileHelper.GetFileStatus(this, _status);
        return _status;
    }

    /// <summary>
    /// Sets the file status.
    /// </summary>
    public void SetFileStatus(FileStatus value) => _status = value;

    /// <summary>
    /// Sets the file unique ID.
    /// </summary>
    public override string UniqID => $"file_{Id}";

    /// <summary>
    /// The file title.
    /// </summary>
    [JsonIgnore]
    public override string Title => FileHelper.GetTitle(this);

    /// <summary>
    /// The file download URL.
    /// </summary>
    [JsonIgnore]
    public string DownloadUrl => FileHelper.GetDownloadUrl(this);

    /// <summary>
    /// Specifies whether the file is locked or not.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Specifies whether the file is a form or not.
    /// </summary>
    public bool IsForm {
        get
        {
            return (FilterType)Category == FilterType.PdfForm;
        }
    }

    /// <summary>
    /// The file category.
    /// </summary>
    public int Category { get; set; }

    /// <summary>
    /// The user who locked the file.
    /// </summary>
    public string LockedBy { get; set; }

    /// <summary>
    /// Specifies whether the file is new or not.
    /// </summary>
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

    /// <summary>
    /// Specifies whether the file is favorite or not.
    /// </summary>
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

    /// <summary>
    /// Specifies whether the file is a template or not.
    /// </summary>
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

    /// <summary>
    /// Specifies whether the file is encrypted or not.
    /// </summary>
    public bool Encrypted { get; set; }

    /// <summary>
    /// The file thumbnail status.
    /// </summary>
    public Thumbnail ThumbnailStatus { get; set; }

    /// <summary>
    /// The file force save type.
    /// </summary>
    public ForcesaveType Forcesave { get; set; }

    /// <summary>
    /// The file converted type.
    /// </summary>
    public string ConvertedType { get; set; }

    /// <summary>
    /// The file converted extension.
    /// </summary>
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
    
    /// <summary>
    /// The date and time when the file was last opened.
    /// </summary>
    public DateTime? LastOpened { get; set; }

    /// <summary>
    /// The file form information.
    /// </summary>
    public FormInfo<T> FormInfo { get; set; }
}

/// <summary>
/// The file form information.
/// </summary>
public record FormInfo<T>
{
    /// <summary>
    /// The linked ID of the form.
    /// </summary>
    public T LinkedId { get; init; }

    /// <summary>
    /// The form properties.
    /// </summary>
    public EntryProperties<T> Properties { get; init; }
    
    /// <summary>
    /// The empty form information.
    /// </summary>
    public static FormInfo<T> Empty => new();
}
