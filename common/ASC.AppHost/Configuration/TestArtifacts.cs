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
/// Cleanup of what an integration-test run leaves on disk. It lives next to the AppHost because the
/// AppHost is what points the services at those folders — every test fixture shares this one
/// implementation instead of guessing the layout for itself.
/// </summary>
public static class TestArtifacts
{
    /// <summary>
    /// Deletes the storage tree handed to the services as <c>$STORAGE_ROOT</c>. Everything in it
    /// belongs to the throwaway portals of the finished run, whose database dies with its container.
    /// Logs are deliberately kept — they are what one looks at after a failure.
    /// </summary>
    /// <param name="appHostDirectory">
    /// Directory of the AppHost project — <c>Projects.ASC_AppHost.ProjectPath</c> in a test fixture.
    /// </param>
    /// <returns>
    /// The failure if the folder could not be removed, otherwise <c>null</c>. Cleanup is
    /// best-effort: it must never turn a green run red, so nothing is thrown to the caller.
    /// </returns>
    public static async Task<Exception?> DeleteStorageAsync(string appHostDirectory)
    {
        var storageRoot = AppPaths.GetTestStorageRoot(AppPaths.GetBasePath(appHostDirectory));

        if (!Directory.Exists(storageRoot))
        {
            return null;
        }

        // A just-stopped service can still hold a handle inside the folder for a moment, and on
        // Windows that fails the delete outright — hence the retries.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Directory.Delete(storageRoot, recursive: true);
                return null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (attempt == 3)
                {
                    return e;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
