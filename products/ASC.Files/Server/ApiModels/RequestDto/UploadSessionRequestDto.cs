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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The request parameters for uploading a file in a session.
/// </summary>
public class UploadSessionRequestDto<T>
{
    /// <summary>
    /// The folder the session was opened against. It is part of the route only and is not matched against the
    /// session, which is found by its own id.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// The session this part belongs to, as returned in `id` when it was created; the parts of one session must be
    /// sent one after another, not in parallel.
    /// </summary>
    /// <example>9f1c7a2b4d3e4f5a8b6c0d1e2f3a4b5c</example>
    [FromRoute(Name = "sessionId")]
    public string SessionId { get; set; }

    /// <summary>
    /// The next part of the file, sent as the multipart field of the same name. Parts are appended in the order they
    /// arrive, and a part larger than the portal chunk size is refused.
    /// </summary>
    /// <example>binary file data</example>
    public IFormFile File { get; set; }
}

/// <summary>
/// The request parameters for async uploading a file chunk in a session.
/// </summary>
public class UploadSessionAsyncRequestDto<T>
{
    /// <summary>
    /// The folder the session was opened against. It is part of the route only and is not matched against the
    /// session, which is found by its own id.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// The session this part belongs to, as returned in `id` when it was created; a 32-character hexadecimal string.
    /// </summary>
    /// <example>9f1c7a2b4d3e4f5a8b6c0d1e2f3a4b5c</example>
    [FromRoute(Name = "sessionId")]
    public string SessionId { get; set; }

    /// <summary>
    /// The position of this part in the file, counted from 1. Sending the same number again replaces that part
    /// instead of adding one, which is how a failed part is retried; leaving the number out makes the server count
    /// the parts itself.
    /// </summary>
    /// <example>1</example>
    [FromQuery]
    public int? ChunkNumber { get; set; }

    /// <summary>
    /// The part of the file to store, sent as the multipart field of the same name. It is kept under the number given
    /// beside it, and a part larger than the portal chunk size is refused.
    /// </summary>
    /// <example>binary file data</example>
    [FromForm]
    public IFormFile File { get; set; }
}
