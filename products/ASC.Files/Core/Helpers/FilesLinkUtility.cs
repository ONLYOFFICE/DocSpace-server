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

[Scope]
public class FilesLinkUtility
{
    public const string FilesBaseVirtualPath = "~/";
    public const string EditorPage = "doceditor";
    public TimeSpan DefaultLinkLifeTime { get; }
    public const int MaxLinkLifeTimeInYears = 10;
    
    private readonly string _filesUploaderUrl;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly BaseCommonLinkUtility _baseCommonLinkUtility;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly CoreSettings _coreSettings;
    private readonly IConfiguration _configuration;
    private readonly InstanceCrypto _instanceCrypto;

    public FilesLinkUtility(
        CommonLinkUtility commonLinkUtility,
        BaseCommonLinkUtility baseCommonLinkUtility,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        InstanceCrypto instanceCrypto)
    {
        _commonLinkUtility = commonLinkUtility;
        _baseCommonLinkUtility = baseCommonLinkUtility;
        _coreBaseSettings = coreBaseSettings;
        _coreSettings = coreSettings;
        _configuration = configuration;
        _instanceCrypto = instanceCrypto;
        _filesUploaderUrl = _configuration["files:uploader:url"] ?? "~";
        DefaultLinkLifeTime = !TimeSpan.TryParse(configuration["externalLink:defaultLifetime"], out var defaultLifetime) ? TimeSpan.FromDays(7) : defaultLifetime;
    }

    public string FilesBaseAbsolutePath
    {
        get { return _baseCommonLinkUtility.ToAbsolute(FilesBaseVirtualPath); }
    }

    public const string FileId = "fileid";
    public const string FolderId = "folderid";
    public const string Version = "version";
    public const string FileUri = "fileuri";
    public const string FileTitle = "title";
    public const string Action = "action";
    public const string TryParam = "try";
    public const string FolderUrl = "folderurl";
    public const string OutType = "outputtype";
    public const string AuthKey = "stream_auth";
    public const string Anchor = "anchor";
    public const string ShareKey = "share";
    public const string FillingSessionId = "filling_session_id";
    public const string IsFile = "is_file";
    public const string View = "view";
    public const string ShardKey = "shardkey";

    public string FileHandlerPath
    {
        get { return FilesBaseAbsolutePath + "filehandler.ashx"; }
    }

    private const string PublicUrlKey = "public";

    public string GetDocServiceUrl()
    {
        var url = GetUrlSetting(PublicUrlKey, out _);
        if (!string.IsNullOrEmpty(url) && url != "/")
        {
            url = url.TrimEnd('/') + "/";
        }

        return url;
    }

    public async Task SetDocServiceUrlAsync(string value)
    {
        await SetUrlSettingAsync(ApiUrlKey, null);

        value = (value ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(value))
        {
            value = value.TrimEnd('/') + "/";
            if (!new Regex(@"(^https?:\/\/)|^\/", RegexOptions.CultureInvariant).IsMatch(value))
            {
                value = "http://" + value;
            }
        }

        await SetUrlSettingAsync(PublicUrlKey, value);
    }

    private const string InternalUrlKey = "internal";

    public string GetDocServiceUrlInternal()
    {
        var url = GetUrlSetting(InternalUrlKey, out _);
        if (string.IsNullOrEmpty(url))
        {
            url = GetDocServiceUrl();
        }
        else
        {
            url = url.TrimEnd('/') + "/";
        }

        return url;
    }

    public async Task SetDocServiceUrlInternalAsync(string value)
    {
        await SetUrlSettingAsync("converter", null);
        await SetUrlSettingAsync("storage", null);
        await SetUrlSettingAsync("command", null);
        await SetUrlSettingAsync("docbuilder", null);

        value = (value ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(value))
        {
            value = value.TrimEnd('/') + "/";
            if (!new Regex(@"(^https?:\/\/)", RegexOptions.CultureInvariant).IsMatch(value))
            {
                value = "http://" + value;
            }
        }

        if (GetDocServiceUrlInternal() != value)
        {
            await SetUrlSettingAsync(InternalUrlKey, value);
        }
    }

    private const string ApiUrlKey = "api";
    public string DocServiceApiUrl
    {
        get
        {
            var url = GetUrlSetting(ApiUrlKey, out _);
            if (string.IsNullOrEmpty(url))
            {
                url = GetDocServiceUrl();
                if (!string.IsNullOrEmpty(url))
                {
                    url += "web-apps/apps/api/documents/api.js";
                }
            }
            return url;
        }
    }

    public string DocServiceConverterUrl
    {
        get
        {
            var url = GetUrlSetting("converter", out _);
            if (string.IsNullOrEmpty(url))
            {
                url = GetDocServiceUrlInternal();
                if (!string.IsNullOrEmpty(url))
                {
                    url += "ConvertService.ashx";
                }
            }
            return url;
        }
    }

    public string DocServiceCommandUrl
    {
        get
        {
            var url = GetUrlSetting("command", out _);
            if (string.IsNullOrEmpty(url))
            {
                url = GetDocServiceUrlInternal();
                if (!string.IsNullOrEmpty(url))
                {
                    url += "coauthoring/CommandService.ashx";
                }
            }
            return url;
        }
    }

    public string DocServiceDocbuilderUrl
    {
        get
        {
            var url = GetUrlSetting("docbuilder", out _);
            if (string.IsNullOrEmpty(url))
            {
                url = GetDocServiceUrlInternal();
                if (!string.IsNullOrEmpty(url))
                {
                    url += "docbuilder";
                }
            }
            return url;
        }
    }

    public string DocServiceHealthcheckUrl
    {
        get
        {
            var url = GetUrlSetting("healthcheck", out _);
            if (string.IsNullOrEmpty(url))
            {
                url = GetDocServiceUrlInternal();
                if (!string.IsNullOrEmpty(url))
                {
                    url += "healthcheck";
                }
            }
            return url;
        }
    }

    private const string PortalUrlKey = "portal";

    public string GetDocServicePortalUrl()
    {
        return GetUrlSetting(PortalUrlKey, out _);
    }

    public async Task SetDocServicePortalUrlAsync(string value)
    {
        value = (value ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(value))
        {
            value = value.TrimEnd('/') + "/";
            if (!new Regex(@"(^https?:\/\/)", RegexOptions.CultureInvariant).IsMatch(value))
            {
                value = "http://" + value;
            }
        }

        await SetUrlSettingAsync(PortalUrlKey, value);
    }

    public bool IsDefault
    {
        get
        {
            GetUrlSetting(PublicUrlKey, out var isDefault);
            if (!isDefault)
            {
                return false;
            }

            GetUrlSetting(InternalUrlKey, out isDefault);
            if (!isDefault)
            {
                return false;
            }

            GetUrlSetting(PortalUrlKey, out isDefault);
            if (!isDefault)
            {
                return false;
            }

            return true;
        }
    }

    public string FileDownloadUrlString
    {
        get { return FileHandlerPath + "?" + Action + "=download&" + FileId + "={0}"; }
    }

    public string GetFileDownloadUrl(object fileId)
    {
        return GetFileDownloadUrl(fileId, 0, string.Empty);
    }

    public string GetFileDownloadUrl(object fileId, int fileVersion, string convertToExtension)
    {
        return string.Format(FileDownloadUrlString, HttpUtility.UrlEncode(fileId.ToString()))
               + (fileVersion > 0 ? "&" + Version + "=" + fileVersion : string.Empty)
               + (string.IsNullOrEmpty(convertToExtension) ? string.Empty : "&" + OutType + "=" + convertToExtension);
    }

    public string FileWebViewerUrlString
    {
        get { return $"{FileWebEditorUrlString}&{Action}=view"; }
    }

    public string FileWebViewerExternalUrlString
    {
        get { return FilesBaseAbsolutePath + EditorPage + "?" + FileUri + "={0}&" + FileTitle + "={1}&" + FolderUrl + "={2}"; }
    }

    public string FileWebEditorUrlString
    {
        get { return $"/{EditorPage}?{FileId}={{0}}"; }
    }

    public string GetFileWebEditorUrl<T>(T fileId, int fileVersion = 0)
    {
        return string.Format(FileWebEditorUrlString, HttpUtility.UrlEncode(fileId.ToString()))
            + (fileVersion > 0 ? "&" + Version + "=" + fileVersion : string.Empty);
    }

    public string FileWebEditorExternalUrlString
    {
        get { return FileHandlerPath + "?" + Action + "=create&" + FileUri + "={0}&" + FileTitle + "={1}"; }
    }

    public string GetFileWebPreviewUrl(FileUtility fileUtility, string fileTitle, object fileId, int fileVersion = 0, bool external = false)
    {
        if (fileUtility.CanImageView(fileTitle) || fileUtility.CanMediaView(fileTitle))
        {
            return GetFileWebMediaViewUrl(fileId, external);
        }

        if (fileUtility.CanWebView(fileTitle))
        {
            if (fileUtility.ExtsMustConvert.Contains(FileUtility.GetFileExtension(fileTitle)))
            {
                return string.Format(FileWebViewerUrlString, HttpUtility.UrlEncode(fileId.ToString()));
            }

            return GetFileWebEditorUrl(fileId, fileVersion);
        }

        return GetFileDownloadUrl(fileId);
    }

    public string FileRedirectPreviewUrlString
    {
        get { return FileHandlerPath + "?" + Action + "=redirect"; }
    }

    public string FileThumbnailUrlString
    {
        get { return FileHandlerPath + "?" + Action + "=thumb&" + FileId + "={0}"; }
    }

    public string GetFileThumbnailUrl(object fileId, int fileVersion)
    {
        return string.Format(FileThumbnailUrlString, HttpUtility.UrlEncode(fileId.ToString()))
               + (fileVersion > 0 ? "&" + Version + "=" + fileVersion : string.Empty);
    }


    public async Task<string> GetInitiateUploadSessionUrlAsync(int tenantId, object folderId, object fileId, string fileName, long contentLength, bool encrypted, SecurityContext securityContext)
    {
        var queryString = string.Format("?initiate=true&{0}={1}&fileSize={2}&tid={3}&userid={4}&culture={5}&encrypted={6}",
                                        FileTitle,
                                        HttpUtility.UrlEncode(fileName),
                                        contentLength,
                                        tenantId,
                                        HttpUtility.UrlEncode(await _instanceCrypto.EncryptAsync(securityContext.CurrentAccount.ID.ToString())),
                                        CultureInfo.CurrentUICulture.Name,
                                        encrypted.ToString().ToLower());

        if (fileId != null)
        {
            queryString = queryString + "&" + FileId + "=" + HttpUtility.UrlEncode(fileId.ToString());
        }

        if (folderId != null)
        {
            queryString = queryString + "&" + FolderId + "=" + HttpUtility.UrlEncode(folderId.ToString());
        }

        return _commonLinkUtility.GetFullAbsolutePath(GetFileUploaderHandlerVirtualPath() + queryString);
    }

    public string GetUploadChunkLocationUrl(string uploadId)
    {
        var queryString = "?uid=" + uploadId;
        return _commonLinkUtility.GetFullAbsolutePath(GetFileUploaderHandlerVirtualPath() + queryString);
    }

    public bool IsLocalFileUploader
    {
        get { return !Regex.IsMatch(_filesUploaderUrl, "^http(s)?://\\.*"); }
    }

    private string GetFileUploaderHandlerVirtualPath()
    {
        return _filesUploaderUrl.EndsWith(".ashx") ? _filesUploaderUrl : _filesUploaderUrl.TrimEnd('/') + "/ChunkedUploader.ashx";
    }

    private string GetUrlSetting(string key, out bool isDefault)
    {
        var value = string.Empty;
        isDefault = false;

        if (_coreBaseSettings.Standalone)
        {
            value = _coreSettings.GetSetting(GetSettingsKey(key));
        }

        if (string.IsNullOrEmpty(value))
        {
            value = GetDefaultUrlSetting(key);
            isDefault = true;
        }

        return value;
    }

    private string GetDefaultUrlSetting(string key)
    {
        var value = _configuration[$"files:docservice:url:{key}"];
        if (!string.IsNullOrEmpty(value))
        {
            value = value.TrimEnd('/') + "/";
        }
        return value;
    }

    private async Task SetUrlSettingAsync(string key, string value)
    {
        if (!_coreBaseSettings.Standalone)
        {
            throw new NotSupportedException("Method for server edition only.");
        }
        value = (value ?? "").Trim();
        if (string.IsNullOrEmpty(value))
        {
            value = null;
        }

        if (value != null)
        {
            var def = GetDefaultUrlSetting(key);
            if (def == value)
            {
                value = null;
            }
        }

        if (GetUrlSetting(key, out _) != value)
        {
            await _coreSettings.SaveSettingAsync(GetSettingsKey(key), value);
        }
    }

    private string GetSettingsKey(string key)
    {
        return "DocKey_" + key;
    }
    
    private string GetFileWebMediaViewUrl(object fileId, bool external = false)
    {
        var id = HttpUtility.UrlEncode(fileId.ToString());
        
        if (external)
        {
            return FilesBaseAbsolutePath + $"share/preview/{id}";
        }
        
        return FilesBaseAbsolutePath + $"#preview/{id}";
    }

    public static string AddQueryString(string uri, Dictionary<string, string> queryString)
    {
        var uriToBeAppended = uri;
        var anchorText = string.Empty;

        var anchorIndex = uri.IndexOf('#');
        if (anchorIndex != -1)
        {
            anchorText = uriToBeAppended.Substring(anchorIndex);
            uriToBeAppended = uriToBeAppended.Substring(0, anchorIndex);
        }

        var queryIndex = uriToBeAppended.IndexOf('?');
        var hasQuery = queryIndex != -1;

        var sb = new StringBuilder();
        sb.Append(uriToBeAppended);
        foreach (var parameter in queryString)
        {
            if (parameter.Value == null)
            {
                continue;
            }

            sb.Append(hasQuery ? '&' : '?');
            sb.Append(HttpUtility.UrlEncode(parameter.Key));
            sb.Append('=');
            sb.Append(HttpUtility.UrlEncode(parameter.Value));
            hasQuery = true;
        }

        sb.Append(anchorText);
        return sb.ToString();
    }
}
