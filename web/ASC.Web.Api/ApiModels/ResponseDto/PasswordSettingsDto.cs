// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The password settings parameters.
/// </summary>
public class PasswordSettingsDto
{
    /// <summary>
    /// The minimum number of characters required for valid passwords.
    /// </summary>
    public int MinLength { get; set; }

    /// <summary>
    /// Specifies whether the password should contain the uppercase letters or not.
    /// </summary>
    public bool UpperCase { get; set; }

    /// <summary>
    /// Specifies whether the password should contain the digits or not.
    /// </summary>
    public bool Digits { get; set; }

    /// <summary>
    /// Specifies whether the password should contain the special symbols or not.
    /// </summary>
    public bool SpecSymbols { get; set; }

    /// <summary>
    /// The allowed password characters in the regex string format.
    /// </summary>
    public string AllowedCharactersRegexStr { get; set; }

    /// <summary>
    /// The password digits in the regex string format.
    /// </summary>
    public string DigitsRegexStr { get; set; }

    /// <summary>
    /// The password uppercase letters in the regex string format.
    /// </summary>
    public string UpperCaseRegexStr { get; set; }

    /// <summary>
    /// The passaword special symbols in the regex string format.
    /// </summary>
    public string SpecSymbolsRegexStr { get; set; }
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