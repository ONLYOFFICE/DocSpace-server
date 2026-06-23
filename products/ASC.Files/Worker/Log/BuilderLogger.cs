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

namespace ASC.Files.Worker.Log;
internal static partial class BuilderLogger
{
    [LoggerMessage(LogLevel.Debug, "MakeThumbnail: FileId: {fileId}.")]
    public static partial void DebugMakeThumbnail1(this ILogger logger, string fileId);

    [LoggerMessage(LogLevel.Debug, "SaveThumbnail: FileId: {fileId}. ThumbnailUrl {url}.")]
    public static partial void DebugMakeThumbnail3(this ILogger logger, string fileId, string url);

    [LoggerMessage(LogLevel.Debug, "SaveThumbnail: FileId: {fileId}. Successfully saved.")]
    public static partial void DebugMakeThumbnail4(this ILogger logger, string fileId);

    [LoggerMessage(LogLevel.Debug, "MakeThumbnail: FileId: {fileId}. Sleep {sleep} after attempt #{attempt}.")]
    public static partial void DebugMakeThumbnailAfter(this ILogger logger, string fileId, int sleep, int attempt);

    [LoggerMessage(LogLevel.Debug, "CropImage: FileId: {fileId}.")]
    public static partial void DebugCropImage(this ILogger logger, string fileId);

    [LoggerMessage(LogLevel.Debug, "CropImage: FileId: {fileId}. Successfully saved.")]
    public static partial void DebugCropImageSuccessfullySaved(this ILogger logger, string fileId);

    [LoggerMessage(LogLevel.Warning, "MakeThumbnail: FileId: {fileId}, ThumbnailUrl: {url}, ResultPercent: {percent}, Attempt: {attempt}. Exception in process generate document thumbnails")]
    public static partial void WarningMakeThumbnail(this ILogger logger, string fileId, string url, int percent, int attempt, Exception exception);

    [LoggerMessage(LogLevel.Error, "BuildThumbnail: TenantId: {tenantId}.")]
    public static partial void ErrorBuildThumbnailsTenantId(this ILogger logger, int tenantId, Exception exception);

    [LoggerMessage(LogLevel.Error, "GenerateThumbnail: FileId: {fileId}.")]
    public static partial void ErrorGenerateThumbnail(this ILogger logger, string fileId, Exception exception);

    [LoggerMessage(LogLevel.Error, "BuildThumbnail: TenantId: {tenantId}. FileDao could not be null.")]
    public static partial void ErrorBuildThumbnailFileDaoIsNull(this ILogger logger, int tenantId);

    [LoggerMessage(LogLevel.Error, "GenerateThumbnail: FileId: {fileId}. File not found.")]
    public static partial void ErrorGenerateThumbnailFileNotFound(this ILogger logger, string fileId);

    [LoggerMessage(LogLevel.Information, "GenerateThumbnail: FileId: {fileId}. Thumbnail already processed.")]
    public static partial void InformationGenerateThumbnail(this ILogger logger, string fileId);
}
