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


namespace ASC.Data.Backup.Core.Tests;

/// <summary>
/// Pins the last-modified time a restore writes for the portal row. The background pass that converts AI agents
/// finds a restored portal by that time: an archive that brought its own, old value would hide the portal from it.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class TenantsModuleSpecificsTests
{
    [Theory]
    [InlineData("2020-01-01 00:00:00")]
    [InlineData("")]
    [InlineData(null)]
    public void TryPrepareValue_TenantLastModified_IsTheTimeOfTheRestore(object? archiveValue)
    {
        var specifics = CreateSpecifics();
        var before = DateTime.UtcNow;

        var (prepared, value) = specifics.PrepareValue("tenants_tenants", "last_modified", archiveValue);

        prepared.Should().BeTrue();
        value.Should().BeOfType<DateTime>().Which.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void TryPrepareValue_AnotherTenantColumn_KeepsTheArchiveValue()
    {
        var specifics = CreateSpecifics();

        var (prepared, value) = specifics.PrepareValue("tenants_tenants", "name", "Portal");

        prepared.Should().BeTrue();
        value.Should().Be("Portal");
    }

    private static TestableTenantsModuleSpecifics CreateSpecifics()
    {
        return new TestableTenantsModuleSpecifics(new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build()))));
    }

    // CoreSettings is read only for the payment id of a row, which these cases do not prepare.
    private sealed class TestableTenantsModuleSpecifics(Helpers helpers) : TenantsModuleSpecifics(null!, helpers)
    {
        public (bool Prepared, object? Value) PrepareValue(string tableName, string columnName, object? value)
        {
            var table = Tables.Single(t => t.Name == tableName);
            var prepared = TryPrepareValue(null!, new ColumnMapper(), table, columnName, ref value!);

            return (prepared, value);
        }
    }
}
