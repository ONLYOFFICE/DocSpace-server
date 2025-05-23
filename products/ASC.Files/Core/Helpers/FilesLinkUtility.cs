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
        var url = GetUrlSetting(PublicUrlKey);
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
        var url = GetUrlSetting(InternalUrlKey);
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
            var url = GetUrlSetting(ApiUrlKey);
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
            var url = GetUrlSetting("converter");
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
            var url = GetUrlSetting("command");
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
            var url = GetUrlSetting("docbuilder");
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
            var url = GetUrlSetting("healthcheck");
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

    public string DocServiceSignatureSecret
    {
        get
        {
            var result = GetSignatureSetting(SignatureSecretKey);

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "";
            }

            return result;
        }
    }

    public string DocServiceSignatureHeader
    {
        get
        {
            var result = GetSignatureSetting(SignatureHeaderKey);

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "Authorization";
            }

            return result;
        }
    }

    private const string PortalUrlKey = "portal";

    public string GetDocServicePortalUrl()
    {
        return GetUrlSetting(PortalUrlKey);
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

    private const string SignatureSecretKey = "secret:value";

    public async Task<string> GetDocServiceSignatureSecretAsync()
    {
        return await GetSignatureSettingAsync(SignatureSecretKey);
    }

    public async Task SetDocServiceSignatureSecretAsync(string value)
    {
        await SetSignatureSettingAsync(SignatureSecretKey, value);
    }

    private const string SignatureHeaderKey = "secret:header";

    public async Task<string> GetDocServiceSignatureHeaderAsync()
    {
        return await GetSignatureSettingAsync(SignatureHeaderKey);
    }

    public async Task SetDocServiceSignatureHeaderAsync(string value)
    {
        await SetSignatureSettingAsync(SignatureHeaderKey, value);
    }

    private const string SslVerificationKey = "sslverification";

    public async Task<bool> GetDocServiceSslVerificationAsync()
    {
        return await GetSslVerificationSettingAsync();
    }

    public async Task SetDocServiceSslVerificationAsync(bool value)
    {
        await SetSslVerificationSettingAsync(value);
    }

    public async Task<bool> IsDefaultAsync()
    {
        if (!await IsDefaultUrlSettingAsync(PublicUrlKey))
        {
            return false;
        }

        if (!await IsDefaultUrlSettingAsync(InternalUrlKey))
        {
            return false;
        }

        if (!await IsDefaultUrlSettingAsync(PortalUrlKey))
        {
            return false;
        }

        if (!await IsDefaultSignatureSettingAsync(SignatureSecretKey))
        {
            return false;
        }

        if (!await IsDefaultSignatureSettingAsync(SignatureHeaderKey))
        {
            return false;
        }

        if (!await IsDefaultSslVerificationAsync())
        {
            return false;
        }

        return true;
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

    private string GetUrlSetting(string key)
    {
        var value = string.Empty;

        if (_coreBaseSettings.Standalone)
        {
            value = _coreSettings.GetSetting(GetSettingsKey(key));
        }

        if (string.IsNullOrEmpty(value))
        {
            value = GetDefaultUrlSetting(key);
        }

        return value;
    }
    
    private async Task<string> GetUrlSettingAsync(string key)
    {
        var value = string.Empty;

        if (_coreBaseSettings.Standalone)
        {
            value = await _coreSettings.GetSettingAsync(GetSettingsKey(key));
        }

        if (string.IsNullOrEmpty(value))
        {
            value = GetDefaultUrlSetting(key);
        }

        return value;
    }
    
    private async Task<bool> IsDefaultUrlSettingAsync(string key)
    {
        var value = string.Empty;

        if (_coreBaseSettings.Standalone)
        {
            value = await _coreSettings.GetSettingAsync(GetSettingsKey(key));
        }

        return string.IsNullOrEmpty(value);
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

        if (await GetUrlSettingAsync(key) != value)
        {
            await _coreSettings.SaveSettingAsync(GetSettingsKey(key), value);
        }
    }

    private string GetSignatureSetting(string key)
    {
        var value = string.Empty;

        if (_coreBaseSettings.Standalone)
        {
            value = _coreSettings.GetSetting(GetSettingsKey(key));
        }

        if (string.IsNullOrEmpty(value))
        {
            value = GetDefaultSignatureSetting(key);
        }

        return value;
    }
    
    private async Task<string> GetSignatureSettingAsync(string key)
    {
        var value = string.Empty;

        if (_coreBaseSettings.Standalone)
        {
            value = await _coreSettings.GetSettingAsync(GetSettingsKey(key));
        }

        if (string.IsNullOrEmpty(value))
        {
            value = GetDefaultSignatureSetting(key);
        }

        return value;
    }

    private async Task<bool> IsDefaultSignatureSettingAsync(string key)
    {
        var value = string.Empty;

        if (_coreBaseSettings.Standalone)
        {
            value = await _coreSettings.GetSettingAsync(GetSettingsKey(key));
        }

        return string.IsNullOrEmpty(value);
    }
    
    private string GetDefaultSignatureSetting(string key)
    {
        return _configuration[$"files:docservice:{key}"];
    }

    private async Task SetSignatureSettingAsync(string key, string value)
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
            var def = GetDefaultSignatureSetting(key);
            if (def == value)
            {
                value = null;
            }
        }

        if (await GetSignatureSettingAsync(key) != value)
        {
            await _coreSettings.SaveSettingAsync(GetSettingsKey(key), value);
        }
    }

    private async Task<bool> GetSslVerificationSettingAsync()
    {
        if (_coreBaseSettings.Standalone)
        {
            var value = await _coreSettings.GetSettingAsync(GetSettingsKey(SslVerificationKey));

            if (bool.TryParse(value, out var result))
            {
                return result;
            }
        }

        return GetDefaultSslVerificationSetting();
    }
    
    private async Task<bool> IsDefaultSslVerificationAsync()
    {
        if (_coreBaseSettings.Standalone)
        {
            var value = await _coreSettings.GetSettingAsync(GetSettingsKey(SslVerificationKey));
            if (bool.TryParse(value, out _))
            {
                return false;
            }
        }

        return true;
    }
    
    private bool GetDefaultSslVerificationSetting()
    {
        var value = _configuration[$"files:docservice:{SslVerificationKey}"];

        return bool.TryParse(value, out var result) ? result : true;
    }

    private async Task SetSslVerificationSettingAsync(bool value)
    {
        if (!_coreBaseSettings.Standalone)
        {
            throw new NotSupportedException("Method for server edition only.");
        }

        var def = GetDefaultSslVerificationSetting();

        await _coreSettings.SaveSettingAsync(GetSettingsKey(SslVerificationKey), def == value ? null : value.ToString());
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
        
        return FilesBaseAbsolutePath + $"media/view/{id}";
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
        
        var hasQuery =  uriToBeAppended.Contains('?');

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
