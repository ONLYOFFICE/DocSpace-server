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
/// External links of items in private rooms. The generated SDK does not carry the key fields of such a link yet, so the
/// link operations go through <see cref="RawApiClient"/> and read the answer as JSON; reads that the SDK models cover
/// go through the SDK.
/// </summary>
public abstract class PrivateLinkTestBase(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    /// <summary>The body of a link request, with the key fields a creator's browser would send.</summary>
    protected sealed class LinkBody
    {
        public Guid? LinkId { get; set; }
        public int Access { get; set; } = (int)FileShare.Read;
        public string? Title { get; set; } = "Private link";
        public string? ExpirationDate { get; set; }
        public bool? DenyDownload { get; set; }
        public bool? Internal { get; set; }
        public bool? Primary { get; set; }
        public string? Password { get; set; }
        public int? LinkType { get; set; }
        public string? LinkPublicKey { get; set; } = RandomBase64(32);
        public string? LinkPrivateKeyEnc { get; set; } = RandomBase64(96);
        public string? LinkProofHash { get; set; } = RandomBase64(32);
        public bool? PasswordRequired { get; set; } = true;
        public SecretWrap[]? LinkSecretWraps { get; set; }

        public LinkBody WithoutKeys()
        {
            LinkPublicKey = null;
            LinkPrivateKeyEnc = null;
            LinkProofHash = null;

            return this;
        }
    }

    /// <summary>A wrap of a link's secret for one user who manages the room's links.</summary>
    protected sealed record SecretWrap(Guid UserId, Guid PublicKeyId, string Wrap)
    {
        public static SecretWrap For(Guid userId) => new(userId, Guid.NewGuid(), RandomBase64(80));
    }

    /// <summary>The links of a room with their key fields, read raw: the SDK model of a link has no key fields yet.</summary>
    protected async Task<List<JsonElement>> RoomLinksJson(int roomId)
    {
        using var response = await Raw.GetAsync($"api/2.0/files/rooms/{roomId}/links", TestContext.Current.CancellationToken);
        ((int)response.StatusCode).Should().Be(200);

        return [.. (await Raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).EnumerateArray().Select(l => l.GetProperty("sharedTo"))];
    }

    /// <summary>A link secret's proof and what the creator's browser stores for it.</summary>
    protected sealed record Proof(string Value, string Hash)
    {
        public static Proof New()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return new Proof(Convert.ToBase64String(bytes), Convert.ToBase64String(SHA256.HashData(bytes)));
        }
    }

    /// <summary>
    /// Sends the proof on the caller's current identity and returns the cookies of the link session it opens, as a
    /// browser keeps them; the test client keeps none.
    /// </summary>
    protected async Task<string[]> OpenLinkSession(string token, Proof proof)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/files/share/{Uri.EscapeDataString(token)}/password")
        {
            Content = JsonContent.Create(new { proof = proof.Value })
        };

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return response.Headers.TryGetValues("Set-Cookie", out var values) ? [.. values.Select(v => v.Split(';')[0])] : [];
    }

    private static readonly JsonSerializerOptions _withoutNulls = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>Sends a request on the caller's current identity, on a link token and with the cookies of its session.</summary>
    protected async Task<HttpResponseMessage> SendOnLink(HttpMethod method, string path, string token, IEnumerable<string> cookies, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        if (body != null)
        {
            request.Content = JsonContent.Create(body, options: _withoutNulls);
        }

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    protected static string RandomBase64(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    protected static string InAWeek() => DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private RawApiClient Raw => new(_filesClient);

    protected Task<HttpResponseMessage> PutFileLink(int fileId, LinkBody body) =>
        Raw.PutAsync($"api/2.0/files/file/{fileId}/links", body, TestContext.Current.CancellationToken);

    protected Task<HttpResponseMessage> PostFilePrimaryLink(int fileId, LinkBody body) =>
        Raw.PostAsync($"api/2.0/files/file/{fileId}/link", body, TestContext.Current.CancellationToken);

    protected Task<HttpResponseMessage> PutFolderLink(int folderId, LinkBody body) =>
        Raw.PutAsync($"api/2.0/files/folder/{folderId}/links", body, TestContext.Current.CancellationToken);

    protected Task<HttpResponseMessage> PostFolderPrimaryLink(int folderId, LinkBody body) =>
        Raw.PostAsync($"api/2.0/files/folder/{folderId}/link", body, TestContext.Current.CancellationToken);

    protected Task<HttpResponseMessage> PutRoomLink(int roomId, LinkBody body)
    {
        body.LinkType = (int)LinkType.External;

        return Raw.PutAsync($"api/2.0/files/rooms/{roomId}/links", body, TestContext.Current.CancellationToken);
    }

    /// <summary>The <c>sharedTo</c> object of a link answer.</summary>
    protected async Task<JsonElement> SharedLinkOf(HttpResponseMessage response)
    {
        var answer = await Raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        return answer.GetProperty("sharedTo");
    }

    /// <summary>Sets a link with key material on a file and returns its id.</summary>
    protected async Task<Guid> StoreFileLink(int fileId, LinkBody? body = null)
    {
        using var response = await PutFileLink(fileId, body ?? new LinkBody());
        ((int)response.StatusCode).Should().Be(200);

        return Guid.Parse((await SharedLinkOf(response)).GetProperty("id").GetString()!);
    }

    /// <summary>Sets a link with key material on a folder and returns its id.</summary>
    protected async Task<Guid> StoreFolderLink(int folderId, LinkBody? body = null)
    {
        using var response = await PutFolderLink(folderId, body ?? new LinkBody());
        ((int)response.StatusCode).Should().Be(200);

        return Guid.Parse((await SharedLinkOf(response)).GetProperty("id").GetString()!);
    }

    /// <summary>A revocation needs no key field, so it goes through the SDK.</summary>
    protected Task RevokeFileLink(int fileId, Guid linkId) =>
        _filesApi.SetFileExternalLinkAsync(fileId, new FileLinkRequest(linkId, FileShare.None), TestContext.Current.CancellationToken);

    /// <summary>A revocation needs no key field, so it goes through the SDK.</summary>
    protected Task RevokeFolderLink(int folderId, Guid linkId) =>
        _foldersApi.SetFolderPrimaryExternalLinkAsync(folderId, new FolderLinkRequest(linkId, FileShare.None), TestContext.Current.CancellationToken);

    /// <summary>The links of a file as JSON: the SDK model of a link has no key fields yet.</summary>
    protected async Task<JsonElement> FileLinksJson(int fileId)
    {
        using var response = await Raw.GetAsync($"api/2.0/files/file/{fileId}/links", TestContext.Current.CancellationToken);

        return await Raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    /// <summary>The total the listing of a file's links reports, which the list itself does not carry.</summary>
    protected async Task<long> FileLinkTotal(int fileId) =>
        (await _filesApi.GetFileLinksAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Total;

    protected async Task<int> FileLinkCount(int fileId) =>
        (await _filesApi.GetFileLinksAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Count;

    protected async Task<int> FolderLinkCount(int folderId) =>
        (await _foldersApi.GetFolderLinksAsync(folderId, TestContext.Current.CancellationToken)).Response.Count;

    protected async Task<int> RoomLinkCount(int roomId) =>
        (await _roomsApi.GetRoomLinksAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response.Count;

    protected async Task<FolderDto> CreateRoomAsOwner(RoomType roomType = RoomType.CustomRoom)
    {
        await _filesClient.Authenticate(Owner);

        return await CreatePrivateRoom("Autotest Private Links", roomType);
    }

    protected async Task<int> CreateFolderIn(int parentId) =>
        (await _foldersApi.CreateFolderAsync(parentId, new CreateFolder("Linked folder"), TestContext.Current.CancellationToken)).Response.Id;
}
