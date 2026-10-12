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
/// The file a chunked upload session is opened for, and how a clash with an existing name is settled.
/// </summary>
public class SessionRequest
{
    /// <summary>
    /// The name to store the file under, extension included. Characters a title cannot hold are replaced and the name
    /// is truncated, so the stored title can differ from the one sent.
    /// </summary>
    /// <example>My Document.docx</example>
    public required string FileName { get; set; }

    /// <summary>
    /// The exact number of bytes that will be sent. The size is reserved when the session opens and compared with the
    /// parts as they arrive; below the portal chunk size the session takes the whole payload in one part, and above
    /// the portal limit for chunked uploads it is refused.
    /// </summary>
    /// <example>10485760</example>
    public long FileSize { get; set; }

    /// <summary>
    /// A slash-separated chain of folder titles under the target folder to store the file in; folders in the chain
    /// that do not exist yet are created. Leave it empty to store the file in the folder from the path itself.
    /// </summary>
    /// <example>subfolder/documents</example>
    public string RelativePath { get; set; }

    /// <summary>
    /// The creation time to stamp on a newly created file instead of the moment the upload finishes. It is ignored
    /// when the upload lands on a file that already exists.
    /// </summary>
    /// <example>2025-01-01T00:00:00+03:00</example>
    public ApiDateTime CreateOn { get; set; }

    /// <summary>
    /// Marks the stored file as client-side encrypted, which is how content uploaded into a private room is kept;
    /// with false the bytes are stored as they arrive.
    /// </summary>
    /// <example>false</example>
    public bool Encrypted { get; set; }

    /// <summary>
    /// Settles the clash when the folder already holds a file with this name: true stores the upload beside it under
    /// a name with a numeric suffix, false takes the existing file over and adds the content to it as a new version.
    /// </summary>
    /// <example>true</example>
    public bool CreateNewIfExist { get; set; }
}

/// <summary>
/// The generic session request parameters.
/// </summary>
public class SessionRequestDto<T>
{
    /// <summary>
    /// The folder that receives the file; take the id from a listing such as `GET api/2.0/files/@root`. A room or an
    /// ordinary folder inside one is accepted, a section root is not.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The file the session is opened for, and how a clash with an existing name is settled.
    /// </summary>
    [FromBody]
    public required SessionRequest Session { get; set; }
}
