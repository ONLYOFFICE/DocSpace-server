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
/// The part of an amount - of one wallet movement, or of a service's usage total - that fell on one sub-account of the
/// portal: the money paid into the wallet, or the bonuses.
/// </summary>
public class OperationSourceDto
{
    /// <summary>
    /// The sub-account the part fell on, by its number in the accounting service. To tell bonuses from the money paid
    /// into the wallet, read `isBonus` rather than comparing this number.
    /// </summary>
    /// <example>53</example>
    public int SubAccount { get; init; }

    /// <summary>
    /// Whether the part fell on the bonuses credited to the portal; `false` means it fell on the money paid into the
    /// wallet.
    /// </summary>
    /// <example>true</example>
    public bool IsBonus { get; init; }

    /// <summary>
    /// The part of the amount on that sub-account, in the `currency` of the movement or of the usage total it belongs
    /// to.
    /// </summary>
    /// <example>4.5</example>
    public decimal Amount { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class OperationSourceDtoMapper
{
    public static partial List<OperationSourceDto> Map(this List<OperationSource> source);
}
