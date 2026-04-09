// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode


namespace ASC.Files.Tests.Tests._02_Folders;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderDeleteTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteFolder_NonExistingFolder_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var nonExistingFolderId = 99999; // Non-existing folder ID

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(
                nonExistingFolderId,
                new DeleteFolder(false, true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteFolder_NoPermissions_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var folder = await CreateFolder("folder_no_permissions", FolderType.USER, Initializer.Owner);

        var user = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(
                folder.Id,
                new DeleteFolder(false, true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
