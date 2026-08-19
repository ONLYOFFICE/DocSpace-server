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

namespace ASC.Api.Documents;

/// <summary>
/// Provides API endpoints for managing privacy rooms and encryption keys.
/// </summary>
[Scope]
[ApiEndpoint("privacyroom")]
public class PrivacyRoomControllerCommon(
    AuthContext authContext,
    PermissionContext permissionContext,
    EncryptionKeyPairDtoHelper encryptionKeyPairHelper)
    : ControllerBase
{
    /// <summary>
    /// Creates and sets encryption keys for the user.
    /// </summary>
    /// <remarks>
    /// Creates and sets encryption keys for the user.
    /// </remarks>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <param name="inDto">The request object containing public and private key information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of encryption key data transfer objects.</returns>
    [SwaggerResponse(201, "The encryption key is created. Answered 200 before DocSpace 4.0; the response body is unchanged", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The key material is missing, blank or too large to be stored")]
    [SwaggerResponse(409, "A key with the same identifier already exists")]
    [HttpPost("keys")]
    public async Task<ActionResult<IEnumerable<EncryptionKeyDto>>> SetKeys([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EncryptionKeyRequestDto inDto)
    {
        await Demand();

        var keys = await encryptionKeyPairHelper.CreateKeyPairAsync(inDto?.Map());

        return Created(Request.Path.Value, keys);
    }

    /// <summary>
    /// Replaces an existing encryption key with a new one for the user.
    /// </summary>
    /// <remarks>
    /// Replaces an existing encryption key with a new one for the user.
    /// </remarks>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <param name="inDto">The request object containing the public and private key information to replace the existing key.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of encryption key data transfer objects.</returns>
    [SwaggerResponse(200, "The encryption key is replaced", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The key material is missing, blank or too large to be stored")]
    [SwaggerResponse(404, "The encryption key to replace is not found")]
    [HttpPut("keys")]
    public async Task<IEnumerable<EncryptionKeyDto>> ReplaceKey([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EncryptionKeyRequestDto inDto)
    {
        await Demand();

        return await encryptionKeyPairHelper.ReplaceKeyPairAsync(inDto?.Map());
    }

    /// <summary>
    /// Retrieves encryption keys associated with the current user.
    /// </summary>
    /// <remarks>
    /// Retrieves encryption keys associated with the current user.
    /// </remarks>
    /// <path>api/2.0/privacyroom/keys</path>
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
    /// <path>api/2.0/privacyroom/{roomId}/access</path>
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
    /// <para>
    /// Breaking change in DocSpace 4.0: the endpoint used to answer 200 with the caller's remaining
    /// encryption keys and now answers 204 with no body. A client that read that list must call
    /// <c>GET api/2.0/privacyroom/keys</c> instead.
    /// </para>
    /// </remarks>
    /// <path>api/2.0/privacyroom/keys/{id}</path>
    /// <returns>A task that represents the asynchronous operation. No content is returned.</returns>
    [SwaggerResponse(204, "The encryption key is deleted. Answered 200 with the remaining keys before DocSpace 4.0")]
    [SwaggerResponse(400, "The key identifier is not a valid GUID")]
    [SwaggerResponse(404, "The encryption key is not found")]
    [HttpDelete("keys/{id}")]
    public async Task<IActionResult> DeleteKeys(DeleteEncryptionKeyRequestDto inDto)
    {
        await Demand();

        await encryptionKeyPairHelper.DeleteAsync(inDto.Id);

        return NoContent();
    }

    private async Task Demand()
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(authContext.CurrentAccount.ID), Constants.Action_EditUser);
    }
}
