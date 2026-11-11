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

namespace ASC.Files.Core.Core.Thirdparty;

/// <summary>
/// Recognises the answer each third-party storage gives when the account behind a connection is out
/// of space, so the storages can replace it with <see cref="ThirdPartyStorageQuotaException"/>. Without
/// that the provider's own error text reaches the user as is — Dropbox answers with the tag path
/// <c>path/insufficient_space/</c> — with a 500 status and a critical log entry.
/// </summary>
public static class ThirdPartyStorageQuota
{
    private const string GoogleDriveReason = "storageQuotaExceeded";
    private const string OneDriveCode = "quotaLimitReached";
    private const string BoxCode = "storage_limit_exceeded";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The exception a storage throws in place of the provider's out-of-space error.
    /// </summary>
    public static ThirdPartyStorageQuotaException CreateException(Exception providerError)
    {
        return new ThirdPartyStorageQuotaException(FilesCommonResource.ErrorMessage_ThirdPartyStorageFull, providerError);
    }

    /// <summary>
    /// Dropbox: <c>insufficient_space</c> in the write error of an upload, an upload session commit or a
    /// folder creation, and <c>insufficient_quota</c> (or a write error with <c>insufficient_space</c>) of a
    /// copy or move.
    /// </summary>
    public static bool IsDropbox(Exception exception)
    {
        return exception switch
        {
            ApiException<UploadError> { ErrorResponse: { IsPath: true } error } => error.AsPath.Value.Reason.IsInsufficientSpace,
            ApiException<UploadSessionFinishError> { ErrorResponse: { IsPath: true } error } => error.AsPath.Value.IsInsufficientSpace,
            ApiException<CreateFolderError> { ErrorResponse: { IsPath: true } error } => error.AsPath.Value.IsInsufficientSpace,
            ApiException<RelocationError> { ErrorResponse: { } error } =>
                error.IsInsufficientQuota
                || (error.IsTo && error.AsTo.Value.IsInsufficientSpace)
                || (error.IsFromWrite && error.AsFromWrite.Value.IsInsufficientSpace),
            _ => false
        };
    }

    /// <summary>
    /// Google Drive through its client library: 403 with the reason <c>storageQuotaExceeded</c>.
    /// </summary>
    public static bool IsGoogleDrive(Exception exception)
    {
        return exception is GoogleApiException { Error.Errors: { } errors } && errors.Any(e => e.Reason == GoogleDriveReason);
    }

    /// <summary>
    /// Google Drive over plain HTTP (the resumable upload): the same reason in the JSON error body.
    /// </summary>
    public static bool IsGoogleDriveResponse(string responseBody)
    {
        var body = Deserialize<GoogleErrorResponse>(responseBody);

        return body?.Error?.Errors?.Any(e => e?.Reason == GoogleDriveReason) == true;
    }

    /// <summary>
    /// OneDrive through its client library: 507 or the code <c>quotaLimitReached</c> anywhere in the error chain.
    /// </summary>
    public static bool IsOneDrive(Exception exception)
    {
        return exception is ServiceException serviceException
            && (serviceException.StatusCode == HttpStatusCode.InsufficientStorage || serviceException.IsMatch(OneDriveCode));
    }

    /// <summary>
    /// OneDrive over plain HTTP (the upload session): 507 or the code <c>quotaLimitReached</c> in the JSON error body.
    /// </summary>
    public static bool IsOneDriveResponse(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode == HttpStatusCode.InsufficientStorage)
        {
            return true;
        }

        return Deserialize<OneDriveErrorResponse>(responseBody)?.Error?.Code == OneDriveCode;
    }

    /// <summary>
    /// Box: the error code <c>storage_limit_exceeded</c>.
    /// </summary>
    public static bool IsBox(Exception exception)
    {
        return exception is BoxAPIException { Error.Code: BoxCode };
    }

    /// <summary>
    /// WebDAV (RFC 4918): 507 Insufficient Storage.
    /// </summary>
    public static bool IsWebDav(int statusCode)
    {
        return statusCode == (int)HttpStatusCode.InsufficientStorage;
    }

    private static T Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            // an error page that is not JSON (a proxy, an HTML answer) says nothing about the quota
            return null;
        }
    }

    private sealed record GoogleErrorResponse(GoogleError Error);

    private sealed record GoogleError(List<GoogleErrorItem> Errors);

    private sealed record GoogleErrorItem(string Reason);

    private sealed record OneDriveErrorResponse(OneDriveError Error);

    private sealed record OneDriveError(string Code);
}
