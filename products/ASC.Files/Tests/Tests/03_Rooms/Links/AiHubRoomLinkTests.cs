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
/// The rooms of the AI rooms section (Instructions, Knowledge, AI project) accept no shared link: the
/// role table of those room types has no external-link subject, so a room link and a link on a file
/// inside are refused with "The role is not available for this user type". A Knowledge room needs
/// vectorization, which the integration-test stack does not configure, so it is not covered here; an
/// Instructions room holds only Markdown files.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubRoomLinkTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string RoleNotAvailable = "The role is not available for this user type";

    public static TheoryData<RoomType> AiHubRoomTypes =>
    [
        RoomType.InstructionsRoom, RoomType.AiProjectRoom
    ];

    public static TheoryData<RoomType> AiHubRoomTypesWithFiles =>
    [
        RoomType.InstructionsRoom
    ];

    [Theory]
    [MemberData(nameof(AiHubRoomTypes))]
    public async Task SetRoomLink_AiHubRoom_RefusedRoleNotAvailable(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom(new CreateRoomRequestDto($"Autotest {roomType} Link", roomType: roomType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(access: FileShare.Read, title: "Autotest Link", linkType: LinkType.External),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain(RoleNotAvailable);
    }

    [Theory]
    [MemberData(nameof(AiHubRoomTypesWithFiles))]
    public async Task SetFileExternalLink_FileInAiHubRoom_RefusedRoleNotAvailable(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom(new CreateRoomRequestDto($"Autotest {roomType} File Link", roomType: roomType));
        // An Instructions room accepts only a skill file: Markdown with a name/description front matter
        var content = Encoding.UTF8.GetBytes("---\nname: autotest-skill\ndescription: Autotest skill for the link check\n---\nAnswer briefly.\n");
        var uploaded = (await _foldersApi.UploadFileAsync(
            room.Id,
            file: new FileParameter("autotest-skill.md", "text/markdown", new MemoryStream(content)),
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var fileId = uploaded.Single().Id;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(
                fileId,
                new FileLinkRequest(access: FileShare.Read, primary: false, title: "Autotest Link"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain(RoleNotAvailable);
    }
}
