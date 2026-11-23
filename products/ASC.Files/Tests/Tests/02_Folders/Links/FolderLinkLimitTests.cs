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

namespace ASC.Files.Tests.Tests._02_Folders.Links;

/// <summary>
/// PUT /files/folder/{id}/links with <c>primary:false</c> — how many additional external links a folder
/// inside a room accepts: 5 in a Public room, none in a Form filling room.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "LinkLimits")]
public class FolderLinkLimitTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string MaxLinksMessage = "The maximum number of links may not exceed";

    [Fact]
    public async Task SetFolderLink_PublicRoomFolder_FiveAdditionalLinksAccepted_SixthRefusedWithLimitFive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Folder Links");
        var folder = await CreateFolder("Autotest Public Folder", room.Id);

        for (var i = 1; i <= 5; i++)
        {
            await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, AdditionalLink(FileShare.Read, $"Link {i}"), TestContext.Current.CancellationToken);
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, AdditionalLink(FileShare.Read, "Link 6"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain($"{MaxLinksMessage} 5");

        var links = (await _foldersApi.GetFolderLinksAsync(folder.Id, TestContext.Current.CancellationToken)).Response;
        links.Count(l => l.SharedLink is { Primary: false }).Should().Be(5);
    }

    [Fact]
    public async Task SetFolderLink_FormFillingRoomFolder_AdditionalLinkRefusedWithLimitZero()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest FFR Folder Links");
        var folder = await CreateFolder("Autotest FFR Folder", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, AdditionalLink(FileShare.FillForms, "Additional"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain($"{MaxLinksMessage} 0");
    }

    private static FolderLinkRequest AdditionalLink(FileShare access, string title)
    {
        return new FolderLinkRequest(access: access, primary: false, title: title);
    }
}
