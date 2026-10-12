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

namespace ASC.Core.Common.Tests;

/// <summary>
/// Covers the alias a tenant is renamed to when it is removed — a restore renames the old tenant this way
/// before putting the restored one in its place, and the alias column is unique.
/// </summary>
[Trait("Category", "Tenants")]
public class DbTenantServiceTests
{
    private const string BaseAlias = "localhost_deleted";

    [Fact]
    public void GetFreeAlias_NothingTaken_KeepsTheBareAlias()
    {
        DbTenantService.GetFreeAlias(BaseAlias, []).Should().Be(BaseAlias);
    }

    [Fact]
    public void GetFreeAlias_BareAliasTaken_AppendsTheFirstIndex()
    {
        DbTenantService.GetFreeAlias(BaseAlias, [BaseAlias]).Should().Be(BaseAlias + "1");
    }

    [Fact]
    public void GetFreeAlias_ContiguousSequence_ContinuesAfterIt()
    {
        string[] taken = [BaseAlias, BaseAlias + "1", BaseAlias + "2"];

        DbTenantService.GetFreeAlias(BaseAlias, taken).Should().Be(BaseAlias + "3");
    }

    [Fact]
    public void GetFreeAlias_SequenceWithAGap_ContinuesPastTheHighestIndex()
    {
        // One of the removed tenants was purged, so the row count no longer matches the highest index:
        // counting yields 4 and collides with the existing localhost_deleted4. The gap is left alone.
        string[] taken = [BaseAlias, BaseAlias + "1", BaseAlias + "3", BaseAlias + "4"];

        var result = DbTenantService.GetFreeAlias(BaseAlias, taken);

        result.Should().Be(BaseAlias + "5");
        taken.Should().NotContain(result);
    }

    [Fact]
    public void GetFreeAlias_OnlyAHighIndexTaken_DoesNotReuseTheBareAlias()
    {
        DbTenantService.GetFreeAlias(BaseAlias, [BaseAlias + "4"]).Should().Be(BaseAlias + "5");
    }

    [Fact]
    public void GetFreeAlias_TakenAliasesDifferInCase_StillTreatsThemAsTaken()
    {
        // The alias column is utf8_general_ci, so "LOCALHOST_DELETED" already occupies the bare name.
        string[] taken = [BaseAlias.ToUpperInvariant()];

        DbTenantService.GetFreeAlias(BaseAlias, taken).Should().Be(BaseAlias + "1");
    }

    [Fact]
    public void GetFreeAlias_UnrelatedLongerAliases_DoNotShiftTheResult()
    {
        // StartsWith also brings in names that merely share the prefix; they must not push the index up.
        string[] taken = [BaseAlias + "2_deleted", BaseAlias + "7_auto_deleted"];

        DbTenantService.GetFreeAlias(BaseAlias, taken).Should().Be(BaseAlias);
    }
}
