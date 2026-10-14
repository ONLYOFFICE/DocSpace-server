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

namespace ASC.Files.Core.Log;

internal static partial class FormFillingReportCreatorLogger
{
    [LoggerMessage(LogLevel.Warning, "Gap sync: skipped form_id={FormId} for table {TableName} — no FormsData in OpenSearch")]
    public static partial void WarnGapSyncSkippedNoData(
        this ILogger<FormFillingReportCreator> logger, int formId, string tableName);

    [LoggerMessage(LogLevel.Error, "Gap sync: failed to upsert form_id={FormId} into table {TableName}")]
    public static partial void ErrorGapSyncUpsertFailed(
        this ILogger<FormFillingReportCreator> logger, Exception exception, int formId, string tableName);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: table {TableName} is not restored, {Count} submissions exceed the search index query limit")]
    public static partial void WarnBuiltinDbRestoreTooLarge(
        this ILogger<FormFillingReportCreator> logger, string tableName, long count);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: failed to record the use of table {TableName}")]
    public static partial void WarnBuiltinDbTouchFailed(
        this ILogger<FormFillingReportCreator> logger, Exception exception, string tableName);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: failed to compare the fields of form {FormId} version {Version} with the previous version")]
    public static partial void WarnBuiltinDbVersionCheckFailed(
        this ILogger<FormFillingReportCreator> logger, Exception exception, int formId, int version);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: version {Version} keeps a table of its own, failed to continue table {TableName}")]
    public static partial void WarnBuiltinDbVersionLinkFailed(
        this ILogger<FormFillingReportCreator> logger, Exception exception, string tableName, int version);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: version {Version} keeps a table of its own, its columns conflict with table {TableName}")]
    public static partial void WarnBuiltinDbColumnConflict(
        this ILogger<FormFillingReportCreator> logger, string tableName, int version);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: table {SourceTable} is not merged into {TargetTable} yet, one of them cannot be restored")]
    public static partial void WarnBuiltinDbMergeDeferred(
        this ILogger<FormFillingReportCreator> logger, string sourceTable, string targetTable);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: failed to merge table {SourceTable} into {TargetTable}")]
    public static partial void WarnBuiltinDbMergeFailed(
        this ILogger<FormFillingReportCreator> logger, Exception exception, string sourceTable, string targetTable);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: table {SourceTable} merged into {TargetTable} is kept, not all of its rows are there")]
    public static partial void WarnBuiltinDbMergedTableKept(
        this ILogger<FormFillingReportCreator> logger, string sourceTable, string targetTable);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: submission {FileId} of form {FormId} version {Version} is left to the index, its table kept moving")]
    public static partial void WarnBuiltinDbSubmissionSkipped(
        this ILogger<FormFillingReportCreator> logger, int fileId, int formId, int version);
}
