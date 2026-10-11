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
    [LoggerMessage(LogLevel.Information, "Form recovery: form {formId} in room {roomId} has {count} completed forms missing from the search index")]
    public static partial void InfoRecoveryOrphansFound(this ILogger<FormRecoveryService> logger, int formId, int roomId, int count);

    [LoggerMessage(LogLevel.Error, "Form recovery: failed to recover form {formId} in room {roomId}")]
    public static partial void ErrorRecoveryFormFailed(this ILogger<FormRecoveryService> logger, Exception exception, int formId, int roomId);

    [LoggerMessage(LogLevel.Error, "Form recovery: failed to recover completed form {fileId} of form {formId}")]
    public static partial void ErrorRecoverySubmissionFailed(this ILogger<FormRecoveryService> logger, Exception exception, int fileId, int formId);

    [LoggerMessage(LogLevel.Warning, "Form recovery: the search index could not be read for folder {folderId}, nothing is recovered")]
    public static partial void WarnRecoveryIndexQueryFailed(this ILogger<FormRecoveryService> logger, int folderId);

    [LoggerMessage(LogLevel.Warning, "Form recovery: failed to read the fields of form {formId} version {version}, the version is not matched")]
    public static partial void WarnRecoveryVersionFieldsFailed(this ILogger<FormRecoveryService> logger, Exception exception, int formId, int version);

    [LoggerMessage(LogLevel.Warning, "Form recovery: completed form {fileId} of form {formId} in room {roomId} matches no form version by its fields, skipped")]
    public static partial void WarnRecoveryVersionUnmatched(this ILogger<FormRecoveryService> logger, int fileId, int formId, int roomId);

    [LoggerMessage(LogLevel.Warning, "Form recovery: completed form {fileId} of form {formId} holds no form data, skipped")]
    public static partial void WarnRecoveryNoFormData(this ILogger<FormRecoveryService> logger, int fileId, int formId);
}
