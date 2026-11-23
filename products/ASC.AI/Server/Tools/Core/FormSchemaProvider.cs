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

/// <summary>
/// Submission table of a started filling-form: name, row count, column definitions, the database holding it, the form
/// version it is read for, the versions whose responses it holds and the earlier versions with responses apart (null
/// when not read or unknown).
/// </summary>
public sealed record FormSchema(
    string TableName,
    long RowCount,
    IReadOnlyList<DbColumnDefinition> Columns,
    ExternalDatabaseClient Client,
    int Version,
    IReadOnlyList<int> VersionsInTable,
    IReadOnlyList<int>? VersionsApart);

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
    BuiltinFormsAnalysisClient builtinFormsAnalysisClient,
    FormFillingReportCreator formFillingReportCreator,
    TenantManager tenantManager,
    IFusionCache fusionCache,
    ILogger<FormSchemaProvider> logger)
{
    private static readonly TimeSpan _tableNameCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan _noTableCacheDuration = TimeSpan.FromMinutes(1);

    // Earlier versions take no new responses (their drafts are refused), so the answer holds; when the index cannot
    // give it, it is asked again soon.
    private static readonly TimeSpan _versionsApartCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan _versionsApartUnknownCacheDuration = TimeSpan.FromMinutes(1);

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

        return builtinFormsDatabaseClient.IsEnabled() && FormFillingReportCreator.HasResponsesInVersionTable(await GetStartedFormFillingAsync(file), file.Version);
    }

    /// <summary>
    /// The cheap half: the submission table name, or null when this is not a started filling-form of its
    /// own or the table does not exist yet. Reads no rows and no metadata. Callers are expected to have
    /// checked <see cref="IsEnabled"/>.
    /// </summary>
    public async Task<string?> TryGetTableNameAsync(File<int> file)
    {
        // Read first and left uncached: filling starts at any moment, and a confirmed rename moves the version to
        // another table; neither costs an external call.
        return await TryGetTableNameAsync(file, await GetStartedFormFillingAsync(file));
    }

    private async Task<string?> TryGetTableNameAsync(File<int> file, FormFillingProperties<int>? formFilling)
    {
        if (!externalDatabaseClient.IsEnabled())
        {
            return await TryGetBuiltinTableNameAsync(file, formFilling);
        }

        if (formFilling is null)
        {
            return null;
        }

        var tableName = FormFillingReportCreator.GetVersionTableName(formFilling, file.Id, file.Version);
        var cacheKey = GetTableNameCacheKey(tenantManager.GetCurrentTenantId(), tableName);

        var cached = await fusionCache.TryGetAsync<string?>(cacheKey);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var exists = await externalDatabaseClient.TableExistsAsync(tableName);

        // An existing table stays, so a hit stays valid. The "not yet" answer is held far shorter — it flips as soon
        // as the first submission lands.
        await fusionCache.SetAsync<string?>(
            cacheKey,
            exists ? tableName : null,
            opt => opt.SetDuration(exists ? _tableNameCacheDuration : _noTableCacheDuration));

        return exists ? tableName : null;
    }

    // Not cached: an idle built-in table may be dropped at any time, and it is restored here.
    private async Task<string?> TryGetBuiltinTableNameAsync(File<int> file, FormFillingProperties<int>? formFilling)
    {
        if (!builtinFormsDatabaseClient.IsEnabled() || formFilling is null || !FormFillingReportCreator.HasResponsesInVersionTable(formFilling, file.Version))
        {
            return null;
        }

        return await formFillingReportCreator.EnsureBuiltinDbTableAsync(file.Id, file.Version, formFilling.RoomId);
    }

    private async Task<FormFillingProperties<int>?> GetStartedFormFillingAsync(File<int> file)
    {
        var formFilling = (await daoFactory.GetFileDao<int>().GetProperties(file.Id))?.FormFilling;

        return formFilling?.StartFilling == true && formFilling.OriginalFormId == file.Id ? formFilling : null;
    }

    private static string GetTableNameCacheKey(int tenantId, string tableName)
    {
        return $"ai:form:table:{tenantId}:{tableName}";
    }

    private static string GetVersionsApartCacheKey(int tenantId, int fileId, int version)
    {
        return $"ai:form:versionsapart:{tenantId}:{fileId}:{version}";
    }

    // The earlier versions with responses in other tables, which only the search index can tell: read once for a form
    // version, since the tools are built for every call the model makes.
    private async Task<IReadOnlyList<int>?> GetVersionsApartAsync(File<int> file, FormFillingProperties<int> formFilling, IReadOnlyList<int> inTable)
    {
        // Every version writes to this table, as when the form never changed: no responses can be anywhere else.
        if (Enumerable.Range(1, file.Version).All(inTable.Contains))
        {
            return [];
        }

        var cacheKey = GetVersionsApartCacheKey(tenantManager.GetCurrentTenantId(), file.Id, file.Version);

        var cached = await fusionCache.TryGetAsync<List<int>?>(cacheKey);
        var apart = cached.HasValue ? cached.Value : null;
        if (!cached.HasValue)
        {
            apart = await formFillingReportCreator.GetVersionsWithResponsesApartAsync(file.Id, formFilling, file.Version);
            if (apart is null)
            {
                logger.WarnFormVersionsApartUnknown(file.Id);
            }

            await fusionCache.SetAsync(
                cacheKey,
                apart,
                opt => opt.SetDuration(apart is null ? _versionsApartUnknownCacheDuration : _versionsApartCacheDuration));
        }

        // A version moved into the table since then is no longer apart.
        return apart?.Where(v => !inTable.Contains(v)).ToList();
    }

    /// <summary>
    /// Full schema. Null when there is no submission table or it cannot be read. The versions with responses apart
    /// cost the search index and are read only when asked for: the model's prompt needs them, the starter questions do
    /// not.
    /// </summary>
    public async Task<FormSchema?> TryReadAsync(File<int> file, bool withVersionsApart = false)
    {
        try
        {
            // One read of the properties: the table and the versions it holds come from the same state.
            var formFilling = await GetStartedFormFillingAsync(file);
            var tableName = await TryGetTableNameAsync(file, formFilling);
            if (tableName is null || formFilling is null)
            {
                return null;
            }

            // The built-in database is read through a read-only, time-bounded client: the queries are built by an assistant.
            var client = externalDatabaseClient.IsEnabled() ? externalDatabaseClient : builtinFormsAnalysisClient;
            var rowCount = await client.CountAsync(tableName);
            // The columns as the table holds them: a field whose name the database cuts to another field's is not there.
            var columns = client.GetStoredColumns(await formFillingReportCreator.GetColumnDefinitionsAsync(file.Id, file.Version));
            var inTable = FormFillingReportCreator.GetVersionsInTable(formFilling, file.Version);
            var apart = withVersionsApart ? await GetVersionsApartAsync(file, formFilling, inTable) : null;

            return new FormSchema(tableName, rowCount, columns, client, file.Version, inTable, apart);
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

    [LoggerMessage(LogLevel.Warning, "The search index could not tell which earlier versions of form {FileId} have responses in other tables")]
    public static partial void WarnFormVersionsApartUnknown(this ILogger<FormSchemaProvider> logger, int fileId);
}
