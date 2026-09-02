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
/// PUT /api/2.0/settings/tfaapp — <c>mandatoryUsers</c>/<c>mandatoryGroups</c> override
/// <c>trustedIps</c> for the accounts they list: TFA is still forced for those accounts even from
/// an address the portal otherwise trusts (<c>TfaSettingsHelperBase&lt;T&gt;.TfaEnabledForUserAsync</c>,
/// <c>web/ASC.Web.Core/Sms/TfaSettingsBase.cs</c>, checks mandatory membership before trustedIps,
/// so a trusted address only ever bypasses TFA for an account that isn't listed). SDK docs
/// describe <c>mandatoryUsers</c> as if it were an allowlist scoping who needs TFA, which reads
/// as misleading — not yet reported as a bug, since "type: App" already requires TFA
/// portal-wide regardless of the list.
///
/// The TS suite seeds <c>trustedIps</c> with the caller's own address read back from login
/// history. That read is unreliable here: on this host the recorded <c>Ip</c> field can come back
/// an empty string (no <c>X-Forwarded-For</c>, and the value observed at request time isn't
/// necessarily what gets persisted), and an empty string is not a restriction
/// <c>IPAddressRange.MatchIPs</c> (<c>common/ASC.Core.Common/Core/IPAddressRange.cs</c>) can ever
/// match — it returns <see langword="false"/> immediately for a blank <c>restrictionIp</c> — so a
/// trustedIps entry seeded from it would never bypass anyone, silently defeating the point of the
/// test. Instead these tests seed <c>trustedIps</c> with two full-range restrictions (one IPv4,
/// one IPv6) that <c>MatchIPs</c> matches unconditionally regardless of which family or address
/// the request actually arrives from, which exercises the exact same code path a real trusted
/// address would without depending on what this environment happens to report back.
/// </summary>
[Trait("Category", "Settings")]
public class TfaTrustedIpsOverrideTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    private static readonly List<string> _anyIpTrustedRanges =
    [
        "0.0.0.0-255.255.255.255",
        "::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"
    ];

    [Fact]
    public async Task MandatoryUsers_OverridesTrustedIps_ForcesTfaForListedUserOnly()
    {
        // Arrange
        var mandatory = await InviteContact(EmployeeType.User);
        var trusted = await InviteContact(EmployeeType.User);

        await EnableTfaAppAsync(mandatoryUsers: [mandatory.Id], trustedIps: _anyIpTrustedRanges);

        // Act — the mandatory user is still forced into TFA despite the trusted IP
        await _webApiClient.Authenticate(null);
        var mandatoryLogin = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: mandatory.Email, password: mandatory.Password), TestContext.Current.CancellationToken);

        // Assert
        mandatoryLogin.Response.Tfa.Should().BeTrue();
        mandatoryLogin.Response.TfaKey.Should().NotBeNullOrEmpty();

        // Act — the non-mandatory user is bypassed via the trusted IP
        await _webApiClient.Authenticate(null);
        var trustedLogin = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: trusted.Email, password: trusted.Password), TestContext.Current.CancellationToken);

        // Assert
        trustedLogin.Response.Tfa.Should().BeFalse();
        trustedLogin.Response.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MandatoryGroups_OverridesTrustedIps_ForcesTfaForMemberOnly()
    {
        // Arrange
        var mandatory = await InviteContact(EmployeeType.User);
        var outsider = await InviteContact(EmployeeType.User);

        var groupApi = new DocSpace.API.SDK.Api.Group.GroupApi(
            _peopleClient,
            new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') });
        var group = await groupApi.AddGroupAsync(
            new GroupRequestDto(members: [mandatory.Id], groupManager: Owner.Id, groupName: "Mandatory TFA group"),
            TestContext.Current.CancellationToken);
        var groupId = group.Response.Id;

        await EnableTfaAppAsync(mandatoryGroups: [groupId], trustedIps: _anyIpTrustedRanges);

        // Act — a member of the mandatory group is still forced into TFA despite the trusted IP
        await _webApiClient.Authenticate(null);
        var mandatoryLogin = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: mandatory.Email, password: mandatory.Password), TestContext.Current.CancellationToken);

        // Assert
        mandatoryLogin.Response.Tfa.Should().BeTrue();
        mandatoryLogin.Response.TfaKey.Should().NotBeNullOrEmpty();

        // Act — a user outside the mandatory group is bypassed via the trusted IP
        await _webApiClient.Authenticate(null);
        var outsiderLogin = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: outsider.Email, password: outsider.Password), TestContext.Current.CancellationToken);

        // Assert
        outsiderLogin.Response.Tfa.Should().BeFalse();
        outsiderLogin.Response.Token.Should().NotBeNullOrEmpty();
    }
}
