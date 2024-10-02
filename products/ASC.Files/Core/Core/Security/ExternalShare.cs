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

namespace ASC.Files.Core.Security;

[Scope]
public class ExternalShare(Global global, 
    IDaoFactory daoFactory, 
    CookiesManager cookiesManager,
    IHttpContextAccessor httpContextAccessor,
    BaseCommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility)
{
    private ExternalSessionSnapshot _snapshot;
    private string _dbKey;
    private const string RoomLinkPattern = "rooms/share?key={0}";

    public async Task<LinkData> GetLinkDataAsync<T>(FileEntry<T> entry, Guid linkId, bool isFile = false)
    {
        var key = await CreateShareKeyAsync(linkId);
        string url = null;
        
        switch (entry)
        {
            case File<T> file:
                if (fileUtility.CanWebView(file.Title))
                {
                    url = filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id);
                }
                else if (fileUtility.CanImageView(file.Title) || fileUtility.CanMediaView(file.Title))
                {
                    url = $"share/preview/{file.Id}";
                }
                else
                {
                    url = file.DownloadUrl;
                }

                url = QueryHelpers.AddQueryString(url, FilesLinkUtility.ShareKey, key);
                break;
            case Folder<T> folder when DocSpaceHelper.IsRoom(folder.FolderType):
                url = string.Format(RoomLinkPattern, key);
                break;
            case Folder<T> { RootFolderType: FolderType.VirtualRooms } folder:
                url = QueryHelpers.AddQueryString(string.Format(RoomLinkPattern, key), "folder", HttpUtility.UrlEncode(folder.Id.ToString()!));
                break;
        }

        if (isFile)
        {
            url = QueryHelpers.AddQueryString(url, FilesLinkUtility.IsFile, "true");
        }

        return new LinkData
        {
            Url = commonLinkUtility.GetFullAbsolutePath(url),
            Token = key
        };
    }
    
    public async Task<Status> ValidateAsync(Guid linkId, bool isAuthenticated)
    {
        var record = await daoFactory.GetSecurityDao<string>().GetSharesAsync(new [] { linkId }).FirstOrDefaultAsync();

        return record == null ? Status.Invalid : await ValidateRecordAsync(record, null, isAuthenticated);
    }
    
    public async Task<Status> ValidateRecordAsync<T>(FileShareRecord<T> record, string password, bool isAuthenticated, FileEntry entry = null)
    {
        if (record.SubjectType is not (SubjectType.ExternalLink or SubjectType.PrimaryExternalLink) ||
            record.Options == null)
        {
            return Status.Ok;
        }
        
        if (record.Options.IsExpired)
        {
            return Status.Expired;
        }

        if (record.Options.Internal && !isAuthenticated)
        {
            return Status.ExternalAccessDenied;
        }

        if (entry is { RootFolderType: FolderType.Archive or FolderType.TRASH })
        {
            return Status.Invalid;
        }

        if (string.IsNullOrEmpty(record.Options.Password))
        {
            return Status.Ok;
        }

        var passwordKey = _snapshot?.PasswordKey;
        if (string.IsNullOrEmpty(passwordKey))
        {
            passwordKey = cookiesManager.GetCookies(CookiesType.ShareLink, record.Subject.ToString(), true);
            if (string.IsNullOrEmpty(passwordKey))
            {
                var key = GetKey();
                if (!string.IsNullOrEmpty(key))
                {
                    var data = await ParseShareKeyAsync(key);
                    passwordKey = data.Password;
                }
            }
        }
        
        if (passwordKey == record.Options.Password)
        {
            return Status.Ok;
        }

        if (string.IsNullOrEmpty(password))
        {
            return Status.RequiredPassword;
        }

        if (await CreatePasswordKeyAsync(password) == record.Options.Password)
        {
            await cookiesManager.SetCookiesAsync(CookiesType.ShareLink, record.Options.Password, true, record.Subject.ToString());
            return Status.Ok;
        }

        cookiesManager.ClearCookies(CookiesType.ShareLink, record.Subject.ToString());
        
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
        var key = httpContextAccessor.HttpContext?.Request.Headers[HttpRequestExtensions.RequestTokenHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(key))
        {
            key = httpContextAccessor.HttpContext?.Request.Query.GetRequestValue(FilesLinkUtility.ShareKey);
        }

        return string.IsNullOrEmpty(key) ? null : key;
    }
    
    public async Task<TokenData> ParseShareKeyAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return new TokenData
            {
                Id = Guid.Empty
            };
        }

        var stringKey = Signature.Read<string>(key, await GetDbKeyAsync());

        if (string.IsNullOrEmpty(stringKey))
        {
            return new TokenData
            {
                Id = Guid.Empty
            };
        }

        if (!stringKey.StartsWith('{') || !stringKey.EndsWith('}'))
        {
            return new TokenData
            {
                Id = Guid.TryParse(stringKey, out var id) ? id : Guid.Empty
            };
        }

        var token = JsonSerializer.Deserialize<TokenData>(stringKey);
        return token;
    }

    public async Task<Guid> GetLinkIdAsync()
    {
        if (_snapshot != null && _snapshot.LinkId != Guid.Empty)
        {
            return _snapshot.LinkId;
        }
        
        var key = GetKey();
        if (string.IsNullOrEmpty(key))
        {
            return Guid.Empty;
        }
        
        var data = await ParseShareKeyAsync(key);
        return data?.Id ?? Guid.Empty;
    }

    public Guid GetSessionId()
    {
        return GetSessionIdAsync().Result;
    }
    
    public async Task<Guid> GetSessionIdAsync()
    {
        if (_snapshot != null && _snapshot.SessionId != Guid.Empty)
        {
            return _snapshot.SessionId;
        }

        if (CustomSynchronizationContext.CurrentContext?.CurrentPrincipal?.Identity is AnonymousSession anonymous)
        {
            return anonymous.SessionId;
        }
        
        var sessionKey = cookiesManager.GetCookies(CookiesType.AnonymousSessionKey);
        if (string.IsNullOrEmpty(sessionKey))
        {
            return Guid.Empty;
        }
        
        var id = Signature.Read<Guid>(sessionKey, await GetDbKeyAsync());
        return id == Guid.Empty ? Guid.Empty : id;
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
        return cookiesManager.GetCookies(CookiesType.AnonymousSessionKey);
    }

    public async Task SetAnonymousSessionKeyAsync()
    {
        await cookiesManager.SetCookiesAsync(CookiesType.AnonymousSessionKey, Signature.Create(Guid.NewGuid(), await GetDbKeyAsync()), true);
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
    
    public async Task<ExternalSessionSnapshot> TakeSessionSnapshotAsync()
    {
        var linkId = await GetLinkIdAsync();
        var sessionId = await GetSessionIdAsync();
        var passwordKey = cookiesManager.GetCookies(CookiesType.ShareLink, linkId.ToString(), true);

        return new ExternalSessionSnapshot(linkId, sessionId, passwordKey);
    }
    
    public void Initialize(ExternalSessionSnapshot snapshot)
    {
        _snapshot = snapshot;
    }
    
    public async Task<string> CreateShareKeyAsync(Guid linkId, string password = null)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Signature.Create(linkId, await GetDbKeyAsync());
        }

        var data = new TokenData
        {
            Id = linkId, 
            Password = password
        };
        
        return Signature.Create(JsonSerializer.Serialize(data), await GetDbKeyAsync());
    }

    private async Task<string> GetDbKeyAsync()
    {
        return _dbKey ??= await global.GetDocDbKeyAsync();
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

    /// <summary>Entity ID</summary>
    /// <type>System.String, System</type>
    public string EntityId { get; set; }
   
    /// <summary>Entity title</summary>
    /// <type>System.String, System</type>
    public string EntryTitle { get; set; }
    
    /// <summary>Sharing rights</summary>
    /// <type>ASC.Files.Core.Security.FileShare, ASC.Files.Core</type>
    public FileShare Access { get; set; }
    
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

public record DownloadSession
{
    public Guid Id { get; init; }
    public Guid LinkId { get; init; }
}

[ProtoContract]
public class ExternalSessionSnapshot
{
    public ExternalSessionSnapshot()
    {
        
    }
    
    public ExternalSessionSnapshot(Guid linkId, Guid sessionId, string passwordKey)
    {
        this.LinkId = linkId;
        this.SessionId = sessionId;
        this.PasswordKey = passwordKey;
    }

    [ProtoMember(1)]
    public Guid LinkId { get; init; }
    
    [ProtoMember(2)]
    public Guid SessionId { get; init; }
    
    [ProtoMember(3)]
    public string PasswordKey { get; init; }
}

public enum Status
{
    Ok,
    Invalid,
    Expired,
    RequiredPassword,
    InvalidPassword,
    ExternalAccessDenied
}

public record TokenData
{
    public Guid Id { get; set; }
    public string Password { get; set; }
}