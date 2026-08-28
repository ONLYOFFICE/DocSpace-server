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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Messages;

/// <summary>
/// POST /api/2.0/settings/sendjoininvite — sending an invitation email with a portal join link.
/// </summary>
[Trait("Category", "Settings")]
public class SendJoinInviteMailTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Commented out for now: the Arrange step of both cases below (SaveMailDomainSettings) saves the
    // tenant, and on the integration host (base-domain = localhost) SaveTenantAsync rewrites the
    // portal alias to 'localhost' and dies on the unique alias index with a 500 — same constraint as
    // the disabled save/restore cases in GreetingSaveTests. Re-enable when that is resolved.
    //
    // Until then the anonymous self-registration path this endpoint serves — the login page's
    // "Register" link, offered while SettingsDto.EnabledJoin is true — has no integration coverage,
    // which is worth knowing before touching its authorization again.
    /*
    [Fact]
    public async Task SendJoinInviteMail_Owner_AlreadyRegisteredEmail_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Trusted domains must accept any domain before an invite is even attempted, otherwise
        // the request is rejected earlier for an unrelated reason. (Ported from TS test titled
        // "BUG 79040" — that test is a plain `test()`, not `test.fail()`, so it documents the
        // already-correct behaviour rather than an open bug; no [Trait("Bug", ...)] is added.)
        await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestsDto(TenantTrustedDomainsType.All, [], true), TestContext.Current.CancellationToken);

        var existingUser = await InviteContact(EmployeeType.User);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _messagesApi.SendJoinInviteMailAsync(
                new AdminMessageBaseSettingsRequestsDto(existingUser.Email), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("User with this email is already registered");
    }

    // The login page's "Register" link is anonymous, and the portal only offers it while a
    // trusted-domain policy is published — the same condition SettingsDto.EnabledJoin reports to
    // that page. This is the case that keeps the authorization added for bug 80727 from taking the
    // login page's self-registration down with it.
    [Fact]
    [Trait("Bug", "80727")]
    public async Task SendJoinInviteMail_Anonymous_TrustedDomainsOpen_ReturnsStringResponse()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestsDto(TenantTrustedDomainsType.All, [], true), TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(null);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var result = await _messagesApi.SendJoinInviteMailAsync(
            new AdminMessageBaseSettingsRequestsDto(email), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNullOrEmpty();
    }
    */

    [Fact]
    [Trait("Bug", "80727")]
    public async Task SendJoinInviteMail_Owner_NewEmail_ReturnsStringResponse()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var result = await _messagesApi.SendJoinInviteMailAsync(
            new AdminMessageBaseSettingsRequestsDto(email), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Bug", "80727")]
    public async Task SendJoinInviteMail_Owner_NewEmailWithCulture_ReturnsStringResponse()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var result = await _messagesApi.SendJoinInviteMailAsync(
            new AdminMessageBaseSettingsRequestsDto(email, "en-US"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Bug", "80727")]
    public async Task SendJoinInviteMail_DocSpaceAdmin_NewEmail_ReturnsStringResponse()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var result = await _messagesApi.SendJoinInviteMailAsync(
            new AdminMessageBaseSettingsRequestsDto(email), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNullOrEmpty();
    }
}
