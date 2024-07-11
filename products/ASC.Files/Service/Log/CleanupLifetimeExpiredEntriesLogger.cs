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

internal static partial class CleanupLifetimeExpiredEntriesLogger
{
    [LoggerMessage(Level = LogLevel.Information, Message = "CleanupLifetimeExpiredEntries Worker running.")]
    public static partial void InformationCleanupLifetimeExpiredEntriesWorkerRunning(this ILogger<CleanupLifetimeExpiredEntriesLauncher> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "CleanupLifetimeExpiredEntries Worker is stopping.")]
    public static partial void InformationCleanupLifetimeExpiredEntriesWorkerStopping(this ILogger<CleanupLifetimeExpiredEntriesLauncher> logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Procedure CleanupLifetimeExpiredEntries: Start.")]
    public static partial void TraceCleanupLifetimeExpiredEntriesProcedureStart(this ILogger<CleanupLifetimeExpiredEntriesLauncher> logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Procedure CleanupLifetimeExpiredEntries: Finish.")]
    public static partial void TraceCleanupLifetimeExpiredEntriesProcedureFinish(this ILogger<CleanupLifetimeExpiredEntriesLauncher> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found: tenant {tenant}, room {room}, expired files {count}")]
    public static partial void InfoCleanupLifetimeExpiredEntriesFound(this ILogger<CleanupLifetimeExpiredEntriesWorker> logger, int tenant, int room, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for data. Sleep {time}.")]
    public static partial void InfoCleanupLifetimeExpiredEntriesWaitingForData(this ILogger<CleanupLifetimeExpiredEntriesWorker> logger, TimeSpan time);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start CleanupLifetimeExpiredEntries tenant {tenant}, room {room}, user {user}, files [{files}]")]
    public static partial void InfoCleanupLifetimeExpiredEntriesStart(this ILogger<CleanupLifetimeExpiredEntriesWorker> logger, int tenant, int room, Guid user, string files);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for tenant {tenant}, room {room}, user {user}...")]
    public static partial void InfoCleanupLifetimeExpiredEntriesWait(this ILogger<CleanupLifetimeExpiredEntriesWorker> logger, int tenant, int room, Guid user);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finish CleanupLifetimeExpiredEntries tenant {tenant}, room {room}, user {user}")]
    public static partial void InfoCleanupLifetimeExpiredEntriesFinish(this ILogger<CleanupLifetimeExpiredEntriesWorker> logger, int tenant, int room, Guid user);
}
