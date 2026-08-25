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
/// What the letter tests add to the shared stack: the MailPit the rendered letters are delivered to,
/// the DocSpace service graph in this process (<see cref="LetterHost"/>) which is what makes a notify
/// action resolvable at all, and the one portal every letter is rendered for.
/// </summary>
/// <remarks>
/// Everything else — starting the Aspire host, registering a portal, the owner credentials, the timing
/// instrumentation, the storage cleanup — comes from <see cref="AspireHostFixture{TClients}"/>.
///
/// One portal for the whole assembly, not one per test as the API suites use: a letter test does not
/// write to the portal, it renders against it. Isolation comes from a service scope of its own per test
/// (<see cref="LetterScope"/>) and a unique recipient address, so no test reads another's mail.
/// </remarks>
public sealed class LetterStackFixture : AspireHostFixture<LetterPortalClients>
{
    private MailPitInbox? _inbox;

    // Held rather than passed straight into the inbox: MailPitInbox does not take ownership of the
    // client it is handed - its Dispose only releases its own semaphore - so whoever creates the
    // client is the one that has to release it.
    private HttpClient? _mailPitApi;

    private LetterHost? _host;
    private LetterPortalClients? _portal;

    /// <summary>The inbox every letter test delivers to and reads its score back from.</summary>
    internal MailPitInbox Inbox => _inbox ?? throw NotStarted();

    /// <summary>The service graph a letter test resolves its notify action from.</summary>
    internal LetterHost Host => _host ?? throw NotStarted();

    /// <summary>The portal the letters are rendered for.</summary>
    internal LetterPortalClients Portal => _portal ?? throw NotStarted();

    /// <summary>
    /// Where the portal answers. Not the alias: <c>core:base-domain</c> is <c>localhost</c> here — both
    /// in buildtools/config and from the AppHost, which sets it for every standalone project — and
    /// <c>Tenant.GetTenantDomain</c> short-circuits on that to <c>localhost</c> whatever the alias is.
    /// So a registered portal answers on the address the stack publishes, exactly like the letters have
    /// always assumed. <see cref="LetterScope"/> checks that against <c>CommonLinkUtility</c> rather
    /// than trusting this comment.
    /// </summary>
    internal string PortalUrl => LetterEnvironment.PortalUrl;

    protected override IEnumerable<string> Resources => [ResourceNames.MailPit];

    /// <summary>
    /// The letter suite's own graph (see the switch in <c>ASC.AppHost/Program.cs</c>): a letter is
    /// rendered in this process out of <see cref="LetterHost"/>, so all the stack owes the suite is
    /// the database the portal was registered into, ApiSystem (the registration), Web.Api (the
    /// password salt the harness reads once) and MailPit. The service processes, socket.io and
    /// OpenResty of <c>integration-test</c> would be pure startup cost and more things that can fail
    /// a run of letter renders.
    /// </summary>
    protected override string LaunchProfile => "notify-test";

    protected override LetterPortalClients CreateClients(PortalContext context)
    {
        return new LetterPortalClients(context);
    }

    protected override async ValueTask OnStartedAsync()
    {
        // Two named endpoints: "smtp" for delivery, "http" for the web API. Aspire publishes both on
        // random host ports, which is why nothing here is hard-coded.
        var smtp = GetEndpoint(ResourceNames.MailPit, "smtp");

        _mailPitApi = CreateHttpClient(ResourceNames.MailPit, "http");

        _inbox = new MailPitInbox(smtp.Host, smtp.Port, _mailPitApi);

        _portal = await Timing.Measure("letter.portal", () => CreatePortalAsync(TestContext.Current.CancellationToken));

        var connectionString = await GetConnectionStringAsync(
            ResourceNames.Database, TestContext.Current.CancellationToken);

        _host = await Timing.Measure("letterhost.build", () => LetterHost.BuildAsync(connectionString, PortalUrl));
    }

    protected override async ValueTask OnDisposingAsync()
    {
        // Before the app goes: the in-process host holds database connections into its container.
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

        // The inbox before the client it reads through, and both before the app they point at.
        _inbox?.Dispose();
        _mailPitApi?.Dispose();
        _portal?.Dispose();
    }

    private static InvalidOperationException NotStarted([CallerMemberName] string member = "")
    {
        return new InvalidOperationException(
            $"The letter stack has no {member}: InitializeAsync did not get that far. The failure that "
            + "stopped it is the one to read, not this.");
    }
}
