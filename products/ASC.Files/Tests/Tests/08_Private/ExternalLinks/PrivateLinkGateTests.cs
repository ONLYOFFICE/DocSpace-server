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
/// The gates that close the links of private items without deleting them: the portal setting
/// (<c>PUT files/settings/external/private</c>), the end-to-end encrypted rooms app, an archived room, and the room-wide
/// restriction on downloading of a private Virtual Data Room. While a gate is shut, setting a link returns 403 and a
/// stored one opens as an invalid link; once it opens again, the stored link is served as before.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkGateTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private sealed record Link(int RoomId, int FileId, string Token, Proof Proof);

    private async Task<Link> FileLinkIn(RoomType roomType = RoomType.CustomRoom)
    {
        var proof = Proof.New();
        var room = await CreateRoomAsOwner(roomType);
        var fileId = await UploadEncryptedFile(room.Id, "inside.docx");

        using var response = await PutFileLink(fileId, new LinkBody { LinkProofHash = proof.Hash });
        ((int)response.StatusCode).Should().Be(200);

        return new Link(room.Id, fileId, (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!, proof);
    }

    /// <summary>Sends the proof as a visitor: anonymous, or signed in, as a link of a private VDR asks.</summary>
    private async Task<ExternalShareStatus> SendProof(Link link, User? visitor = null)
    {
        await _filesClient.Authenticate(visitor);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/files/share/{Uri.EscapeDataString(link.Token)}/password")
        {
            Content = JsonContent.Create(new { proof = link.Proof.Value })
        };

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var status = (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("response").GetProperty("status");

        return status.ValueKind == JsonValueKind.Number
            ? (ExternalShareStatus)status.GetInt32()
            : Enum.Parse<ExternalShareStatus>(status.GetString()!, ignoreCase: true);
    }

    /// <summary>Sets another link on the file as the owner and returns the status code.</summary>
    private async Task<int> SetAnotherLink(int fileId)
    {
        await _filesClient.Authenticate(Owner);
        using var response = await PutFileLink(fileId, new LinkBody());

        return (int)response.StatusCode;
    }

    private async Task<HttpResponseMessage> SetSetting(bool enabled)
    {
        return await new RawApiClient(_filesClient).PutAsync("api/2.0/files/settings/external/private", new { set = enabled }, TestContext.Current.CancellationToken);
    }

    private async Task<bool> SettingInFilesSettings()
    {
        using var response = await new RawApiClient(_filesClient).GetAsync("api/2.0/files/settings", TestContext.Current.CancellationToken);

        return (await new RawApiClient(_filesClient).ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).GetProperty("privateRoomLinks").GetBoolean();
    }

    private async Task SetApp(bool enabled)
    {
        await _webApiClient.Authenticate(Owner);
        var apps = new DocSpace.API.SDK.Api.Apps.AppsApi(_webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
        await apps.SetEnabledAsync("e2e-rooms", new SetAppEnabledRequest(enabled), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TheSetting_ShutsTheLinks_AndOpensThemAgain()
    {
        var link = await FileLinkIn();

        await _filesClient.Authenticate(Owner);
        (await SettingInFilesSettings()).Should().BeTrue();
        using (var off = await SetSetting(false))
        {
            ((int)off.StatusCode).Should().Be(200);
        }

        (await SettingInFilesSettings()).Should().BeFalse();
        (await SetAnotherLink(link.FileId)).Should().Be(403);
        (await SendProof(link)).Should().Be(ExternalShareStatus.Invalid);

        await _filesClient.Authenticate(Owner);
        using (var on = await SetSetting(true))
        {
            ((int)on.StatusCode).Should().Be(200);
        }

        (await SendProof(link)).Should().Be(ExternalShareStatus.Ok);
    }

    [Fact]
    public async Task ARoomManagerWhoIsNoDocSpaceAdmin_CannotChangeTheSetting()
    {
        var manager = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);
        using var response = await SetSetting(false);

        ((int)response.StatusCode).Should().Be(403);
        await _filesClient.Authenticate(Owner);
        (await SettingInFilesSettings()).Should().BeTrue();
    }

    [Fact]
    public async Task TheApp_ShutsTheLinks_AndOpensThemAgain()
    {
        var link = await FileLinkIn();
        (await SendProof(link)).Should().Be(ExternalShareStatus.Ok);

        await SetApp(false);
        try
        {
            (await SendProof(link)).Should().Be(ExternalShareStatus.Invalid);
        }
        finally
        {
            await SetApp(true);
        }

        (await SendProof(link)).Should().Be(ExternalShareStatus.Ok);
    }

    [Fact]
    public async Task ThePrivateVdrRestriction_ShutsTheLinks_RefusesNewOnes_AndOpensThemAgain()
    {
        var visitor = await InviteWithKeys(EmployeeType.RoomAdmin);
        var link = await FileLinkIn(RoomType.VirtualDataRoom);
        (await SendProof(link, visitor)).Should().Be(ExternalShareStatus.Ok);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UpdateRoomAsync(link.RoomId, new UpdateRoomRequest(denyDownload: true), TestContext.Current.CancellationToken);

        (await SendProof(link, visitor)).Should().Be(ExternalShareStatus.Invalid);
        (await SetAnotherLink(link.FileId)).Should().Be(403);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UpdateRoomAsync(link.RoomId, new UpdateRoomRequest(denyDownload: false), TestContext.Current.CancellationToken);

        (await SendProof(link, visitor)).Should().Be(ExternalShareStatus.Ok);
    }

    [Fact]
    public async Task AnArchivedRoom_ShutsItsLinks_AndTheRestoreOpensThem()
    {
        var link = await FileLinkIn();
        (await SendProof(link)).Should().Be(ExternalShareStatus.Ok);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.ArchiveRoomAsync(link.RoomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);

        (await PollUntil(() => SendProof(link), s => s == ExternalShareStatus.Invalid)).Should().Be(ExternalShareStatus.Invalid);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UnarchiveRoomAsync(link.RoomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);

        (await PollUntil(() => SendProof(link), s => s == ExternalShareStatus.Ok)).Should().Be(ExternalShareStatus.Ok);
    }
}
