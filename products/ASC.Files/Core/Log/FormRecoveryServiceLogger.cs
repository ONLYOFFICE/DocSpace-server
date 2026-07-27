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

internal static partial class FormRecoveryServiceLogger
{
    [LoggerMessage(LogLevel.Information, "FormRecovery: room {roomId} — found {count} orphaned form(s) to repair.")]
    public static partial void InfoRecoveryOrphansFound(this ILogger<FormRecoveryService> logger, int roomId, int count);

    [LoggerMessage(LogLevel.Information, "FormRecovery: rebuilt report for form {formId} key set #{version} with {submissionCount} submission(s); results file at version {fileVersion}.")]
    public static partial void InfoRecoveryReportVersionRebuilt(this ILogger<FormRecoveryService> logger, int formId, int version, int submissionCount, int fileVersion);

    [LoggerMessage(LogLevel.Error, "FormRecovery: failed to recover form {formId} in room {roomId}.")]
    public static partial void ErrorRecoveryFormFailed(this ILogger<FormRecoveryService> logger, Exception exception, int formId, int roomId);

    [LoggerMessage(LogLevel.Warning, "FormRecovery: could not query the search index for folder {folderId}; skipping it for this run.")]
    public static partial void WarnRecoveryIndexQueryFailed(this ILogger<FormRecoveryService> logger, int folderId);

    [LoggerMessage(LogLevel.Error, "FormRecovery: failed to rebuild the xlsx report for form {formId} in room {roomId}.")]
    public static partial void ErrorRecoveryXlsxRebuildFailed(this ILogger<FormRecoveryService> logger, Exception exception, int formId, int roomId);

    [LoggerMessage(LogLevel.Warning, "FormRecovery: failed to clean up temp forms-data file {formsDataUrl}.")]
    public static partial void WarnRecoveryTempCleanupFailed(this ILogger<FormRecoveryService> logger, Exception exception, string formsDataUrl);

    [LoggerMessage(LogLevel.Warning, "FormRecovery: could not resolve the field layout of form {formId} version {version}; it won't participate in version matching.")]
    public static partial void WarnRecoveryTemplateVersionExtractFailed(this ILogger<FormRecoveryService> logger, Exception exception, int formId, int version);

    [LoggerMessage(LogLevel.Warning, "FormRecovery: completed form {fileId} of form {formId} in room {roomId} matched no template version by field set; routed to the current version.")]
    public static partial void WarnRecoveryVersionUnmatched(this ILogger<FormRecoveryService> logger, int fileId, int formId, int roomId);
}
