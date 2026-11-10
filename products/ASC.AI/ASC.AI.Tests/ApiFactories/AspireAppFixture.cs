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

namespace ASC.AI.Tests.ApiFactories;

/// <summary>
/// The AI suite's Aspire host: the Node chat service (the public <c>/api/2.0/ai</c> surface) and ASC.AI behind it,
/// plus Files and People for the entities an AI test needs (a room to scope preferences to, a member to act as),
/// and the stub model provider every profile of the run points at.
/// </summary>
public class AspireAppFixture : AspireHostFixture<PortalClients>
{
    // The AI worker writes the exported files (the text-to-docx event) and runs the vectorization: the run waits for it
    // so the first export does not go out before it subscribes. It reports no health check of its own, so "healthy" is
    // "running", as for the backup worker of the backup suite.
    protected override IEnumerable<string> Resources =>
        [ResourceNames.AiChat, ResourceNames.Ai, ResourceNames.AiWorker, ResourceNames.Files, ResourceNames.People];

    // Only the third-party room tests (NeedsLazyResources) connect to Nextcloud, and they wait for it themselves.
    protected override IEnumerable<string> LazyResources => [ResourceNames.Nextcloud];

    /// <summary>The model provider shared by the run: profiles are created against it, chats answered by it.</summary>
    public StubModelProvider ModelProvider { get; } = StubModelProvider.CreateShared();

    protected override PortalClients CreateClients(PortalContext context)
    {
        return new PortalClients(context);
    }

    // A chat can still be answered until the services stop, so the model provider outlives them.
    protected override ValueTask OnStoppedAsync()
    {
        ModelProvider.Dispose();

        return ValueTask.CompletedTask;
    }

    protected override async ValueTask WarmUpAsync(PortalClients clients)
    {
        await clients.AiChatHttpClient.Authenticate(clients.Owner);
        await clients.FilesHttpClient.Authenticate(clients.Owner);

        await clients.AiProfilesApi.AiProfilesListAsync(TestContext.Current.CancellationToken);

        // The owner's root folder tree is provisioned lazily on first access — warm that too,
        // it is what every test creating a room hits right after registration.
        await clients.FoldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
