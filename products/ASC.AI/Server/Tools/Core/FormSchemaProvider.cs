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

/// <summary>Submission table of a started filling-form: name, row count, column definitions and the database holding it.</summary>
public sealed record FormSchema(
    string TableName,
    long RowCount,
    IReadOnlyList<DbColumnDefinition> Columns,
    ExternalDatabaseClient Client);

/// <summary>
/// Reads the submission table behind a filling-form: in the external database when the portal has one, otherwise in
/// the built-in one. Shared by the form-data tools and the attachment pre-analysis so the "is this form analysable"
/// rules stay in one place.
/// </summary>
[Scope]
public class FormSchemaProvider(
    IDaoFactory daoFactory,
    ExternalDatabaseClient externalDatabaseClient,
    BuiltinFormsDatabaseClient builtinFormsDatabaseClient,
    FormFillingReportCreator formFillingReportCreator,
    TenantManager tenantManager,
    IFusionCache fusionCache,
    ILogger<FormSchemaProvider> logger)
{
    private static readonly TimeSpan _tableNameCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan _noTableCacheDuration = TimeSpan.FromMinutes(1);

    /// <summary>Whether a database for form submissions is configured: the external one or the built-in one.</summary>
    public bool IsEnabled() => externalDatabaseClient.IsEnabled() || builtinFormsDatabaseClient.IsEnabled();

    /// <summary>
    /// Whether the form's responses can be analysed. For the built-in database it needs no database call: a dropped
    /// table is restored when the schema is read.
    /// </summary>
    public async Task<bool> CanAnalyzeAsync(File<int> file)
    {
        if (externalDatabaseClient.IsEnabled())
        {
            return await TryGetTableNameAsync(file) is not null;
        }

        return builtinFormsDatabaseClient.IsEnabled() && await GetStartedFormFillingAsync(file) is { ResultFormNumber: > 0 };
    }

    /// <summary>
    /// The cheap half: the submission table name, or null when this is not a started filling-form of its
    /// own or the table does not exist yet. Reads no rows and no metadata. Callers are expected to have
    /// checked <see cref="IsEnabled"/>.
    /// </summary>
    public async Task<string?> TryGetTableNameAsync(File<int> file)
    {
        if (!externalDatabaseClient.IsEnabled())
        {
            return await TryGetBuiltinTableNameAsync(file);
        }

        var cacheKey = GetTableNameCacheKey(tenantManager.GetCurrentTenantId(), file);

        var cached = await fusionCache.TryGetAsync<string?>(cacheKey);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        // Left uncached: filling starts at any moment, and this branch costs no external call anyway.
        if (await GetStartedFormFillingAsync(file) is null)
        {
            return null;
        }

        var tableName = FormFillingReportCreator.GetTableName(file.Id, file.Version);
        var exists = await externalDatabaseClient.TableExistsAsync(tableName);

        // A table is created once per form version and never renamed, so a hit stays valid. The
        // "not yet" answer is held far shorter — it flips as soon as the first submission lands.
        await fusionCache.SetAsync<string?>(
            cacheKey,
            exists ? tableName : null,
            opt => opt.SetDuration(exists ? _tableNameCacheDuration : _noTableCacheDuration));

        return exists ? tableName : null;
    }

    // Not cached: an idle built-in table may be dropped at any time, and it is restored here.
    private async Task<string?> TryGetBuiltinTableNameAsync(File<int> file)
    {
        if (!builtinFormsDatabaseClient.IsEnabled() || await GetStartedFormFillingAsync(file) is not { ResultFormNumber: > 0 } formFilling)
        {
            return null;
        }

        return await formFillingReportCreator.EnsureBuiltinDbTableAsync(file.Id, file.Version, formFilling.RoomId)
            ? FormFillingReportCreator.GetTableName(file.Id, file.Version)
            : null;
    }

    private async Task<FormFillingProperties<int>?> GetStartedFormFillingAsync(File<int> file)
    {
        var formFilling = (await daoFactory.GetFileDao<int>().GetProperties(file.Id))?.FormFilling;

        return formFilling?.StartFilling == true && formFilling.OriginalFormId == file.Id ? formFilling : null;
    }

    private static string GetTableNameCacheKey(int tenantId, File<int> file)
    {
        return $"ai:form:table:{tenantId}:{file.Id}:{file.Version}";
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

            var client = externalDatabaseClient.IsEnabled() ? externalDatabaseClient : builtinFormsDatabaseClient;
            var rowCount = await client.CountAsync(tableName);
            var columns = (await formFillingReportCreator.GetColumnDefinitionsAsync(file.Id, file.Version)).ToList();

            return new FormSchema(tableName, rowCount, columns, client);
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
