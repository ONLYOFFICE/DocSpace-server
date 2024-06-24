﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Files.Service.Services;

namespace ASC.Files.Service.Log;
internal static partial class AutoCleanUpServiceLogger
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Found {count} users (without restrictions) in active portals")]
    public static partial void InfoFoundUsers(this ILogger<AutoCleanTrashService> logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for data. Sleep {time}.")]
    public static partial void InfoWaitingForData(this ILogger<AutoCleanTrashService> logger, TimeSpan time);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start clean up for tenant id {tenantId} with trash id {trashId}")]
    public static partial void InfoCleanUp(this ILogger<AutoCleanTrashService> logger, int tenantId, int trashId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for for tenant id {tenantId} with trash id {trashId}...")]
    public static partial void InfoCleanUpWait(this ILogger<AutoCleanTrashService> logger, int tenantId, int trashId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finish clean up for tenant id {tenantId} with trash id {trashId}")]
    public static partial void InfoCleanUpFinish(this ILogger<AutoCleanTrashService> logger, int tenantId, int trashId);
}
