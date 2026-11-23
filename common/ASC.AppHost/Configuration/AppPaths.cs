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

namespace ASC.AppHost.Configuration;

/// <summary>
/// Where the orchestrated services keep their data and logs. The integration-test paths are public
/// because the test fixtures reuse them: they have to delete the very folders the AppHost hands to
/// the services, so both sides must agree on the location.
/// </summary>
public static class AppPaths
{
    private const string TestSubFolder = "test";

    // The main checkout's folder under Data/test. GetCheckoutName never gives it: a name starting with "_" is trimmed.
    private const string MainCheckoutFolder = "_main";

    /// <summary>
    /// The repository root that owns <c>Data/</c>, <c>Logs/</c> and <c>buildtools/</c>: the nearest folder above
    /// the AppHost that holds <c>buildtools/</c>. In a plain checkout that is the parent of <c>server/</c>, three
    /// levels up; in a git worktree under <c>server/.claude/worktrees/&lt;name&gt;/</c> it is further up, and a fixed
    /// three levels would point into the worktrees folder, where no buildtools, client or logs exist. Without
    /// any such folder the fixed three levels are kept, as before.
    /// </summary>
    public static string GetBasePath(string appHostDirectory)
    {
        var fallback = Path.GetFullPath(Path.Combine(appHostDirectory, "..", "..", ".."));

        for (var directory = new DirectoryInfo(Path.GetFullPath(appHostDirectory)).Parent; directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "buildtools")))
            {
                return directory.FullName;
            }
        }

        return fallback;
    }

    /// <summary>
    /// The configuration directory the services read (<c>buildtools/config</c> under <see cref="GetBasePath"/>).
    /// Handed to every service started from the sources, whose own <c>appsettings.json</c> names it by a path
    /// relative to the project folder that is wrong in a git worktree.
    /// </summary>
    public static string GetConfigDirectory(string basePath)
    {
        return Path.Combine(basePath, "buildtools", "config");
    }

    /// <summary>
    /// What sets the test folders of one checkout apart from another's: empty for the main checkout (a folder named
    /// <c>server</c> right under <see cref="GetBasePath"/>), otherwise the checkout's folder name - a git worktree's -
    /// in <c>[a-z0-9_.-]</c>, with a hash of its path added when that loses a character. Checkouts share a base path -
    /// every worktree reaches the main one's, or, with a <c>buildtools</c> link in <c>.claude/worktrees</c>, that
    /// folder - so without it two test runs on different branches would write into the same storage and logs.
    /// </summary>
    /// <param name="appHostDirectory">Directory of the AppHost project.</param>
    public static string GetCheckoutName(string appHostDirectory)
    {
        const string mainCheckout = "server";

        var checkout = new DirectoryInfo(Path.GetFullPath(Path.Combine(appHostDirectory, "..", "..")));
        var basePath = Path.TrimEndingDirectorySeparator(GetBasePath(appHostDirectory));

        if (checkout.Parent is null
            || (string.Equals(checkout.Name, mainCheckout, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.TrimEndingDirectorySeparator(checkout.Parent.FullName), basePath, StringComparison.OrdinalIgnoreCase)))
        {
            return string.Empty;
        }

        return ToFolderName(checkout.Name, checkout.FullName);
    }

    /// <summary>
    /// <c>$STORAGE_ROOT</c> for the integration-test profile — a subfolder, so a test run never
    /// touches the developer's own <c>Data</c> tree and can be wiped as a whole afterwards:
    /// <c>Data/test/&lt;checkout&gt;/&lt;suite&gt;</c>. The checkout is <see cref="GetCheckoutName"/>, or
    /// <c>_main</c> for the main checkout, a name no worktree gets; the suite is the test assembly the AppHost
    /// runs in (<see cref="GetSuiteName"/>). No run's root holds another's, so the cleanup of one never deletes
    /// the storage of a run still going, of another checkout or of another suite of the same one.
    /// </summary>
    /// <param name="appHostDirectory">Directory of the AppHost project.</param>
    public static string GetTestStorageRoot(string appHostDirectory)
    {
        var checkout = GetCheckoutName(appHostDirectory);

        return Path.Combine(GetBasePath(appHostDirectory), "Data", TestSubFolder, checkout.Length == 0 ? MainCheckoutFolder : checkout, GetSuiteName());
    }

    /// <summary>
    /// The test assembly this process runs: the test fixture boots the AppHost inside it, so the AppHost and the
    /// fixture's cleanup name the same suite. <c>asc.apphost</c> when the AppHost runs on its own (<c>dotnet run</c>),
    /// <c>apphost</c> when the process has no entry assembly.
    /// </summary>
    public static string GetSuiteName()
    {
        var name = Assembly.GetEntryAssembly()?.GetName().Name;

        return string.IsNullOrEmpty(name) ? "apphost" : ToFolderName(name, name);
    }

    /// <summary>
    /// A folder name made of <c>[a-z0-9_.-]</c>, starting with a letter or a digit. When that
    /// loses something of <paramref name="name"/> - a character replaced, a prefix trimmed, nothing left - a hash of
    /// <paramref name="identity"/> is added, so such a name meets no other. Names that differ only in letter case, and
    /// worktrees of one name in two different folders that share a base path, still meet.
    /// </summary>
    private static string ToFolderName(string name, string identity)
    {
        var lower = name.ToLowerInvariant();
        var folder = new string(lower.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray())
            .TrimStart('-', '_', '.');

        if (folder == lower)
        {
            return folder;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToLowerInvariant())))[..8];

        return folder.Length == 0 ? hash : $"{folder}-{hash}";
    }

    /// <summary>
    /// <c>web:temp</c> for the integration-test profile. Without it every service falls back to
    /// <c>&lt;ContentRoot&gt;/temp</c> (see <c>TempPath</c>) and litters the source tree — e.g.
    /// <c>products/ASC.Files/Worker/temp</c>. Keeping it inside the test storage root means the
    /// existing cleanup wipes it along with the rest of the run's data.
    /// </summary>
    /// <param name="appHostDirectory">Directory of the AppHost project.</param>
    public static string GetTestTempDirectory(string appHostDirectory)
    {
        return Path.Combine(GetTestStorageRoot(appHostDirectory), "temp");
    }

    /// <summary>
    /// Log directory for the integration-test profile: <c>Logs/test</c> for the main checkout,
    /// <c>Logs/test/&lt;worktree&gt;</c> for a git worktree (<see cref="GetCheckoutName"/>).
    /// </summary>
    /// <param name="appHostDirectory">Directory of the AppHost project.</param>
    public static string GetTestLogsDirectory(string appHostDirectory)
    {
        return Path.Combine(GetBasePath(appHostDirectory), "Logs", TestSubFolder, GetCheckoutName(appHostDirectory));
    }
}
