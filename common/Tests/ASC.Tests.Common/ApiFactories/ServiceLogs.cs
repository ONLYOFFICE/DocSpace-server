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
/// Reads the log files a service of the test stand wrote: its main log, its SQL log and its ASP.NET log, with the
/// archives it rotated since a given moment. For tests that a value never reaches a log.
/// </summary>
public static partial class ServiceLogs
{
    /// <summary>
    /// Waits until <paramref name="marker"/> appears in the logs of <paramref name="service"/> written since
    /// <paramref name="since"/>, so that the lines of the requests before it are on disk, then returns which of
    /// <paramref name="values"/> appear there as well.
    /// </summary>
    /// <param name="service">The log name of the service, such as <c>files</c>.</param>
    /// <param name="since">The start of the test, in local time, which is what the file times use.</param>
    /// <param name="marker">A value the requests surely wrote, such as an id the SQL log binds.</param>
    /// <param name="values">The values that must not be there.</param>
    /// <returns>The values found; throws when the marker does not appear within 30 seconds.</returns>
    public static async Task<string[]> FindAfterAsync(string service, DateTime since, string marker, IReadOnlyCollection<string> values)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var found = await FindAsync(service, since, [marker, .. values]);
            if (found.Contains(marker))
            {
                return [.. found.Where(v => v != marker)];
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"'{marker}' did not reach the logs of {service} within 30 seconds");
            }

            await Task.Delay(500);
        }
    }

    private static async Task<HashSet<string>> FindAsync(string service, DateTime since, IReadOnlyCollection<string> values)
    {
        var found = new HashSet<string>();
        var directory = AppPaths.GetTestLogsDirectory(Projects.ASC_AppHost.ProjectPath);

        if (!Directory.Exists(directory))
        {
            return found;
        }

        // Each file is written from the start of a rotation, so one written before the test began holds none of its lines.
        var paths = Directory.EnumerateFiles(directory, service + ".*")
            .Where(p => OwnFile().IsMatch(Path.GetFileName(p)[(service.Length + 1)..]) && File.GetLastWriteTime(p) >= since);

        foreach (var path in paths)
        {
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);

            if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = new ZipArchive(file, ZipArchiveMode.Read);
                foreach (var entry in archive.Entries)
                {
                    await using var content = entry.Open();
                    await ScanAsync(content, values, found);
                }
            }
            else
            {
                await ScanAsync(file, values, found);
            }
        }

        return found;
    }

    private static async Task ScanAsync(Stream content, IReadOnlyCollection<string> values, HashSet<string> found)
    {
        using var reader = new StreamReader(content);

        while (await reader.ReadLineAsync() is { } line)
        {
            foreach (var value in values)
            {
                if (line.Contains(value, StringComparison.Ordinal))
                {
                    found.Add(value);
                }
            }
        }
    }

    /// <summary>The files of the service itself (<c>log</c>, <c>sql.log</c>, <c>asp.11-24.3.zip</c>), not of its worker.</summary>
    [GeneratedRegex(@"^((sql|asp)\.)?(log|\d{2}-\d{2}\.\d+\.zip)$")]
    private static partial Regex OwnFile();
}
