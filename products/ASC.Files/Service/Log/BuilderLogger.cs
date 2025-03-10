// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Service.Log;
internal static partial class BuilderLogger
{
    [LoggerMessage(LogLevel.Debug, "MakeThumbnail: FileId: {fileId}.")]
    public static partial void DebugMakeThumbnail1(this ILogger logger, string fileId);

    [LoggerMessage(LogLevel.Debug, "MakeThumbnail: FileId: {fileId}. Sleep {sleep} after attempt #{attempt}. ")]
    public static partial void DebugMakeThumbnail2(this ILogger logger, string fileId, int sleep, int attempt);

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

    [LoggerMessage(LogLevel.Error, "BuildThumbnails: filesWithoutThumbnails.Count: {count}.")]
    public static partial void ErrorBuildThumbnailsCount(this ILogger logger, int count, Exception exception);

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
