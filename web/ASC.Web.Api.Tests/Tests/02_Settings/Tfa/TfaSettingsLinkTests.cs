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
/// PUT /api/2.0/settings/tfaappwithlink — updates the TFA policy the same way as
/// <c>tfaapp</c>, but also hands back a confirmation URL. Confirming that link end-to-end requires
/// a real browser (the confirmation key travels as an httpOnly cookie, and the setup secret is
/// scraped off the rendered confirm page) — that flow belongs to the DocSpace-e2e-tests project,
/// not here.
/// </summary>
[Trait("Category", "Settings")]
public class TfaSettingsLinkTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    [Fact]
    public async Task UpdateTfaSettingsLink_Owner_EnablesApp_ReturnsConfirmationUrl()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsLinkAsync(
            new TfaRequestsDto(TfaRequestsDtoType.App), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNullOrEmpty();
    }

    // Docs: 405 "SMS settings are not available" when no SMS provider is configured. Live API
    // returns 403 instead.
    [Trait("Bug", "82974")]
    [Fact]
    public async Task UpdateTfaSettingsLink_NoSmsProviderConfigured_ShouldReturn405()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UpdateTfaSettingsLinkAsync(
                new TfaRequestsDto(TfaRequestsDtoType.Sms), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }
}
