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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The request parameters for uploading a file.
/// </summary>
public class UploadRequestDto : IModelWithFile
{
    /// <summary>
    /// The content to store, sent as a `multipart/form-data` part; the name of that part becomes the title of the
    /// stored file, with characters a title cannot hold replaced and the name cut to 170 characters. A request
    /// without it is rejected as invalid.
    /// </summary>
    /// <example>binary file data</example>
    [FromForm]
    public IFormFile File { get; set; }

    /// <summary>
    /// Settles the clash with a file already carrying that title: left out, the content is written as the next
    /// version of that file; set to true, both survive and the new one gets a numeric suffix in its title.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "createNewIfExist")]
    public bool CreateNewIfExist { get; set; }

    /// <summary>
    /// Reaches further than this request: it writes a setting on the calling account, the same one
    /// `PUT api/2.0/files/storeoriginal` writes, and it stays in force for later uploads. True keeps both the
    /// uploaded file and the copy the portal converts it into, false replaces the uploaded file with the converted
    /// one, and leaving it out keeps whatever the account already has.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "storeOriginalFile")]
    public bool? StoreOriginalFileFlag { get; set; }

    /// <summary>
    /// Decides whether the outcome of the background conversion outlives the conversion itself. True keeps the queue
    /// record, so `GET api/2.0/files/file/{fileId}/checkconversion` can still report the result or the error; left
    /// out, the record is cleared the moment the conversion ends and that call finds nothing.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "keepConvertStatus")]
    public bool KeepConvertStatus { get; set; }
}

/// <summary>
/// The request parameters for uploading a file to a specific folder.
/// </summary>
public class UploadWithFolderRequestDto<T> : UploadRequestDto
{
    /// <summary>
    /// The folder that receives the file; take the id from a listing such as `GET api/2.0/files/@root`. A room or an
    /// ordinary folder inside one is accepted, a section root is not.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }
}
