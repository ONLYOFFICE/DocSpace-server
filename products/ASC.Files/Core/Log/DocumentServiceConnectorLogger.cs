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
internal static partial class DocumentServiceConnectorLogger
{
    [LoggerMessage(LogLevel.Debug, "DocService convert from {fromExtension} to {toExtension} - {documentUri}, DocServiceConverterUrl:{docServiceConverterUrl}")]
    public static partial void DebugDocServiceConvert(this ILogger<DocumentServiceConnector> logger, string fromExtension, string toExtension, string documentUri, string docServiceConverterUrl);

    [LoggerMessage(LogLevel.Debug, "DocService command {method} fileId {fileId} docKey {docKey} callbackUrl {callbackUrl} users {users} meta {meta}")]
    public static partial void DebugDocServiceCommand(this ILogger<DocumentServiceConnector> logger, string method, string fileId, string docKey, string callbackUrl, string users, string meta);

    [LoggerMessage(LogLevel.Debug, "DocService builder requestKey {requestKey} async {isAsync}")]
    public static partial void DebugDocServiceBuilderRequestKey(this ILogger<DocumentServiceConnector> logger, string requestKey, bool isAsync);

    [LoggerMessage(LogLevel.Debug, "DocService request version")]
    public static partial void DebugDocServiceRequestVersion(this ILogger<DocumentServiceConnector> logger);

    [LoggerMessage(LogLevel.Information, "DocService command response: '{error}' {errorString}")]
    public static partial void InfoDocServiceCommandResponse(this ILogger<DocumentServiceConnector> logger, ErrorTypes error, string errorString);

    [LoggerMessage(LogLevel.Error, "DocService command error")]
    public static partial void ErrorDocServiceCommandError(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Healthcheck DocService check error")]
    public static partial void ErrorDocServiceHealthcheck(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Converter DocService check error")]
    public static partial void ErrorConverterDocServiceCheckError(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Document DocService check error")]
    public static partial void ErrorDocumentDocServiceCheckError(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Command DocService check error")]
    public static partial void ErrorCommandDocServiceCheckError(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService check error")]
    public static partial void ErrorDocServiceCheck(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService error")]
    public static partial void ErrorDocServiceError(this ILogger<DocumentServiceConnector> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService command response: '{error}' {errorString}")]
    public static partial void ErrorDocServiceCommandResponse(this ILogger<DocumentServiceConnector> logger, ErrorTypes error, string errorString);
}