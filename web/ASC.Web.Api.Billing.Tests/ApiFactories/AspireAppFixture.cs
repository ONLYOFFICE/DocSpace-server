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


namespace ASC.Web.Api.Billing.Tests.ApiFactories;

/// <summary>
/// The payment suite's Aspire host: the <c>integration-test-billing</c> stack, a SaaS installation whose billing,
/// accounting and docscloud services are <see cref="PaymentServiceStub"/>. Web.Api and ApiSystem are started by the
/// base fixture; People invites the members the role tests need, and Web.Studio runs the wallet services.
/// </summary>
public class AspireAppFixture : AspireHostFixture<PortalClients>
{
    /// <summary>The payment services every portal of the run talks to.</summary>
    public PaymentServiceStub Payments { get; private set; } = null!;

    protected override string LaunchProfile => "integration-test-billing";

    protected override bool Standalone => false;

    protected override IEnumerable<string> Resources => [ResourceNames.People, ResourceNames.WebStudio];

    protected override PortalClients CreateClients(PortalContext context)
    {
        return new PortalClients(context);
    }

    protected override ValueTask OnStartingAsync()
    {
        Payments = PaymentServiceStub.Start();

        return ValueTask.CompletedTask;
    }

    // The services ask billing until they stop, so the stub outlives them.
    protected override ValueTask OnStoppedAsync()
    {
        Payments?.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The renewal journal of Web.Studio in the stack's Redis, for a test of a renewal a stopped process left behind.
    /// </summary>
    public async Task<RenewalJournal> OpenRenewalJournalAsync(CancellationToken cancellationToken = default)
    {
        return await RenewalJournal.OpenAsync(await GetConnectionStringAsync(ResourceNames.Cache, cancellationToken));
    }

    protected override async ValueTask WarmUpAsync(PortalClients clients)
    {
        await clients.WebApiHttpClient.Authenticate(clients.Owner);

        // The tariff is what every payment endpoint reads first.
        await clients.PortalQuotaApi.GetPortalTariffAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
