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

namespace ASC.Files.Core;

/// <summary>
/// The chunked upload session data.
/// </summary>
[DebuggerDisplay("{Id} into {FolderId}")]
public class ChunkedUploadSession<T>(File<T> file, long bytesTotal) : CommonChunkedUploadSession(bytesTotal)
{
    /// <summary>
    /// The chunked upload session folder ID.
    /// </summary>
    public T FolderId { get; set; }

    /// <summary>
    /// The chunked upload session file.
    /// </summary>
    public File<T> File { get; set; } = file;

    /// <summary>
    /// Specifies if the chunked upload session data is encrypted or not.
    /// </summary>
    public bool Encrypted { get; set; }

    /// <summary>
    /// Specifies whether to keep the file version after the chunked upload session or not.
    /// </summary>
    public bool KeepVersion { get; set; }

    //hack for Backup bug 48873
    /// <summary>
    /// Specifies whether to check quota of the chunked upload session data or not.
    /// </summary>
    [NonSerialized]
    public bool CheckQuota = true;

    /// <summary>
    /// Clones the chunked upload session data.
    /// </summary>
    public override object Clone()
    {
        var clone = (ChunkedUploadSession<T>)MemberwiseClone();
        clone.File = (File<T>)File.Clone();

        return clone;
    }
}

/// <summary>
/// Represents the response returned from a chunked upload session.
/// </summary>
public class ChunkedUploadSessionResponse<T>
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    /// <example>0af4bc0d-9a9d-450c-a72b-f14d9ac55c89</example>
    public string Id { get; init; }

    /// <summary>
    /// Represents the hierarchical path of folders associated with a chunked upload session.
    /// </summary>
    /// <example>["123", "456", "789"]</example>
    public IEnumerable<T> Path { get; init; }

    /// <summary>
    /// The timestamp indicating when the chunked upload session was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Created { get; init; }

    /// <summary>
    /// The date and time when the chunked upload session is set to expire.
    /// </summary>
    /// <example>2024-01-15T11:30:00Z</example>
    public DateTime Expired { get; init; }

    /// <summary>
    /// Represents the URI or path of the chunked upload session's current location.
    /// </summary>
    /// <example>https://example.com/products/files/httphandlers/filehandler.ashx?action=upload</example>
    public string Location { get; init; }

    /// <summary>
    /// The total size, in bytes, of the file being uploaded in the chunked upload session.
    /// </summary>
    /// <example>10485760</example>
    [JsonPropertyName("bytes_total")]
    public long BytesTotal { get; init; }
}

[Scope]
public class ChunkedUploadSessionHelper(ILogger<ChunkedUploadSessionHelper> logger, BreadCrumbsManager breadCrumbsManager)
{
    public async Task<ChunkedUploadSessionResponse<T>> ToResponseObjectAsync<T>(ChunkedUploadSession<T> session, bool appendBreadCrumbs = false)
    {
        var breadCrumbs = await breadCrumbsManager.GetBreadCrumbsAsync(session.FolderId); //todo: check how?
        var pathFolder = appendBreadCrumbs
            ? breadCrumbs.Select(f =>
            {
                if (f == null)
                {
                    logger.ErrorInUserInfoRequest(session.FolderId.ToString());

                    return default;
                }

                if (f is Folder<string> f1)
                {
                    return IdConverter.Convert<T>(f1.Id);
                }

                if (f is Folder<int> f2)
                {
                    return IdConverter.Convert<T>(f2.Id);
                }

                return IdConverter.Convert<T>(0);
            })
            : new List<T> { session.FolderId };

        return new ChunkedUploadSessionResponse<T>
        {
            Id = session.Id,
            Path = pathFolder,
            Created = session.Created,
            Expired = session.Expired,
            Location = session.Location,
            BytesTotal = session.BytesTotal
        };
    }
}