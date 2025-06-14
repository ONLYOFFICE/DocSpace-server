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

using ASC.Api.Core.Auth;
using ASC.Core.Common.EF.Model;

using AutoMapper;


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
    IHttpContextAccessor httpContextAccessor,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    ///  Creates a user API key with the parameters specified in the request.
    /// </summary>  
    /// <short>
    ///  Create a user API key
    /// </short>
    /// <path>api/2.0/keys</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Create a user api key", typeof(ApiKeyResponseDto))]
    [HttpPost]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<ApiKeyResponseDto> CreateApiKey(CreateApiKeyRequestDto apiKey)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;

        var tenantDevToolsAccessSettings  = await settingsManager.LoadAsync<TenantDevToolsAccessSettings>();
           
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
        
        var apiKeyResponseDto = mapper.Map<ApiKeyResponseDto>(result.keyData);

        messageService.Send(MessageAction.ApiKeyCreated, MessageTarget.Create(apiKeyResponseDto.Id), apiKeyResponseDto.Key);

        apiKeyResponseDto.Key = result.apiKey;

        return apiKeyResponseDto;
    }

    /// <summary>
    ///  Returns a list of all available permissions for the API key.
    /// </summary>  
    /// <short>
    /// Get API key permissions
    /// </short>
    /// <path>api/2.0/keys/permissions</path>
    /// <collection>list</collection>
    [Tags("Api keys")]
    [SwaggerResponse(200, "List of all available permissions for key", typeof(IEnumerable<string>))]
    [HttpGet("permissions")]
    public IEnumerable<string> GetAllPermissions()
    {
        var scopes = AuthorizationExtension.ScopesMap;

        return scopes.AllKeys.SelectMany(key => scopes[key]?.Split(',')).Distinct().Order();
    }


    /// <summary>
    ///  Returns a list of all API keys for the current user.
    /// </summary>  
    /// <short>
    ///  Get user API keys
    /// </short>
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
            yield return mapper.Map<ApiKeyResponseDto>(apiKey);
        }
    }

    
    /// <summary>
    ///  Returns current user API key info.
    /// </summary>  
    /// <short>
    ///  Get user API key info
    /// </short>
    /// <path>api/2.0/keys/@self</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "List of api keys for user", typeof(ApiKeyResponseDto))]
    [HttpGet("@self")]
    [Authorize(AuthenticationSchemes = ApiKeyBearerDefaults.AuthenticationScheme)]
    public async Task<ApiKeyResponseDto> GetApiKey()
    {
        var token = httpContextAccessor?.HttpContext?.Request.Headers.Authorization.ToString()["Bearer ".Length..];

        var apiKey = await apiKeyManager.GetApiKeyAsync(token);
        
        return mapper.Map<ApiKeyResponseDto>(apiKey);
    }
    
    
    /// <summary>
    ///  Updates an existing API key changing its name, permissions and status.
    /// </summary>  
    /// <short>
    ///  Update an API key
    /// </short>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Update optional params for user api keys", typeof(bool))]
    [HttpPut("{keyId:guid}")]
    public async Task<bool> UpdateApiKey(UpdateApiKeyRequestDto requestDto)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;
        var apiKey = await apiKeyManager.GetApiKeyAsync(requestDto.KeyId);

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

    /// <summary>
    ///  Delete a user API key by its ID.
    /// </summary>  
    /// <short>
    ///  Delete a user API key
    /// </short>
    /// <param name="keyId">The API key ID.</param>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Delete a user api key", typeof(bool))]
    [HttpDelete("{keyId}")]
    public async Task<bool> DeleteApiKey(Guid keyId)
    {
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
        if (permission == null || !permission.Any())
        {
            return true;
        }

        var scopes = AuthorizationExtension.ScopesMap;
        var orderedScopes = scopes.AllKeys.SelectMany(key => scopes[key]?.Split(','))
                                                                 .Concat(["*"])
                                                                 .Distinct()
                                                                 .Order();

        return permission.All(x => orderedScopes.Contains(x));
    }
}