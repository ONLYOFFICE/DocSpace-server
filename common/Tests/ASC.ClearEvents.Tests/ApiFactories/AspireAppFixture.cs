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

namespace ASC.ClearEvents.Tests.ApiFactories;

/// <summary>
/// What the audit cleanup tests add to the shared stack: Files, which writes the history of rooms and files,
/// Web.Studio, which persists the audit events that history is made of, and the ASC.ClearEvents service graph
/// in this process (<see cref="CleanerHost"/>), so that a test runs a cleanup pass when it is ready for one
/// instead of waiting for the daily one.
/// </summary>
public sealed class AspireAppFixture : AspireHostFixture<PortalClients>
{
    private CleanerHost? _cleaner;

    /// <summary>The service graph a test runs a cleanup pass in and reads the audit trail through.</summary>
    internal CleanerHost Cleaner => _cleaner
        ?? throw new InvalidOperationException(
            "The cleanup host was not built: InitializeAsync did not get that far. The failure that stopped it is the one to read, not this.");

    protected override IEnumerable<string> Resources => [ResourceNames.Files, ResourceNames.WebStudio];

    protected override PortalClients CreateClients(PortalContext context)
    {
        return new PortalClients(context);
    }

    protected override async ValueTask OnStartedAsync()
    {
        var connectionString = await GetConnectionStringAsync(ResourceNames.Database, TestContext.Current.CancellationToken);

        _cleaner = await Timing.Measure("cleanerhost.build", () => CleanerHost.BuildAsync(connectionString));
    }

    protected override async ValueTask OnDisposingAsync()
    {
        // Before the app goes: the in-process host holds database connections into its container.
        if (_cleaner is not null)
        {
            await _cleaner.DisposeAsync();
        }
    }
}
