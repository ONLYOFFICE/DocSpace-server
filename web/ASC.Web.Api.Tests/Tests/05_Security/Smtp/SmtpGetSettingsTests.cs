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

namespace ASC.Web.Api.Tests.Tests._05_Security.Smtp;

/// <summary>
/// GET /api/2.0/smtpsettings/smtp — reads back the portal's SMTP settings. Readable by the owner
/// and a DocSpaceAdmin; every other role, including an anonymous caller, is denied.
/// </summary>
[Trait("Category", "Security")]
public class SmtpGetSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetSmtpSettings_Owner_ReturnsSavedSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = SmtpSettingsTestData.CreateValid();
        await _smtpSettingsApi.SaveSmtpSettingsAsync(dto, TestContext.Current.CancellationToken);

        // Act
        var result = await _smtpSettingsApi.GetSmtpSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Host.Should().Be(dto.Host);
        result.Data.Response.Port.Should().Be(dto.Port);
        result.Data.Response.SenderAddress.Should().Be(dto.SenderAddress);
        result.Data.Response.SenderDisplayName.Should().Be(dto.SenderDisplayName);
        result.Data.Response.EnableAuth.Should().Be(dto.EnableAuth);
        result.Data.Response.EnableSSL.Should().Be(dto.EnableSSL);
    }

    [Fact]
    public async Task GetSmtpSettings_DocSpaceAdmin_ReturnsSavedSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = SmtpSettingsTestData.CreateValid();
        await _smtpSettingsApi.SaveSmtpSettingsAsync(dto, TestContext.Current.CancellationToken);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _smtpSettingsApi.GetSmtpSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Host.Should().Be(dto.Host);
        result.Data.Response.Port.Should().Be(dto.Port);
        result.Data.Response.SenderAddress.Should().Be(dto.SenderAddress);
        result.Data.Response.SenderDisplayName.Should().Be(dto.SenderDisplayName);
        result.Data.Response.EnableAuth.Should().Be(dto.EnableAuth);
        result.Data.Response.EnableSSL.Should().Be(dto.EnableSSL);
    }

    [Fact]
    public async Task GetSmtpSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _smtpSettingsApi.GetSmtpSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetSmtpSettings_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _smtpSettingsApi.GetSmtpSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
