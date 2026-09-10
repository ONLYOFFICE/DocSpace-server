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

namespace ASC.Files.Tests.Tests._07_Settings.SharingDefaults;

[Trait("Category", "Settings")]
[Trait("Feature", "SharingDefaults")]
public class ExternalSharingSettingsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private async Task SetExternalSharingAsync(
        bool externalShare,
        bool defaultLinkInternal = false,
        bool applyToDocuments = true,
        bool applyToRooms = true,
        bool blockExisting = true)
    {
        await _filesClient.Authenticate(Owner);
        await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
            new ExternalSharingSettingsRequestDto(
                externalShare: externalShare,
                defaultShareLinkInternal: defaultLinkInternal,
                externalShareApplyToDocuments: applyToDocuments,
                externalShareApplyToRooms: applyToRooms,
                blockExistingLinksOnRestrict: blockExisting),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ChangeExternalSharingSettings_AllSettings_PersistedAndReflectedInGet()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var request = new ExternalSharingSettingsRequestDto(
            externalShare: false,
            defaultShareLinkInternal: true,
            externalShareApplyToDocuments: false,
            externalShareApplyToRooms: true,
            blockExistingLinksOnRestrict: false);

        // Act
        var putResult = (await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
            request, TestContext.Current.CancellationToken)).Response;

        var getResult = (await _filesSettingsApi.GetFilesSettingsAsync(
            TestContext.Current.CancellationToken)).Response;

        // Reset external sharing changes
        await SetExternalSharingAsync(externalShare: true);

        // Assert — PUT response echoes the request values
        putResult.ExternalShare.Should().Be(request.ExternalShare);
        putResult.DefaultShareLinkInternal.Should().Be(request.DefaultShareLinkInternal);
        putResult.ExternalShareApplyToDocuments.Should().Be(request.ExternalShareApplyToDocuments);
        putResult.ExternalShareApplyToRooms.Should().Be(request.ExternalShareApplyToRooms);
        putResult.BlockExistingLinksOnRestrict.Should().Be(request.BlockExistingLinksOnRestrict);

        // Assert — GET settings reflect the persisted values
        getResult.ExternalShare.Should().Be(request.ExternalShare);
        getResult.DefaultShareLinkInternal.Should().Be(request.DefaultShareLinkInternal);
        getResult.ExternalShareApplyToDocuments.Should().Be(request.ExternalShareApplyToDocuments);
        getResult.ExternalShareApplyToRooms.Should().Be(request.ExternalShareApplyToRooms);
        getResult.BlockExistingLinksOnRestrict.Should().Be(request.BlockExistingLinksOnRestrict);
    }

    [Fact]
    public async Task ChangeExternalSharingSettings_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var nonAdmin = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(nonAdmin);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ChangeExternalSharingSettingsAsync(
                new ExternalSharingSettingsRequestDto(externalShare: false),
                TestContext.Current.CancellationToken));

        // Reset external sharing changes
        await SetExternalSharingAsync(externalShare: true);

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ChangeExternalSharingSettings_DisableExternalShare_AlsoDisablesSocialMedia()
    {
        // Arrange & Act
        await SetExternalSharingAsync(externalShare: false);

        await _filesClient.Authenticate(Owner);
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(
            TestContext.Current.CancellationToken)).Response;

        // Reset external sharing changes
        await SetExternalSharingAsync(externalShare: true);

        // Assert
        settings.ExternalShare.Should().BeFalse();
        settings.ExternalShareSocialMedia.Should().BeFalse();
    }
}
