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

namespace ASC.Files.Core.Helpers;

[Singleton]
public class FileUtilityConfiguration(IConfiguration configuration)
{
    private List<string> _extsIndexing;
    public List<string> ExtsIndexing { get => _extsIndexing ??= configuration.GetSection("files:index").Get<List<string>>() ?? new List<string>(); }

    private List<string> _extsImagePreviewed;
    public List<string> ExtsImagePreviewed { get => _extsImagePreviewed ??= configuration.GetSection("files:viewed-images").Get<List<string>>() ?? new List<string>(); }

    private List<string> _extsMediaPreviewed;
    public List<string> ExtsMediaPreviewed { get => _extsMediaPreviewed ??= configuration.GetSection("files:viewed-media").Get<List<string>>() ?? new List<string>(); }

    private List<string> _extsWebPreviewed;
    public List<string> ExtsWebPreviewed
    {
        get
        {
            return _extsWebPreviewed ??= configuration.GetSection("files:docservice:viewed-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsWebEdited;
    public List<string> ExtsWebEdited
    {
        get
        {
            return _extsWebEdited ??= configuration.GetSection("files:docservice:edited-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsWebEncrypt;
    public List<string> ExtsWebEncrypt { get => _extsWebEncrypt ??= configuration.GetSection("files:docservice:encrypted-docs").Get<List<string>>() ?? new List<string>(); }

    private List<string> _extsWebReviewed;
    public List<string> ExtsWebReviewed
    {
        get
        {
            return _extsWebReviewed ??= configuration.GetSection("files:docservice:reviewed-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsWebCustomFilterEditing;
    public List<string> ExtsWebCustomFilterEditing
    {
        get
        {
            return _extsWebCustomFilterEditing ??= configuration.GetSection("files:docservice:customfilter-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsWebRestrictedEditing;
    public List<string> ExtsWebRestrictedEditing
    {
        get
        {
            return _extsWebRestrictedEditing ??= configuration.GetSection("files:docservice:formfilling-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsWebCommented;
    public List<string> ExtsWebCommented
    {
        get
        {
            return _extsWebCommented ??= configuration.GetSection("files:docservice:commented-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsWebTemplate;
    public List<string> ExtsWebTemplate
    {
        get
        {
            return _extsWebTemplate ??= configuration.GetSection("files:docservice:template-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsMustConvert;
    public List<string> ExtsMustConvert
    {
        get
        {
            return _extsMustConvert ??= configuration.GetSection("files:docservice:convert-docs").Get<List<string>>() ?? new List<string>();
        }
    }

    private List<string> _extsCoAuthoring;
    public List<string> ExtsCoAuthoring
    {
        get => _extsCoAuthoring ??= configuration.GetSection("files:docservice:coauthor-docs").Get<List<string>>() ?? new List<string>();
    }

    private string _masterFormExtension;
    public string MasterFormExtension
    {
        get => _masterFormExtension ??= configuration["files:docservice:internal-form"] ?? ".pdf";
    }

    private List<LogoColor> _logoColors;
    public List<LogoColor> LogoColors
    {
        get => _logoColors ??= configuration.GetSection("logocolors").Get<List<LogoColor>>() ?? new List<LogoColor>();
    }

    public Dictionary<FileType, string> InternalExtension
    {
        get => new()
        {
                { FileType.Document, configuration["files:docservice:internal-doc"] ?? ".docx" },
                { FileType.Spreadsheet, configuration["files:docservice:internal-xls"] ?? ".xlsx" },
                { FileType.Presentation, configuration["files:docservice:internal-ppt"] ?? ".pptx" }
            };
    }

    internal string GetSignatureSecret()
    {
        var result = configuration["files:docservice:secret:value"] ?? "";

        var regex = new Regex(@"^\s+$");

        if (regex.IsMatch(result))
        {
            result = "";
        }

        return result;
    }

    internal string GetSignatureHeader()
    {
        var result = (configuration["files:docservice:secret:header"] ?? "").Trim();
        if (string.IsNullOrEmpty(result))
        {
            result = "Authorization";
        }

        return result;
    }


    internal bool GetCanForcesave()
    {
        return !bool.TryParse(configuration["files:docservice:forcesave"] ?? "", out var canForcesave) || canForcesave;
    }
}

public class LogoColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}

public enum Accessibility
{
    ImageView,
    MediaView,
    WebView,
    WebEdit,
    WebReview,
    WebCustomFilterEditing,
    WebRestrictedEditing,
    WebComment,
    CoAuhtoring,
    CanConvert,
    MustConvert
}

[Scope]
public class FileUtility(
    FileUtilityConfiguration fileUtilityConfiguration,
    FilesLinkUtility filesLinkUtility,
    IDbContextFactory<FilesDbContext> dbContextFactory)
{
    #region method

    public static string GetFileExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        string extension = null;
        try
        {
            extension = Path.GetExtension(fileName);
        }
        catch (Exception)
        {
            var position = fileName.LastIndexOf('.');
            if (0 <= position)
            {
                extension = fileName[position..].Trim().ToLower();
            }
        }
        return extension == null ? string.Empty : extension.Trim().ToLower();
    }

    public string GetInternalExtension(string fileName)
    {
        var extension = GetFileExtension(fileName);
        return InternalExtension.GetValueOrDefault(GetFileTypeByExtention(extension), extension);
    }

    public string GetGoogleDownloadableExtension(string googleExtension)
    {
        googleExtension = GetFileExtension(googleExtension);
        if (googleExtension.Equals(".gdraw"))
        {
            return ".pdf";
        }

        return GetInternalExtension(googleExtension);
    }

    public string GetInternalConvertExtension(string fileName)
    {
        return "ooxml";
    }

    public static string ReplaceFileExtension(string fileName, string newExtension)
    {
        newExtension = string.IsNullOrEmpty(newExtension) ? string.Empty : newExtension;
        return Path.GetFileNameWithoutExtension(fileName)
                 + "." + newExtension.TrimStart('.');
    }

    public static FileType GetFileTypeByFileName(string fileName)
    {
        return GetFileTypeByExtention(GetFileExtension(fileName));
    }

    public static FileType GetFileTypeByExtention(string extension)
    {
        extension = extension.ToLower();

        if (ExtsPdf.Contains(extension))
        {
            return FileType.Pdf;
        }

        if (ExtsDocument.Contains(extension))
        {
            return FileType.Document;
        }

        if (ExtsFormTemplate.Contains(extension))
        {
            return FileType.OFormTemplate;
        }

        if (ExtsOForm.Contains(extension))
        {
            return FileType.OForm;
        }

        if (ExtsSpreadsheet.Contains(extension))
        {
            return FileType.Spreadsheet;
        }

        if (ExtsPresentation.Contains(extension))
        {
            return FileType.Presentation;
        }

        if (ExtsImage.Contains(extension))
        {
            return FileType.Image;
        }

        if (ExtsArchive.Contains(extension))
        {
            return FileType.Archive;
        }

        if (ExtsAudio.Contains(extension))
        {
            return FileType.Audio;
        }

        if (ExtsVideo.Contains(extension))
        {
            return FileType.Video;
        }

        return FileType.Unknown;
    }

    public async Task<IDictionary<Accessibility, bool>> GetAccessibility<T>(File<T> file)
    {
        var fileName = file.Title;
        
        var result = new Dictionary<Accessibility, bool>();

        foreach (var r in Enum.GetValues<Accessibility>())
        {
            var val = r switch
            {
                Accessibility.ImageView => CanImageView(fileName),
                Accessibility.MediaView => CanMediaView(fileName),
                Accessibility.WebView => GetWebViewAccessibility(fileName),
                Accessibility.WebEdit => CanWebEdit(fileName),
                Accessibility.WebReview => CanWebReview(fileName),
                Accessibility.WebCustomFilterEditing => CanWebCustomFilterEditing(fileName),
                Accessibility.WebRestrictedEditing => CanWebRestrictedEditing(fileName),
                Accessibility.WebComment => CanWebComment(fileName),
                Accessibility.CoAuhtoring => CanCoAuthoring(fileName),
                Accessibility.CanConvert => await CanConvert(file),
                Accessibility.MustConvert => MustConvert(fileName),
                _ => false
            };

            result.Add(r, val);
        }

        return result;
    }

    public bool CanImageView(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsImagePreviewed.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanMediaView(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsMediaPreviewed.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }
    public bool GetWebViewAccessibility(string fileName)
    {
        var ext = GetFileExtension(fileName).ToLower();
        return !ext.Equals(".pdf") && ExtsWebPreviewed.Contains(ext);
    }
    public bool CanWebView(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsWebPreviewed.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanWebEdit(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsWebEdited.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanWebReview(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsWebReviewed.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanWebCustomFilterEditing(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsWebCustomFilterEditing.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanWebRestrictedEditing(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsWebRestrictedEditing.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanWebComment(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsWebCommented.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanCoAuthoring(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsCoAuthoring.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }
    
    public async Task<bool> CanConvert<T>(File<T> file)
    {
        var ext = GetFileExtension(file.Title);
        return (await GetExtsConvertibleAsync()).ContainsKey(ext);
    }

    public bool MustConvert(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsMustConvert.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanIndex(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ExtsIndexing.Exists(r => r.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region member

    private static readonly SemaphoreSlim _semaphoreSlim = new(1);
    private static ConcurrentDictionary<string, List<string>> _extsConvertible;

    public async Task<IDictionary<string, List<string>>> GetExtsConvertibleAsync()
    {
        if (_extsConvertible != null)
        {
            return _extsConvertible;
        }

        await _semaphoreSlim.WaitAsync();
        
        _extsConvertible = new ConcurrentDictionary<string, List<string>>();
        if (string.IsNullOrEmpty(filesLinkUtility.DocServiceConverterUrl))
        {
            return _extsConvertible;
        }

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var list = await Queries.FoldersAsync(filesDbContext).ToListAsync();

        foreach (var item in list)
        {
            var input = item.Input;
            var output = item.Output;
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            {
                continue;
            }

            input = input.ToLower().Trim();
            output = output.ToLower().Trim();
            if (!_extsConvertible.ContainsKey(input))
            {
                _extsConvertible[input] = new List<string>();
            }

            _extsConvertible[input].Add(output);
        }

        _semaphoreSlim.Release();
        return _extsConvertible;
    }

    private List<string> _extsUploadable;

    public List<string> ExtsUploadable
    {
        get
        {
            if (_extsUploadable == null)
            {
                _extsUploadable = new List<string>();
                _extsUploadable.AddRange(ExtsWebPreviewed);
                _extsUploadable.AddRange(ExtsWebEdited);
                _extsUploadable.AddRange(ExtsImagePreviewed);
                _extsUploadable = _extsUploadable.Distinct().ToList();
            }
            return _extsUploadable;
        }
    }

    private List<string> ExtsIndexing { get => fileUtilityConfiguration.ExtsIndexing; }

    public List<string> ExtsImagePreviewed { get => fileUtilityConfiguration.ExtsImagePreviewed; }

    public List<string> ExtsMediaPreviewed { get => fileUtilityConfiguration.ExtsMediaPreviewed; }

    public List<string> ExtsWebPreviewed
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsWebPreviewed;
        }
    }

    public List<string> ExtsWebEdited
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsWebEdited;
        }
    }

    public List<string> ExtsWebEncrypt { get => fileUtilityConfiguration.ExtsWebEncrypt; }

    public List<string> ExtsWebReviewed
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsWebReviewed;
        }
    }

    public List<string> ExtsWebCustomFilterEditing
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsWebCustomFilterEditing;
        }
    }

    public List<string> ExtsWebRestrictedEditing
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsWebRestrictedEditing;
        }
    }

    public List<string> ExtsWebCommented
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsWebCommented;
        }
    }

    public List<string> ExtsWebTemplate
    {
        get => fileUtilityConfiguration.ExtsWebTemplate;
    }

    public List<string> ExtsMustConvert
    {
        get
        {
            if (string.IsNullOrEmpty(filesLinkUtility.DocServiceConverterUrl))
            {
                return new List<string>();
            }

            return fileUtilityConfiguration.ExtsMustConvert;
        }
    }

    public List<string> ExtsCoAuthoring
    {
        get => fileUtilityConfiguration.ExtsCoAuthoring;
    }

    public static readonly ImmutableList<string> ExtsArchive =  new List<string>
    {
                ".zip", ".rar", ".ace", ".arc", ".arj",
                ".bh", ".cab", ".enc", ".gz", ".ha",
                ".jar", ".lha", ".lzh", ".pak", ".pk3",
                ".tar", ".tgz", ".gz", ".uu", ".uue", ".xxe",
                ".z", ".zoo"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsVideo =  new List<string>
    {
                ".3gp", ".asf", ".avi", ".f4v",
                ".fla", ".flv", ".m2ts", ".m4v",
                ".mkv", ".mov", ".mp4", ".mpeg",
                ".mpg", ".mts", ".ogv", ".svi",
                ".vob", ".webm", ".wmv"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsAudio =  new List<string>
    {
                ".aac", ".ac3", ".aiff", ".amr",
                ".ape", ".cda", ".flac", ".m4a",
                ".mid", ".mka", ".mp3", ".mpc",
                ".oga", ".ogg", ".pcm", ".ra",
                ".raw", ".wav", ".wma"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsImage =  new List<string>
    {
                ".bmp", ".cod", ".gif", ".ief", ".jpe", ".jpeg", ".jpg",
                ".jfif", ".tiff", ".tif", ".cmx", ".ico", ".pnm", ".pbm",
                ".png", ".ppm", ".rgb", ".svg", ".xbm", ".xpm", ".xwd",
                ".svgt", ".svgy", ".gdraw", ".webp"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsSpreadsheet = new List<string>
    {
                ".xls", ".xlsx", ".xlsm",
                ".xlt", ".xltx", ".xltm",
                ".ods", ".fods", ".ots", ".csv",
                ".sxc", ".et", ".ett",
                ".xlst", ".xlsy", ".xlsb",
                ".gsheet"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsPresentation = new List<string>
    {
                ".pps", ".ppsx", ".ppsm",
                ".ppt", ".pptx", ".pptm",
                ".pot", ".potx", ".potm",
                ".odp", ".fodp", ".otp",
                ".dps", ".dpt", ".sxi",
                ".pptt", ".ppty",
                ".gslides"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsDocument = new List<string>
    {
                ".doc", ".docx", ".docm",
                ".dot", ".dotx", ".dotm",
                ".odt", ".fodt", ".ott", ".rtf", ".txt",
                ".html", ".htm", ".mht", ".mhtml", ".xml",
                ".pdf", ".djvu", ".fb2", ".epub", ".xps",".oxps",
                ".sxw", ".stw", ".wps", ".wpt",
                ".doct", ".docy",
                ".gdoc",
                ".drawio",
                ".md", ".markdown"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsFormTemplate = new List<string>
    {
                ".docxf"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsOForm = new List<string>
    {
                ".oform"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsPdf = new List<string>
    {
                ".pdf"
            }.ToImmutableList();

    public static readonly ImmutableList<string> ExtsTemplate = new List<string>
    {
                ".ott", ".ots", ".otp",
                ".dot", ".dotm", ".dotx",
                ".xlt", ".xltm", ".xltx",
                ".pot", ".potm", ".potx"
    }.ToImmutableList();
    public Dictionary<FileType, string> InternalExtension => fileUtilityConfiguration.InternalExtension;

    public string MasterFormExtension { get => fileUtilityConfiguration.MasterFormExtension; }
    public enum CsvDelimiter
    {
        None = 0,
        Tab = 1,
        Semicolon = 2,
        Colon = 3,
        Comma = 4,
        Space = 5
    }
    public string SignatureSecret { get => GetSignatureSecret(); }
    public string SignatureHeader { get => GetSignatureHeader(); }

    private string GetSignatureSecret() => fileUtilityConfiguration.GetSignatureSecret();

    private string GetSignatureHeader() => fileUtilityConfiguration.GetSignatureHeader();
    
    public bool GetCanForcesave() => fileUtilityConfiguration.GetCanForcesave();

    #endregion
}

static file class Queries
{
    public static readonly Func<FilesDbContext, IAsyncEnumerable<FilesConverts>> FoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx) => ctx.FilesConverts);
}