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
/// The proof a visitor sends, the hash a creator stores, the sealed key of a link and the sealed private key of a user's
/// key pair never reach the service's logs:
/// neither the main log, nor the SQL log that records the values of the parameters it binds, nor the ASP.NET log. The
/// tests send them on the paths that write the most, a refused request included, then read the log files of the run.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkLogTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task PostProof(string token, string proof)
    {
        await _filesClient.Authenticate(null);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/files/share/{Uri.EscapeDataString(token)}/password")
        {
            Content = JsonContent.Create(new { proof })
        };

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        ((int)response.StatusCode).Should().Be(200);
    }

    /// <summary>The value as it is written, and as a JSON writer escapes it (<c>+</c> is <c>+</c>).</summary>
    private static IEnumerable<(string Name, string Text)> Spellings(string name, string value) =>
        new[] { value, JsonSerializer.Serialize(value).Trim('"') }.Distinct().Select(text => (name, text));

    [Fact]
    public async Task TheProof_TheHash_AndTheSealedKeys_NeverReachTheLogs_RefusedRequestsIncluded()
    {
        var since = DateTime.Now.AddSeconds(-1);
        var proof = Proof.New();
        var wrongProof = Proof.New();

        // A user's own key pair is kept in account_links: its sealed private half is written there too.
        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        var memberKeys = await SetFakeKeys(publicKeyPrefix: "member");

        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        await UploadEncryptedFile(folderId, "inside.docx");

        var stored = new LinkBody { LinkProofHash = proof.Hash };
        string token;
        using (var response = await PutFolderLink(folderId, stored))
        {
            ((int)response.StatusCode).Should().Be(200);
            token = (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;
        }

        // Refused with 400 after the body was read: the public key has the wrong size.
        var refused = new LinkBody { LinkPublicKey = RandomBase64(8) };
        using (var response = await PutFolderLink(folderId, refused))
        {
            ((int)response.StatusCode).Should().Be(400);
        }

        await PostProof(token, wrongProof.Value);
        await PostProof(token, proof.Value);

        // Written after everything above: once it is on disk, so are their lines.
        var marker = $"Log marker {Guid.NewGuid():N}";
        await _filesClient.Authenticate(Owner);
        await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder(marker), TestContext.Current.CancellationToken);

        var secrets = Spellings("proof", proof.Value)
            .Concat(Spellings("wrong proof", wrongProof.Value))
            .Concat(Spellings("linkProofHash", proof.Hash))
            .Concat(Spellings("linkPrivateKeyEnc", stored.LinkPrivateKeyEnc!))
            .Concat(Spellings("linkPrivateKeyEnc of the refused request", refused.LinkPrivateKeyEnc!))
            .Concat(Spellings("privateKeyEnc of a user's key pair", memberKeys.PrivateKeyEnc))
            .ToList();

        var found = await ServiceLogs.FindAfterAsync("files", since, marker, [.. secrets.Select(s => s.Text)]);

        secrets.Where(s => found.Contains(s.Text)).Select(s => s.Name).Should().BeEmpty();
    }
}
