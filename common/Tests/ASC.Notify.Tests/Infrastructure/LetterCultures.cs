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
/// By default that is <b>every culture the product ships a translation for</b> — see
/// <see cref="Names"/>. Narrow it to a few for a single run with <c>LETTER_CULTURES</c>:
/// <c>LETTER_CULTURES=en-US,ru,de dotnet test ...</c>. A culture without its own
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

    /// <summary>
    /// The cultures used when <c>LETTER_CULTURES</c> is not set: every culture the portal offers, read
    /// from <c>web:cultures</c> in <c>appsettings.json</c>. Taking the list from the same configuration
    /// the product uses means a new language is covered the day it is switched on, and nobody has to
    /// remember this file.
    ///
    /// Checking English alone would be worse than useless: the defects these tests exist to catch — a
    /// translated tag name, a lost <c>$</c>, a textile link glued to the next character — cannot occur
    /// in the default culture at all.
    /// </summary>
    public static readonly string[] Names = Discover();

    /// <summary>Culture names as xUnit test data, one case per culture.</summary>
    public static TheoryData<string> All { get; } = BuildTheoryData();

    /// <summary>
    /// The portal's own culture list, falling back to the cultures the binaries actually carry a
    /// translation for when the buildtools configuration is not next to the checkout. The default
    /// culture leads, the rest follow in a stable order, whichever source answered.
    /// </summary>
    private static string[] Discover()
    {
        var configured = LetterEnvironment.Configuration.GetSection("web:cultures")
            .GetChildren()
            .Select(entry => entry.Value)
            .OfType<string>()
            .ToArray();

        var names = configured.Length > 0 ? configured : Shipped();

        if (names.Length == 0)
        {
            throw new InvalidOperationException(
                "Neither 'web:cultures' in appsettings.json nor the satellite assemblies next to "
                + $"'{AppContext.BaseDirectory}' name a single culture, so the letter tests would check "
                + "the default one only — exactly the blind spot they exist to close.");
        }

        return names.Where(name => name != DefaultCultureName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Prepend(DefaultCultureName)
            .ToArray();
    }

    /// <summary>
    /// The cultures the letters are actually translated into, one satellite folder each. Other
    /// satellites next to the binaries (the test platform's, System.ServiceModel's) carry cultures the
    /// product does not translate letters into, so the file name is what makes the list right.
    /// </summary>
    private static string[] Shipped()
    {
        const string satellite = "ASC.Web.Core.resources.dll";

        return Directory.EnumerateDirectories(AppContext.BaseDirectory)
            .Where(directory => File.Exists(Path.Combine(directory, satellite)))
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToArray();
    }

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
