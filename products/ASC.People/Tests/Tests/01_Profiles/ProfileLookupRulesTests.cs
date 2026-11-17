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

namespace ASC.People.Tests.Tests._01_Profiles;

/// <summary>
/// The lookups by address and the token diagnostics: <c>GET /people/email</c> without an address,
/// <c>GET /people/exists</c> for a signed-in account and for an invitation-link holder, and
/// <c>GET /people/tokendiagnostics</c>.
/// </summary>
public class ProfileLookupRulesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetProfileByEmail_NeitherEmailNorEncEmail_ReturnsBadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.GetProfileByEmailAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CheckUserExistsByEmail_TakenAddress_ReportsExistsAndActiveStatus()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(member);

        var result = await _profilesApi.CheckUserExistsByEmailAsync(email: Owner.Email, cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Exists.Should().BeTrue();
        result.Response.Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public async Task CheckUserExistsByEmail_FreeAddress_ReportsNotExistsAndNoStatus()
    {
        var result = await _profilesApi.CheckUserExistsByEmailAsync(email: Initializer.FakerMember.Generate().Email, cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Exists.Should().BeFalse();
        result.Response.Status.Should().BeNull();
    }

    [Fact]
    public async Task CheckUserExistsByEmail_BlankAddress_ReturnsBadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.CheckUserExistsByEmailAsync(email: "   ", cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CheckUserExistsByEmail_AnonymousWithoutConfirmKey_ReturnsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.CheckUserExistsByEmailAsync(email: Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// An anonymous caller holding a portal invitation link (role <c>LinkInvite</c>, passed the
    /// way the client passes it: the link's query in the <c>confirm</c> header) may ask.
    /// </summary>
    [Fact]
    public async Task CheckUserExistsByEmail_AnonymousWithInvitationLinkKey_ReportsExists()
    {
        await _webApiClient.Authenticate(Owner);
        var link = (await _portalUsersApi.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.User),
            TestContext.Current.CancellationToken)).Response;

        // The link comes back as a short /s/ link; Web.Api redirects it to the long one, whose
        // query carries the key. Either the redirect is not followed (Location) or it is (final URI).
        using var shortResponse = await _webApiClient.GetAsync(new Uri(link.Url).PathAndQuery, TestContext.Current.CancellationToken);
        var url = (shortResponse.Headers.Location ?? shortResponse.RequestMessage!.RequestUri!).ToString();

        var query = new Uri(url).Query.TrimStart('?');
        query.Should().Contain("key=", "the invitation link carries its confirmation key in the query: {0}", url);

        await _peopleClient.Authenticate(null);
        _peopleClient.DefaultRequestHeaders.Add("confirm", query);

        try
        {
            var result = await _profilesApi.CheckUserExistsByEmailAsync(email: Owner.Email, cancellationToken: TestContext.Current.CancellationToken);

            result.Response.Exists.Should().BeTrue();
        }
        finally
        {
            _peopleClient.DefaultRequestHeaders.Remove("confirm");
        }
    }

    [Fact]
    public async Task GetClaims_SignedIn_ReturnsNameAndClaimsAsTypeColonValue()
    {
        var result = await _profilesApi.GetClaimsAsync(TestContext.Current.CancellationToken);

        result.Response.Name.Should().NotBeNullOrEmpty();
        result.Response.Claims.Should().NotBeEmpty();
        result.Response.Claims.Should().OnlyContain(c => c.IndexOf(':') > 0, "each claim is written as type:value");
        result.Response.Claims.Should().Contain(c => c.EndsWith(":" + Owner.Id), "one of the claims carries the account ID");
    }
}
