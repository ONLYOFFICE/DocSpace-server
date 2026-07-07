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

namespace ASC.Files.Tests.Data;

/// <summary>
/// Temporary per-phase stopwatch instrumentation. Writes elapsed ms to the test output so we can
/// see where a test's wall-clock time actually goes. Remove once the profiling is done.
/// </summary>
internal static class Timing
{
    public static async Task<T> Measure<T>(string name, Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await action();
        }
        finally
        {
            Write(name, sw.ElapsedMilliseconds);
        }
    }

    public static async ValueTask Measure(string name, Func<ValueTask> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await action();
        }
        finally
        {
            Write(name, sw.ElapsedMilliseconds);
        }
    }

    public static void Write(string name, long elapsedMs)
    {
        var message = $"[timing] {name}: {elapsedMs} ms";
        var ctx = TestContext.Current;

        // TestOutputHelper is attached per-test (shows under the test node in the IDE). During
        // InitializeAsync it can be null, so fall back to the diagnostic sink.
        if (ctx.TestOutputHelper != null)
        {
            ctx.TestOutputHelper.WriteLine(message);
        }
        else
        {
            ctx.SendDiagnosticMessage(message);
        }
    }
}
