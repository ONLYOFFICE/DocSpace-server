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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// <c>GET /storage/{module}/root/{pathInfo}</c> (Web.Studio) requested with the anonymous session cookie a public
/// room link hands out. The handler used to accept that cookie for every storage module and resolved
/// <c>..\</c> segments of the path, so a visitor of any public link read arbitrary files of the server
/// (<c>/etc/passwd</c>, <c>appsettings.json</c> with the connection strings and the session key). Now the path
/// may not leave the module's root, and the cookie opens only the pictures a public room page shows: member
/// avatars and room logos. The storage handler is not part of the REST API, so these requests are raw.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Category", "Security")]
[Trait("Bug", "84541")]
public class RoomLinkStorageAccessTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    private const string AnonymousSessionCookie = "anonymous_session_key";

    /// <summary>
    /// A path that climbs out of the <c>userPhotos</c> root with <c>..\</c> segments must not be served - neither
    /// to a public link visitor nor to the portal owner. The backslashes are sent percent-encoded, as Kestrel
    /// decodes them into the route value just like the raw <c>\</c> of <c>curl --path-as-is</c>, while
    /// <see cref="Uri"/> would otherwise fold them into <c>/</c> and collapse the dot segments client-side.
    /// The absolute variant names the file by its full path instead (<c>C:\Windows\win.ini</c>): a drive-letter
    /// segment resets the base path in <see cref="Path.Combine(string[])"/>, and the request must be refused
    /// the same way, not end in a 400 from the storage's own containment check. That variant is Windows-only: the
    /// path is split on both separators, so no segment can carry one, and on Linux only a leading <c>/</c> is
    /// rooted - a segment like <c>C:</c> there is a plain directory name that cannot leave the storage root.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task GetStorageFile_PathOutsideModuleRoot_NotServed(bool anonymous, bool absolute)
    {
        // Arrange
        Assert.SkipWhen(absolute && !OperatingSystem.IsWindows(), "Only Windows has rooted path segments (drive letters).");

        if (anonymous)
        {
            await UsePublicRoomSessionAsync();
        }
        else
        {
            await _studioClient.Authenticate(Owner);
        }

        var url = "storage/userPhotos/root/" + (absolute ? OutsideFileAbsolute() : string.Concat(Enumerable.Repeat("..%5C", 20)) + OutsideFile());

        // Act
        using var response = await _studioClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// The traversal that matters most in a multi-tenant installation: from the visitor's <c>userPhotos</c> root
    /// (<c>$STORAGE_ROOT/Studio/{tenant}/userphotos</c>) five levels up to <c>$STORAGE_ROOT</c> and down into the
    /// stored content of a document (<c>Products/Files/{tenant}/folder_N/file_N/v1/content.docx</c>), which
    /// otherwise needs a signed, expiring link and a download right. Unlike the OS file of the cases above, the
    /// target is a real file of this very portal, so the case depends neither on the drive nor on the OS. Before
    /// the fix it came back with 200 and the document.
    /// </summary>
    [Fact]
    public async Task GetStorageFile_PathIntoFilesContent_NotServed()
    {
        // Arrange
        var (roomId, token) = await CreatePublicRoomLinkAsync();
        var file = await CreateFile("Autotest Storage Traversal.docx", roomId);
        await OpenLinkAnonymouslyAsync(roomId, token);

        // TenantPath.CreatePath: the tenant id as "00/00/00".
        var tenantPath = TenantId.ToString("00/00/00", CultureInfo.InvariantCulture).Replace("/", "%5C");
        var url = "storage/userPhotos/root/" + string.Concat(Enumerable.Repeat("..%5C", 5)) +
                  $"Products%5CFiles%5C{tenantPath}%5Cfolder_{(file.Id / 1000 + 1) * 1000}%5Cfile_{file.Id}%5Cv1%5Ccontent.docx";

        // Act
        using var response = await _studioClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// The anonymous session cookie opens only the modules a public room page shows. Outside them it must not
    /// pass the handler's authorization gate at all: the gate runs before the file lookup, so a cookie that still
    /// passed it would answer 404 for a missing file (and 200 for an existing one), while a closed module answers
    /// 403 whatever the path is. <c>files</c> and <c>migration</c> also require a signed, expiring link; a cookie
    /// that passed the gate reached that check without one and ended in a 400, never in the 403 of a closed module.
    /// </summary>
    [Theory]
    [InlineData("webplugins")]
    [InlineData("customnavigation")]
    [InlineData("files")]
    [InlineData("migration")]
    public async Task GetStorageFile_OtherModuleWithAnonymousSession_Forbidden(string module)
    {
        // Arrange
        await UsePublicRoomSessionAsync();

        // Act
        using var response = await _studioClient.GetAsync($"storage/{module}/root/autotest/{Guid.NewGuid():N}.json", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// What the anonymous session is for: the logo of a room is shown to a visitor of its public link
    /// (bug 77628), while without the session the logo stays closed.
    /// </summary>
    [Fact]
    public async Task GetStorageFile_RoomLogo_ServedOnlyWithAnonymousSession()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Storage Access Logo");

        var tmpFile = await UploadLogo(CreateTestImageBytes());
        var logo = (await CreateLogo(room.Id, tmpFile)).Logo.Original;
        var url = PathOf(logo);
        url.Should().Contain("/storage/room_logos/root/");

        var token = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink.RequestToken;

        using (var withoutSession = await _studioClient.GetAsync(url, TestContext.Current.CancellationToken))
        {
            withoutSession.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        await OpenLinkAnonymouslyAsync(room.Id, token);

        // Act
        using var response = await _studioClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    /// <summary>The server returns storage urls either absolute or relative; the Web.Studio client needs the path.</summary>
    private static string PathOf(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme.StartsWith("http", StringComparison.Ordinal)
            ? absolute.PathAndQuery
            : url;
    }

    /// <summary>A file every host has, addressed from the file system root.</summary>
    private static string OutsideFile()
    {
        return OperatingSystem.IsWindows()
            ? Path.GetRelativePath(Path.GetPathRoot(Environment.SystemDirectory)!, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "win.ini")).Replace("\\", "%5C")
            : "etc%5Cpasswd";
    }

    /// <summary>The same file by its absolute path (Windows only), with the separators percent-encoded.</summary>
    private static string OutsideFileAbsolute()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "win.ini").Replace("\\", "%5C");
    }

    private async Task UsePublicRoomSessionAsync()
    {
        var (roomId, token) = await CreatePublicRoomLinkAsync();
        await OpenLinkAnonymouslyAsync(roomId, token);
    }

    private async Task<(int RoomId, string Token)> CreatePublicRoomLinkAsync()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Storage Access");
        var token = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink.RequestToken;

        return (room.Id, token);
    }

    /// <summary>
    /// Opens the link the way the browser does, which hands out the anonymous session cookie, and puts that cookie
    /// on the Web.Studio client. <see cref="_filesClient"/> is left signed out.
    /// </summary>
    private async Task OpenLinkAnonymouslyAsync(int roomId, string token)
    {
        await _filesClient.Authenticate(null);

        var opened = await _sharingApi.GetExternalShareDataWithHttpInfoAsync(token, folderId: roomId.ToString(), cancellationToken: TestContext.Current.CancellationToken);
        var sessionCookie = opened.Headers.TryGetValue("Set-Cookie", out var cookies)
            ? cookies.Select(c => c.Split(';')[0]).FirstOrDefault(c => c.StartsWith(AnonymousSessionCookie + "=", StringComparison.Ordinal))
            : null;
        sessionCookie.Should().NotBeNull("opening the link hands an anonymous visitor their session cookie");

        _studioClient.DefaultRequestHeaders.Remove("Cookie");
        _studioClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", sessionCookie);
    }
}
