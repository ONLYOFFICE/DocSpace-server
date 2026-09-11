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

namespace ASC.People.Tests.Tests._02_UserType;

/// <summary>
/// A user type change (<c>startUserTypeUpdate</c>) runs as a background task - the outcome, and
/// sometimes even the permission check, only becomes visible through the progress endpoint some
/// time after the start call returns. Every test that starts one polls through here instead of
/// reading the progress once, so the assertion never races the background task.
/// </summary>
internal static class UserTypeUpdatePolling
{
    /// <summary>
    /// Polls <c>GET /people/type/progress/{userid}</c> on a deadline and returns the last observed
    /// progress - completed or not - so a timing-out assertion still shows what was actually there.
    /// </summary>
    public static async Task<TaskProgressResponseDto> WaitForCompletionAsync(UserTypeApi userTypeApi, Guid userId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        TaskProgressResponseDto last;

        while (true)
        {
            last = (await userTypeApi.GetUserTypeUpdateProgressAsync(userId, TestContext.Current.CancellationToken)).Response;

            if (last.IsCompleted || DateTime.UtcNow >= deadline)
            {
                return last;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
