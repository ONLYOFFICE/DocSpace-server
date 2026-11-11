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

namespace ASC.Files.Tests.Tests._01_Files.Links;

/// <summary>
/// PUT /files/file/{id}/links — how many additional external links a file accepts, by where the file
/// lives (<c>FileSecurity</c> link-count table of entries: 6 in My documents, Custom, Collaboration and
/// VDR rooms; 5 in a Public room; none in a Form filling room), the refusal message, and the
/// independence of a file's count from its room's own links.
/// </summary>
[Trait("Category", "Files")]
[Trait("Feature", "LinkLimits")]
public class FileLinkLimitTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string MaxLinksMessage = "The maximum number of links may not exceed";

    /// <summary>The locations whose files accept six additional links; <c>USER</c> is My documents.</summary>
    public static TheoryData<FolderType> SixLinkLocations =>
    [
        FolderType.USER, FolderType.CustomRoom, FolderType.EditingRoom, FolderType.VirtualDataRoom
    ];

    [Theory]
    [MemberData(nameof(SixLinkLocations))]
    public async Task SetFileExternalLink_SixAdditionalLinksAccepted_SeventhRefusedWithLimitSix(FolderType location)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileAt(location, "Autotest Six Links.docx");

        for (var i = 1; i <= 6; i++)
        {
            await _filesApi.SetFileExternalLinkAsync(file.Id, AdditionalLink(FileShare.Read, $"Link {i}"), TestContext.Current.CancellationToken);
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(file.Id, AdditionalLink(FileShare.Read, "Link 7"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain($"{MaxLinksMessage} 6");

        var links = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        links.Count(l => l.SharedLink is { Primary: false }).Should().Be(6);
    }

    [Fact]
    public async Task SetFileExternalLink_PublicRoomFile_FiveAdditionalLinksAccepted_SixthRefusedWithLimitFive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room File Links");
        var file = await CreateFile("Autotest Public Five Links.docx", room.Id);

        for (var i = 1; i <= 5; i++)
        {
            await _filesApi.SetFileExternalLinkAsync(file.Id, AdditionalLink(FileShare.Read, $"Link {i}"), TestContext.Current.CancellationToken);
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(file.Id, AdditionalLink(FileShare.Read, "Link 6"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain($"{MaxLinksMessage} 5");
    }

    [Fact]
    public async Task SetFileExternalLink_FormFillingRoomFile_AdditionalLinkRefusedWithLimitZero()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest FFR File Links");
        var file = await CreateFile("Autotest FFR Additional Link.pdf", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(file.Id, AdditionalLink(FileShare.FillForms, "Additional"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain($"{MaxLinksMessage} 0");
    }

    [Fact]
    public async Task SetFileExternalLink_CustomRoomWithAllSixRoomLinks_FileInsideStillAcceptsLink()
    {
        // Arrange - the room holds its primary link and its five additional ones, the most it may have
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Full Room Links");
        var file = await CreateFile("Autotest File In Full Room.docx", room.Id);

        await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken);

        for (var i = 1; i <= 5; i++)
        {
            await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(access: FileShare.Read, title: $"Room Link {i}", linkType: LinkType.External), TestContext.Current.CancellationToken);
        }

        var roomLimit = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(access: FileShare.Read, title: "Room Link 6", linkType: LinkType.External), TestContext.Current.CancellationToken));
        roomLimit.ErrorCode.Should().Be(403);

        // Act
        var fileLink = (await _filesApi.SetFileExternalLinkAsync(file.Id, AdditionalLink(FileShare.Read, "File Link"), TestContext.Current.CancellationToken)).Response;

        // Assert
        fileLink.SubjectType.Should().Be(SubjectType.ExternalLink);
        fileLink.SharedLink.Id.Should().NotBeEmpty();
    }

    private static FileLinkRequest AdditionalLink(FileShare access, string title)
    {
        return new FileLinkRequest(access: access, primary: false, title: title);
    }

    private async Task<FileDto> CreateFileAt(FolderType location, string title)
    {
        if (location == FolderType.USER)
        {
            return await CreateFileInMy(title, Owner);
        }

        var room = location switch
        {
            FolderType.CustomRoom => await CreateCustomRoom("Autotest Custom File Links"),
            FolderType.EditingRoom => await CreateCollaborationRoom("Autotest Collaboration File Links"),
            _ => await CreateVDRRoom("Autotest VDR File Links")
        };

        return await CreateFile(title, room.Id);
    }
}
