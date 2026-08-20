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
/// The DocSpace service graph, standing in this process instead of behind HTTP. It exists so that a
/// letter test can ask the real notify action what it would say: an action is a <c>[Scope]</c> service
/// whose <c>Init</c> reads <see cref="CommonLinkUtility"/>, <see cref="StudioNotifyHelper"/> and the URL
/// shortener, so there is no calling it without a container, a database and a current tenant.
///
/// This is the <c>ASC.Studio.Notify</c> service's own graph — the same <c>Startup</c> over the same
/// configuration files — built but never started. Everything that would make it a running service (the
/// service launcher, the notify scheduler, the event bus subscription) happens after <c>Build()</c> in
/// that service's <c>Program</c>, which is why stopping there is safe rather than merely convenient.
/// </summary>
internal sealed class LetterHost : IAsyncDisposable
{
    /// <summary>
    /// Pinned so that the first DbContext does not pay for <c>ServerVersion.AutoDetect</c>, which opens
    /// a connection of its own while MySQL may still be warming up.
    /// </summary>
    private const string MySqlServerVersion = "8.4.0";

    private readonly WebApplication _app;

    private LetterHost(WebApplication app)
    {
        _app = app;
    }

    /// <summary>
    /// Builds the graph against the database Aspire provisioned. <paramref name="portalUrl"/> is the
    /// address the registered portal answers on: the notification image folder is configured from it, so
    /// that the images a letter carries sit on the same host as its links.
    /// </summary>
    public static async Task<LetterHost> BuildAsync(string connectionString, string portalUrl)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });

        // Read back by AddDefaultConfiguration, which resolves the rest of the files against it.
        builder.Configuration["pathToConf"] = LetterEnvironment.ConfigDirectory;

        builder.Configuration.AddDefaultConfiguration(builder.Environment)
                             .AddStudioNotifyConfiguration(builder.Environment)
                             .AddInMemoryCollection(BuildOverrides(connectionString, portalUrl));

        // Autofac and NLog. Autofac is not optional: IUrlShortener resolves to BaseUrlShortener, whose
        // ConsumerFactory only has constructors taking IContainer / ILifetimeScope.
        builder.Host.ConfigureDefault();

        await new ASC.Studio.Notify.Startup(builder.Configuration).ConfigureServices(builder);

        // Loads autofac.json and autofac.consumers.json, which is what makes the consumers above
        // resolvable by name.
        builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
        {
            containerBuilder.Register(context.Configuration);
        });

        return new LetterHost(builder.Build());
    }

    /// <summary>
    /// A scope per test. The services a letter needs are <c>[Scope]</c> with plain fields — the current
    /// tenant among them — so scopes are what keeps parallel letter classes from seeing each other.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return _app.Services.CreateScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    private static Dictionary<string, string?> BuildOverrides(string connectionString, string portalUrl)
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:default:connectionString"] = connectionString,
            ["mysqlServerVersion"] = MySqlServerVersion,

            // The letters need neither: FusionCache stays L1-only, cache notifications fall back to the
            // in-process implementation and the event bus to its in-memory subscriptions manager. The
            // containers in the Aspire graph are there for ApiSystem, not for this host.
            ["Redis:Enabled"] = "false",
            ["RabbitMQ:Enabled"] = "false",

            ["openTelemetry:enable"] = "false",

            // Nothing may leave this process: the tests deliver to MailPit themselves.
            ["core:notify:postman"] = "log",

            // Short-circuits StudioNotifyHelper.GetNotificationImageUrl, which otherwise goes through
            // WebImageSupplier -> WebPath: a sync-over-async .Result and, on a standalone portal, the
            // static uploader. A letter only ever needs the folder its images are served from.
            ["web:notification:image:path"] = LetterEnvironment.NotificationImagePathFor(portalUrl)
        };
    }
}
