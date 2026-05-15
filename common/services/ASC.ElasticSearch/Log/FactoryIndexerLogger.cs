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

namespace ASC.ElasticSearch.Log;
internal static partial class FactoryIndexerLogger
{
    [LoggerMessage(LogLevel.Critical, "IndexAll")]
    public static partial void CriticalIndexAll(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "CheckState ping {debugInfo}")]
    public static partial void DebugCheckStatePing(this ILogger logger, string debugInfo);

    [LoggerMessage(LogLevel.Error, "Select")]
    public static partial void ErrorSelect(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Index")]
    public static partial void ErrorIndex(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "inner")]
    public static partial void ErrorInner(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Update")]
    public static partial void ErrorUpdate(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Delete")]
    public static partial void ErrorDelete(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "FactoryIndexer CheckState")]
    public static partial void ErrorFactoryIndexerCheckState(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Queue")]
    public static partial void ErrorQueue(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Ping false")]
    public static partial void ErrorPingFalse(this ILogger logger, Exception exception);
}