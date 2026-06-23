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
internal static partial class FileHandlerLogger
{
    [LoggerMessage(LogLevel.Debug, "DocService StreamFile payload: {payload}")]
    public static partial void DebugDocServiceStreamFilePayload(this ILogger<FileHandlerService> logger, string payload);

    [LoggerMessage(LogLevel.Debug, "DocService track fileid: {fileId}")]
    public static partial void DebugDocServiceTrackFileid(this ILogger<FileHandlerService> logger, string fileId);

    [LoggerMessage(LogLevel.Debug, "DocService track body: {body}")]
    public static partial void DebugDocServiceTrackBody(this ILogger<FileHandlerService> logger, string body);

    [LoggerMessage(LogLevel.Debug, "DocService track payload: {payload}")]
    public static partial void DebugDocServiceTrackPayload(this ILogger<FileHandlerService> logger, string payload);

    [LoggerMessage(LogLevel.Error, "BulkDownloadFile failed for user {id}")]
    public static partial void ErrorBulkDownloadFileFailed(this ILogger<FileHandlerService> logger, Guid id, Exception exception);

    [LoggerMessage(LogLevel.Error, "DownloadFile")]
    public static partial void ErrorDownloadFile(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Url: {url} IsClientConnected:{isCancellationRequested}, line number:{line} frame:{stackFrame}")]
    public static partial void ErrorUrl(this ILogger<FileHandlerService> logger, Uri url, bool isCancellationRequested, int line, StackFrame stackFrame, Exception exception);

    [LoggerMessage(LogLevel.Error, "{authKey} {validateResult}: {url}")]
    public static partial void Error(this ILogger<FileHandlerService> logger, string authKey, EmailValidationKeyProvider.ValidationResult validateResult, Uri url, Exception exception);

    [LoggerMessage(LogLevel.Error, "Download stream header {url}")]
    public static partial void ErrorDownloadStreamHeader(this ILogger<FileHandlerService> logger, Uri url, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error for: {url}")]
    public static partial void ErrorForUrl(this ILogger<FileHandlerService> logger, Uri url, Exception exception);

    [LoggerMessage(LogLevel.Error, "StreamFile")]
    public static partial void ErrorStreamFile(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "EmptyFile")]
    public static partial void ErrorEmptyFile(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "TempFile")]
    public static partial void ErrorTempFile(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DifferenceFile")]
    public static partial void ErrorDifferenceFile(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Thumbnail")]
    public static partial void ErrorThumbnail(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "FileHandler")]
    public static partial void ErrorFileHandler(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService track error read body")]
    public static partial void ErrorDocServiceTrackReadBody(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService track header")]
    public static partial void ErrorDocServiceTrackHeader(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService track:")]
    public static partial void ErrorDocServiceTrack(this ILogger<FileHandlerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "BulkDownload file error. File is not exist on storage. UserId: {userId}.")]
    public static partial void ErrorBulkDownloadFile(this ILogger<FileHandlerService> logger, Guid userId);

    [LoggerMessage(LogLevel.Error, "Download file error. File is not exist on storage. File id: {fileId}.")]
    public static partial void ErrorDownloadFile2(this ILogger<FileHandlerService> logger, string fileId);

    [LoggerMessage(LogLevel.Error, "DocService track auth error: {validateResult}, {authKey}: {auth}")]
    public static partial void ErrorDocServiceTrackAuth(this ILogger<FileHandlerService> logger, EmailValidationKeyProvider.ValidationResult validateResult, string authKey, string auth);

    [LoggerMessage(LogLevel.Error, "DocService track header is null")]
    public static partial void ErrorDocServiceTrackHeaderIsNull(this ILogger<FileHandlerService> logger);

    [LoggerMessage(LogLevel.Information, "Converting {fileTitle} (fileId: {fileId}) to mp4")]
    public static partial void InformationConvertingToMp4(this ILogger<FileHandlerService> logger, string fileTitle, string fileId);

    [LoggerMessage(LogLevel.Information, "Starting file download (chunk {offset}-{endOffset})")]
    public static partial void InformationStartingFileDownLoad(this ILogger<FileHandlerService> logger, long offset, long endOffset);
}