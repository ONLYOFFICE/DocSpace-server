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
    /// Creates an API key that authenticates requests as the caller.
    /// Any signed-in user can call it, except a guest. A guest gets 403.
    /// If the portal limits developer tools to DocSpace admins, other callers also get 403.
    /// The call changes data. Do not repeat it. Each call creates a new key.
    /// The response has the full secret in `key`. No other call returns it, so store it now.
    /// Send it in the `Authorization` header as `Bearer sk-...`.
    /// Pass `*` or leave out `permissions` to create a key without scope restrictions.
    /// By default, the limit is 5 calls in 15 minutes per user. More calls return 429.
    /// The call returns 400 if `name` is missing, empty, or too long, or if `expiresInDays` is out of range.
    /// It also returns 400 if `permissions` is empty or has an unknown scope.
    /// </remarks>
    /// <summary>
    /// Create a user API key
    /// </summary>
    /// <path>api/2.0/keys</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "The new API key, with the full secret in the key field", typeof(ApiKeyResponseDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `name`, the name is empty or longer than 30 characters, `expiresInDays` is outside 1-365, or the permissions array is empty or contains a scope the portal does not know")]
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
    /// Returns every scope that the `permissions` array of an API key accepts.
    /// Use these values in `POST api/2.0/keys` and `PUT api/2.0/keys/{keyId}`.
    /// Those calls accept no other value.
    /// Any signed-in user can call it, except a guest. A guest gets 403.
    /// The call is read-only.
    /// The list is in alphabetical order and the same for every caller.
    /// Besides the scopes of each area, it has `*:read`, `*:write`, and `*`.
    /// The value `*` means a key without scope restrictions.
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
    /// Returns the API keys that the caller can see.
    /// A DocSpace admin gets every key of the portal. Other users get only the keys they created.
    /// Any signed-in user can call it, except a guest. A guest gets 403.
    /// The call is read-only.
    /// The list includes expired and deactivated keys, so check `expiresAt` and `isActive`.
    /// For a user, an empty list means they created no keys, not that the portal has none.
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
    /// Returns the API key that authenticated this request.
    /// So a key holder can read its scopes, expiry, and state without knowing its ID.
    /// Send the key in the `Authorization` header as `Bearer sk-...`.
    /// Any key can call it, whatever its scopes.
    /// If the request uses a cookie or a user token, the call returns 400.
    /// The call is read-only.
    /// To read other keys, use `GET api/2.0/keys`.
    /// </remarks>
    /// <summary>
    /// Get the current API key
    /// </summary>
    /// <path>api/2.0/keys/@self</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "The API key that authenticated this request", typeof(ApiKeyResponseDto))]
    [SwaggerResponse(400, "The request is not authenticated by an API key, as with a session authenticated by a cookie or by a user token")]
    [HttpGet("@self")]
    public async Task<ApiKeyResponseDto> GetApiKey()
    {
        var header = httpContextAccessor?.HttpContext?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The request is not authenticated by an API key");
        }

        var apiKey = await apiKeyManager.GetApiKeyAsync(header["Bearer ".Length..])
            ?? throw new ArgumentException("The request is not authenticated by an API key");

        return await mapper.MapManual(apiKey);
    }


    /// <remarks>
    /// Renames an API key, replaces its scopes, or turns it on or off. The secret stays the same.
    /// Requires the creator of the key or a DocSpace admin. Other callers get 403.
    /// The call changes data. You can repeat it with the same body.
    /// It changes only the fields in the body.
    /// Pass only `*` in `permissions` to remove all scope restrictions.
    /// With `isActive` set to false, the key stops authenticating requests but stays in the list.
    /// The call returns `true` after a change.
    /// For an expired key, it changes nothing and returns `false`.
    /// To remove a key for good, use `DELETE api/2.0/keys/{keyId}`.
    /// If no key has this ID, the call returns 404.
    /// The call returns 400 if the name is empty, has only spaces, or is too long.
    /// It also returns 400 if `permissions` is empty or has an unknown scope.
    /// </remarks>
    /// <summary>
    /// Update an API key
    /// </summary>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "True if the key was changed, false if it was left untouched because it has already expired", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read, the name is empty, made of whitespace only or longer than 30 characters, or the permissions array is empty or contains a scope the portal does not know")]
    [SwaggerResponse(403, "The key belongs to another member and the caller is not a DocSpace admin")]
    [SwaggerResponse(404, "The portal has no key with this ID")]
    [HttpPut("{keyId:guid}")]
    public async Task<bool> UpdateApiKey(UpdateApiKeyRequestDto requestDto)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;
        var apiKey = await apiKeyManager.GetApiKeyAsync(requestDto.KeyId) ?? throw new ItemNotFoundException("API key not found");

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
    /// Deletes an API key. The key stops authenticating requests immediately.
    /// Requires the creator of the key or a DocSpace admin. Other callers get 403.
    /// The call cannot be undone, and a repeated call returns 404.
    /// It always returns `true`.
    /// To stop a key for a time, set `isActive` to false with `PUT api/2.0/keys/{keyId}`.
    /// To get a new key, use `POST api/2.0/keys`.
    /// If no key has this ID, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Delete an API key
    /// </summary>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "True if the key was removed", typeof(bool))]
    [SwaggerResponse(403, "The key belongs to another member and the caller is not a DocSpace admin")]
    [SwaggerResponse(404, "The portal has no key with this ID")]
    [HttpDelete("{keyId:guid}")]
    public async Task<bool> DeleteApiKey(DeleteApiKeyRequestDto requestDto)
    {
        var keyId = requestDto.KeyId;
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;
        var apiKey = await apiKeyManager.GetApiKeyAsync(keyId) ?? throw new ItemNotFoundException("API key not found");

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