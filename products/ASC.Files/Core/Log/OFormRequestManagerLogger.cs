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

internal static partial class OFormRequestManagerLogger
{
    [LoggerMessage(LogLevel.Warning, "The template gallery answered {status} to {uri}")]
    public static partial void WarningOFormGalleryStatus(this ILogger<OFormRequestManager> logger, Uri uri, int status);

    [LoggerMessage(LogLevel.Warning, "The template gallery answered {uri} without its template list")]
    public static partial void WarningOFormGalleryNoList(this ILogger<OFormRequestManager> logger, Uri uri);

    [LoggerMessage(LogLevel.Warning, "The file from the template gallery lacks the expected format signature: {uri}")]
    public static partial void WarningOFormGalleryNotForm(this ILogger<OFormRequestManager> logger, Uri uri);

    [LoggerMessage(LogLevel.Warning, "The file from the template gallery is larger than {maxSize} bytes: {uri}")]
    public static partial void WarningOFormGalleryFileTooLarge(this ILogger<OFormRequestManager> logger, Uri uri, long maxSize);

    [LoggerMessage(LogLevel.Warning, "The template gallery gave a file address that cannot be read: {url}")]
    public static partial void WarningOFormGalleryFileAddress(this ILogger<OFormRequestManager> logger, string url);

    [LoggerMessage(LogLevel.Warning, "The template gallery could not be reached or gave an unreadable answer")]
    public static partial void WarningOFormGalleryUnreachable(this ILogger<OFormRequestManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "The download of a template from the template gallery failed")]
    public static partial void ErrorOFormGalleryDownloadFailed(this ILogger<OFormRequestManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The check of the template gallery got {status} from {uri}")]
    public static partial void WarningOFormGalleryProbeStatus(this ILogger<OFormRequestManager> logger, Uri uri, int status);

    [LoggerMessage(LogLevel.Warning, "The check of the template gallery could not reach it or read its answer")]
    public static partial void WarningOFormGalleryProbeFailed(this ILogger<OFormRequestManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "The check of the template gallery failed for a reason other than the gallery")]
    public static partial void ErrorOFormGalleryProbeError(this ILogger<OFormRequestManager> logger, Exception exception);
}
