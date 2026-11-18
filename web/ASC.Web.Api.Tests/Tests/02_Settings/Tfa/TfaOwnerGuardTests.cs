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
/// The owner guard of <c>PUT settings/tfaapp</c> (403 when the owner is named in <c>id</c> or <c>mandatoryUsers</c>
/// while <c>id</c> is not the caller's own; the check reads <c>id</c>, not the caller's role), the empty string
/// <c>PUT settings/tfaappwithlink</c> returns when the method is off or the caller is exempt, and the empty body of
/// <c>GET settings/tfaapp/confirm</c> when no method applies to the caller. <c>type</c> None is used where the
/// method itself does not matter, because switching a method on resets every session of the portal.
/// </summary>
[Trait("Category", "Settings")]
public class TfaOwnerGuardTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    private static readonly List<string> _anyAddress =
    [
        "0.0.0.0-255.255.255.255",
        "::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"
    ];

    [Fact]
    public async Task UpdateTfaSettings_DocSpaceAdminNamesOwnerInMandatoryUsers_ThrowsForbidden()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UpdateTfaSettingsAsync(
                new TfaRequestDto(TfaType.None, mandatoryUsers: [Owner.Id]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateTfaSettings_DocSpaceAdminSendsOwnersId_ThrowsForbidden()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UpdateTfaSettingsAsync(
                new TfaRequestDto(TfaType.None, id: Owner.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // AC-8: the guard compares `id` with the caller, so even the owner is refused for listing themselves
    // without sending their own `id`.
    [Fact]
    public async Task UpdateTfaSettings_OwnerNamesThemselvesWithoutId_ThrowsForbidden()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UpdateTfaSettingsAsync(
                new TfaRequestDto(TfaType.None, mandatoryUsers: [Owner.Id]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateTfaSettings_OwnerNamesThemselvesWithOwnId_IsAccepted()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestDto(TfaType.None, id: Owner.Id, mandatoryUsers: [Owner.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTfaSettingsLink_TypeNone_ReturnsEmptyString()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsLinkAsync(
            new TfaRequestDto(TfaType.None), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTfaSettingsLink_AppWithCallerOnTrustedAddress_ReturnsEmptyString()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsLinkAsync(
            new TfaRequestDto(TfaType.App, trustedIps: _anyAddress), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTfaConfirmData_TfaOff_ReturnsEmptyBody()
    {
        // Act
        var result = await _tfaSettingsApi.GetTfaConfirmDataAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task GetTfaConfirmData_CallerOnTrustedAddress_ReturnsEmptyBody()
    {
        // Arrange: switching the method on ended the owner's session; the trusted address lets them back in
        // with a password alone
        await EnableTfaAppAsync(trustedIps: _anyAddress);
        await _webApiClient.Authenticate(Owner, forceRefresh: true);

        // Act
        var result = await _tfaSettingsApi.GetTfaConfirmDataAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeNull();
    }
}
