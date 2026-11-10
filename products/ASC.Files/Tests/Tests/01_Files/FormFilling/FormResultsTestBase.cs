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
/// Shared setup for the form results suites (<c>submissions</c> and <c>xlsx</c>): a started form in
/// a form filling room, and polling of the asynchronous report task.
/// </summary>
public abstract class FormResultsTestBase(AspireAppFixture fixture) : FormFillingTestBase(fixture)
{
    /// <summary>
    /// As the owner, creates a form filling room holding one form whose filling has been started.
    /// Leaves <c>_filesClient</c> authenticated as the owner.
    /// </summary>
    protected async Task<(int RoomId, int FormId)> CreateStartedForm(string title)
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateFillingFormsRoom(title + " " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        return (room.Id, form.Id);
    }

    /// <summary>
    /// Polls <c>GET /files/file/{fileId}/xlsx</c> on a deadline until the caller's run is completed,
    /// then asserts that it is and returns the task. The loop ends by returning the last observed
    /// state (never by throwing), so a failure shows what was actually there.
    /// </summary>
    protected async Task<DocumentBuilderTaskDto> WaitForXlsxCompleted(int formId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        DocumentBuilderTaskDto? task;

        while (true)
        {
            task = (await _filesApi.GetXlsxAsync(formId, TestContext.Current.CancellationToken)).Response;

            if (task?.IsCompleted == true || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        task.Should().NotBeNull("a run was queued for the form");
        task!.IsCompleted.Should().BeTrue("the report must finish within the deadline (last error: {0})", task.Error);

        return task;
    }

    /// <summary>Lists the <c>.xlsx</c> files currently in the given folder.</summary>
    protected async Task<List<FileEntryBaseDto>> GetXlsxFilesIn(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Files.Where(f => f.Title.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
