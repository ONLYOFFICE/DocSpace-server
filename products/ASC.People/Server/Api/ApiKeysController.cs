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
    IMapper mapper) : ControllerBase
{
    /// <summary>
    ///  Create a user api key
    /// </summary>  
    /// <short>
    ///  Create a user api key
    /// </short>
    /// <param name="apiKey">User api key params</param>
    /// <path>api/2.0/keys</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Create a user api key", typeof(ApiKeyResponseDto))]
    [HttpPost]
    public async Task<ApiKeyResponseDto> CreateApiKey(CreateApiKeyRequestDto apiKey)
    {
        var expiresAt = apiKey.ExpiresInDays.HasValue ? TimeSpan.FromDays(apiKey.ExpiresInDays.Value) : (TimeSpan?)null;

        var result = await apiKeyManager.CreateApiKeyAsync(apiKey.Name,
            apiKey.Permissions,
            expiresAt);

        messageService.Send(MessageAction.ApiKeyCreated, MessageTarget.Create(result.keyData.Id));

        var apiKeyResponseDto = mapper.Map<ApiKeyResponseDto>(result.keyData);
        apiKeyResponseDto.Key = result.apiKey;

        return apiKeyResponseDto;
    }
  
    /// <summary>
    ///  Get list api keys for user
    /// </summary>  
    /// <short>
    ///  Get list api keys for user
    /// </short>
    /// <path>api/2.0/keys</path>
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
    ///  Change status for a user api key. Change field isActive true|false
    /// </summary>  
    /// <short>
    ///  Change status for a user api key
    /// </short>
    /// <param name="keyId">Api key id</param>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Change status for a user api key", typeof(bool))]
    [HttpPut("{keyId}")]
    public async Task<bool> ChangeStatusApiKey(Guid keyId)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;

        if (!isAdmin)
        {
            var apiKey = await apiKeyManager.GetApiKeyAsync(keyId);

            if (apiKey.CreateBy != authContext.CurrentAccount.ID)
            {
                return false;
            }
        }

        var result = await apiKeyManager.ChangeStatusApiKeyAsync(keyId);

        if (result)
        {
            messageService.Send(MessageAction.ApiKeyChangedStatus, MessageTarget.Create(keyId));
        }

        return result;
    }

    /// <summary>
    ///  Delete a user api key by key id
    /// </summary>  
    /// <short>
    ///  Delete a user api key by key id
    /// </short>
    /// <param name="keyId">Api key id</param>
    /// <path>api/2.0/keys/{keyId}</path>
    [Tags("Api keys")]
    [SwaggerResponse(200, "Delete a user api key", typeof(bool))]
    [HttpDelete("{keyId}")]
    public async Task<bool> DeleteApiKey(Guid keyId)
    {
        var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        var isAdmin = currentType is EmployeeType.DocSpaceAdmin;

        if (!isAdmin)
        {
            var apiKey = await apiKeyManager.GetApiKeyAsync(keyId);

            if (apiKey.CreateBy != authContext.CurrentAccount.ID)
            {
                return false;
            }
        }

        var result = await apiKeyManager.DeleteApiKeyAsync(keyId);

        messageService.Send(MessageAction.ApiKeyDeleted, MessageTarget.Create(keyId));

        return result;
    }
}