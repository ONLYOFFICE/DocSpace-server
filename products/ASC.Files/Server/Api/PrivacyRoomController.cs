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
    /// Stores a new encryption key pair for the calling user and answers with that user's whole key set. The material
    /// is end-to-end: `publicKey` is the half other members use to encrypt file keys for this user, while
    /// `privateKeyEnc` arrives already encrypted with the user's own password, so the portal keeps it as opaque text.
    /// A member must hold at least one key before they can be invited to a private room, which makes this the first
    /// call of the private-room flow. Every authenticated member manages their own keys and only their own, there is
    /// no parameter for somebody else's, and a guest is refused, which is also why a guest cannot become a member of
    /// a private room. The call is mutating and is not safe to repeat: `id` names the pair inside the caller's set
    /// and an `id` that is already stored is answered with 409, while a request that omits or blanks either half is
    /// rejected as invalid and stores nothing. A successful call answers 201 with every key the caller now holds. To
    /// change the material of an existing pair use `PUT api/2.0/privacyroom/keys`.
    /// </remarks>
    /// <summary>
    /// Create an encryption key
    /// </summary>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <collection>list</collection>
    [SwaggerResponse(201, "The encryption key is created. Answered 200 before DocSpace 4.0; the response body is unchanged", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The key material is missing, blank or too large to be stored")]
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
    /// Rotates one encryption key pair of the calling user: the entry whose `id` matches is overwritten with the
    /// submitted `publicKey` and `privateKeyEnc`, and the caller's other pairs are left untouched. The pair has to
    /// exist already, an `id` that is not in the caller's set is answered with 404, and a first key is created with
    /// `POST api/2.0/privacyroom/keys`. This is a full replacement rather than a merge: both halves are mandatory,
    /// and a request that omits or blanks one of them is rejected as invalid with the stored pair surviving
    /// unchanged, so a rotation that means to keep the private half has to send it again. Omitting `id` targets the
    /// all-zero pair, the one a client that never sets an id keeps rotating. Every authenticated member rotates their
    /// own keys and only their own, and a guest is refused. The call is mutating, and repeating it with the same body
    /// leaves the same state. It answers with every key the caller holds afterwards, and from then on
    /// `GET api/2.0/privacyroom/{roomId}/access` reports the new public half for this member.
    /// </remarks>
    /// <summary>
    /// Rotate an encryption key
    /// </summary>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <collection>list</collection>
    [SwaggerResponse(200, "The encryption key is replaced", typeof(IEnumerable<EncryptionKeyDto>))]
    [SwaggerResponse(400, "The key material is missing, blank or too large to be stored")]
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
    /// Returns every encryption key pair the calling user holds, the encrypted private half included, which is the
    /// material a client needs in order to decrypt content in a private room. The set is personal and there is no
    /// parameter for another user's keys: an authenticated caller reads only their own, and a guest, who cannot own
    /// key material at all, always reads an empty set. The call is read-only. An empty answer, whether an empty list
    /// or none at all, means no key has been created yet, and until `POST api/2.0/privacyroom/keys` creates one the
    /// user cannot be invited to a private room. Each entry carries the pair's `id`, its owner in `userId`, the
    /// moment the material was stored in `date`, the public half, the private half encrypted with the user's
    /// password, and the portal-wide crypto engine in `cryptoEngineId`. For the keys that open a whole private room
    /// use `GET api/2.0/privacyroom/{roomId}/access`, and for the keys a single file is shared with use
    /// `GET api/2.0/files/file/{fileId}/publickeys`; this operation is about the caller alone.
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
    /// Returns the encryption keys that give access to a private room: one entry per key held by each of its members,
    /// which is what a client needs in order to encrypt a file key for everyone allowed to open the room's content.
    /// Only the caller's own entries carry `privateKeyEnc`; another member's entry carries the public half alone, and
    /// an entry with no public half is not reported as access at all. The room has to be a private one, a room
    /// created without private mode holds no access keys and the call is refused, and it has to still exist: an
    /// unknown room, or one already moved to Trash, is reported as missing, while an archived private room still
    /// answers. Access follows room membership and not portal role: any member from read access upwards receives the
    /// full set, whereas a DocSpace administrator who is not a member is refused, and so is a caller holding no key
    /// of their own, the room creator included once they delete their last key. The call is read-only. For the keys
    /// of a single file use `GET api/2.0/files/file/{fileId}/publickeys`.
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
    [HttpGet("{roomId:int}/access")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetUserKeysForRoom(int roomId)
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairForRoomAsync(roomId);
    }

    /// <remarks>
    /// Removes one encryption key pair from the calling user's own key set and answers 204 with no body. The pair is
    /// named by the `id` of an entry of `GET api/2.0/privacyroom/keys`; the caller's other pairs stay as they are.
    /// The call is destructive and cannot be repeated: the key material is gone for good, a second delete of the same
    /// `id`, like an `id` that was never stored, is answered with 404, and there is no parameter for another user's
    /// keys, so an authenticated member only ever deletes their own while a guest is refused. Deleting the last key
    /// the caller holds locks them out of the private rooms they belong to, their own rooms included: the rooms and
    /// their content survive untouched and stay listed as private, but `GET api/2.0/privacyroom/{roomId}/access` then
    /// refuses the caller until a new key is stored with `POST api/2.0/privacyroom/keys`. Before DocSpace 4.0 the
    /// call answered 200 with the caller's remaining keys, so a client that read that list has to call
    /// `GET api/2.0/privacyroom/keys` instead.
    /// </remarks>
    /// <summary>
    /// Delete an encryption key
    /// </summary>
    /// <path>api/2.0/privacyroom/keys/{id}</path>
    [SwaggerResponse(204, "The encryption key is deleted. Answered 200 with the remaining keys before DocSpace 4.0")]
    [SwaggerResponse(400, "The key identifier is not a valid GUID")]
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
