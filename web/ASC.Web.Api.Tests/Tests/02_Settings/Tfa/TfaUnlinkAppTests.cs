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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Tfa;

/// <summary>
/// PUT /api/2.0/settings/tfaappnewapp — an Owner unlinking a TFA App, either their own (rejected)
/// or another user's (allowed, and invalidates that user's session).
/// </summary>
[Trait("Category", "Settings")]
public class TfaUnlinkAppTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    // Docs: 405 "TFA application settings are not available" when TFA App is disabled. Live API
    // returns 403 instead.
    [Trait("Bug", "82983")]
    [Fact]
    public async Task UnlinkTfaApp_TfaAppDisabled_ShouldReturn405()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UnlinkTfaAppAsync(
                new TfaRequestsDto(id: user.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }

    // TfaRequestsDto.Id is typed Guid — a malformed value ("not-a-guid") is a value the typed
    // constructor cannot carry at all, so this goes raw.
    [Fact]
    public async Task UnlinkTfaApp_MalformedId_ReturnsValidationError()
    {
        // Act
        using var response = await _webApi.PutAsync(
            "api/2.0/settings/tfaappnewapp",
            new { id = "not-a-guid" },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("$.id");
    }

    [Fact]
    public async Task UnlinkTfaApp_EmptyId_ReturnsValidationError()
    {
        // Act
        using var response = await _webApi.PutAsync(
            "api/2.0/settings/tfaappnewapp",
            new { id = "" },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("$.id");
    }

    [Fact]
    public async Task UnlinkTfaApp_Owner_UnlinksAnotherUsersTfaApp_InvalidatesThatUsersSession()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();
        var user = await InviteContact(EmployeeType.User);
        await LinkTfaAppAsync(user);
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _tfaSettingsApi.UnlinkTfaAppWithHttpInfoAsync(
            new TfaRequestsDto(id: user.Id), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Unlinking invalidates that user's session, same as enabling TFA does.
        await _webApiClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.GetTfaSettingsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UnlinkTfaApp_Owner_CannotUnlinkTheirOwnAppThisWay()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UnlinkTfaAppAsync(
                new TfaRequestsDto(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
