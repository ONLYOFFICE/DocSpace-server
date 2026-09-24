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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// How far the parse or the import queued for this portal has got, and what it produced once it stopped.
/// </summary>
/// <example>
/// {
///   "progress": 99.99,
///   "error": "Connection failed",
///   "parseResult": {
///     "migratorName": "Nextcloud",
///     "operation": "parse"
///   },
///   "isCompleted": true
/// }
/// </example>
public class MigrationStatusDto
{
    /// <summary>
    /// The share of the job that is done, from 0 to 100. It advances unevenly, since the stages differ in
    /// length, so poll `isCompleted` rather than waiting for this to reach 100.
    /// </summary>
    /// <example>99.99</example>
    public double Progress { get; set; }

    /// <summary>
    /// The message that ended the job, in the portal language. It stays empty while nothing has gone wrong, so
    /// once `isCompleted` is `true` this field is what tells success from failure.
    /// </summary>
    /// <example>Connection failed</example>
    public string Error { get; set; }

    /// <summary>
    /// What the migrator has read so far. After a parse pass it holds the users, the groups and the archives it
    /// could not read, which is the body to edit and post to `POST api/2.0/migration/migrate`; during an import it
    /// also carries the accounts that were created and the ones that failed. Its own `operation` field, `parse`
    /// or `migration`, is what tells the two stages apart.
    /// </summary>
    /// <example>
    /// {
    ///   "migratorName": "Nextcloud",
    ///   "operation": "parse"
    /// }
    /// </example>
    public MigrationApiInfo ParseResult { get; set; }

    /// <summary>
    /// Whether the job has stopped, successfully or not. It is the field to poll on; the whole body comes back
    /// empty instead when the portal has no job at all, which is not an error.
    /// </summary>
    /// <example>true</example>
    public bool IsCompleted { get; set; }
}