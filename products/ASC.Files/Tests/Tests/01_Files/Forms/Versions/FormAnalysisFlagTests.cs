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
/// <see cref="FormFillingReportCreator.HasResponsesInVersionTable"/>, behind the <c>analyzeResponses</c> flag: analysis
/// reads the table of the current version, so a version that took a table of its own has nothing to analyse until its
/// first response, however many the earlier versions had. Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormAnalysisFlagTests
{
    [Fact]
    public void HasResponsesInVersionTable_NoResponses_IsFalse()
    {
        FormFillingReportCreator.HasResponsesInVersionTable(new FormFillingProperties(), 1).Should().BeFalse();
        FormFillingReportCreator.HasResponsesInVersionTable<int>(null!, 1).Should().BeFalse();
    }

    // v1 had 10 responses, v2 took a table of its own and has none yet.
    [Fact]
    public void HasResponsesInVersionTable_NewVersionWithTableOfItsOwn_IsFalseUntilItsFirstResponse()
    {
        var formFilling = new FormFillingProperties { ResultFormNumber = 10, LastSubmittedVersion = 1 };

        FormFillingReportCreator.HasResponsesInVersionTable(formFilling, 2).Should().BeFalse();

        formFilling.LastSubmittedVersion = 2;
        FormFillingReportCreator.HasResponsesInVersionTable(formFilling, 2).Should().BeTrue();
    }

    // v2 continues the table of v1, whose responses it can analyse before any of its own.
    [Fact]
    public void HasResponsesInVersionTable_NewVersionLinkedToTheSharedTable_IsTrue()
    {
        var formFilling = new FormFillingProperties
        {
            ResultFormNumber = 10,
            LastSubmittedVersion = 1,
            VersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }]
        };

        FormFillingReportCreator.HasResponsesInVersionTable(formFilling, 2).Should().BeTrue();
    }

    // Responses taken before the submitted version was kept: the counter decides, as before.
    [Fact]
    public void HasResponsesInVersionTable_SubmittedVersionUnknown_FollowsTheCounter()
    {
        FormFillingReportCreator.HasResponsesInVersionTable(new FormFillingProperties { ResultFormNumber = 3 }, 5).Should().BeTrue();
    }
}
