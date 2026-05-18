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

namespace ASC.ActiveDirectory.Log;
internal static partial class LdapSettingsCheckerLogger
{
    [LoggerMessage(LogLevel.Error, "CheckSettings(acceptCertificate={acceptCertificate}): NovellLdapTlsCertificateRequestedException")]
    public static partial void ErrorNovellLdapTlsCertificateRequestedException(this ILogger<LdapSettingsChecker> logger, bool acceptCertificate, Exception exception);

    [LoggerMessage(LogLevel.Error, "CheckSettings(): NotSupportedException")]
    public static partial void ErrorNotSupportedException(this ILogger<LdapSettingsChecker> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "CheckSettings(): SocketException")]
    public static partial void ErrorSocketException(this ILogger<LdapSettingsChecker> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "CheckSettings(): ArgumentException")]
    public static partial void ErrorArgumentException(this ILogger<LdapSettingsChecker> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "CheckSettings(): SecurityException")]
    public static partial void ErrorSecurityException(this ILogger<LdapSettingsChecker> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "CheckSettings(): SystemException")]
    public static partial void ErrorSystemException(this ILogger<LdapSettingsChecker> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "CheckSettings(): Exception")]
    public static partial void ErrorCheckSettingsException(this ILogger<LdapSettingsChecker> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Wrong User DN parameter: {userDn}")]
    public static partial void ErrorWrongUserDn(this ILogger<LdapSettingsChecker> logger, string userDn, Exception exception);

    [LoggerMessage(LogLevel.Error, "Wrong Group DN parameter: {groupDn}")]
    public static partial void ErrorWrongGroupDn(this ILogger<LdapSettingsChecker> logger, string groupDn, Exception exception);
}