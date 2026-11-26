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
/// The decision of the system API registration after a failed registration: a name another registration took is a
/// 400, the registration's own portal saved before a later step failed is the server's failure. Making the steps after
/// the insert fail needs a broken database or settings store, which no test profile starts, so the decision is tested
/// here as a function of what the lookup found.
/// </summary>
public sealed class RegistrationAliasCheckTests
{
    private const string Alias = "autotest-alias";
    private const string Email = "owner@example.com";

    [Fact]
    public void IsTakenByAnother_NoPortalUnderTheAlias_IsFalse()
    {
        RegistrationAliasCheck.IsTakenByAnother(null, Alias, null, Email).Should().BeFalse();
    }

    [Fact]
    public void IsTakenByAnother_PortalFoundByAMappedDomain_IsFalse()
    {
        RegistrationAliasCheck.IsTakenByAnother(new Tenant("another-alias"), Alias, "other@example.com", Email).Should().BeFalse();
    }

    [Theory]
    [InlineData(Email)]
    [InlineData("Owner@Example.com")]
    [InlineData(" owner@example.com ")]
    public void IsTakenByAnother_PortalOfThisRegistration_IsFalse(string foundOwnerEmail)
    {
        RegistrationAliasCheck.IsTakenByAnother(new Tenant(Alias), Alias, foundOwnerEmail, Email).Should().BeFalse();
    }

    [Fact]
    public void IsTakenByAnother_PortalOfAnotherOwner_IsTrue()
    {
        RegistrationAliasCheck.IsTakenByAnother(new Tenant(Alias), Alias, "other@example.com", Email).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsTakenByAnother_OwnerOfTheFoundPortalUnknown_IsFalse(string? foundOwnerEmail)
    {
        RegistrationAliasCheck.IsTakenByAnother(new Tenant(Alias), Alias, foundOwnerEmail, Email).Should().BeFalse();
    }
}
