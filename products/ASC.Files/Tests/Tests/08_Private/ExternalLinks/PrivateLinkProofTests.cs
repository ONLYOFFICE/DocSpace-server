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
/// A link of an item in a private room opens against the proof the visitor's page derives from the link's secret:
/// <c>POST /files/share/{key}/password</c> with <c>proof</c> (the base64 of 32 bytes) compares SHA-256 of those bytes
/// with the stored <c>linkProofHash</c>, answers the sealed key to the request whose proof matched, and remembers the
/// session in a cookie. Wrong proofs count towards the same block as wrong passwords. The test client keeps no cookies,
/// so the tests carry the session cookie themselves, as a browser does.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkProofTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private const int DefaultAttemptCount = 5;

    private sealed record ShareAnswer(ExternalShareStatus Status, bool? IsPrivate, bool? PasswordRequired, string? LinkPrivateKeyEnc, string[] Cookies);

    private static ExternalShareStatus StatusOf(JsonElement status) =>
        status.ValueKind == JsonValueKind.Number
            ? (ExternalShareStatus)status.GetInt32()
            : Enum.Parse<ExternalShareStatus>(status.GetString()!, ignoreCase: true);

    private static bool? BoolOf(JsonElement answer, string name) =>
        answer.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    private async Task<ShareAnswer> SendAsync(HttpMethod method, string token, object? body = null, IEnumerable<string>? cookies = null)
    {
        await _filesClient.Authenticate(null);

        var path = $"api/2.0/files/share/{Uri.EscapeDataString(token)}" + (method == HttpMethod.Post ? "/password" : "");
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (cookies != null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        }

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ApiException(403, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var answer = json.GetProperty("response");
        var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Select(v => v.Split(';')[0]).ToArray()
            : [];

        return new ShareAnswer(
            StatusOf(answer.GetProperty("status")),
            BoolOf(answer, "isPrivate"),
            BoolOf(answer, "passwordRequired"),
            answer.TryGetProperty("linkPrivateKeyEnc", out var key) && key.ValueKind == JsonValueKind.String ? key.GetString() : null,
            setCookies);
    }

    private Task<ShareAnswer> ReadLink(string token, IEnumerable<string>? cookies = null) => SendAsync(HttpMethod.Get, token, cookies: cookies);

    private Task<ShareAnswer> SendProof(string token, string? proof, string? password = null) =>
        SendAsync(HttpMethod.Post, token, new { proof, password });

    /// <summary>Lists a folder anonymously on a link token, with the cookies of a session, the way a browser does.</summary>
    private async Task<HttpStatusCode> ListStatus(int folderId, string token, IEnumerable<string> cookies)
    {
        await _filesClient.Authenticate(null);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/2.0/files/{folderId}");
        request.Headers.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    private async Task<(int FolderId, string Token, Proof Proof, string SealedKey)> FolderLinkWithProof(Proof? proof = null)
    {
        proof ??= Proof.New();
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        await UploadEncryptedFile(folderId, "inside.docx");
        var body = new LinkBody { LinkProofHash = proof.Hash };
        using var response = await PutFolderLink(folderId, body);
        ((int)response.StatusCode).Should().Be(200);
        var token = (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;

        return (folderId, token, proof, body.LinkPrivateKeyEnc!);
    }

    [Fact]
    public async Task ValidProof_OpensTheLink_ReturnsTheSealedKey_AndTheSessionListsTheFolder()
    {
        var (folderId, token, proof, sealedKey) = await FolderLinkWithProof();

        var before = await ReadLink(token);
        var opened = await SendProof(token, proof.Value);
        var listed = await ListStatus(folderId, token, opened.Cookies);
        var reread = await ReadLink(token, opened.Cookies);

        before.Status.Should().Be(ExternalShareStatus.RequiredPassword);
        before.IsPrivate.Should().BeTrue();
        before.PasswordRequired.Should().BeTrue();
        before.LinkPrivateKeyEnc.Should().BeNull();

        opened.Status.Should().Be(ExternalShareStatus.Ok);
        opened.LinkPrivateKeyEnc.Should().Be(sealedKey);
        listed.Should().Be(HttpStatusCode.OK, "the session cookie set by the proof opens the folder");

        reread.Status.Should().Be(ExternalShareStatus.Ok, "the session is remembered");
        reread.LinkPrivateKeyEnc.Should().BeNull("the sealed key goes only to the request whose proof opened the link");
    }

    [Fact]
    public async Task WrongProof_OrNone_OrAPassword_OpensNothing_ThenTheRightOneOpens()
    {
        var (folderId, token, proof, sealedKey) = await FolderLinkWithProof();

        var wrong = await SendProof(token, Proof.New().Value);
        var none = await SendProof(token, null, password: "abcdefghij");
        var notBase64 = await SendProof(token, "not a proof");
        var wrongListed = await ListStatus(folderId, token, wrong.Cookies);
        var right = await SendProof(token, proof.Value);

        wrong.Status.Should().Be(ExternalShareStatus.InvalidPassword);
        wrong.LinkPrivateKeyEnc.Should().BeNull();
        none.Status.Should().Be(ExternalShareStatus.RequiredPassword, "a password is not looked at for such a link");
        none.LinkPrivateKeyEnc.Should().BeNull();
        notBase64.Status.Should().Be(ExternalShareStatus.InvalidPassword);
        wrongListed.Should().Be(HttpStatusCode.Forbidden);

        right.Status.Should().Be(ExternalShareStatus.Ok);
        right.LinkPrivateKeyEnc.Should().Be(sealedKey);
    }

    [Fact]
    public async Task ProofOfTheBase64Text_IsWrong()
    {
        // The hash is taken over the base64 text instead of the 32 bytes it encodes; the right proof hashes the bytes.
        var bytes = RandomNumberGenerator.GetBytes(32);
        var value = Convert.ToBase64String(bytes);
        var hashOfText = new Proof(value, Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value))));
        var (_, textToken, _, _) = await FolderLinkWithProof(hashOfText);
        var (_, bytesToken, _, _) = await FolderLinkWithProof(new Proof(value, Convert.ToBase64String(SHA256.HashData(bytes))));

        var overBytes = await SendProof(bytesToken, value);
        var overText = await SendProof(textToken, value);

        overBytes.Status.Should().Be(ExternalShareStatus.Ok);
        overText.Status.Should().Be(ExternalShareStatus.InvalidPassword);
        overText.LinkPrivateKeyEnc.Should().BeNull();
    }

    [Fact]
    public async Task MoreWrongProofsThanAllowed_BlockTheLinkWith403_EvenForTheRightProof()
    {
        var (_, token, proof, _) = await FolderLinkWithProof();
        var wrong = Proof.New().Value;

        for (var i = 0; i < DefaultAttemptCount; i++)
        {
            (await SendProof(token, wrong)).Status.Should().Be(ExternalShareStatus.InvalidPassword);
        }

        var blocked = await Assert.ThrowsAsync<ApiException>(() => SendProof(token, wrong));
        var blockedRight = await Assert.ThrowsAsync<ApiException>(() => SendProof(token, proof.Value));

        blocked.ErrorCode.Should().Be(403);
        blockedRight.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task LinkOfAPrivateVirtualDataRoom_AsksAnAnonymousVisitorToSignIn_EvenWithTheProof()
    {
        var proof = Proof.New();
        var room = await CreateRoomAsOwner(RoomType.VirtualDataRoom);
        var fileId = await UploadEncryptedFile(room.Id, "vdr.docx");
        using var response = await PutFileLink(fileId, new LinkBody { LinkProofHash = proof.Hash });
        ((int)response.StatusCode).Should().Be(200);
        var token = (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;

        // A Custom room link with the same proof opens, so the refusal below is the audience of the room type.
        var (_, customToken, _, _) = await FolderLinkWithProof(proof);
        var custom = await SendProof(customToken, proof.Value);
        var vdr = await SendProof(token, proof.Value);

        custom.Status.Should().Be(ExternalShareStatus.Ok);
        vdr.Status.Should().Be(ExternalShareStatus.ExternalAccessDenied);
        vdr.LinkPrivateKeyEnc.Should().BeNull();
    }

    /// <summary>Reads the keys of a file anonymously on a link token, with the cookies of a session.</summary>
    private async Task<(HttpStatusCode Status, JsonElement Keys)> VisitorKeys(int fileId, string token, IEnumerable<string> cookies)
    {
        await _filesClient.Authenticate(null);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/2.0/files/file/{fileId}/publickeys");
        request.Headers.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (response.StatusCode, default);
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        return (response.StatusCode, json.GetProperty("response").Clone());
    }

    private static JsonElement LinkEntryOf(JsonElement keys) =>
        keys.EnumerateArray().Single(k => k.TryGetProperty("isLink", out var isLink) && isLink.ValueKind == JsonValueKind.True);

    private static bool HasPrivateHalf(JsonElement entry) =>
        entry.TryGetProperty("privateKeyEnc", out var value) && value.ValueKind == JsonValueKind.String;

    private static string? FileKeyEncOf(JsonElement entry) =>
        entry.TryGetProperty("fileKeyEnc", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    [Fact]
    public async Task Visitor_GetsTheWrapOfTheLink_AndTheMembersPublicKeys_OnceAMemberWroteIt()
    {
        var proof = Proof.New();
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var fileId = await UploadEncryptedFile(folderId, "inside.docx");
        var body = new LinkBody { LinkProofHash = proof.Hash };
        using var linkResponse = await PutFolderLink(folderId, body);
        ((int)linkResponse.StatusCode).Should().Be(200);
        var sharedTo = await SharedLinkOf(linkResponse);
        var linkId = Guid.Parse(sharedTo.GetProperty("id").GetString()!);
        var token = sharedTo.GetProperty("requestToken").GetString()!;

        var withoutSession = await VisitorKeys(fileId, token, []);
        var session = (await SendProof(token, proof.Value)).Cookies;
        var beforeWrap = await VisitorKeys(fileId, token, session);

        await _filesClient.Authenticate(Owner);
        var ownerId = Owner.Id;
        (await WriteKeys(fileId, linkId)).Should().BeEmpty();
        var member = (await _sharingApi.GetEncryptionAccessAsync(fileId, TestContext.Current.CancellationToken)).Response;
        var afterWrap = await VisitorKeys(fileId, token, session);

        withoutSession.Status.Should().Be(HttpStatusCode.Forbidden);

        beforeWrap.Status.Should().Be(HttpStatusCode.OK);
        var notReady = LinkEntryOf(beforeWrap.Keys);
        notReady.GetProperty("userId").GetGuid().Should().Be(linkId);
        notReady.GetProperty("publicKey").GetString().Should().Be(body.LinkPublicKey);
        FileKeyEncOf(notReady).Should().BeNull("no member wrapped the key for the link yet");

        afterWrap.Status.Should().Be(HttpStatusCode.OK);
        FileKeyEncOf(LinkEntryOf(afterWrap.Keys)).Should().Be("wrapped");
        var memberKeys = afterWrap.Keys.EnumerateArray().Where(k => !k.TryGetProperty("isLink", out var isLink) || isLink.ValueKind != JsonValueKind.True).ToList();
        memberKeys.Should().Contain(k => k.GetProperty("userId").GetGuid() == ownerId, "the visitor finds the author of the wrap among the members");
        memberKeys.Select(HasPrivateHalf).Should().NotContain(true, "a visitor gets no member's private half");

        member.Should().Contain(k => k.UserId == ownerId && !string.IsNullOrEmpty(k.PrivateKeyEnc), "a member keeps the answer of a member");
    }

    [Fact]
    public async Task Visitor_IsRefusedTheKeysOfAFileTheLinkDoesNotOpen()
    {
        var proof = Proof.New();
        var room = await CreateRoomAsOwner();
        var linkedFolder = await CreateFolderIn(room.Id);
        var inside = await UploadEncryptedFile(linkedFolder, "inside.docx");
        var outside = await UploadEncryptedFile(room.Id, "outside.docx");
        using var response = await PutFolderLink(linkedFolder, new LinkBody { LinkProofHash = proof.Hash });
        var token = (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;
        var session = (await SendProof(token, proof.Value)).Cookies;

        var insideKeys = await VisitorKeys(inside, token, session);
        var outsideKeys = await VisitorKeys(outside, token, session);

        insideKeys.Status.Should().Be(HttpStatusCode.OK);
        outsideKeys.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SignedInVisitorWhoIsNoMember_GetsTheWrapOfTheLink_NotTheMemberAnswer()
    {
        var proof = Proof.New();
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var fileId = await UploadEncryptedFile(folderId, "inside.docx");
        using var linkResponse = await PutFolderLink(folderId, new LinkBody { LinkProofHash = proof.Hash });
        var sharedTo = await SharedLinkOf(linkResponse);
        var linkId = Guid.Parse(sharedTo.GetProperty("id").GetString()!);
        var token = sharedTo.GetProperty("requestToken").GetString()!;
        (await WriteKeys(fileId, linkId)).Should().BeEmpty();
        var visitor = await InviteWithKeys(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(visitor);
        var session = await OpenLinkSession(token, proof);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/2.0/files/file/{fileId}/publickeys");
        request.Headers.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", session));
        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        var keys = (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("response");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        FileKeyEncOf(LinkEntryOf(keys)).Should().Be("wrapped", "the visitor opens the file with the wrap made for the link");
        keys.EnumerateArray().Select(HasPrivateHalf).Should().NotContain(true, "a visitor gets no private half, not even their own");
    }

    /// <summary>Every text a cookie value holds in readable form: the value, and each base64 or base64url part of it decoded.</summary>
    private static IEnumerable<string> ReadableParts(string cookie)
    {
        var value = Uri.UnescapeDataString(cookie[(cookie.IndexOf('=') + 1)..]);
        yield return value;

        foreach (var part in Regex.Split(value, "[^A-Za-z0-9+/_=-]+").Where(p => p.Length >= 8))
        {
            var base64 = part.Replace('-', '+').Replace('_', '/').TrimEnd('=');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            byte[]? bytes = null;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                // Not base64: the part was already checked as text above.
            }

            if (bytes != null)
            {
                yield return Encoding.UTF8.GetString(bytes);
                yield return Convert.ToBase64String(bytes);
            }
        }
    }

    [Fact]
    public async Task TheSessionCookie_HoldsNoProofHash_InAnyReadableForm()
    {
        var (_, token, proof, _) = await FolderLinkWithProof();
        var hashBytes = Convert.FromBase64String(proof.Hash);

        var opened = await SendProof(token, proof.Value);

        opened.Status.Should().Be(ExternalShareStatus.Ok);
        opened.Cookies.Should().NotBeEmpty();
        foreach (var cookie in opened.Cookies)
        {
            foreach (var part in ReadableParts(cookie))
            {
                part.Should().NotContain(proof.Hash, "a cookie the visitor keeps must not carry what the proof is checked against");
                part.Should().NotContain(Convert.ToBase64String(hashBytes).TrimEnd('='));
                part.Should().NotContain(JsonSerializer.Serialize(proof.Hash).Trim('"'), "a JSON writer escapes + in the hash");
            }
        }
    }

    [Fact]
    public async Task AnAnonymousCallerWithoutALink_IsRefusedTheKeysOfAnyFile()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "private.docx");

        await _filesClient.Authenticate(null);
        foreach (var id in new[] { fileId, int.MaxValue })
        {
            using var response = await _filesClient.GetAsync($"api/2.0/files/file/{id}/publickeys", TestContext.Current.CancellationToken);
            ((int)response.StatusCode).Should().Be(403, $"file {id} is read with no link, so the caller has no right to ask");
        }
    }
}
