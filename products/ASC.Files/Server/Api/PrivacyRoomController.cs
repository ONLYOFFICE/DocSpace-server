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
/// Manages the personal encryption keys that end-to-end encrypted private rooms are built on, and reports the keys
/// that give access to such a room.
/// </summary>
[Scope]
[ApiEndpoint("privacyroom")]
[Tags("Rooms / Privacy room")]
public class PrivacyRoomControllerCommon(
    AuthContext authContext,
    PermissionContext permissionContext,
    EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
    MessageService messageService)
    : ControllerBase
{
    /// <remarks>
    /// Creates an encryption key pair for the calling user. A user needs at least one key before anyone can invite
    /// them to a private room. Any signed-in user can call it, except a guest. A guest gets 403. The call changes data,
    /// and a repeated call with the same `id` returns 409. A body without `id` uses the all-zero `id`, so its repeat
    /// also returns 409. It returns 201 with every key the caller now holds. Before DocSpace 4.0, it returned 200 with
    /// the same body. To change an existing pair, use `PUT api/2.0/privacyroom/keys`. The call returns 400 if the body
    /// is missing, `publicKey` or `privateKeyEnc` is blank, or a key is too long. A malformed `id` also returns 400.
    /// </remarks>
    /// <summary>
    /// Create an encryption key
    /// </summary>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <collection>list</collection>
    [SwaggerResponse(201, "The encryption key is created. Answered 200 before DocSpace 4.0; the response body is unchanged", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The key material is missing, blank or too large to be stored")]
    [SwaggerResponse(403, "The caller is a guest, who cannot own encryption keys")]
    [SwaggerResponse(409, "A key with the same identifier already exists")]
    [HttpPost("keys")]
    public async Task<ActionResult<IEnumerable<EncryptionKeyDto>>> SetKeys([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EncryptionKeyRequestDto inDto)
    {
        await Demand();

        var keyPair = inDto?.Map();

        var keys = await encryptionKeyPairHelper.CreateKeyPairAsync(keyPair);

        messageService.Send(MessageAction.PrivacyRoomKeyCreated, MessageTarget.Create(keyPair!.Id), keyPair.Id.ToString());

        return Created(Request.Path.Value, keys);
    }

    /// <remarks>
    /// Replaces the key material of one encryption key pair of the calling user. The call finds the pair by `id` and
    /// leaves the other pairs unchanged. Without `id`, it replaces the pair with the all-zero `id`. The call replaces
    /// both halves, so send the private half even when it does not change. Any signed-in user can call it, except a
    /// guest. A guest gets 403. The call changes data, and you can repeat it with the same body. It returns every key
    /// the caller holds after the change. After this call, `GET api/2.0/privacyroom/{roomId}/access` returns the new
    /// public half. To create a pair, use `POST api/2.0/privacyroom/keys`. If the caller has no pair with this `id`,
    /// the call returns 404. The call returns 400 if the body is missing, `publicKey` or `privateKeyEnc` is blank, or
    /// a key is too long. A malformed `id` also returns 400. A 400 leaves the stored pair unchanged.
    /// </remarks>
    /// <summary>
    /// Rotate an encryption key
    /// </summary>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <collection>list</collection>
    [SwaggerResponse(200, "The encryption key is replaced", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The key material is missing, blank or too large to be stored")]
    [SwaggerResponse(403, "The caller is a guest, who cannot own encryption keys")]
    [SwaggerResponse(404, "The encryption key to replace is not found")]
    [HttpPut("keys")]
    public async Task<IEnumerable<EncryptionKeyDto>> ReplaceKey([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EncryptionKeyRequestDto inDto)
    {
        await Demand();

        var keyPair = inDto?.Map();

        var keys = await encryptionKeyPairHelper.ReplaceKeyPairAsync(keyPair);

        messageService.Send(MessageAction.PrivacyRoomKeyUpdated, MessageTarget.Create(keyPair!.Id), keyPair.Id.ToString());

        return keys;
    }

    /// <remarks>
    /// Returns every encryption key pair of the calling user, including the encrypted private half. Any signed-in user
    /// can call it. The call is read-only. If the caller has no key yet, the answer is an empty list or has no body. A
    /// guest cannot create keys, so a guest gets this empty answer unless they had keys before. A user without a key
    /// cannot be invited to a private room. To create a key, use `POST api/2.0/privacyroom/keys`. To get the keys of a
    /// private room, use `GET api/2.0/privacyroom/{roomId}/access`. To get the keys of one file, use `GET
    /// api/2.0/files/file/{fileId}/publickeys`.
    /// </remarks>
    /// <summary>
    /// Get own encryption keys
    /// </summary>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <collection>list</collection>
    [SwaggerResponse(200, "The encryption keys of the current user", typeof(IEnumerable<EncryptionKeyDto>))]
    [HttpGet("keys")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetUserKeys()
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairAsync();
    }

    /// <remarks>
    /// Returns the encryption keys of all members of a private room, one entry per key. An app uses them to encrypt a
    /// file key for every member. Requires membership in the room, with any access level, and a key of the caller's
    /// own. Other callers get 403. A DocSpace admin who is not a member also gets 403. The call is read-only. Only the
    /// caller's own entries include `privateKeyEnc`. The call skips entries without a public key. An archived private
    /// room still returns its keys. To get the keys of one file, use `GET api/2.0/files/file/{fileId}/publickeys`. To
    /// get the keys of users before you invite them, use `GET api/2.0/privacyroom/{roomId}/access/candidates`. If the
    /// room is not private, the call returns 400. If the room is unknown or in Trash, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get private room access keys
    /// </summary>
    /// <path>api/2.0/privacyroom/{roomId}/access</path>
    /// <collection>list</collection>
    /// <param name="roomId">
    /// The private room whose access keys are read. Take it from the `id` of the room returned by
    /// `POST api/2.0/files/rooms` or listed by `GET api/2.0/files/rooms`.
    /// </param>
    [SwaggerResponse(200, "The encryption keys associated with the privacy room", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The room is not a private room")]
    [SwaggerResponse(403, "The caller has no read access to the room or holds no encryption key of their own")]
    [SwaggerResponse(404, "The room is not found")]
    [HttpGet("{roomId:int}/access")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetUserKeysForRoom(int roomId)
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairForRoomAsync(roomId);
    }

    /// <remarks>
    /// Returns the public encryption keys of users that you plan to invite to a private room. Use them to encrypt the
    /// file keys of the room for these users before you invite them. Requires the right to change the access of the
    /// room, which the room owner and room managers have. Other callers get 403. A DocSpace admin who is not a member
    /// also gets 403. It also returns 403 for an archived room. The call is read-only and invites nobody. To invite
    /// the users, use `PUT api/2.0/files/rooms/{id}/share`. The answer has one entry per key and never includes
    /// `privateKeyEnc`, even for the caller. It has no entry for a user without a key, a guest, a disabled user, or an
    /// unknown user. A user without a key must first create one with `POST api/2.0/privacyroom/keys`. The call counts
    /// a repeated user ID once. Without `userIds`, it returns an empty list. To get the keys of current members, use
    /// `GET api/2.0/privacyroom/{roomId}/access`. The call returns 400 if the room is not private or `roomId` is a
    /// folder inside a room. A malformed user ID also returns 400. If the room is unknown, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get public keys of room invitees
    /// </summary>
    /// <path>api/2.0/privacyroom/{roomId}/access/candidates</path>
    /// <collection>list</collection>
    [SwaggerResponse(200, "The public encryption keys of the listed users who can be invited", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The room is not a private room, or a user ID is malformed")]
    [SwaggerResponse(403, "The caller has no read access to the room or cannot change its access")]
    [SwaggerResponse(404, "The room is not found")]
    [HttpGet("{roomId:int}/access/candidates")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetRoomCandidateKeys(GetRoomCandidateKeysRequestDto inDto)
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairForRoomCandidatesAsync(inDto.RoomId, inDto.UserIds);
    }

    /// <remarks>
    /// Returns the encrypted files of a private room that each user cannot open yet, in all folders of the room. An app
    /// then writes only the missing keys with `PUT api/2.0/files/{fileId}/access`. With `userIds`, the call checks
    /// these users, members or not. Without `userIds`, it checks the members of the room and the room owner. Before
    /// you change the room owner with `POST api/2.0/files/owner`, check the new owner here. That call refuses a new
    /// owner who misses a key. Requires the right to change the access of the room, which the room owner and room
    /// managers have. Other callers get 403. A DocSpace admin who is not a member also gets 403. It also returns 403
    /// for an archived room. The call is read-only. The answer has one entry per user who misses a key. An empty list
    /// means that no key is missing. The call skips files stored without encryption and IDs of unknown users. The call
    /// returns 400 if the room is not private or `roomId` is a folder inside a room.
    /// A malformed user ID also returns 400. If the room is unknown, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get missing file keys of a room
    /// </summary>
    /// <path>api/2.0/privacyroom/{roomId}/access/missing</path>
    /// <collection>list</collection>
    [SwaggerResponse(200, "The users who miss a file key and the files concerned", typeof(IEnumerable<RoomMissingFileKeysDto>))]
    [SwaggerResponse(400, "The room is not a private room, or a user ID is malformed")]
    [SwaggerResponse(403, "The caller has no read access to the room or cannot change its access")]
    [SwaggerResponse(404, "The room is not found")]
    [HttpGet("{roomId:int}/access/missing")]
    public async Task<IEnumerable<RoomMissingFileKeysDto>> GetRoomMissingFileKeys(GetRoomMissingFileKeysRequestDto inDto)
    {
        await Demand();

        return await encryptionKeyPairHelper.GetMissingFileKeysAsync(inDto.RoomId, inDto.UserIds);
    }

    /// <remarks>
    /// Deletes one encryption key pair of the calling user. The other pairs stay unchanged. Any signed-in user can call
    /// it, except a guest. A guest gets 403. The deletion cannot be undone, and a repeated call returns 404. The call
    /// returns 204 with no body. Before DocSpace 4.0, it returned 200 with the remaining keys. To read the remaining
    /// keys, use `GET api/2.0/privacyroom/keys`. After the caller deletes the last key,
    /// `GET api/2.0/privacyroom/{roomId}/access` returns 403 for all their private rooms. The rooms and their content
    /// stay unchanged. To restore access, create a key with `POST api/2.0/privacyroom/keys`. If the caller has no
    /// pair with this `id`, the call returns 404. A malformed `id` returns 400.
    /// </remarks>
    /// <summary>
    /// Delete an encryption key
    /// </summary>
    /// <path>api/2.0/privacyroom/keys/{id}</path>
    [SwaggerResponse(204, "The encryption key is deleted. Answered 200 with the remaining keys before DocSpace 4.0")]
    [SwaggerResponse(400, "The key identifier is not a valid GUID")]
    [SwaggerResponse(403, "The caller is a guest, who cannot own encryption keys")]
    [SwaggerResponse(404, "The encryption key is not found")]
    [HttpDelete("keys/{id}")]
    public async Task<IActionResult> DeleteKeys(DeleteEncryptionKeyRequestDto inDto)
    {
        await Demand();

        await encryptionKeyPairHelper.DeleteAsync(inDto.Id);

        messageService.Send(MessageAction.PrivacyRoomKeyDeleted, MessageTarget.Create(inDto.Id), inDto.Id.ToString());

        return NoContent();
    }

    private async Task Demand()
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(authContext.CurrentAccount.ID), Constants.Action_EditUser);
    }
}
