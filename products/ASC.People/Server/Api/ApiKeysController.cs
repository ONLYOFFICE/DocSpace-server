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

namespace ASC.People.Api;

/// <remarks>
/// API keys API.
/// </remarks>
[Scope]
[ApiEndpoint("keys")]
public class ApiKeysController(
    ApiKeyManager apiKeyManager,
    AuthContext authContext,
    UserManager userManager,
    MessageService messageService,
    SettingsManager settingsManager,
    ApiKeyMapper mapper,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    /// <remarks>
    /// Creates an API key that authenticates requests as the calling account, and is the only operation that ever
    /// returns the secret.
    /// Any portal member except a guest may create one; when the portal limits developer tools to administrators,
    /// only a DocSpace administrator may call it.
    /// The call is not idempotent - every call issues a new key - and it is throttled, so a client that retries on a
    /// timeout can end up with several keys.
    /// The answer carries the full secret in `key`: it is shown here and never again, later reads expose only the
    /// last four characters in `keyPostfix`, so store it now.
    /// Pass the scopes the key may use in `permissions`, taking the values from
    /// `GET api/2.0/keys/permissions`; pass `*` or omit the field to record a key without scope restrictions, and set
    /// `expiresInDays` to make it expire, otherwise it stays valid until it is deleted.
    /// An empty `permissions` array and an unknown scope are both rejected with 400.
    /// Send the key in the `Authorization` header as `Bearer sk-...` to use it.
    /// </remarks>
    /// <summary>
    /// Create a user API key
    /// </summary>
    /// <path>api/2.0/keys</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "The new API key, with the full secret in the key field", typeof(ApiKeyResponseDto))]
    [SwaggerResponse(400, "The permissions array is empty or contains a scope the portal does not know")]
    [SwaggerResponse(403, "The caller is a guest, or the portal limits developer tools to administrators")]
    [HttpPost]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<ApiKeyResponseDto> CreateApiKey(CreateApiKeyRequestDto apiKey)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;

        var tenantDevToolsAccessSettings = await settingsManager.LoadAsync<TenantDevToolsAccessSettings>();

        if (!isAdmin && tenantDevToolsAccessSettings is { LimitedAccessForUsers: true })
        {
            throw new UnauthorizedAccessException("This operation available only for portal owner/admins");
        }

        if (currentType == EmployeeType.Guest)
        {
            throw new CustomHttpException(HttpStatusCode.Forbidden, "This operation unavailable for user with guest role");
        }

        var expiresAt = apiKey.ExpiresInDays.HasValue ? TimeSpan.FromDays(apiKey.ExpiresInDays.Value) : (TimeSpan?)null;

        if (!IsValidPermission(apiKey.Permissions))
        {
            throw new ArgumentException("Permissions are not valid.");
        }

        var result = await apiKeyManager.CreateApiKeyAsync(apiKey.Name,
            apiKey.Permissions,
            expiresAt);

        var apiKeyResponseDto = await mapper.MapManual(result.keyData);

        messageService.Send(MessageAction.ApiKeyCreated, MessageTarget.Create(apiKeyResponseDto.Id), apiKeyResponseDto.Key);

        apiKeyResponseDto.Key = result.apiKey;

        return apiKeyResponseDto;
    }

    /// <remarks>
    /// Returns every scope value the portal accepts in the `permissions` array of an API key.
    /// Read it before `POST api/2.0/keys` or `PUT api/2.0/keys/{keyId}`, because any other value is rejected with
    /// 400.
    /// Any portal member except a guest may call it, and the call is read-only.
    /// The answer is a flat list sorted alphabetically, holding the per-area scopes such as `accounts:read`,
    /// `files:write` and `rooms:write`, the portal-wide `*:read` and `*:write`, and `*` which stands for a key
    /// without scope restrictions.
    /// The list is fixed for the portal and identical for every caller, so it can be cached by the client.
    /// </remarks>
    /// <summary>
    /// Get API key permissions
    /// </summary>
    /// <path>api/2.0/keys/permissions</path>
    /// <collection>list</collection>
    [Tags("Api keys")]
    [SwaggerResponse(200, "The scope values accepted in the permissions array of an API key", typeof(IEnumerable<string>))]
    [SwaggerResponse(403, "The caller is a guest")]
    [HttpGet("permissions")]
    public async Task<IEnumerable<string>> GetAllPermissions()
    {
        await DemandNotGuestAsync();

        return GetPermissions();
    }

    private static IEnumerable<string> GetPermissions()
    {
        var scopes = AuthorizationExtension.ScopesMap;

        var globalScopes = new List<string>
        {
            AuthConstants.Claim_ScopeGlobalRead.Value,
            AuthConstants.Claim_ScopeGlobalWrite.Value
        };

        return scopes.Keys.SelectMany(key => scopes[key]).Union(globalScopes).Distinct().Order();
    }


    /// <remarks>
    /// Returns the API keys the caller is allowed to see, which is not the same set for everybody: a DocSpace
    /// administrator gets every key of the portal, while any other member gets only the keys they created
    /// themselves.
    /// Any portal member except a guest may call it, and the call is read-only.
    /// The secrets are not returned - each entry identifies its key by `id` and by the last four characters in
    /// `keyPostfix`, and a secret can only be read once, at the moment `POST api/2.0/keys` creates it.
    /// Expired and deactivated keys stay in the list, so check `expiresAt` against the current time and read
    /// `isActive` before treating an entry as usable.
    /// An empty list means the caller has created no keys, not that the portal has none.
    /// </remarks>
    /// <summary>
    /// Get the API keys
    /// </summary>
    /// <path>api/2.0/keys</path>
    /// <collection>list</collection>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Every key of the portal for a DocSpace admin, or the keys created by the caller for anybody else", typeof(IAsyncEnumerable<ApiKeyResponseDto>))]
    [SwaggerResponse(403, "The caller is a guest")]
    [HttpGet]
    public async IAsyncEnumerable<ApiKeyResponseDto> GetApiKeys()
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        if (currentType == EmployeeType.Guest)
        {
            throw new SecurityException("Access denied");
        }

        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;

        IAsyncEnumerable<ApiKey> result;

        if (isAdmin)
        {
            result = apiKeyManager.GetAllApiKeysAsync();
        }
        else
        {

            result = apiKeyManager.GetApiKeysAsync(authContext.CurrentAccount.ID);
        }

        await foreach (var apiKey in result)
        {
            yield return await mapper.MapManual(apiKey);
        }
    }


    /// <remarks>
    /// Returns the API key that authenticated this very request, letting the holder of a key find out what it is
    /// allowed to do without knowing its ID.
    /// The key is identified by the `Authorization` header of the call itself, so the request has to be sent as
    /// `Bearer sk-...`; a session authenticated in any other way has no key to report and this operation is not
    /// usable for it.
    /// The call is read-only and returns one entry, with the same fields as `GET api/2.0/keys` and without the
    /// secret - read `permissions` for the granted scopes, `expiresAt` for the expiry and `isActive` for the state.
    /// To look at a key other than the one in use, call `GET api/2.0/keys` instead.
    /// </remarks>
    /// <summary>
    /// Get the current API key
    /// </summary>
    /// <path>api/2.0/keys/@self</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "The API key that authenticated this request", typeof(ApiKeyResponseDto))]
    [HttpGet("@self")]
    public async Task<ApiKeyResponseDto> GetApiKey()
    {
        var token = httpContextAccessor?.HttpContext?.Request.Headers.Authorization.ToString()["Bearer ".Length..];

        var apiKey = await apiKeyManager.GetApiKeyAsync(token);
        return await mapper.MapManual(apiKey);
    }


    /// <remarks>
    /// Renames an API key, replaces the scopes it may use, or activates and deactivates it, without changing the
    /// secret.
    /// The caller may update a key they created themselves, and a DocSpace administrator may update any key of the
    /// portal.
    /// Take the values for `permissions` from `GET api/2.0/keys/permissions`; an unknown scope or an empty array is
    /// rejected with 400, and the fields that are left out keep their current values.
    /// The answer is a plain boolean: true when the key was changed, and false when it was not - which is also what
    /// an already expired key returns, because such a key is left untouched instead of being reported as an error.
    /// Deactivating a key through `isActive` stops it from authenticating while keeping it in the list, so use it
    /// when the key may be needed again and `DELETE api/2.0/keys/{keyId}` when it may not.
    /// </remarks>
    /// <summary>
    /// Update an API key
    /// </summary>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "True if the key was changed, false if it was left untouched because it has already expired", typeof(bool))]
    [SwaggerResponse(400, "The permissions array is empty or contains a scope the portal does not know")]
    [SwaggerResponse(403, "The key belongs to another member and the caller is not a DocSpace admin")]
    [HttpPut("{keyId:guid}")]
    public async Task<bool> UpdateApiKey(UpdateApiKeyRequestDto requestDto)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;
        var apiKey = await apiKeyManager.GetApiKeyAsync(requestDto.KeyId);

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            return false;
        }

        if (!isAdmin && apiKey.CreateBy != authContext.CurrentAccount.ID)
        {
            throw new SecurityException("Access denied");
        }

        if (!IsValidPermission(requestDto.Changed.Permissions))
        {
            throw new ArgumentException("Permissions are not valid.");
        }

        var result = await apiKeyManager.UpdateApiKeyAsync(
            requestDto.KeyId,
            requestDto.Changed.Permissions,
            requestDto.Changed.Name,
            requestDto.Changed.IsActive);

        if (result)
        {
            messageService.Send(MessageAction.ApiKeyUpdated, MessageTarget.Create(apiKey.Id), apiKey.Key);
        }

        return result;
    }

    /// <remarks>
    /// Deletes the API key with the ID given in the route, so that it stops authenticating requests immediately.
    /// The caller may delete a key they created themselves, and a DocSpace administrator may delete any key of the
    /// portal.
    /// The removal is permanent and cannot be undone: the secret was only ever readable at creation time, so a
    /// deleted key cannot be restored and a new one has to be issued through `POST api/2.0/keys`.
    /// To stop a key temporarily instead, set `isActive` to false through `PUT api/2.0/keys/{keyId}`.
    /// The answer is a plain boolean reporting whether the key was removed.
    /// </remarks>
    /// <summary>
    /// Delete an API key
    /// </summary>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "True if the key was removed", typeof(bool))]
    [SwaggerResponse(403, "The key belongs to another member and the caller is not a DocSpace admin")]
    [HttpDelete("{keyId:guid}")]
    public async Task<bool> DeleteApiKey(DeleteApiKeyRequestDto requestDto)
    {
        var keyId = requestDto.KeyId;
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;
        var apiKey = await apiKeyManager.GetApiKeyAsync(keyId);

        if (!isAdmin && apiKey.CreateBy != authContext.CurrentAccount.ID)
        {
            throw new SecurityException("Access denied");
        }

        var result = await apiKeyManager.DeleteApiKeyAsync(keyId);

        messageService.Send(MessageAction.ApiKeyDeleted, MessageTarget.Create(apiKey.Id), apiKey.Key);

        return result;
    }

    private static bool IsValidPermission(List<string> permission)
    {
        if (permission == null)
        {
            return true;
        }

        if (permission.Count == 0)
        {
            return false;
        }

        var orderedScopes = GetPermissions().Union(new List<string> { "*" });

        return permission.All(x => orderedScopes.Contains(x));
    }

    private async Task DemandNotGuestAsync()
    {
        if (await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID) == EmployeeType.Guest)
        {
            throw new SecurityException("Access denied");
        }
    }
}