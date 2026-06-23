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
internal static partial class FileStorageServiceLogger
{
    [LoggerMessage(LogLevel.Error, "DocEditor")]
    public static partial void ErrorDocEditor(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "CreateThumbnails")]
    public static partial void ErrorCreateThumbnails(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "FileStorageService")]
    public static partial void ErrorFileStorageService(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Reassign provider {providerId} from {fromUser} to {toUser}")]
    public static partial void InformationReassignProvider(this ILogger logger, int providerId, Guid fromUser, Guid toUser);

    [LoggerMessage(LogLevel.Information, "Delete provider {providerId} for {userId}")]
    public static partial void InformationDeleteProvider(this ILogger logger, int providerId, Guid userId);

    [LoggerMessage(LogLevel.Information, "Reassign folders from {fromUser} to {toUser}")]
    public static partial void InformationReassignFolders(this ILogger logger, Guid fromUser, Guid toUser);

    [LoggerMessage(LogLevel.Information, "Reassign files from {fromUser} to {toUser}")]
    public static partial void InformationReassignFiles(this ILogger logger, Guid fromUser, Guid toUser);

    [LoggerMessage(LogLevel.Information, "Delete personal data for {userId}")]
    public static partial void InformationDeletePersonalData(this ILogger logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "Unable to perform file {fileId} move copy operation: {reason}")]
    public static partial void InformationUnableFileMoveCopyOperation(this ILogger logger, string fileId, string reason);
}