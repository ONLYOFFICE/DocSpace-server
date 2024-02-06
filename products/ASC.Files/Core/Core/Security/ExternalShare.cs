// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Core.Security;

[Scope]
public class ExternalShare
{
    private readonly Global _global;
    private readonly IDaoFactory _daoFactory;
    private readonly CookiesManager _cookiesManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly FilesLinkUtility _filesLinkUtility;
    private readonly FileUtility _fileUtility;
    private Guid _linkId;
    private Guid _sessionId;
    private string _passwordKey;
    private string _dbKey;

    public ExternalShare(
        Global global, 
        IDaoFactory daoFactory, 
        CookiesManager cookiesManager,
        IHttpContextAccessor httpContextAccessor,
        CommonLinkUtility commonLinkUtility, 
        FilesLinkUtility filesLinkUtility, 
        FileUtility fileUtility)
    {
        _global = global;
        _daoFactory = daoFactory;
        _cookiesManager = cookiesManager;
        _httpContextAccessor = httpContextAccessor;
        _commonLinkUtility = commonLinkUtility;
        _filesLinkUtility = filesLinkUtility;
        _fileUtility = fileUtility;
    }
    
    public async Task<LinkData> GetLinkDataAsync<T>(FileEntry<T> entry, Guid linkId)
    {
        var key = await CreateShareKeyAsync(linkId);
        string url = null;
        
        switch (entry)
        {
            case File<T> file:
                url = _fileUtility.CanWebView(file.Title)
                    ? _filesLinkUtility.GetFileWebPreviewUrl(_fileUtility, file.Title, file.Id)
                    : file.DownloadUrl;

                url += $"&{FilesLinkUtility.ShareKey}={key}";
                break;
            case Folder<T> folder when DocSpaceHelper.IsRoom(folder.FolderType):
                url = $"rooms/share?key={key}";
                break;
        }
        
        return new LinkData
        {
            Url = _commonLinkUtility.GetFullAbsolutePath(url),
            Token = key
        };
    }
    
    public async Task<Status> ValidateAsync(Guid linkId)
    {
        var record = await _daoFactory.GetSecurityDao<int>().GetSharesAsync(new [] { linkId }).FirstOrDefaultAsync();

        return record == null ? Status.Invalid : await ValidateRecordAsync(record, null);
    }
    
    public async Task<Status> ValidateRecordAsync(FileShareRecord record, string password)
    {
        if (record.SubjectType is not (SubjectType.ExternalLink or SubjectType.PrimaryExternalLink) ||
            record.Subject == FileConstant.ShareLinkId ||
            record.Options == null)
        {
            return Status.Ok;
        }
        
        if (record.Options.IsExpired)
        {
            return Status.Expired;
        }

        if (string.IsNullOrEmpty(record.Options.Password))
        {
            return Status.Ok;
        }
        
        if (string.IsNullOrEmpty(_passwordKey))
        {
            _passwordKey = _cookiesManager.GetCookies(CookiesType.ShareLink, record.Subject.ToString(), true);
        }

        if (_passwordKey == record.Options.Password)
        {
            return Status.Ok;
        }

        if (string.IsNullOrEmpty(password))
        {
            return Status.RequiredPassword;
        }

        if (await CreatePasswordKeyAsync(password) == record.Options.Password)
        {
            await _cookiesManager.SetCookiesAsync(CookiesType.ShareLink, record.Options.Password, true, record.Subject.ToString());
            return Status.Ok;
        }

        _cookiesManager.ClearCookies(CookiesType.ShareLink, record.Subject.ToString());
        
        return Status.InvalidPassword;
    }
    
    public async Task<string> CreatePasswordKeyAsync(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return Signature.Create(password, await GetDbKeyAsync());
    }

    public async Task<string> GetPasswordAsync(string passwordKey)
    {
        return string.IsNullOrEmpty(passwordKey) ? null : Signature.Read<string>(passwordKey, await GetDbKeyAsync());
    }

    public string GetKey()
    {
        var key = _httpContextAccessor.HttpContext?.Request.Headers[HttpRequestExtensions.RequestTokenHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(key))
        {
            key = _httpContextAccessor.HttpContext?.Request.Query.GetRequestValue(FilesLinkUtility.ShareKey);
        }

        return string.IsNullOrEmpty(key) ? null : key;
    }
    
    public async Task<Guid> ParseShareKeyAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        
        return Signature.Read<Guid>(key, await GetDbKeyAsync());
    }

    public Guid GetLinkId()
    {
        return GetLinkIdAsync().Result;
    }

    public async Task<Guid> GetLinkIdAsync()
    {
        if (_linkId != Guid.Empty)
        {
            return _linkId;
        }
        
        var key = GetKey();
        
        if (string.IsNullOrEmpty(key))
        {
            return Guid.Empty;
        }
        
        var linkId = await ParseShareKeyAsync(key);
        
        if (linkId == Guid.Empty)
        {
            return Guid.Empty;
        }
        
        _linkId = linkId;

        return linkId;
    }

    public Guid GetSessionId()
    {
        return GetSessionIdAsync().Result;
    }

    public async Task<Guid> GetSessionIdAsync()
    {
        if (_sessionId != Guid.Empty)
        {
            return _sessionId;
        }
        
        var sessionKey = _cookiesManager.GetCookies(CookiesType.AnonymousSessionKey);
        
        if (string.IsNullOrEmpty(sessionKey))
        {
            return Guid.Empty;
        }
        
        var id = Signature.Read<Guid>(sessionKey, await GetDbKeyAsync());
        
        if (id == Guid.Empty)
        {
            return Guid.Empty;
        }

        _sessionId = id;

        return id;
    }
    
    public async Task<ExternalShareData> GetCurrentShareDataAsync()
    {
        var linkId = await GetLinkIdAsync();
        var sessionId = await GetSessionIdAsync();
        var password = string.IsNullOrEmpty(_passwordKey) ? _cookiesManager.GetCookies(CookiesType.ShareLink, _linkId.ToString(), true) : _passwordKey;

        return new ExternalShareData(linkId, sessionId, password);
    }

    public void SetCurrentShareData(ExternalShareData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        if (_linkId == Guid.Empty)
        {
            _linkId = data.LinkId;
        }

        if (_sessionId == Guid.Empty)
        {
            _sessionId = data.SessionId;
        }

        if (string.IsNullOrEmpty(_passwordKey))
        {
            _passwordKey = data.PasswordKey;
        }
    }

    public async Task<string> CreateDownloadSessionKeyAsync()
    {
        var linkId = await GetLinkIdAsync();
        var sessionId = await GetSessionIdAsync();
        
        var session = new DownloadSession
        {
            Id = sessionId,
            LinkId = linkId
        };

        return Signature.Create(session, await GetDbKeyAsync());
    }

    public async Task<DownloadSession> ParseDownloadSessionKeyAsync(string sessionKey)
    {
        return Signature.Read<DownloadSession>(sessionKey, await GetDbKeyAsync());
    }
    
    public string GetAnonymousSessionKey()
    {
        return _cookiesManager.GetCookies(CookiesType.AnonymousSessionKey);
    }

    public async Task SetAnonymousSessionKeyAsync()
    {
        await _cookiesManager.SetCookiesAsync(CookiesType.AnonymousSessionKey, Signature.Create(Guid.NewGuid(), await GetDbKeyAsync()), true);
    }
    
    public string GetUrlWithShare(string url, string key = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        key ??= GetKey();

        return !string.IsNullOrEmpty(key)
            ? QueryHelpers.AddQueryString(url, FilesLinkUtility.ShareKey, key)
            : url;
    }
    
    private async Task<string> CreateShareKeyAsync(Guid linkId)
    {
        return Signature.Create(linkId, await GetDbKeyAsync());
    }

    private async Task<string> GetDbKeyAsync()
    {
        return _dbKey ??= await _global.GetDocDbKeyAsync();
    }
}

public class LinkData
{
    public string Url { get; init; }
    public string Token { get; init; }
}

/// <summary>
/// </summary>
public class ValidationInfo
{
    /// <summary>External data status</summary>
    /// <type>ASC.Files.Core.Security.Status, ASC.Files.Core</type>
    public Status Status { get; set; }
   
    /// <summary>External data ID</summary>
    /// <type>System.String, System</type>
    public string Id { get; set; }
   
    /// <summary>External data title</summary>
    /// <type>System.String, System</type>
    public string Title { get; set; }

    /// <summary>Sharing rights</summary>
    /// <type>ASC.Files.Core.Security.FileShare, ASC.Files.Core</type>
    public FileShare Access { get; set; }

    /// <summary>Type of a folder where the external data is located</summary>
    /// <type>ASC.Files.Core.FolderType, ASC.Files.Core</type>
    public FolderType FolderType { get; set; }

    /// <summary>Room logo</summary>
    /// <type>ASC.Files.Core.VirtualRooms.Logo, ASC.Files.Core</type>
    public Logo Logo { get; set; }

    /// <summary>Tenant ID</summary>
    /// <type>System.Int32, System</type>
    public int TenantId { get; set; }

    /// <summary>Specifies whether to share the external data or not</summary>
    /// <type>System.Boolean, System</type>
    public bool Shared { get; set; }
    
    /// <summary>Link ID</summary>
    /// <type>System.Guid, System</type>
    public Guid LinkId { get; set; }
}

public class ExternalShareData
{
    public Guid LinkId { get; }
    public Guid SessionId { get; }
    public string PasswordKey { get; }
    
    public ExternalShareData(Guid linkId, Guid sessionId, string passwordKey)
    {
        LinkId = linkId;
        SessionId = sessionId;
        PasswordKey = passwordKey;
    }
}

public class DownloadSession
{
    public Guid Id { get; init; }
    public Guid LinkId { get; init; }
}

/// <summary>
/// </summary>
public enum Status
{
    Ok,
    Invalid,
    Expired,
    RequiredPassword,
    InvalidPassword
}