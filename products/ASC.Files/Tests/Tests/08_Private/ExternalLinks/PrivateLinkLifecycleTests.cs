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


namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// The life of a link of a private item after it is made: a new password replaces the sealed key and the proof hash
/// and keeps the id, the public key and the wraps, so the earlier proof and its sessions stop opening it; another
/// public key is refused; a revocation deletes everything the store holds of the link and ends its sessions.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkLifecycleTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private sealed record Link(int RoomId, int FileId, Guid Id, string Token, LinkBody Body);

    private async Task<Link> RoomLinkWithAWrappedFile(Proof proof)
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "inside.docx");

        await _filesClient.Authenticate(Owner);
        var body = new LinkBody { LinkProofHash = proof.Hash, LinkSecretWraps = [SecretWrap.For(Owner.Id)] };
        using var response = await PutRoomLink(room.Id, body);
        ((int)response.StatusCode).Should().Be(200);
        var link = await SharedLinkOf(response);
        var id = Guid.Parse(link.GetProperty("id").GetString()!);

        (await WriteKeys(fileId, id)).Should().BeEmpty();

        return new Link(room.Id, fileId, id, link.GetProperty("requestToken").GetString()!, body);
    }

    private static ExternalShareStatus StatusIn(JsonElement json)
    {
        var status = json.GetProperty("response").GetProperty("status");

        return status.ValueKind == JsonValueKind.Number
            ? (ExternalShareStatus)status.GetInt32()
            : Enum.Parse<ExternalShareStatus>(status.GetString()!, ignoreCase: true);
    }

    /// <summary>The status of the link as its visitor sees it, with the cookies of a session or without.</summary>
    private async Task<ExternalShareStatus> StatusOf(string token, IEnumerable<string>? cookies = null)
    {
        await _filesClient.Authenticate(null);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/2.0/files/share/{Uri.EscapeDataString(token)}");
        if (cookies != null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        }

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return StatusIn(await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken));
    }

    private async Task<ExternalShareStatus> SendProof(string token, string proof)
    {
        await _filesClient.Authenticate(null);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/files/share/{Uri.EscapeDataString(token)}/password")
        {
            Content = JsonContent.Create(new { proof })
        };

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return StatusIn(await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken));
    }

    private async Task<object?> Scalar(string sql, Guid id)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@tenant", TenantId);
        command.Parameters.AddWithValue("@id", id.ToString());

        return await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
    }

    private async Task<long> KeyRows(Guid linkId) =>
        Convert.ToInt64(await Scalar("SELECT COUNT(*) FROM files_share_link_keys WHERE tenant_id = @tenant AND link_id = @id;", linkId), CultureInfo.InvariantCulture);

    private async Task<long> SecretWrapRows(Guid linkId) =>
        Convert.ToInt64(await Scalar("SELECT COUNT(*) FROM files_share_link_secret_wraps WHERE tenant_id = @tenant AND link_id = @id;", linkId), CultureInfo.InvariantCulture);

    private async Task<long> FileKeyRows(Guid linkId) =>
        Convert.ToInt64(await Scalar("SELECT COUNT(*) FROM files_file_keys WHERE tenant_id = @tenant AND user_id = @id;", linkId), CultureInfo.InvariantCulture);

    private async Task<string?> StoredProofHash(Guid linkId) =>
        (string?)await Scalar("SELECT proof_hash FROM files_share_link_keys WHERE tenant_id = @tenant AND link_id = @id;", linkId);

    [Fact]
    public async Task ANewPassword_RefusesTheEarlierProof_EndsItsSession_AndOpensWithTheNewOne()
    {
        var oldProof = Proof.New();
        var link = await RoomLinkWithAWrappedFile(oldProof);
        await _filesClient.Authenticate(null);
        var oldSession = await OpenLinkSession(link.Token, oldProof);
        (await StatusOf(link.Token, oldSession)).Should().Be(ExternalShareStatus.Ok);

        var newProof = Proof.New();
        await _filesClient.Authenticate(Owner);
        using (var replaced = await PutRoomLink(link.RoomId, new LinkBody
               {
                   LinkId = link.Id,
                   LinkPublicKey = link.Body.LinkPublicKey,
                   LinkPrivateKeyEnc = RandomBase64(96),
                   LinkProofHash = newProof.Hash
               }))
        {
            ((int)replaced.StatusCode).Should().Be(200);
            (await SharedLinkOf(replaced)).GetProperty("id").GetString().Should().Be(link.Id.ToString());
        }

        (await StatusOf(link.Token, oldSession)).Should().Be(ExternalShareStatus.RequiredPassword);
        (await SendProof(link.Token, oldProof.Value)).Should().Be(ExternalShareStatus.InvalidPassword);
        (await SendProof(link.Token, newProof.Value)).Should().Be(ExternalShareStatus.Ok);

        // The wraps of the file keys and of the secret stay: they were made for the same key pair.
        (await FileKeyRows(link.Id)).Should().Be(1);
        (await SecretWrapRows(link.Id)).Should().Be(1);
    }

    [Fact]
    public async Task AnotherPublicKey_Returns400_AndChangesNothing()
    {
        var proof = Proof.New();
        var link = await RoomLinkWithAWrappedFile(proof);

        await _filesClient.Authenticate(Owner);
        using (var refused = await PutRoomLink(link.RoomId, new LinkBody { LinkId = link.Id, LinkProofHash = Proof.New().Hash }))
        {
            ((int)refused.StatusCode).Should().Be(400);
        }

        (await StoredProofHash(link.Id)).Should().Be(proof.Hash);
        (await SendProof(link.Token, proof.Value)).Should().Be(ExternalShareStatus.Ok);
    }

    [Fact]
    public async Task ARevokedLink_LeavesNothingInTheStore_AndItsSessionGetsInvalid()
    {
        var proof = Proof.New();
        var link = await RoomLinkWithAWrappedFile(proof);
        await _filesClient.Authenticate(null);
        var session = await OpenLinkSession(link.Token, proof);

        (await KeyRows(link.Id)).Should().Be(1);
        (await SecretWrapRows(link.Id)).Should().Be(1);
        (await FileKeyRows(link.Id)).Should().Be(1);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomLinkAsync(link.RoomId, new RoomLinkRequest(link.Id, FileShare.None, linkType: LinkType.External), TestContext.Current.CancellationToken);

        (await KeyRows(link.Id)).Should().Be(0);
        (await SecretWrapRows(link.Id)).Should().Be(0);
        (await FileKeyRows(link.Id)).Should().Be(0);
        (await StatusOf(link.Token, session)).Should().Be(ExternalShareStatus.Invalid);
    }
}
