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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// Represents a balance with an account number and a list of sub-accounts.
/// </summary>
public class BalanceDto
{
    /// <summary>
    /// The account number.
    /// </summary>
    /// <example>12345</example>
    public int AccountNumber { get; init; }

    /// <summary>
    /// The sub-account number.
    /// </summary>
    /// <example>12345</example>
    public int SubAccountNumber { get; init; }

    /// <summary>
    /// The account name.
    /// </summary>
    /// <example>account name</example>
    public string AccountName { get; init; }

    /// <summary>
    /// The account currency.
    /// </summary>
    /// <example>"USD"</example>
    public string AccountCurrency { get; init; }

    /// <summary>
    /// A list of sub-accounts.
    /// </summary>
    /// <example>[{"currency": "USD", "amount": 1500.75}]</example>
    public List<SubAccountDto> SubAccounts { get; init; }

    /// <summary>
    /// The most recent credit transaction applied to the account.
    /// </summary>
    /// <example>{"date": "2024-01-15T10:30:00Z", "currency": "USD", "amount": 1500.75}</example>
    public TransactionInfoDto LastCredit { get; init; }
}

/// <summary>
/// A sub-account of the wallet: its currency and balance.
/// </summary>
public class SubAccountDto
{
    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>"USD"</example>
    public string Currency { get; init; }

    /// <summary>
    /// The amount in the specified currency.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Amount { get; init; }
}

/// <summary>
/// Represents information about the transaction applied to an account.
/// </summary>
public class TransactionInfoDto
{
    /// <summary>
    /// The date and time when the credit transaction occurred.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Date { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>"USD"</example>
    public string Currency { get; init; }

    /// <summary>
    /// The amount in the specified currency.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Amount { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class BalanceDtoMapper
{
    public static partial BalanceDto Map(this Balance source);
}
