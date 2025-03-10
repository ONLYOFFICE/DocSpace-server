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
