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

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("keys")]
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
    ///  Creates a user API key with the parameters specified in the request.
    /// </remarks>  
    /// <summary>
    ///  Create a user API key
    /// </summary>
    /// <path>api/2.0/keys</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Create a user api key", typeof(ApiKeyResponseDto))]
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
            throw new UnauthorizedAccessException("This operation unavailable for user with guest role");
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
    ///  Returns a list of all available permissions for the API key.
    /// </remarks>  
    /// <summary>
    /// Get API key permissions
    /// </summary>
    /// <path>api/2.0/keys/permissions</path>
    /// <collection>list</collection>
    [Tags("Api keys")]
    [SwaggerResponse(200, "List of all available permissions for key", typeof(IEnumerable<string>))]
    [HttpGet("permissions")]
    public IEnumerable<string> GetAllPermissions()
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
    ///  Returns a list of all API keys for the current user.
    /// </remarks>  
    /// <summary>
    ///  Get current user's API keys
    /// </summary>
    /// <path>api/2.0/keys</path>
    /// <collection>list</collection>
    [Tags("Api keys")]
    [SwaggerResponse(200, "List of api keys for user", typeof(IAsyncEnumerable<ApiKeyResponseDto>))]
    [HttpGet]
    public async IAsyncEnumerable<ApiKeyResponseDto> GetApiKeys()
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
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
    /// Returns information about the current user's API key.
    /// </remarks>  
    /// <summary>
    ///  Get current user's API key
    /// </summary>
    /// <path>api/2.0/keys/@self</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "List of api keys for user", typeof(ApiKeyResponseDto))]
    [HttpGet("@self")]
    public async Task<ApiKeyResponseDto> GetApiKey()
    {
        var token = httpContextAccessor?.HttpContext?.Request.Headers.Authorization.ToString()["Bearer ".Length..];

        var apiKey = await apiKeyManager.GetApiKeyAsync(token);
        return await mapper.MapManual(apiKey);
    }


    /// <remarks>
    ///  Updates an existing API key changing its name, permissions, and status.
    /// </remarks>  
    /// <summary>
    ///  Update an API key
    /// </summary>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Update optional params for user api keys", typeof(bool))]
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

        if (!isAdmin)
        {

            if (apiKey.CreateBy != authContext.CurrentAccount.ID)
            {
                return false;
            }
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
    ///  Deletes a user API key by its ID.
    /// </remarks>  
    /// <summary>
    ///  Delete a user API key
    /// </summary>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Delete a user api key", typeof(bool))]
    [HttpDelete("{keyId:guid}")]
    public async Task<bool> DeleteApiKey(DeleteApiKeyRequestDto requestDto)
    {
        var keyId = requestDto.KeyId;
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;
        var apiKey = await apiKeyManager.GetApiKeyAsync(keyId);

        if (!isAdmin)
        {

            if (apiKey.CreateBy != authContext.CurrentAccount.ID)
            {
                return false;
            }
        }

        var result = await apiKeyManager.DeleteApiKeyAsync(keyId);

        messageService.Send(MessageAction.ApiKeyDeleted, MessageTarget.Create(apiKey.Id), apiKey.Key);

        return result;
    }

    private bool IsValidPermission(List<string> permission)
    {
        if (permission == null || permission.Count == 0)
        {
            return true;
        }

        var orderedScopes = GetAllPermissions().Union(new List<string> { "*" });

        return permission.All(x => orderedScopes.Contains(x));
    }
}