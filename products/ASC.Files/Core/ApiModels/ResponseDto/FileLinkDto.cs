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
/// The address the content of a file is fetched from, together with the signature that authorises the fetch, as
/// the document service is handed it.
/// </summary>
public class FileLinkDto
{
    /// <summary>
    /// The format the stored content is in, lower-cased and with the leading dot, which is how the document
    /// service learns how to read the bytes behind the address. It stays empty when the file title carries no
    /// extension at all.
    /// </summary>
    /// <example>.docx</example>
    [JsonPropertyName("filetype")]
    public required string FileType { get; set; }

    /// <summary>
    /// Signs the address and the format above so that the document service can trust them. It stays empty on a
    /// portal that has no signature secret configured for the document service, and the address is then meant
    /// to be fetched unsigned.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; }

    /// <summary>
    /// Where the content is fetched from: the portal download handler, pinned to the revision the file was at
    /// when the address was issued and carrying an authorisation key of limited validity. It is addressed to
    /// the host the document service can reach, which on a deployment with a private editor network is not the
    /// address a browser should follow.
    /// </summary>
    /// <example>https://portal.example.com/filehandler.ashx?action=stream&amp;fileid=512&amp;version=3</example>
    [Url]
    public required string Url { get; set; }
}
