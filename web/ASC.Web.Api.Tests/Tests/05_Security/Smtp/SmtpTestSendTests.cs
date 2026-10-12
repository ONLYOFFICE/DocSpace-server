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
/// GET /api/2.0/smtpsettings/smtp/test — kicks off a background job that sends a test message.
/// The controller enqueues the job and immediately returns its freshly-created status, so the
/// functional cases here only assert that shape (an id was assigned, no error has been recorded
/// yet) — never that the message was actually delivered. This suite's <c>AspireAppFixture</c>
/// starts only the People resource alongside the always-on Web.Api/ApiSystem, so there is no
/// reachable SMTP server to deliver through; the completed-with-no-error outcome that the
/// TypeScript suite polls for is covered by <c>SmtpOperationStatusTests</c>'s summary instead of a
/// weakened assertion here.
/// </summary>
[Trait("Category", "Security")]
public class SmtpTestSendTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task TestSmtpSettings_Owner_EnqueuesTestJob()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _smtpSettingsApi.SaveSmtpSettingsAsync(SmtpSettingsTestData.CreateValid(), TestContext.Current.CancellationToken);

        // Act
        var result = await _smtpSettingsApi.TestSmtpSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Id.Should().NotBeNullOrEmpty();
        result.Data.Response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task TestSmtpSettings_DocSpaceAdmin_EnqueuesTestJob()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _smtpSettingsApi.SaveSmtpSettingsAsync(SmtpSettingsTestData.CreateValid(), TestContext.Current.CancellationToken);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _smtpSettingsApi.TestSmtpSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Id.Should().NotBeNullOrEmpty();
        result.Data.Response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task TestSmtpSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _smtpSettingsApi.TestSmtpSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task TestSmtpSettings_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _smtpSettingsApi.TestSmtpSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
