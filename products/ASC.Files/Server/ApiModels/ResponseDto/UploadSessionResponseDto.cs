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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// How far a chunked upload has got, and the file it produced once the last byte has arrived.
/// </summary>
public class UploadSessionResponseDto<T>
{
    /// <summary>
    /// The file the parts are being written into. An upload that took over a file of the same title carries it from
    /// the start, while an upload that creates a new file has nothing to name yet and reports 0 until the answer that
    /// sets `uploaded` to true.
    /// </summary>
    /// <example>1234</example>
    public T ID { get; set; }

    /// <summary>
    /// The folder receiving the file. It is the folder the upload was reserved against, or the sub-folder created for
    /// it when the reservation declared a relative path.
    /// </summary>
    /// <example>10</example>
    public T FolderId { get; set; }

    /// <summary>
    /// The revision the content is being written as: 1 for a file that did not exist, the next number when the upload
    /// took over a file of the same title, and the unchanged current number for an upload opened over an existing
    /// file, which replaces its content in place.
    /// </summary>
    /// <example>1</example>
    public int Version { get; set; }

    /// <summary>
    /// The title the file is stored under, after characters a title cannot hold were replaced and, where a second
    /// copy was asked for, a numeric suffix was added - so it can differ from the name that was sent.
    /// </summary>
    /// <example>Quarterly report.docx</example>
    public string Title { get; set; }

    /// <summary>
    /// The third-party service holding the destination, such as `GoogleDrive` or `OneDrive`, and null for a folder
    /// stored on the portal itself.
    /// </summary>
    /// <example>GoogleDrive</example>
    public string ProviderKey { get; set; }

    /// <summary>
    /// False while bytes are still missing, when the answer only reports progress; true in the answer that reports
    /// the stored file, which is also the answer that arrives with 201.
    /// </summary>
    /// <example>false</example>
    public bool Uploaded { get; set; }

    /// <summary>
    /// The file as it stands. It is filled in both answers, but while `uploaded` is false it describes a file that
    /// has not been written yet, so its identifier, size and links are only worth reading once that flag turns true.
    /// </summary>
    /// <example>{"id": 1234, "title": "Quarterly report.docx"}</example>
    public FileDto<T> File { get; set; }
}