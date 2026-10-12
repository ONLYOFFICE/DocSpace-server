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

    /// <summary>
    /// The repository root that owns <c>Data/</c>, <c>Logs/</c> and <c>buildtools/</c>.
    /// </summary>
    public static string GetBasePath(string appHostDirectory)
    {
        return Path.GetFullPath(Path.Combine(appHostDirectory, "..", "..", ".."));
    }

    /// <summary>
    /// <c>$STORAGE_ROOT</c> for the integration-test profile — a subfolder, so a test run never
    /// touches the developer's own <c>Data</c> tree and can be wiped as a whole afterwards.
    /// </summary>
    public static string GetTestStorageRoot(string basePath)
    {
        return Path.Combine(basePath, "Data", TestSubFolder);
    }

    /// <summary>
    /// <c>web:temp</c> for the integration-test profile. Without it every service falls back to
    /// <c>&lt;ContentRoot&gt;/temp</c> (see <c>TempPath</c>) and litters the source tree — e.g.
    /// <c>products/ASC.Files/Worker/temp</c>. Keeping it inside the test storage root means the
    /// existing cleanup wipes it along with the rest of the run's data.
    /// </summary>
    public static string GetTestTempDirectory(string basePath)
    {
        return Path.Combine(GetTestStorageRoot(basePath), "temp");
    }

    /// <summary>
    /// Log directory for the integration-test profile.
    /// </summary>
    public static string GetTestLogsDirectory(string basePath)
    {
        return Path.Combine(basePath, "Logs", TestSubFolder);
    }
}
