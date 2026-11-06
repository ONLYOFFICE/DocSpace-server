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

namespace ASC.Tests.Common.ApiFactories;

/// <summary>
/// Finds the notifications the socket service refused during a run, by reading its access log.
/// </summary>
/// <remarks>
/// The notifiers build some of their method names at run time and send them in the background, and a refused one fails
/// nothing: the operation succeeds and the client just stays stale. So the contract between the C# notifiers and the
/// routes of <c>ASC.Socket.IO</c> is checked on what the run really sent, whichever suite and whichever test caused it.
/// </remarks>
internal static partial class SocketHubRefusals
{
    private const string LogDateFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Returns "route status" with the number of times each happened since <paramref name="since"/>.
    /// </summary>
    /// <param name="logsDirectory">The log folder of the run.</param>
    /// <param name="since">The start of the run, in local time, which is what the service writes.</param>
    public static async Task<Dictionary<string, int>> FindAsync(string logsDirectory, DateTime since)
    {
        var refusals = new Dictionary<string, int>();

        if (!Directory.Exists(logsDirectory))
        {
            return refusals;
        }

        // The service rotates at 50 MB and compresses what it rotated, so a long run spreads over several files.
        foreach (var path in Directory.EnumerateFiles(logsDirectory, "socket-io.*").Where(p => File.GetLastWriteTime(p) >= since))
        {
            await ScanAsync(path, since, refusals);
        }

        return refusals;
    }

    private static async Task ScanAsync(string path, DateTime since, Dictionary<string, int> refusals)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
        await using Stream content = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(file, CompressionMode.Decompress)
            : file;
        using var reader = new StreamReader(content);

        while (await reader.ReadLineAsync() is { } line)
        {
            if (!line.Contains("/controller/", StringComparison.Ordinal))
            {
                continue;
            }

            using var entry = JsonDocument.Parse(line);

            var date = entry.RootElement.GetProperty("date").GetString();
            var match = AccessLine().Match(entry.RootElement.GetProperty("message").GetString() ?? "");

            if (match.Success
                && int.Parse(match.Groups["status"].Value, CultureInfo.InvariantCulture) >= 400
                && DateTime.ParseExact(date!, LogDateFormat, CultureInfo.InvariantCulture) >= since)
            {
                var key = $"{match.Groups["route"].Value} {match.Groups["status"].Value}";

                refusals[key] = refusals.GetValueOrDefault(key) + 1;
            }
        }
    }

    [GeneratedRegex(@"^POST (?<route>/controller/\S+) (?<status>\d{3}) ")]
    private static partial Regex AccessLine();
}
