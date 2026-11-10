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


namespace ASC.Web.Api.Billing.Tests.Tests.Wallet;

/// <summary>
/// The other side of <see cref="WalletPayerPermissionsTests"/>: the payer is let through every wallet operation - the
/// owner who pays, and a member who pays for a portal somebody else owns, whatever the member's role - so the refusals
/// there come from the role and not from a refusal of everybody; attaching a payment method asks for a DocSpace
/// administrator as well, so a payer below one is refused it; and an owner who does not pay is refused like any other
/// caller.
/// </summary>
public class WalletPermissionsControlTests(AspireAppFixture fixture) : PaymentPermissionsTestBase(fixture)
{
    public static TheoryData<string> PayerOperations => [.. WalletPayerOperations];

    public static TheoryData<string, EmployeeType> PayerOnlyOperationsByPayersBelowAdministrator =>
        Matrix([.. WalletPayerOperations.Where(operation => operation != PayerAndAdministratorOperation)], PayersBelowAdministrator);

    public static TheoryData<EmployeeType> PayerRolesBelowAdministrator => [.. PayersBelowAdministrator];

    [Theory]
    [MemberData(nameof(PayerOperations))]
    public async Task WalletOperation_OwnerWhoIsThePayer_IsNotRefused(string operation)
    {
        await AssertOwnerNotRefusedAsync(operation);
    }

    [Theory]
    [MemberData(nameof(PayerOperations))]
    public async Task WalletOperation_MemberWhoIsThePayer_IsNotRefused(string operation)
    {
        await AssertPayerNotRefusedAsync(operation);
    }

    [Theory]
    [MemberData(nameof(PayerOnlyOperationsByPayersBelowAdministrator))]
    public async Task PayerOnlyOperation_PayerBelowAdministrator_IsNotRefused(string operation, EmployeeType role)
    {
        await AssertPayerNotRefusedAsync(operation, role);
    }

    [Theory]
    [MemberData(nameof(PayerRolesBelowAdministrator))]
    public async Task CheckoutSetupUrl_PayerBelowAdministrator_ThrowsForbidden(EmployeeType role)
    {
        await AssertPayerForbiddenAsync(PayerAndAdministratorOperation, role);
    }

    [Theory]
    [MemberData(nameof(PayerOperations))]
    public async Task WalletOperation_OwnerWhoIsNotThePayer_ThrowsForbidden(string operation)
    {
        await AssertOwnerWhoIsNotThePayerForbiddenAsync(operation);
    }
}
