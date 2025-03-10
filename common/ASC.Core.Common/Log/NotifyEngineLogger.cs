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

using ASC.Core.Common.Notify.Engine;

namespace ASC.Core.Common.Log;
public static partial class NotifyEngineLogger
{
    [LoggerMessage(LogLevel.Debug, "NotifySchedulerService is starting.")]
    public static partial void DebugNotifySchedulerServiceStarting(this ILogger<NotifyEngine> logger);

    [LoggerMessage(LogLevel.Debug, "NotifySchedulerService background task is stopping.")]
    public static partial void DebugNotifySchedulerServiceStopping(this ILogger<NotifyEngine> logger);

    [LoggerMessage(LogLevel.Debug, "NotifySchedulerService background task with instance id {instanceId} is't active.")]
    public static partial void DebugNotifySchedulerServiceIsNotActive(this ILogger<NotifyEngine> logger, string instanceId);

    [LoggerMessage(LogLevel.Warning, "error styling message")]
    public static partial void WarningErrorStyling(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "InvokeSendMethod")]
    public static partial void ErrorInvokeSendMethod(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "NotifyScheduler")]
    public static partial void ErrorNotifyScheduler(this ILogger<NotifySenderService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "SendNotify")]
    public static partial void ErrorSendNotify(this ILogger<NotifySenderService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "NotifySender")]
    public static partial void ErrorNotifySender(this ILogger<NotifySenderService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Prepare {action} {recipient}")]
    public static partial void ErrorPrepare(this ILogger logger, Exception exception, INotifyAction action, IRecipient recipient);

    [LoggerMessage(LogLevel.Error, "UpdateScheduleDate")]
    public static partial void ErrorUpdateScheduleDate(this ILogger logger, Exception exception);
}
