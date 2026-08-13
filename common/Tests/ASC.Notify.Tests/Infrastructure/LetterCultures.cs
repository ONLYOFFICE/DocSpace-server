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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// The cultures every letter is rendered and sent in — one test case per culture. Letters are sent in
/// the recipient's culture, so this is how a translated letter gets previewed.
///
/// Add cultures to <see cref="Names"/>, or set <c>LETTER_CULTURES</c> for a single run:
/// <c>LETTER_CULTURES=en-US,ru-RU,de-DE dotnet test ...</c>. A culture without its own
/// <c>WebstudioNotifyPatternResource.&lt;culture&gt;.resx</c> entry falls back to the default one, which is
/// exactly what production does until the translation lands.
/// </summary>
internal static class LetterCultures
{
    private const string EnvironmentVariable = "LETTER_CULTURES";

    /// <summary>
    /// The culture the default <c>.resx</c> is written in. Wording checks only run for it: in any other
    /// culture the same key may legitimately carry a translation.
    /// </summary>
    public const string DefaultCultureName = "en-US";

    /// <summary>The cultures used when <c>LETTER_CULTURES</c> is not set.</summary>
    public static readonly string[] Names = [DefaultCultureName];

    /// <summary>Culture names as xUnit test data, one case per culture.</summary>
    public static TheoryData<string> All { get; } = BuildTheoryData();

    private static TheoryData<string> BuildTheoryData()
    {
        var names = Environment.GetEnvironmentVariable(EnvironmentVariable)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var data = new TheoryData<string>();

        foreach (var name in names is { Length: > 0 } ? names : Names)
        {
            data.Add(name);
        }

        return data;
    }
}
