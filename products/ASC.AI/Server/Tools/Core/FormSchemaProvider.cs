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

namespace ASC.AI.Tools.Core;

/// <summary>Submission table of a started filling-form: name, row count and column definitions.</summary>
public sealed record FormSchema(
    string TableName,
    long RowCount,
    IReadOnlyList<DbColumnDefinition> Columns);

/// <summary>
/// Reads the external-database submission table behind a filling-form. Shared by the form-data tools and
/// the attachment pre-analysis so the "is this form analysable" rules stay in one place.
/// </summary>
[Scope]
public class FormSchemaProvider(
    IDaoFactory daoFactory,
    ExternalDatabaseClient externalDatabaseClient,
    FormFillingReportCreator formFillingReportCreator,
    ILogger<FormSchemaProvider> logger)
{
    /// <summary>
    /// The cheap half: the submission table name, or null when this is not a started filling-form of its
    /// own or the table does not exist yet. Reads no rows and no metadata. Callers are expected to have
    /// checked <see cref="ExternalDatabaseClient.IsEnabled"/>.
    /// </summary>
    public async Task<string?> TryGetTableNameAsync(File<int> file)
    {
        var properties = await daoFactory.GetFileDao<int>().GetProperties(file.Id);
        var formFilling = properties?.FormFilling;

        if (formFilling?.StartFilling != true || formFilling.OriginalFormId != file.Id)
        {
            return null;
        }

        var tableName = FormFillingReportCreator.GetTableName(file.Id, file.Version);

        return await externalDatabaseClient.TableExistsAsync(tableName) ? tableName : null;
    }

    /// <summary>Full schema. Null when there is no submission table or it cannot be read.</summary>
    public async Task<FormSchema?> TryReadAsync(File<int> file)
    {
        try
        {
            var tableName = await TryGetTableNameAsync(file);
            if (tableName is null)
            {
                return null;
            }

            var rowCount = await externalDatabaseClient.CountAsync(tableName);
            var columns = (await formFillingReportCreator.GetColumnDefinitionsAsync(file.Id, file.Version)).ToList();

            return new FormSchema(tableName, rowCount, columns);
        }
        catch (Exception e)
        {
            logger.WarnFormSchemaReadFailed(e, file.Id);
            return null;
        }
    }
}

internal static partial class FormSchemaProviderLogger
{
    [LoggerMessage(LogLevel.Warning, "Failed to read the form submission schema for file {FileId}")]
    public static partial void WarnFormSchemaReadFailed(this ILogger<FormSchemaProvider> logger, Exception exception, int fileId);
}
