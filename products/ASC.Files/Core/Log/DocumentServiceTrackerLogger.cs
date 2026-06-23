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
internal static partial class DocumentServiceTrackerLogger
{
    [LoggerMessage(LogLevel.Debug, "Drop command: fileId '{fileId}' docKey '{fileKey}' for user {user}")]
    public static partial void DebugDropCommand(this ILogger<DocumentServiceTrackerHelper> logger, string fileId, string fileKey, string user, Exception exception);

    [LoggerMessage(LogLevel.Debug, "DocService storing to {path}")]
    public static partial void DebugDocServiceStoring(this ILogger<DocumentServiceTrackerHelper> logger, string path);

    [LoggerMessage(LogLevel.Error, "DocService save error. Version update. File id: '{fileId}'. UserId: {userId}. DocKey '{fileData}'")]
    public static partial void ErrorDocServiceSaveVersionUpdate(this ILogger<DocumentServiceTrackerHelper> logger, string fileId, Guid userId, string fileData, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService save error. File id: '{fileId}'. UserId: {userId}. DocKey '{docKey}'. DownloadUri: {downloadUri}")]
    public static partial void ErrorDocServiceSave(this ILogger<DocumentServiceTrackerHelper> logger, string fileId, Guid userId, string docKey, string downloadUri, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService mailMerge{index} error: userId - {userId}, url - {url}")]
    public static partial void ErrorDocServiceMailMerge(this ILogger<DocumentServiceTrackerHelper> logger, string index, Guid userId, string url, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService Error on save file to temp store")]
    public static partial void ErrorDocServiceSaveFileToTempStore(this ILogger<DocumentServiceTrackerHelper> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService save history error")]
    public static partial void ErrorDocServiceSavehistory(this ILogger<DocumentServiceTrackerHelper> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DocService drop failed for users {users}")]
    public static partial void ErrorDocServiceDropFailed(this ILogger<DocumentServiceTrackerHelper> logger, List<string> users);

    [LoggerMessage(LogLevel.Error, "DocService saving file {fileId} ({docKey}) with key {fileData}")]
    public static partial void ErrorDocServiceSavingFile(this ILogger<DocumentServiceTrackerHelper> logger, string fileId, string docKey, string fileData);

    [LoggerMessage(LogLevel.Error, "DocService save error. Empty url. File id: '{fileId}'. UserId: {userId}. DocKey '{key}'")]
    public static partial void ErrorDocServiceSave2(this ILogger<DocumentServiceTrackerHelper> logger, string fileId, Guid userId, string key);

    [LoggerMessage(LogLevel.Information, "DocService save error: anonymous author - {userId}")]
    public static partial void InformationDocServiceSaveError(this ILogger<DocumentServiceTrackerHelper> logger, Guid userId, Exception exception);

    [LoggerMessage(LogLevel.Information, "DocService editing file {fileId} ({docKey}) with key {fileKey} for {users}")]
    public static partial void InformationDocServiceEditingFile(this ILogger<DocumentServiceTrackerHelper> logger, string fileId, string docKey, string fileKey, List<string> users);

    [LoggerMessage(LogLevel.Information, "DocService userId is not Guid: {user}")]
    public static partial void InformationDocServiceUserIdIsNotGuid(this ILogger<DocumentServiceTrackerHelper> logger, string user);

    [LoggerMessage(LogLevel.Information, "DocService mailMerge {index}/{count} send: {response}")]
    public static partial void InformationDocServiceMailMerge(this ILogger<DocumentServiceTrackerHelper> logger, int index, int count, string response);
}