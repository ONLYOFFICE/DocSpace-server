// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.MessagingSystem.EF.Model;

namespace ASC.Api.Documents;

/// <summary>
/// Provides API endpoints for managing privacy rooms and encryption keys.
/// </summary>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("privacyroom")]
public class PrivacyRoomControllerCommon(
    AuthContext authContext,
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
    MessageService messageService)
    : ControllerBase
{
    /// <summary>
    /// Creates and sets encryption keys for the user.
    /// </summary>
    /// <remarks>
    /// Creates and sets encryption keys for the user.
    /// </remarks>
    /// <param name="inDto">The request object containing public and private key information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of encryption key data transfer objects.</returns>
    [HttpPost("keys")]
    public Task<IEnumerable<EncryptionKeyDto>> SetKeys(EncryptionKeyRequestDto inDto)
    {
        return CreateKeysAsync([inDto], false);
    }

    /// <summary>
    /// Replaces an existing encryption key with a new one for the user.
    /// </summary>
    /// <remarks>
    /// Replaces an existing encryption key with a new one for the user.
    /// </remarks>
    /// <param name="inDto">The request object containing the public and private key information to replace the existing key.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of encryption key data transfer objects.</returns>
    [HttpPut("keys")]
    public Task<IEnumerable<EncryptionKeyDto>> ReplaceKey(EncryptionKeyRequestDto inDto)
    {
        return CreateKeysAsync([inDto], true);
    }

    /// <summary>
    /// Retrieves a specific user encryption key based on the provided filter conditions.
    /// </summary>
    /// <remarks>
    /// Retrieves a specific user encryption key based on the provided filter conditions.
    /// </remarks>
    /// <param name="id">The optional identifier of the encryption key to filter by.</param>
    /// <param name="type">The optional type of the encryption key to filter by.</param>
    /// <param name="version">The optional version of the encryption key to filter by.</param>
    /// <param name="publicKey">The optional public key to filter by.</param>
    /// <param name="privateKeyEnc">The optional encrypted private key to filter by.</param>
    /// <returns>The encryption key data transfer object that matches the provided filter conditions, or null if no match is found.</returns>
    [HttpGet("keys/filter")]
    public async Task<EncryptionKeyDto> GetUserKeysByFilter(Guid? id, EncryptionKeyType? type, string version, string publicKey, string privateKeyEnc)
    {
        await Demand();

        return (await encryptionKeyPairHelper.GetKeyPairAsync()).FirstOrDefault(r =>
        {
            var result = false;
            
            if (id.HasValue)
            {
                if (r.Id != id)
                {
                    return false;
                }

                result = true;
            }
            
            // if (type.HasValue)
            // {                
            //     if (r.Type != type.Value)
            //     {
            //         return false;
            //     }
            //     
            //     result = true;
            // }
            //
            // if (!string.IsNullOrEmpty(version))
            // {
            //     if (!r.Version.Equals(version, StringComparison.OrdinalIgnoreCase))
            //     {
            //         return false;
            //     }
            //     
            //     result = true;
            // }
            
            if (!string.IsNullOrEmpty(publicKey))
            {
                if (!r.PublicKey.Equals(publicKey, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                result = true;
            }
            
            if (!string.IsNullOrEmpty(privateKeyEnc))
            {
                if (!r.PrivateKeyEnc.Equals(privateKeyEnc, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                result = true;
            }

            return result;
        });
    }

    /// <summary>
    /// Retrieves encryption keys associated with the current user.
    /// </summary>
    /// <remarks>
    /// Retrieves encryption keys associated with the current user.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of encryption key data transfer objects.</returns>
    [HttpGet("keys")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetUserKeys()
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairAsync();
    }

    /// <summary>
    /// Retrieves the encryption keys associated with a specific privacy room.
    /// </summary>
    /// <remarks>
    /// Retrieves the encryption keys associated with a specific privacy room.
    /// </remarks>
    /// <param name="roomId">The identifier of the privacy room.</param>
    /// <returns>A task containing a collection of encryption key data transfer objects for the specified room.</returns>
    [HttpGet("{roomId:int}/access")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetUserKeysForRoom(int roomId)
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairForRoomAsync(roomId);
    }

    /// <summary>
    /// Deletes an encryption key and removes it from the system.
    /// </summary>
    /// <remarks>
    /// Deletes an encryption key and removes it from the system based on the provided key identifier.
    /// </remarks>
    /// <param name="id">The unique identifier of the encryption key to be deleted.</param>
    /// <returns>The task result contains a collection of remaining encryption key data transfer objects after the deletion.</returns>
    [HttpDelete("keys/{id:guid}")]
    public async Task<IEnumerable<EncryptionKeyDto>> DeleteKeys(Guid id)
    {
        await Demand();

        return await encryptionKeyPairHelper.DeleteAsync(id);
    }

    /// <summary>
    /// Retrieves the current settings for the Privacy Room functionality.
    /// </summary>
    /// <remarks>
    /// Retrieves the current settings for the Privacy Room functionality.
    /// </remarks>
    /// <returns>A task result indicating whether the Privacy Room feature is enabled.</returns>
    [HttpGet]
    public async Task<bool> GetPrivacyRoomSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await PrivacyRoomSettings.GetEnabledAsync(settingsManager);
    }

    /// <summary>
    /// Configures the privacy room settings for the portal.
    /// </summary>
    /// <remarks>
    /// Configures the privacy room settings for the portal.
    /// </remarks>
    /// <param name="inDto">The request object containing the privacy room enable or disable flag.</param>
    /// <returns>A task result indicating whether the privacy room was successfully enabled or disabled.</returns>
    [HttpPut]
    public async Task<bool> SetPrivacyRoomSettings(PrivacyRoomEnableRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inDto.Enable)
        {
            if (!PrivacyRoomSettings.IsAvailable())
            {
                throw new BillingException(Resource.ErrorNotAllowedOption);
            }
        }

        await PrivacyRoomSettings.SetEnabledAsync(settingsManager, inDto.Enable);

        await messageService.SendAsync(inDto.Enable ? MessageAction.PrivacyRoomEnable : MessageAction.PrivacyRoomDisable, MessageTarget.Create(authContext.CurrentAccount.ID));

        return inDto.Enable;
    }
    
    private async Task Demand()
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(authContext.CurrentAccount.ID), Constants.Action_EditUser);

        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new SecurityException();
        }
    }
    
    private async Task<IEnumerable<EncryptionKeyDto>> CreateKeysAsync(IEnumerable<EncryptionKeyRequestDto> inDto, bool replace)
    {
        await Demand();

        return await encryptionKeyPairHelper.SetKeyPairAsync(inDto.Select(r=> r.Map()), replace);
    }
}
