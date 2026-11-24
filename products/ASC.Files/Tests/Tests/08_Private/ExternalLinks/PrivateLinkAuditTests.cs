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
/// The links of private items are on record in the audit trail like other links: created, and opened with a proof,
/// once per link session (action 5166). Neither record names the private room by its title. The events are
/// read raw: the SDK's <c>MessageAction</c> does not know the new action yet.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkAuditTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private const int FileExternalLinkCreated = 5091;
    private const int PrivateLinkOpened = 5166;

    private async Task<List<JsonElement>> LastEvents()
    {
        await _webApiClient.Authenticate(Owner);
        var raw = new RawApiClient(_webApiClient);
        using var response = await raw.GetAsync("api/2.0/security/audit/events/last", TestContext.Current.CancellationToken);
        ((int)response.StatusCode).Should().Be(200);

        return [.. (await raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).EnumerateArray()];
    }

    private static int ActionIdOf(JsonElement e) => e.GetProperty("actionId").ValueKind == JsonValueKind.Number
        ? e.GetProperty("actionId").GetInt32()
        : -1;

    [Fact]
    public async Task TheLinkIsOnRecordWhenCreatedAndOpened_OnceForTheSession_WithoutTheTitles()
    {
        var proof = Proof.New();
        var linkTitle = $"Audit link {Guid.NewGuid():N}";
        var room = await CreateRoomAsOwner();
        var file = await UploadEncryptedFileInfo(room.Id, "audited.docx");

        await _filesClient.Authenticate(Owner);
        string token;
        using (var response = await PutFileLink(file.Id, new LinkBody { Title = linkTitle, LinkProofHash = proof.Hash }))
        {
            ((int)response.StatusCode).Should().Be(200);
            token = (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;
        }

        await _filesClient.Authenticate(null);
        var session = await OpenLinkSession(token, proof);

        // Reading through the open session is no new opening.
        using (var read = await SendOnLink(HttpMethod.Get, $"api/2.0/files/file/{file.Id}", token, session))
        {
            ((int)read.StatusCode).Should().Be(200);
        }

        var events = await PollUntil(LastEvents, e => e.Any(x => ActionIdOf(x) == PrivateLinkOpened && x.GetProperty("action").GetString()!.Contains(linkTitle)));

        var ofThisLink = events.Where(e => e.GetProperty("action").GetString()!.Contains(linkTitle)).ToList();
        ofThisLink.Count(e => ActionIdOf(e) == FileExternalLinkCreated).Should().Be(1);
        ofThisLink.Count(e => ActionIdOf(e) == PrivateLinkOpened).Should().Be(1);

        // The room is named "Private room"; a file of a private room carries a generated title, which says nothing.
        foreach (var e in ofThisLink)
        {
            e.GetRawText().Should().NotContain(room.Title);
            e.GetProperty("context").GetString().Should().Contain("Private room");
        }
    }
}
