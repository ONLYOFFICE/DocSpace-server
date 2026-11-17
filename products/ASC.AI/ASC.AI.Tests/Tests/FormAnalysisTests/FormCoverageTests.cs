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

namespace ASC.AI.Tests.Tests.FormAnalysisTests;

/// <summary>
/// What the form analysis model is told about the form versions behind the rows: a table continued by later versions
/// must not be taken for the responses to its first version only, and versions without responses must not be named.
/// </summary>
[Trait("Category", "Unit")]
public class FormCoverageTests
{
    [Fact]
    public void FormatCoverage_TableOfSeveralVersions_NamesThemAll()
    {
        var text = FormSchemaFormatter.FormatCoverage(3, [1, 3], []);

        text.Should().StartWith("These are all the responses to the form, given to versions 1, 3; version 3 is the current one.");
        text.Should().Contain("keeps its earlier answers");
        text.Should().NotContain("kept apart");
    }

    [Fact]
    public void FormatCoverage_EarlierVersionsWithoutResponses_SaysAllAndNamesNoOtherVersion()
    {
        // Versions 1 and 2 were saved before the filling started: none of them has a response.
        FormSchemaFormatter.FormatCoverage(3, [3], []).Should().Be("These are all the responses to the form.");
    }

    [Fact]
    public void FormatCoverage_ResponsesInAnotherTable_NamesOnlyThoseVersions()
    {
        var text = FormSchemaFormatter.FormatCoverage(4, [3, 4], [1]);

        text.Should().StartWith("These are the responses to the form, given to versions 3, 4;");
        text.Should().EndWith("The responses to version 1 are kept apart and are not counted here.");
    }

    [Fact]
    public void FormatCoverage_IndexCannotTell_ClaimsNeitherAllNorApart()
    {
        var text = FormSchemaFormatter.FormatCoverage(2, [2], null);

        text.Should().Be("These are the responses to version 2 of the form, the current one.");
    }
}
