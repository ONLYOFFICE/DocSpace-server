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
/// What a link of a private item announces goes to members only: a DocSpace admin outside the room, subscribed to every
/// webhook, gets no delivery about the link being set, opened, read through or revoked. The journal of the
/// subscription is read once it holds the delivery of an action the admin takes afterwards, so a delivery about the
/// link, written earlier, would be there too.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkEventTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<long> Deliveries(int configId, string text)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM webhooks_logs WHERE tenant_id = @tenant AND config_id = @config AND request_payload LIKE CONCAT('%', @text, '%');";
        command.Parameters.AddWithValue("@tenant", TenantId);
        command.Parameters.AddWithValue("@config", configId);
        command.Parameters.AddWithValue("@text", text);

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task ADocSpaceAdminOutsideTheRoom_GetsNoWebhookAboutTheLink()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var configId = await SubscribeToWebhook(WebhookTrigger.All, admin);

        var proof = Proof.New();
        var room = await CreateRoomAsOwner();
        var file = await UploadEncryptedFileInfo(room.Id, "announced.docx");

        await _filesClient.Authenticate(Owner);
        Guid linkId;
        string token;
        using (var response = await PutFileLink(file.Id, new LinkBody { LinkProofHash = proof.Hash }))
        {
            ((int)response.StatusCode).Should().Be(200);
            var link = await SharedLinkOf(response);
            linkId = Guid.Parse(link.GetProperty("id").GetString()!);
            token = link.GetProperty("requestToken").GetString()!;
        }

        await _filesClient.Authenticate(null);
        var session = await OpenLinkSession(token, proof);
        using (var read = await SendOnLink(HttpMethod.Get, $"api/2.0/files/file/{file.Id}", token, session))
        {
            ((int)read.StatusCode).Should().Be(200);
        }

        await _filesClient.Authenticate(Owner);
        await RevokeFileLink(file.Id, linkId);

        // An action of the admin's own, which the subscription is delivered.
        var marker = $"Autotest webhook marker {Guid.NewGuid():N}.docx";
        await _filesClient.Authenticate(admin);
        await CreateFile(marker, FolderType.USER, admin);

        (await PollUntil(() => Deliveries(configId, marker), count => count > 0, TimeSpan.FromSeconds(30))).Should().BeGreaterThan(0);
        (await Deliveries(configId, linkId.ToString())).Should().Be(0);
        (await Deliveries(configId, file.Title)).Should().Be(0);
    }
}
