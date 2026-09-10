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

namespace ASC.Files.Tests.Tests._06_Operations.Statuses;

/// <summary>
/// <c>GET /api/2.0/files/fileops</c> — functional coverage. An operation record disappears once it
/// finishes, so both cases here only ever observe an empty list; there is no test that asserts on a
/// still-running operation because none of the owner's own operations on a brand-new, empty portal
/// take long enough to observe mid-flight (that case belongs to the terminate suite, which triggers
/// a slow operation on purpose).
/// </summary>
[Trait("Category", "Operations")]
public class GetOperationStatusesTests(
    AspireAppFixture fixture)
    : OperationsStatusesTestBase(fixture)
{
    [Fact]
    public async Task GetOperationStatuses_NoActiveOperations_ReturnsEmptyArray()
    {
        // Act
        var statuses = await GetStatuses();

        // Assert
        statuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOperationStatuses_NonExistentOperationId_ReturnsEmptyArray()
    {
        // Act
        var statuses = await GetStatuses("00000000-0000-0000-0000-000000000000");

        // Assert
        statuses.Should().BeEmpty();
    }
}
