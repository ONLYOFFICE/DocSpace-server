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


namespace ASC.Web.Api.Billing.Tests.Tests.Checkout;

/// <summary>
/// The other side of <see cref="SubscriptionPayerPermissionsTests"/>: the payer is let through every subscription
/// operation - the owner who pays, and a member who pays for a portal somebody else owns - so the refusals there come
/// from the role and not from a refusal of everybody; and an owner who does not pay is refused like any other caller,
/// except on the billing account page, which the owner may always open.
/// </summary>
public class SubscriptionPermissionsControlTests(AspireAppFixture fixture) : PaymentPermissionsTestBase(fixture)
{
    public static TheoryData<string> PayerOperations => [.. SubscriptionPayerOperations];

    public static TheoryData<string> PayerOnlyOperations => [.. SubscriptionPayerOperations.Where(operation => operation != PayerOrOwnerOperation)];

    [Theory]
    [MemberData(nameof(PayerOperations))]
    public async Task SubscriptionOperation_OwnerWhoIsThePayer_IsNotRefused(string operation)
    {
        await AssertOwnerNotRefusedAsync(operation);
    }

    [Theory]
    [MemberData(nameof(PayerOperations))]
    public async Task SubscriptionOperation_MemberWhoIsThePayer_IsNotRefused(string operation)
    {
        await AssertPayerNotRefusedAsync(operation);
    }

    [Theory]
    [MemberData(nameof(PayerOnlyOperations))]
    public async Task PayerOnlyOperation_OwnerWhoIsNotThePayer_ThrowsForbidden(string operation)
    {
        await AssertOwnerWhoIsNotThePayerForbiddenAsync(operation);
    }

    [Fact]
    public async Task PaymentAccount_OwnerWhoIsNotThePayer_IsNotRefused()
    {
        await AssertOwnerWhoIsNotThePayerNotRefusedAsync(PayerOrOwnerOperation);
    }
}
