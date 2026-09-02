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
/// The endpoint exists only while the portal publishes a trusted-domain policy: it serves the
/// "Register" link the login page shows an anonymous visitor, and that link is offered on exactly
/// the same condition (<c>SettingsDto.EnabledJoin</c>). A portal without one has nothing to join and
/// says so with 405 — to every caller alike, since whether the feature exists is not a question of
/// who is asking.
/// </summary>
[Trait("Category", "Settings")]
public class SendJoinInviteMailTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// BUG 80727: with no trusted domains configured the endpoint threw <c>MethodAccessException</c>,
    /// which nothing maps, so every caller got 500. It answers 405 "Method not available" now. The
    /// role makes no difference here on purpose — this is the feature being switched off, not an
    /// access decision, and the roles are what the disabled block below would cover.
    /// </summary>
    [Trait("Bug", "80727")]
    [Theory]
    [MemberData(nameof(Callers))]
    public async Task SendJoinInviteMail_TrustedDomainsNotConfigured_ThrowsMethodNotAllowed(EmployeeType? actingRole)
    {
        // Arrange
        await ActAsAsync(actingRole);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _messagesApi.SendJoinInviteMailAsync(
                new AdminMessageBaseSettingsRequestsDto(email), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
        exception.ErrorContent?.ToString().Should().Contain("Method not available");
    }

    /// <summary>Every kind of caller the endpoint can see; <c>null</c> is the anonymous visitor.</summary>
    public static TheoryData<EmployeeType?> Callers =>
    [
        null,
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    /// <summary>Authenticates as the given role, as the Owner when it is not given, or anonymously for <c>null</c>.</summary>
    private async Task ActAsAsync(EmployeeType? actingRole)
    {
        if (actingRole is null)
        {
            await _webApiClient.Authenticate(null);
            return;
        }

        var member = await InviteMember(actingRole.Value);
        await _webApiClient.Authenticate(member);
    }

    [Trait("Bug", "80727")]
    [Fact]
    public async Task SendJoinInviteMail_Owner_TrustedDomainsNotConfigured_ThrowsMethodNotAllowed()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _messagesApi.SendJoinInviteMailAsync(
                new AdminMessageBaseSettingsRequestsDto(email), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }

    // Everything below needs the portal to accept a trusted domain first, and the Arrange step that
    // does it (SaveMailDomainSettings) saves the tenant: on the integration host
    // (base-domain = localhost) SaveTenantAsync rewrites the portal alias to 'localhost' and dies on
    // the unique alias index with a 500 — the same constraint as the disabled save/restore cases in
    // GreetingSaveTests. Re-enable when that is resolved.
    //
    // Until then everything this endpoint actually does — the anonymous self-registration the login
    // page's "Register" link performs, the domain filter, the already-registered rejection — has no
    // integration coverage, which is worth knowing before touching it again.
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

    [Fact]
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

    [Fact]
    public async Task SendJoinInviteMail_Owner_NewEmail_ReturnsStringResponse()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestsDto(TenantTrustedDomainsType.All, [], true), TestContext.Current.CancellationToken);

        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var result = await _messagesApi.SendJoinInviteMailAsync(
            new AdminMessageBaseSettingsRequestsDto(email, "en-US"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNullOrEmpty();
    }
    */
}
