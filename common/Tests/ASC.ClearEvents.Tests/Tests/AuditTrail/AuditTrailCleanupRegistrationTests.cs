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
/// writes the instance registration on every replica for a pass that returns at once. The enabled case claims the
/// worker name, which a process may register only once; the in-process hosts of the suite run with the cleanup
/// disabled, so the single test here that enables it is the one claim in the process — a second would fail.
/// </summary>
public class AuditTrailCleanupRegistrationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void AddClearEventsServices_CleanupNotEnabled_RegistersOnlyTheLoginHistoryCleanup(string? enabled)
    {
        // Arrange
        var configuration = RetentionSection.Of(("enabled", enabled));

        var services = new ServiceCollection();

        // Act
        services.AddClearEventsServices(configuration);

        // Assert
        var implementations = services.Select(d => d.ImplementationType).ToList();

        implementations.Should().Contain(typeof(ClearEventsService));
        implementations.Should().NotContain(typeof(ClearAuditEventsService));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void AddClearEventsServices_UnreadableSection_DoesNotThrow(string? enabled)
    {
        // Arrange: a period the binder cannot convert, which used to throw out of the registration and stop the host.
        var configuration = RetentionSection.Of(("enabled", enabled), ("period", "1d"));

        var services = new ServiceCollection();

        // Act
        var register = () => services.AddClearEventsServices(configuration);

        // Assert
        register.Should().NotThrow();

        var implementations = services.Select(d => d.ImplementationType).ToList();

        implementations.Should().Contain(typeof(ClearEventsService));
        implementations.Should().NotContain(typeof(ClearAuditEventsService));
    }

    [Fact]
    public void AddClearEventsServices_CleanupEnabled_RegistersTheAuditTrailCleanup()
    {
        // Arrange
        var configuration = RetentionSection.Of(("enabled", "true"));

        var services = new ServiceCollection();

        // Act
        services.AddClearEventsServices(configuration);

        // Assert
        var implementations = services.Select(d => d.ImplementationType).ToList();

        implementations.Should().Contain(typeof(ClearEventsService));
        implementations.Should().Contain(typeof(ClearAuditEventsService));
    }
}
