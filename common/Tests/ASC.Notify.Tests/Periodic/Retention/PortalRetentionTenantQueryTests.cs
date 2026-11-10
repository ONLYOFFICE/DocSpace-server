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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// The query the retention job walks portals with. It runs against the stack's real database, so it
/// proves the compiled query translates, not only that it compiles. It only reads: the portal
/// is shared by every letter test, so its status is never changed here.
/// </summary>
public class PortalRetentionTenantQueryTests
{
    [Fact]
    public async Task TenantIdsByStatus_ReturnsThePortalsInTheGivenStates()
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        var tenantManager = scope.Services.GetRequiredService<TenantManager>();

        var walked = await tenantManager.GetTenantIdsByStatusAsync(TenantStatus.Active, TenantStatus.Blocked);

        walked.Should().ContainSingle(id => id == stack.Portal.TenantId).And.OnlyHaveUniqueItems();

        (await tenantManager.GetTenantIdsByStatusAsync(TenantStatus.Blocked))
            .Should().NotContain(stack.Portal.TenantId, "an active portal is not a blocked one");
    }
}
