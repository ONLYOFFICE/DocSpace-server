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

namespace ASC.ClearEvents.Tests.Infrastructure;

/// <summary>
/// The ASC.ClearEvents service graph, standing in this process: the service's own <c>Startup</c> over the same
/// configuration files, built but never started. Starting it is what would launch the hosted services — the
/// daily pass and the active/passive registration — so stopping at <c>Build()</c> leaves a test in charge of
/// when a pass runs and with which settings.
/// </summary>
/// <remarks>
/// One thing is replaced: <see cref="ITariffService"/>, with <see cref="TestTariffs"/>. The stack runs as a
/// standalone installation, where every portal shares the one tariff of the installation, so a paid portal next
/// to a free one cannot be made any other way.
/// </remarks>
internal sealed class CleanerHost : IAsyncDisposable
{
    /// <summary>
    /// Pinned so that the first DbContext does not pay for <c>ServerVersion.AutoDetect</c>, which opens a
    /// connection of its own while MySQL may still be warming up.
    /// </summary>
    private const string MySqlServerVersion = "8.4.0";

    private readonly WebApplication _app;

    private CleanerHost(WebApplication app)
    {
        _app = app;
    }

    public static async Task<CleanerHost> BuildAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });

        // Read back by AddDefaultConfiguration, which resolves the rest of the files against it.
        builder.Configuration["pathToConf"] = BuildtoolsConfig.FindDirectory()
            ?? throw new InvalidOperationException(
                "No buildtools configuration found: set 'pathToConf', or check out buildtools next to the server repository.");

        builder.Configuration.AddDefaultConfiguration(builder.Environment)
                             .AddInMemoryCollection(new Dictionary<string, string?>
                             {
                                 ["ConnectionStrings:default:connectionString"] = connectionString,
                                 ["mysqlServerVersion"] = MySqlServerVersion,

                                 // What the AppHost injects into every service process, and what makes the
                                 // installation standalone, like the rest of the stack.
                                 ["core:base-domain"] = "localhost",

                                 // A pass needs neither: FusionCache stays L1-only and the event bus falls
                                 // back to its in-memory subscriptions manager.
                                 ["Redis:Enabled"] = "false",
                                 ["RabbitMQ:Enabled"] = "false",

                                 ["openTelemetry:enable"] = "false",

                                 // The daily pass is never started here; a test runs its own passes through
                                 // AuditTrailCleaner with settings of its own, whatever the local config says.
                                 [$"{AuditTrailRetentionConfiguration.SectionName}:enabled"] = "false"
                             });

        builder.Host.ConfigureDefault();

        await new Startup(builder.Configuration).ConfigureServices(builder);

        // After the Startup, so that it is the registration resolved.
        builder.Services.AddScoped(_ => TestTariffs.Service);

        builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
        {
            containerBuilder.Register(context.Configuration);
        });

        return new CleanerHost(builder.Build());
    }

    /// <summary>Runs one cleanup pass over every tenant, with <paramref name="retention"/> for its settings.</summary>
    public Task ClearAsync(AuditTrailRetentionConfiguration retention, CancellationToken cancellationToken)
    {
        return _app.Services.GetRequiredService<AuditTrailCleaner>().ClearAsync(retention, cancellationToken);
    }

    /// <summary>A context on the audit trail, for seeding events and reading back what a pass left.</summary>
    public Task<MessagesContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        return _app.Services.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }
}
