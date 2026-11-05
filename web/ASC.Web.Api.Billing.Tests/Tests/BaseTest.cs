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


namespace ASC.Web.Api.Billing.Tests.Tests;

/// <summary>
/// The base of every payment test: a portal of its own, its owner signed in, and what the payment services say about
/// that portal (<see cref="Payments"/>), which nothing else of the run shares.
/// </summary>
public class BaseTest(AspireAppFixture fixture) : IAsyncLifetime
{
    private PortalClients _clients = null!;

    protected User Owner => _clients.Owner;

    protected int TenantId => _clients.TenantId;

    /// <summary>What billing, accounting and docscloud say about this test's portal.</summary>
    protected PortalPayments Payments { get; private set; } = null!;

    protected HttpClient _webApiClient = null!;
    protected HttpClient _peopleClient = null!;

    protected PaymentApi _paymentApi = null!;
    protected PortalQuotaApi _portalQuotaApi = null!;
    protected DocsCloudApi _docsCloudApi = null!;
    protected CommonSettingsApi _commonSettingsApi = null!;
    protected ProfilesApi _profilesApi = null!;

    /// <summary>The payment services of the whole run, for what belongs to no single portal.</summary>
    protected PaymentServiceStub PaymentServices => fixture.Payments;

    /// <summary>The requests the payment services got about this test's portal, in the order they arrived.</summary>
    protected IReadOnlyList<PaymentServiceCall> PaymentCalls => fixture.Payments.CallsFor(TenantId);

    public async ValueTask InitializeAsync()
    {
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        Payments = fixture.Payments.For(_clients.TenantId);

        _webApiClient = _clients.WebApiHttpClient;
        _peopleClient = _clients.PeopleHttpClient;

        _paymentApi = _clients.PaymentApi;
        _portalQuotaApi = _clients.PortalQuotaApi;
        _docsCloudApi = _clients.DocsCloudApi;
        _commonSettingsApi = _clients.CommonSettingsApi;
        _profilesApi = _clients.ProfilesApi;

        await _webApiClient.Authenticate(Owner);
    }

    public ValueTask DisposeAsync()
    {
        _clients.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <summary>Adds a member of <paramref name="employeeType"/> to the portal, as its owner, and returns it signed out.</summary>
    protected async Task<User> InviteMember(EmployeeType employeeType)
    {
        await _peopleClient.Authenticate(Owner);

        var fakeMember = Initializer.FakerMember.Generate();

        var member = await _profilesApi.AddMemberAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Email = fakeMember.Email,
            Password = fakeMember.Password,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,
            Type = employeeType
        }, TestContext.Current.CancellationToken);

        return new User(fakeMember.Email, fakeMember.Password) { Id = member.Response.Id };
    }
}
