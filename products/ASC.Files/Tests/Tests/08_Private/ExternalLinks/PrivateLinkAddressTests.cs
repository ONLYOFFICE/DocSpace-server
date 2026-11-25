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
/// The address of a link of a private item is the visitor's page, <c>share/private</c>, built by the server without a
/// fragment: the creator's page appends the secret after <c>#</c>, and the browser keeps it across the redirect of the
/// short address, whose <c>Location</c> has none. An unknown short address sends the browser to the portal root with an
/// empty fragment, which drops the one it carried.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkAddressTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private sealed record Redirect(string Location, string StoredLink);

    /// <summary>Opens a short address without following it, and reads the address its row holds.</summary>
    private async Task<Redirect> OpenShort(string shareLink)
    {
        var uri = new Uri(shareLink);
        uri.AbsolutePath.Should().MatchRegex("^/s/[A-Za-z0-9_-]+$");

        var client = CreateClientWithoutRedirects(ResourceNames.WebApi);
        using var response = await client.GetAsync(uri.PathAndQuery, TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(302);

        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT link FROM short_links WHERE tenant_id = @tenant AND short = @short;";
        command.Parameters.AddWithValue("@tenant", TenantId);
        command.Parameters.AddWithValue("@short", uri.AbsolutePath["/s/".Length..]);
        var stored = (string?)await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        return new Redirect(response.Headers.Location!.OriginalString, stored!);
    }

    private static void ShouldBeTheVisitorsPage(string address, string token, int? folderId)
    {
        address.Should().NotContain("#");

        var uri = new Uri(address);
        uri.AbsolutePath.Should().Be("/share/private");

        var query = HttpUtility.ParseQueryString(uri.Query);
        query.AllKeys.Should().BeEquivalentTo(folderId == null ? ["key"] : new[] { "key", "folder" });
        query["key"].Should().Be(token);
        if (folderId != null)
        {
            query["folder"].Should().Be(folderId.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    [Theory]
    [InlineData("contract.docx")]
    [InlineData("photo.png")]
    [InlineData("archive.zip")]
    public async Task FileLink_IsTheVisitorsPage_WithoutAFragment_InTheAnswer_TheListing_AndTheShortAddress(string title)
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, title);

        string shareLink, token;
        using (var response = await PutFileLink(fileId, new LinkBody()))
        {
            ((int)response.StatusCode).Should().Be(200);
            var link = await SharedLinkOf(response);
            shareLink = link.GetProperty("shareLink").GetString()!;
            token = link.GetProperty("requestToken").GetString()!;
        }

        var listed = (await FileLinksJson(fileId)).EnumerateArray()
            .Select(l => l.GetProperty("sharedTo"))
            .Single(l => l.GetProperty("requestToken").GetString() == token)
            .GetProperty("shareLink").GetString()!;

        var redirect = await OpenShort(shareLink);

        shareLink.Should().NotContain("#");
        listed.Should().Be(shareLink);
        redirect.StoredLink.Should().Be(redirect.Location);
        ShouldBeTheVisitorsPage(redirect.Location, token, folderId: null);
    }

    [Fact]
    public async Task FolderLink_IsTheVisitorsPage_WithTheFolder()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);

        using var response = await PutFolderLink(folderId, new LinkBody());
        ((int)response.StatusCode).Should().Be(200);
        var link = await SharedLinkOf(response);

        var redirect = await OpenShort(link.GetProperty("shareLink").GetString()!);

        ShouldBeTheVisitorsPage(redirect.Location, link.GetProperty("requestToken").GetString()!, folderId);
    }

    [Fact]
    public async Task RoomLink_IsTheVisitorsPage()
    {
        var room = await CreateRoomAsOwner();

        using var response = await PutRoomLink(room.Id, new LinkBody());
        ((int)response.StatusCode).Should().Be(200);
        var link = await SharedLinkOf(response);

        var redirect = await OpenShort(link.GetProperty("shareLink").GetString()!);

        ShouldBeTheVisitorsPage(redirect.Location, link.GetProperty("requestToken").GetString()!, folderId: null);
    }

    [Fact]
    public async Task OrdinaryLink_KeepsItsAddress()
    {
        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("Autotest ordinary link", roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;
        var file = await CreateFile("ordinary.docx", room.Id);

        var link = (await _filesApi.SetFileExternalLinkAsync(file.Id, new FileLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        var redirect = await OpenShort(link.SharedLink.ShareLink);

        new Uri(redirect.Location).AbsolutePath.Should().NotBe("/share/private");
        HttpUtility.ParseQueryString(new Uri(redirect.Location).Query)["share"].Should().Be(link.SharedLink.RequestToken);
    }

    [Fact]
    public async Task UnknownShortAddress_RedirectsToTheRoot_WithAnEmptyFragment()
    {
        var client = CreateClientWithoutRedirects(ResourceNames.WebApi);

        using var response = await client.GetAsync("/s/zzzzzzzzzz", TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(302);
        var location = response.Headers.Location!.OriginalString;
        location.Should().EndWith("/#");
        new Uri(location).AbsolutePath.Should().Be("/");
    }

}
