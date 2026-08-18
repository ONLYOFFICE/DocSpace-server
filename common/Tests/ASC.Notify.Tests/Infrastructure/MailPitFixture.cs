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


namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// The MailPit the letter tests deliver to. It is started by the test run itself: the Aspire AppHost
/// is booted on the <c>integration-test</c> launch profile, exactly like the Files/People/AI suites
/// do, so a letter test never has to look for a MailPit somebody else happened to leave running.
///
/// The resource graph is trimmed to MailPit alone before the host is built. That profile also brings
/// up MySQL, RabbitMQ, Redis, OpenSearch, OpenResty and six services, and a letter needs none of
/// them — rendering happens in-process (<see cref="LetterPreview"/>) and the only thing left to talk
/// to is the inbox. Dropping the rest turns a multi-minute start into a container start.
/// </summary>
public sealed class MailPitFixture : IAsyncLifetime
{
    /// <summary>The resource name <c>ConnectionStringManager.AddMailPit</c> registers it under.</summary>
    private const string ResourceName = "mailpit";

    private DistributedApplication _app = null!;
    private HttpClient _api = null!;

    /// <summary>The inbox every letter test delivers to and reads its score back from.</summary>
    internal MailPitInbox Inbox { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(
            ["DOTNET_LAUNCH_PROFILE=integration-test", "SKIP_CLIENT=true", "APP_HOSTING_STANDALONE=true"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        // Everything but the inbox goes. Safe in this order because MailPit has no dependencies of
        // its own, so nothing that stays behind points at anything removed.
        foreach (var resource in appHost.Resources.Where(resource => resource.Name != ResourceName).ToArray())
        {
            appHost.Resources.Remove(resource);
        }

        _app = await appHost.BuildAsync();

        await _app.StartAsync();

        // The MailPit integration ships a container health check, so this waits for the API to answer
        // and not merely for the container to exist. Bounded, because a container that dies on startup
        // never reports any state at all and would otherwise hang the whole assembly instead of
        // failing it — the budget is generous enough for a cold image pull.
        using var startUp = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        await _app.ResourceNotifications.WaitForResourceHealthyAsync(ResourceName, startUp.Token);

        // Two named endpoints: "smtp" (1025) for delivery, "http" (8025) for the web API. Aspire
        // publishes both on random host ports, which is why nothing here is hard-coded.
        var smtp = _app.GetEndpoint(ResourceName, "smtp");

        _api = _app.CreateHttpClient(ResourceName, "http");

        Inbox = new MailPitInbox(smtp.Host, smtp.Port, _api);
    }

    public async ValueTask DisposeAsync()
    {
        Inbox.Dispose();
        _api.Dispose();

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
