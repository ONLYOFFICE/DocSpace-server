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

namespace ASC.Files.Tests.Tests._01_Files.Log;

/// <summary>
/// Shared setup for the <c>GET /files/file/{id}/log</c> suite: inviting a member into the room whose
/// file log is being read.
/// </summary>
public abstract class FileLogTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Polls a file's log until it holds at least one entry, since audit entries are written after
    /// the request that caused them returns. Returns the last observed page, empty if nothing landed
    /// before the deadline.
    /// </summary>
    protected async Task<List<HistoryDto>> PollFileHistoryAsync(int fileId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (true)
        {
            var history = (await _filesApi.GetFileHistoryAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (history.Count > 0 || DateTime.UtcNow >= deadline)
            {
                return history;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }
}
