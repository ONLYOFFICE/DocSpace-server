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
    /// <returns>The encryption key data transfer object that matches the provided filter conditions, or null if no match is found.</returns>
    [HttpGet("keys/filter")]
    public async Task<EncryptionKeyDto> GetUserKeysByFilter([FromQuery] GetUserKeysByFilterRequestDto inDto)
    {
        await Demand();

        return (await encryptionKeyPairHelper.GetKeyPairAsync()).FirstOrDefault(r =>
        {
            var result = false;

            if (inDto.Id.HasValue)
            {
                if (r.Id != inDto.Id)
                {
                    return false;
                }

                result = true;
            }

            // if (inDto.Type.HasValue)
            // {
            //     if (r.Type != inDto.Type.Value)
            //     {
            //         return false;
            //     }
            //
            //     result = true;
            // }
            //
            // if (!string.IsNullOrEmpty(inDto.Version))
            // {
            //     if (!r.Version.Equals(inDto.Version, StringComparison.OrdinalIgnoreCase))
            //     {
            //         return false;
            //     }
            //
            //     result = true;
            // }

            if (!string.IsNullOrEmpty(inDto.PublicKey))
            {
                if (!r.PublicKey.Equals(inDto.PublicKey, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                result = true;
            }

            if (!string.IsNullOrEmpty(inDto.PrivateKeyEnc))
            {
                if (!r.PrivateKeyEnc.Equals(inDto.PrivateKeyEnc, StringComparison.OrdinalIgnoreCase))
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
    /// <returns>The task result contains a collection of remaining encryption key data transfer objects after the deletion.</returns>
    [HttpDelete("keys/{id:guid}")]
    public async Task<IEnumerable<EncryptionKeyDto>> DeleteKeys(DeleteEncryptionKeyRequestDto inDto)
    {
        await Demand();

        return await encryptionKeyPairHelper.DeleteAsync(inDto.Id);
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
