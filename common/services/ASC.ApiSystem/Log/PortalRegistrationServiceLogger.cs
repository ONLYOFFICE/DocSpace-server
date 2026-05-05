// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.ApiSystem.Log;

internal static partial class PortalRegistrationServiceLogger
{
    [LoggerMessage(LogLevel.Debug, "PortalName = {portalName}; unoccupied alias")]
    public static partial void DebugCheckExistingNamePortal(this ILogger logger, string portalName);

    [LoggerMessage(LogLevel.Debug, "PortalName = {portalName}; language = {language}, culture = {culture}")]
    public static partial void DebugLanguageCulture(this ILogger logger, string portalName, string language, string culture);

    [LoggerMessage(LogLevel.Debug, "CheckValidName failed: {name};")]
    public static partial void DebugCheckValidNameFailed(this ILogger logger, string name);

    [LoggerMessage(LogLevel.Debug, "ProvisionAsync: configured OAuth provider, portalName = {portalName}, provider = {provider}")]
    public static partial void DebugProvisionOAuthConfigured(this ILogger logger, string portalName, string provider);

    [LoggerMessage(LogLevel.Error, "ProvisionAsync: OAuth configuration failed, portalName = {portalName}, provider = {provider}")]
    public static partial void ErrorProvisionOAuthFailed(this ILogger logger, string portalName, string provider, Exception exception);

    [LoggerMessage(LogLevel.Error, "Third-party profile processing failed")]
    public static partial void ErrorThirdPartyProfile(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "PortalName = {portalName}; Elapsed ms. ValidateRecaptcha via app key: {appKey}")]
    public static partial void DebugRecaptchaByAppKey(this ILogger logger, string portalName, string appKey);

    [LoggerMessage(LogLevel.Debug, "PortalName = {portalName}; Elapsed ms. ValidateRecaptcha error: {data}")]
    public static partial void DebugRecaptchaError(this ILogger logger, string portalName, string data);

    [LoggerMessage(LogLevel.Debug, "PortalName = {portalName}; Elapsed ms. ValidateRecaptcha: {data}")]
    public static partial void DebugRecaptchaSuccess(this ILogger logger, string portalName, string data);

    [LoggerMessage(LogLevel.Error, "CheckExistingNamePortal")]
    public static partial void ErrorCheckExistingNamePortal(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Tenant registered: {portalName}")]
    public static partial void InfoTenantRegistered(this ILogger logger, string portalName);

    [LoggerMessage(LogLevel.Error, "Tenant registration failed")]
    public static partial void ErrorTenantRegistrationFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Configure wizard failed")]
    public static partial void ErrorConfigureWizard(this ILogger logger, Exception exception);
}
