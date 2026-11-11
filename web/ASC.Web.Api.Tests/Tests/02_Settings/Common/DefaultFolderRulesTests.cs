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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// PUT /api/2.0/settings/defaultfolder — the list of sections a start page may be set to, My documents refused
/// for a Guest, and the audit event (<c>SettingsController.SaveDefaultFolder</c>).
/// </summary>
[Trait("Category", "Settings")]
public class DefaultFolderRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(FolderType.AiAgents)]
    [InlineData(FolderType.USER)]
    [InlineData(FolderType.SHARE)]
    [InlineData(FolderType.Recent)]
    [InlineData(FolderType.Forms)]
    [InlineData(FolderType.PrivateRooms)]
    [InlineData(FolderType.AiHub)]
    [InlineData(FolderType.DEFAULT)]
    public async Task SaveDefaultFolder_SectionOnTheAllowedList_IsSavedAndReported(FolderType folderType)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var saved = await _commonSettingsApi.SaveDefaultFolderAsync(new DefaultProductRequestDto(folderType), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.DefaultFolderType.Should().Be(folderType);
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.DefaultFolderType.Should().Be(folderType);
    }

    [Theory]
    [InlineData(FolderType.TRASH)]
    [InlineData(FolderType.Archive)]
    [InlineData(FolderType.Templates)]
    [InlineData(FolderType.CustomRoom)]
    public async Task SaveDefaultFolder_SectionOffTheAllowedList_ThrowsBadRequestAndChoiceStays(FolderType folderType)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveDefaultFolderAsync(new DefaultProductRequestDto(FolderType.Favorites), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDefaultFolderAsync(new DefaultProductRequestDto(folderType), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.DefaultFolderType.Should().Be(FolderType.Favorites);
    }

    [Fact]
    public async Task SaveDefaultFolder_GuestChoosesMyDocuments_ThrowsBadRequest()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDefaultFolderAsync(new DefaultProductRequestDto(FolderType.USER), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SaveDefaultFolder_Saved_WritesDefaultStartPageSettingsUpdatedAuditEvent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _commonSettingsApi.SaveDefaultFolderAsync(new DefaultProductRequestDto(FolderType.Favorites), TestContext.Current.CancellationToken);

        // Assert
        var events = await SettingsAudit.PollAsync(_auditTrailDataApi, MessageAction.DefaultStartPageSettingsUpdated);
        events.Should().NotBeEmpty();
    }
}
