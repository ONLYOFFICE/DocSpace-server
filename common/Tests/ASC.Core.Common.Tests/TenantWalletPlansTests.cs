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
/// Which wallet quotas are the administrators plan of the portal: the monthly and the yearly one, and no add-on.
/// Every member of the enum is listed, so a member added later has to be placed on one side.
/// </summary>
public class TenantWalletPlansTests
{
    public static TheoryData<TenantWalletService, bool> Services => new()
    {
        { TenantWalletService.Storage, false },
        { TenantWalletService.Backup, false },
        { TenantWalletService.AITools, false },
        { TenantWalletService.Admin, true },
        { TenantWalletService.DocsCloud, false },
        { TenantWalletService.DocsCloudDevPack, false },
        { TenantWalletService.AISearch, false },
        { TenantWalletService.AdminYear, true }
    };

    [Theory]
    [MemberData(nameof(Services))]
    public void IsAdminPlan_AnswersForTheService(TenantWalletService service, bool expected)
    {
        TenantWalletPlans.IsAdminPlan((int)service).Should().Be(expected);
    }

    [Fact]
    public void Services_ListEveryMemberOfTheEnum()
    {
        Services.Select(row => row.Data.Item1).Should().BeEquivalentTo(Enum.GetValues<TenantWalletService>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-9)]
    [InlineData(-10)]
    [InlineData(1)]
    public void IsAdminPlan_QuotaThatIsNotAWalletService_IsFalse(int quotaId)
    {
        TenantWalletPlans.IsAdminPlan(quotaId).Should().BeFalse();
    }
}
