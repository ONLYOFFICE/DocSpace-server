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

namespace ASC.Files.Core.Log;
internal static partial class DocuSignHelperLogger
{
    [LoggerMessage(LogLevel.Debug, "DocuSing userInfo: {userInfo}")]
    public static partial void DebugDocuSingUserInfo(this ILogger<DocuSignHelper> logger, string userInfo);

    [LoggerMessage(LogLevel.Debug, "DocuSign hook url: {url}")]
    public static partial void DebugDocuSingHookUrl(this ILogger<DocuSignHelper> logger, string url);

    [LoggerMessage(LogLevel.Debug, "DocuSign createdEnvelope: {envelopeId}")]
    public static partial void DebugDocuSingCreatedEnvelope(this ILogger<DocuSignHelper> logger, string envelopeId);

    [LoggerMessage(LogLevel.Debug, "DocuSign senderView: {url}")]
    public static partial void DebugDocuSingSenderView(this ILogger<DocuSignHelper> logger, string url);

    [LoggerMessage(LogLevel.Error, "DocuSign refresh token for user {id}")]
    public static partial void ErrorDocuSignRefreshToken(this ILogger<DocuSignHelper> logger, Guid id, Exception exception);

    [LoggerMessage(LogLevel.Error, "Signer is undefined")]
    public static partial void ErrorSignerIsUndefined(this ILogger<DocuSignHelper> logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "DocuSign refresh token for user {userId}")]
    public static partial void InformationDocuSignRefreshToken(this ILogger<DocuSignHelper> logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "DocuSign webhook get stream: {documentId}")]
    public static partial void InformationDocuSignWebhookGetStream(this ILogger<DocuSignHelper> logger, string documentId);
}