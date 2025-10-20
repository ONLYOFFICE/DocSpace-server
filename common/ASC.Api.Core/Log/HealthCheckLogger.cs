// common/ASC.Api.Core/Log/HealthCheckLogger.cs
// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU AGPL
// version 3 for more details.
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

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ASC.Api.Core.Log;

internal static partial class HealthCheckLogger
{
    [LoggerMessage(LogLevel.Error, "Health check failed. Status: {Status}. Duration: {Duration}ms")]
    public static partial void ErrorHealthCheckFailed(this ILogger logger, string Status, double Duration);

    [LoggerMessage(LogLevel.Error, "Failed health check: {HealthCheckName}, Status: {Status}, Duration: {Duration}ms, Description: {Description}")]
    public static partial void ErrorHealthCheckEntry(this ILogger logger, string HealthCheckName, string Status, double Duration, string Description);
}