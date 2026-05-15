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
internal static partial class NovellLdapSearcherLogger
{
    [LoggerMessage(LogLevel.Debug, "ldapConnection.Connect(Server='{server}', PortNumber='{portNumber}');")]
    public static partial void DebugldapConnection(this ILogger<NovellLdapSearcher> logger, string server, int portNumber);

    [LoggerMessage(LogLevel.Debug, "ldapConnection.StartTls();")]
    public static partial void DebugStartTls(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Debug, "LDAP certificate confirmation requested.")]
    public static partial void DebugLdapCertificateConfirmationRequested(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Debug, "ldapConnection.Bind(Anonymous)")]
    public static partial void DebugBindAnonymous(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Debug, "ldapConnection.Bind(Login: '{login}')")]
    public static partial void DebugBind(this ILogger<NovellLdapSearcher> logger, string login);

    [LoggerMessage(LogLevel.Warning, "ServerCertValidationHandler: sslPolicyErrors = {sslPolicyErrors}")]
    public static partial void WarnSslPolicyErrors(this ILogger<NovellLdapSearcher> logger, SslPolicyErrors sslPolicyErrors);

    [LoggerMessage(LogLevel.Warning, "The size of the search results is limited. Start TrySearchSimple()")]
    public static partial void WarnStartTrySearchSimple(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Error, "Search({searchFilter}) failed")]
    public static partial void ErrorSearch(this ILogger<NovellLdapSearcher> logger, string searchFilter, Exception exception);

    [LoggerMessage(LogLevel.Error, "TrySearchSimple() failed")]
    public static partial void ErrorTrySearchSimple(this ILogger<NovellLdapSearcher> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "SearchSimple({searchFilter}) failed")]
    public static partial void ErrorSearchSimple(this ILogger<NovellLdapSearcher> logger, string searchFilter, Exception exception);

    [LoggerMessage(LogLevel.Debug, "{i}. DN: {distinguishedName}")]
    public static partial void DebugDnEnumeration(this ILogger<NovellLdapSearcher> logger, int i, string distinguishedName);

    [LoggerMessage(LogLevel.Debug, "No controls returned")]
    public static partial void DebugNoControlsReturned(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Error, "GetCapabilities()->LoopResults failed")]
    public static partial void ErrorGetCapabilitiesLoopResultsFailed(this ILogger<NovellLdapSearcher> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "GetCapabilities() failed")]
    public static partial void ErrorGetCapabilitiesFailed(this ILogger<NovellLdapSearcher> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "GetLdapUniqueId()")]
    public static partial void ErrorGetLdapUniqueId(this ILogger<NovellLdapSearcher> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "ldapConnection.StopTls();")]
    public static partial void DebugLdapConnectionStopTls(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Debug, "ldapConnection.Disconnect();")]
    public static partial void DebugLdapConnectionDisconnect(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Debug, "ldapConnection.Dispose();")]
    public static partial void DebugLdapConnectionDispose(this ILogger<NovellLdapSearcher> logger);

    [LoggerMessage(LogLevel.Error, "LDAP->Dispose() failed")]
    public static partial void ErrorLdapDisposeFailed(this ILogger<NovellLdapSearcher> logger, Exception exception);
}