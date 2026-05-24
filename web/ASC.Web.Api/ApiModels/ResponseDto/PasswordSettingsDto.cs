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
/// The password settings parameters.
/// </summary>
public class PasswordSettingsDto
{
    /// <summary>
    /// The minimum number of characters required for valid passwords.
    /// </summary>
    /// <example>8</example>
    public required int MinLength { get; set; }

    /// <summary>
    /// Specifies whether the password should contain the uppercase letters or not.
    /// </summary>
    /// <example>true</example>
    public required bool UpperCase { get; set; }

    /// <summary>
    /// Specifies whether the password should contain the digits or not.
    /// </summary>
    /// <example>true</example>
    public required bool Digits { get; set; }

    /// <summary>
    /// Specifies whether the password should contain the special symbols or not.
    /// </summary>
    /// <example>false</example>
    public required bool SpecSymbols { get; set; }

    /// <summary>
    /// The allowed password characters in the regex string format.
    /// </summary>
    /// <example>^[a-zA-Z0-9!@#$%^&amp;*()]+$</example>
    public required string AllowedCharactersRegexStr { get; set; }

    /// <summary>
    /// The password digits in the regex string format.
    /// </summary>
    /// <example>(?=.*\\d)</example>
    public required string DigitsRegexStr { get; set; }

    /// <summary>
    /// The password uppercase letters in the regex string format.
    /// </summary>
    /// <example>(?=.*[A-Z])</example>
    public required string UpperCaseRegexStr { get; set; }

    /// <summary>
    /// The passaword special symbols in the regex string format.
    /// </summary>
    /// <example>(?=.*[!@#$%^&amp;*()])</example>
    public required string SpecSymbolsRegexStr { get; set; }
}

[Singleton]
public sealed class PasswordSettingsConverter(PasswordSettingsManager settingsManager)
{
    public PasswordSettingsDto Convert(PasswordSettings passwordSettings)
    {
        return new PasswordSettingsDto
        {
            MinLength = passwordSettings.MinLength,
            UpperCase = passwordSettings.UpperCase,
            Digits = passwordSettings.Digits,
            SpecSymbols = passwordSettings.SpecSymbols,
            AllowedCharactersRegexStr = settingsManager.AllowedCharactersRegexStr,
            DigitsRegexStr = settingsManager.DigitsRegexStr,
            UpperCaseRegexStr = settingsManager.UpperCaseRegexStr,
            SpecSymbolsRegexStr = settingsManager.SpecSymbolsRegexStr
        };
    }
}