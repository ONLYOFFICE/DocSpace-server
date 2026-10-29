// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Tests.Tests._01_Files.Forms.Versions;

/// <summary>
/// The pieces of the spreadsheet report and the field question that do not need a portal: the names of the report
/// sheets, one per group of linked versions, and the field tips the document builder script reads.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormVersionReportTests
{
    [Theory]
    [InlineData(new[] { 1 }, "v1")]
    [InlineData(new[] { 1, 2 }, "v1-v2")]
    [InlineData(new[] { 2, 4 }, "v2, v4")]
    [InlineData(new[] { 1, 2, 3, 5 }, "v1-v3, v5")]
    public void GetSheetName_WritesConsecutiveVersionsAsRange(int[] versions, string expected)
    {
        FormFillingReportBuilder.GetSheetName(versions).Should().Be(expected);
    }

    [Fact]
    public void GetSheetName_LongerThanExcelAllows_IsCut()
    {
        // An Excel sheet name holds at most 31 characters.
        var result = FormFillingReportBuilder.GetSheetName([1, 3, 5, 7, 9, 11, 13, 15, 17, 19]);

        result.Should().HaveLength(31).And.StartWith("v1, v3, v5").And.EndWith("...");
    }

    [Fact]
    public void ParseTips_KeepsNonBlankStringTips()
    {
        var result = FormFillingReportCreator.ParseTips("""{"formsdata":[],"tips":{"name":"Your full name","email":"  ","age":5}}""");

        result.Should().Equal(new Dictionary<string, string> { ["name"] = "Your full name" });
    }

    [Theory]
    [InlineData("""{"formsdata":[]}""")]
    [InlineData("""{"formsdata":[],"tips":["a"]}""")]
    [InlineData("""{"formsdata":null,"tips":null}""")]
    public void ParseTips_NoTipsObject_ReturnsEmpty(string json)
    {
        FormFillingReportCreator.ParseTips(json).Should().BeEmpty();
    }
}
