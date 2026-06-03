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


namespace ASC.Files.Tests.Tests._04_Security;

[Collection("Test Collection")]
[Trait("Category", "Security")]
[Trait("Feature", "ExternalSharing")]
public class ExternalShareAccessTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // private async Task SetExternalSharingAsync(
    //     bool externalShare,
    //     bool defaultLinkInternal = false,
    //     bool applyToDocuments = true,
    //     bool applyToRooms = true,
    //     bool blockExisting = true)
    // {
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
    //         new ExternalSharingSettingsRequestDto(
    //             externalShare: externalShare,
    //             defaultShareLinkInternal: defaultLinkInternal,
    //             externalShareApplyToDocuments: applyToDocuments,
    //             externalShareApplyToRooms: applyToRooms,
    //             blockExistingLinksOnRestrict: blockExisting),
    //         TestContext.Current.CancellationToken);
    // }
    //
    // [Fact]
    // public async Task CreateFileLink_WhenRestrictedForDocuments_IsInternal()
    // {
    //     // Arrange — restrict external sharing for My Documents
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: true, applyToRooms: false);
    //
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var file = await CreateFileInMy("file.docx", Initializer.Owner);
    //
    //     // Act — explicitly request a public (non-internal) link
    //     var linkParams = new FileLinkRequest(access: FileShare.Read, @internal: false);
    //     await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken);
    //     var result = (await _filesApi.GetFilePrimaryExternalLinkAsync(
    //         file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert — server enforcement overrides the requested flag
    //     result.SharedLink.Internal.Should().BeTrue();
    // }
    //
    // [Fact]
    // public async Task CreateRoomLink_WhenRestrictedForRooms_IsInternal()
    // {
    //     // Arrange — restrict external sharing for Rooms only
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: false, applyToRooms: true);
    //
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var room = await CreateCustomRoom("restricted-room");
    //
    //     // Act — retrieve (or auto-create) the primary external link
    //     var result = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(
    //         room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert — enforcement applies to the room link
    //     result.SharedLink.Internal.Should().BeTrue();
    // }
    //
    // [Fact]
    // public async Task CreateFileLink_WhenRestrictedForRoomsOnly_IsPublic()
    // {
    //     // Arrange — restrict only Rooms, leave My Documents unrestricted
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: false, applyToRooms: true);
    //
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var file = await CreateFileInMy("file.docx", Initializer.Owner);
    //
    //     // Act — request a public link for a file in My Documents
    //     var linkParams = new FileLinkRequest(access: FileShare.Read, @internal: false);
    //     await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken);
    //     var result = (await _filesApi.GetFilePrimaryExternalLinkAsync(
    //         file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert — no enforcement for My Documents when only Rooms are restricted
    //     result.SharedLink.Internal.Should().BeFalse();
    // }
    //
    // [Fact]
    // public async Task AccessExistingFileLink_WhenBlockEnabled_AsAnonymous_ReturnsExternalAccessDenied()
    // {
    //     // Arrange — create a public link while sharing is still allowed
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var (token, _) = await CreateFileAndShare(FileShare.Read, varInternal: false);
    //
    //     // Apply restriction and block existing links
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: true, blockExisting: true);
    //
    //     // Act — anonymous access attempt
    //     await _filesClient.Authenticate(null);
    //     var result = (await _sharingApi.GetExternalShareDataAsync(
    //         token, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert
    //     result.Status.Should().Be(Status.ExternalAccessDenied);
    // }
    //
    // [Fact]
    // public async Task AccessExistingRoomLink_WhenBlockEnabled_AsAnonymous_ReturnsExternalAccessDenied()
    // {
    //     // Arrange — create a public room link while sharing is allowed
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var room = await CreateCustomRoom("public-room");
    //     var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(
    //         room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //     var token = link.SharedLink.RequestToken;
    //
    //     // Apply restriction and block existing links for Rooms
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: false, applyToRooms: true, blockExisting: true);
    //
    //     // Act — anonymous access attempt
    //     await _filesClient.Authenticate(null);
    //     var result = (await _sharingApi.GetExternalShareDataAsync(
    //         token, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert
    //     result.Status.Should().Be(Status.ExternalAccessDenied);
    // }
    //
    // [Fact]
    // public async Task AccessExistingFileLink_WhenBlockDisabled_AsAnonymous_ReturnsOk()
    // {
    //     // Arrange — create a public link while sharing is allowed
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var (token, _) = await CreateFileAndShare(FileShare.Read, varInternal: false);
    //
    //     // Apply restriction but allow existing links to continue working
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: true, blockExisting: false);
    //
    //     // Act — anonymous access
    //     await _filesClient.Authenticate(null);
    //     var result = (await _sharingApi.GetExternalShareDataAsync(
    //         token, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert — existing link still works when block is disabled
    //     result.Status.Should().Be(Status.Ok);
    // }
    //
    // [Fact]
    // public async Task AccessExistingFileLink_WhenBlockEnabled_AsAuthenticatedUser_ReturnsOk()
    // {
    //     // Arrange — create a public link while sharing is allowed
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var (token, _) = await CreateFileAndShare(FileShare.Read, varInternal: false);
    //
    //     // Apply restriction and block existing links
    //     await SetExternalSharingAsync(externalShare: false, applyToDocuments: true, blockExisting: true);
    //
    //     // Act — authenticated user accesses the link
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var result = (await _sharingApi.GetExternalShareDataAsync(
    //         token, cancellationToken: TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert — authenticated users bypass the global block
    //     result.Status.Should().Be(Status.Ok);
    // }
}
