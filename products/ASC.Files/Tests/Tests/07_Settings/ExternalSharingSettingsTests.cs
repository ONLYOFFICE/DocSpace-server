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

using EmployeeType = DocSpace.API.SDK.Model.EmployeeType;

namespace ASC.Files.Tests.Tests._07_Settings;

[Collection("Test Collection")]
[Trait("Category", "Settings")]
[Trait("Feature", "ExternalSharing")]
public class ExternalSharingSettingsTests(AspireAppFixture fixture) : BaseTest(fixture)
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
    // public async Task ChangeExternalSharingSettings_AllSettings_PersistedAndReflectedInGet()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //
    //     var request = new ExternalSharingSettingsRequestDto(
    //         externalShare: false,
    //         defaultShareLinkInternal: true,
    //         externalShareApplyToDocuments: false,
    //         externalShareApplyToRooms: true,
    //         blockExistingLinksOnRestrict: false);
    //
    //     // Act
    //     var putResult = (await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
    //         request, TestContext.Current.CancellationToken)).Response;
    //
    //     var getResult = (await _filesSettingsApi.GetFilesSettingsAsync(
    //         TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert — PUT response echoes the request values
    //     putResult.ExternalShare.Should().Be(request.ExternalShare);
    //     putResult.DefaultShareLinkInternal.Should().Be(request.DefaultShareLinkInternal);
    //     putResult.ExternalShareApplyToDocuments.Should().Be(request.ExternalShareApplyToDocuments);
    //     putResult.ExternalShareApplyToRooms.Should().Be(request.ExternalShareApplyToRooms);
    //     putResult.BlockExistingLinksOnRestrict.Should().Be(request.BlockExistingLinksOnRestrict);
    //
    //     // Assert — GET settings reflect the persisted values
    //     getResult.ExternalShare.Should().Be(request.ExternalShare);
    //     getResult.DefaultShareLinkInternal.Should().Be(request.DefaultShareLinkInternal);
    //     getResult.ExternalShareApplyToDocuments.Should().Be(request.ExternalShareApplyToDocuments);
    //     getResult.ExternalShareApplyToRooms.Should().Be(request.ExternalShareApplyToRooms);
    //     getResult.BlockExistingLinksOnRestrict.Should().Be(request.BlockExistingLinksOnRestrict);
    // }
    //
    // [Fact]
    // public async Task ChangeExternalSharingSettings_AsNonAdmin_ReturnsForbidden()
    // {
    //     // Arrange
    //     var nonAdmin = await Initializer.InviteContact(EmployeeType.User);
    //     await _filesClient.Authenticate(nonAdmin);
    //
    //     // Act & Assert
    //     var exception = await Assert.ThrowsAsync<ApiException>(
    //         async () => await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
    //             new ExternalSharingSettingsRequestDto(externalShare: false),
    //             TestContext.Current.CancellationToken));
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     exception.ErrorCode.Should().Be(403);
    // }
    //
    // [Fact]
    // public async Task ChangeExternalSharingSettings_DisableExternalShare_AlsoDisablesSocialMedia()
    // {
    //     // Arrange & Act
    //     await SetExternalSharingAsync(externalShare: false);
    //
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var settings = (await _filesSettingsApi.GetFilesSettingsAsync(
    //         TestContext.Current.CancellationToken)).Response;
    //
    //     // Reset external sharing changes
    //     await SetExternalSharingAsync(externalShare: true);
    //
    //     // Assert
    //     settings.ExternalShare.Should().BeFalse();
    //     settings.ExternalShareSocialMedia.Should().BeFalse();
    // }
}
