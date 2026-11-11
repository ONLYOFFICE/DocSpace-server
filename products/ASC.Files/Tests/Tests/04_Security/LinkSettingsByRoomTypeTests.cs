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

namespace ASC.Files.Tests.Tests._04_Security;

/// <summary>
/// Which link settings each room type offers: a password on a Public room link, the lifetime of a
/// Public room's primary link versus its additional links and the links of its entries, every setting
/// on the entry links of a Collaboration room and of a VDR, and the sign-in a VDR link demands.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "LinkSettings")]
public class LinkSettingsByRoomTypeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task PublicRoomLink_WithPassword_AnonymousVisitorAskedForPassword_RightPasswordOpens()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Password");
        var password = NewPassword();

        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, title: "Password Link", linkType: LinkType.External, password: password),
            TestContext.Current.CancellationToken)).Response;
        var token = link.SharedLink.RequestToken;

        // Act
        await _filesClient.Authenticate(null);
        var withoutPassword = (await _sharingApi.GetExternalShareDataAsync(token, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var withPassword = (await _sharingApi.ApplyExternalSharePasswordAsync(
            token,
            new ExternalShareRequestParam { Password = password },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        withoutPassword.Status.Should().Be(ExternalShareStatus.RequiredPassword);
        withPassword.Status.Should().Be(ExternalShareStatus.Ok);
    }

    [Fact]
    public async Task PublicRoomPrimaryLink_ExpirationWithAccessKept_IsNotStored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Primary Lifetime");
        var primary = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Act - the access is sent as it is, so this is an update and not a revocation
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(
                linkId: primary.SharedLink.Id,
                access: primary.Access,
                title: primary.SharedLink.Title,
                linkType: LinkType.External,
                expirationDate: Future()),
            TestContext.Current.CancellationToken);

        // Assert
        var after = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.SharedLink.Id.Should().Be(primary.SharedLink.Id);
        after.SharedLink.ExpirationDate.Should().BeNull();
    }

    [Fact]
    public async Task PublicRoomAdditionalLink_Expiration_IsStored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Additional Lifetime");

        // Act
        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, title: "Expiring Link", linkType: LinkType.External, expirationDate: Future()),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Primary.Should().BeFalse();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public async Task PublicRoomFilePrimaryLink_ExpirationWithAccessKept_IsStored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Entry Lifetime");
        var file = await CreateFile("Autotest Public Entry Lifetime.docx", room.Id);
        var primary = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(linkId: primary.SharedLink.Id, access: primary.Access, primary: true, expirationDate: Future()),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var stored = links.Single(l => l.SharedLink.Id == primary.SharedLink.Id);
        stored.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CollaborationRoomFileLink_EverySettingStored(bool isInternal)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCollaborationRoom("Autotest Collaboration Link Settings");
        var file = await CreateFile("Autotest Collaboration Link Settings.docx", room.Id);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(
                access: FileShare.Editing,
                primary: false,
                title: "All Settings",
                @internal: isInternal,
                denyDownload: true,
                password: NewPassword(),
                expirationDate: Future()),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Access.Should().Be(FileShare.Editing);
        link.SharedLink.Internal.Should().Be(isInternal);
        link.SharedLink.DenyDownload.Should().BeTrue();
        link.SharedLink.Password.Should().NotBeNullOrEmpty();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Editing)]
    public async Task VdrFileLink_AccessPasswordDenyDownloadLifetimeStored_AudienceForcedInternal(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest VDR Link Settings");
        var file = await CreateFile("Autotest VDR Link Settings.docx", room.Id);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(
                access: access,
                primary: false,
                title: "VDR Settings",
                @internal: false,
                denyDownload: true,
                password: NewPassword(),
                expirationDate: Future()),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Access.Should().Be(access);
        link.SharedLink.Internal.Should().BeTrue();
        link.SharedLink.DenyDownload.Should().BeTrue();
        link.SharedLink.Password.Should().NotBeNullOrEmpty();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public async Task VdrFileLink_AnonymousVisitorRefused_SignedInUserOpens()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest VDR Link Sign In");
        var file = await CreateFile("Autotest VDR Link Sign In.docx", room.Id);
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var token = link.SharedLink.RequestToken;
        var user = await InviteContact(EmployeeType.User);

        // Act - anonymous
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);
        var anonymous = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Act - signed in, same token
        await _filesClient.Authenticate(user);
        var signedIn = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        link.SharedLink.Internal.Should().BeTrue();
        anonymous.ErrorCode.Should().Be(403);
        signedIn.Title.Should().Be(file.Title);
    }

    private static ApiDateTime Future()
    {
        return new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(3) };
    }

    private static string NewPassword()
    {
        return $"Lnk{Guid.NewGuid():N}"[..19];
    }
}
