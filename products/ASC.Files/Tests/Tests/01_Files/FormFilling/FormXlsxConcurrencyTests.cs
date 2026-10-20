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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// <c>POST /files/file/{fileId}/xlsx</c> — parallel calls. The endpoint documents that calling it
/// again while a run is still going answers with that run instead of starting a second one.
/// </summary>
[Trait("Category", "Concurrency")]
[Trait("Feature", "FormFilling")]
public class FormXlsxConcurrencyTests(
    AspireAppFixture fixture)
    : FormResultsTestBase(fixture)
{
    [Fact]
    public async Task GenerateXlsx_ParallelCalls_ShareOneRunAndProduceOneReport()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Race Room");

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(
            _ => _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken)));

        // Assert
        results.Select(r => r.Response.Task.Id).Distinct().Should().HaveCount(1);

        var resultsFolderId = results[0].Response.Form.ResultsFolderId!.Value;
        await WaitForXlsxCompleted(formId);
        (await GetXlsxFilesIn(resultsFolderId)).Should().HaveCount(1);
    }
}
