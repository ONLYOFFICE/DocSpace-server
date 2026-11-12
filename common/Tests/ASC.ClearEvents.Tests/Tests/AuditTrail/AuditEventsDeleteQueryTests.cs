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
/// The delete a pass ends each page with, called directly. It checks the references again, because
/// <c>files_audit_reference</c> cascades from <c>audit_events</c>: an event that became part of a history after its
/// page was read would otherwise be deleted, and silently taken out of that history with it. It is also bound to the
/// tenant it is given, whatever ids it is handed.
/// </summary>
public class AuditEventsDeleteQueryTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>A room id no portal has: the query only checks that a reference exists.</summary>
    private const int RoomId = int.MaxValue - 1;

    [Fact]
    public async Task DeleteAuditEventsToClearAsync_EventOutsideHistory_IsDeleted()
    {
        // Arrange
        var auditEvent = await SeedEventAsync(PaidLifeTimeDays + 30);

        // Act
        var deleted = await DeleteAsync(TenantId, auditEvent);

        // Assert
        deleted.Should().Be(1);
        (await RemainingEventsAsync([auditEvent])).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAuditEventsToClearAsync_EventInHistory_IsKeptWithItsReference()
    {
        // Arrange: as if the event had joined the history between the read of its page and this delete.
        var auditEvent = await SeedEventAsync(PaidLifeTimeDays + 30, RoomId);

        // Act
        var deleted = await DeleteAsync(TenantId, auditEvent);

        // Assert
        deleted.Should().Be(0);
        (await RemainingEventsAsync([auditEvent])).Should().BeEquivalentTo([auditEvent]);
        (await HistoryEventsAsync(RoomId, FolderEntry)).Should().BeEquivalentTo([auditEvent]);
    }

    [Fact]
    public async Task DeleteAuditEventsToClearAsync_EventOfAnotherTenant_IsKept()
    {
        // Arrange
        using var otherPortal = await Fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        var otherTenantEvent = await SeedEventForTenantAsync(otherPortal.TenantId, PaidLifeTimeDays + 30);

        // Act
        var deleted = await DeleteAsync(TenantId, otherTenantEvent);

        // Assert
        deleted.Should().Be(0);
        (await RemainingEventsAsync([otherTenantEvent])).Should().BeEquivalentTo([otherTenantEvent]);
    }

    private async Task<int> DeleteAsync(int tenantId, int eventId)
    {
        await using var ef = await Fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        return await ef.DeleteAuditEventsToClearAsync(tenantId, [eventId]);
    }
}
