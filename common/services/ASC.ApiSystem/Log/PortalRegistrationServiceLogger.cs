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
