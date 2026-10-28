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

namespace ASC.ClearEvents.Tests.Tests.AuditTrail;

/// <summary>
/// The daily pass is registered only when the cleanup is enabled: registered, its active/passive pair polls and
/// writes the instance registration on every replica for a pass that returns at once. Only the disabled case is
/// checked here — registering the enabled one in this process would claim the worker name it may hold only once.
/// </summary>
public class AuditTrailCleanupRegistrationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void AddClearEventsServices_CleanupNotEnabled_RegistersOnlyTheLoginHistoryCleanup(string? enabled)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AuditTrailRetentionConfiguration.SectionName}:enabled"] = enabled
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddClearEventsServices(configuration);

        // Assert
        var implementations = services.Select(d => d.ImplementationType).ToList();

        implementations.Should().Contain(typeof(ClearEventsService));
        implementations.Should().NotContain(typeof(ClearAuditEventsService));
    }
}
