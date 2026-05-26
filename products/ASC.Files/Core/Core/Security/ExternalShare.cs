// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

using HttpRequestExtensions = System.Web.HttpRequestExtensions;

namespace ASC.Files.Core.Security;

[Scope]
public class ExternalShare(
    IDaoFactory daoFactory,
    CookiesManager cookiesManager,
    IHttpContextAccessor httpContextAccessor,
    BaseCommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    CoreSettings coreSettings,
    FilesSettingsHelper filesSettingsHelper)
{
    private ExternalSessionSnapshot _snapshot;
    private string _dbKey;
    private const string RoomLinkPattern = "rooms/share?key={0}";

    public async Task<LinkData> GetLinkDataAsync<T>(FileEntry<T> entry, Guid linkId)
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
                    url = filesLinkUtility.GetFileDownloadUrl(file.Id);
                }

                url = QueryHelpers.AddQueryString(url, FilesLinkUtility.ShareKey, key);
                break;
            case Folder<T> { IsRoom: true }:
                url = string.Format(RoomLinkPattern, key);
                break;
            case Folder<T> { RootFolderType: FolderType.VirtualRooms or FolderType.USER } folder:
                url = QueryHelpers.AddQueryString(string.Format(RoomLinkPattern, key), "folder", HttpUtility.UrlEncode(folder.Id.ToString()!));
                break;
        }

        return new LinkData
        {
            Url = commonLinkUtility.GetFullAbsolutePath(url),
            Token = key
        };
    }

    public async Task<Status> ValidateAsync(Guid linkId, bool isAuthenticated)
    {
        var record = await daoFactory.GetSecurityDao<string>().GetSharesAsync([linkId]).FirstOrDefaultAsync();

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

        if (!record.Options.Internal && !isAuthenticated && entry != null)
        {
            if (await IsGloballyRestrictedAsync(entry))
            {
                return Status.ExternalAccessDenied;
            }
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

    public string GetUrlWithFillingSessionId(string url, string fillingSessionId)
    {
        return !string.IsNullOrEmpty(fillingSessionId)
            ? QueryHelpers.AddQueryString(url, FilesLinkUtility.FillingSessionId, fillingSessionId)
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
        return _dbKey ??= await coreSettings.GetDocDbKeyAsync();
    }

    /// <summary>
    /// Returns <c>true</c> when an existing public link should be blocked because the admin
    /// has both disabled external sharing for the entry's section and enabled the
    /// "block existing links" option. Used only for access-validation paths.
    /// </summary>
    public async Task<bool> IsGloballyRestrictedAsync(FileEntry entry)
    {
        var settings = await filesSettingsHelper.GetTenantFilesSettingsAsync();
        return IsGlobalRestrictionApplies(entry, settings);
    }

    /// <summary>
    /// Returns <c>true</c> when creating a new public link should be prevented because the
    /// admin has disabled external sharing for the entry's section.
    /// Unlike <see cref="IsGloballyRestrictedAsync"/>, this does <b>not</b> consult
    /// <c>BlockExistingLinksOnRestrict</c> — that flag controls only existing-link access,
    /// not whether new links may be created as public.
    /// </summary>
    public async Task<bool> IsCreationRestrictedAsync(FileEntry entry)
    {
        var settings = await filesSettingsHelper.GetTenantFilesSettingsAsync();

        if (!settings.DisableShareLinkSetting)
        {
            return false;
        }

        return entry.RootFolderType switch
        {
            FolderType.USER => settings.ExternalShareApplyToDocumentsSetting,
            FolderType.VirtualRooms => settings.ExternalShareApplyToRoomsSetting,
            _ => false
        };
    }

    private static bool IsGlobalRestrictionApplies(FileEntry entry, FilesSettings settings)
    {
        if (!settings.DisableShareLinkSetting)
        {
            return false;
        }

        if (!settings.BlockExistingLinksOnRestrictSetting)
        {
            return false;
        }

        return entry.RootFolderType switch
        {
            FolderType.USER => settings.ExternalShareApplyToDocumentsSetting,
            FolderType.VirtualRooms => settings.ExternalShareApplyToRoomsSetting,
            _ => false
        };
    }
}

/// <summary>
/// The external link data.
/// </summary>
public class LinkData
{
    /// <summary>
    /// The link URL address.
    /// </summary>
    public string Url { get; init; }

    /// <summary>
    /// The link token.
    /// </summary>
    public string Token { get; init; }
}

/// <summary>
/// The validation parameters of the external data.
/// </summary>
public class ValidationInfo
{
    /// <summary>
    /// The external data status.
    /// </summary>
    public Status Status { get; set; }

    /// <summary>
    /// The external data ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The external data title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The type of the external data.
    /// </summary>
    public FileEntryType? Type { get; set; }

    /// <summary>
    /// The entity ID of the external data.
    /// </summary>
    public string EntityId { get; set; }

    /// <summary>
    /// The entry title of the external data.
    /// </summary>
    public string EntityTitle { get; set; }

    /// <summary>
    /// The entry type of the external data.
    /// </summary>
    public FileEntryType? EntityType { get; set; }

    /// <summary>
    /// Indicates whether the entity represents a room.
    /// </summary>
    public bool? IsRoom { get; set; } //TODO:rename

    /// <summary>
    /// The access rights type of the external data.
    /// </summary>
    public FileShare Access { get; set; }

    /// <summary>
    /// The tenant ID of the external data.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Specifies whether to share the external data or not.
    /// </summary>
    public bool Shared { get; set; }

    /// <summary>
    /// The link ID of the external data.
    /// </summary>
    public Guid LinkId { get; set; }

    /// <summary>
    /// Specifies whether the user is authenticated or not.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// The room ID of the external data.
    /// </summary>
    public bool IsRoomMember { get; set; }
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
        LinkId = linkId;
        SessionId = sessionId;
        PasswordKey = passwordKey;
    }

    [ProtoMember(1)]
    public Guid LinkId { get; init; }

    [ProtoMember(2)]
    public Guid SessionId { get; init; }

    [ProtoMember(3)]
    public string PasswordKey { get; init; }
}

/// <summary>
/// The external data status.
/// </summary>
public enum Status
{
    [Description("Ok")]
    Ok,

    [Description("Invalid")]
    Invalid,

    [Description("Expired")]
    Expired,

    [Description("Required password")]
    RequiredPassword,

    [Description("Invalid password")]
    InvalidPassword,

    [Description("External access denied")]
    ExternalAccessDenied
}

public record TokenData
{
    public Guid Id { get; set; }
    public string Password { get; set; }
}
