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
/// The file reference parameters.
/// </summary>
public class FileReferenceDto
{
    /// <summary>
    /// How this document is named when another spreadsheet refers to it. Send it back as it stands to resolve the
    /// reference again.
    /// </summary>
    /// <example>{"fileKey": "512", "instanceId": "1"}</example>
    public FileReferenceDataDto ReferenceData { get; set; }

    /// <summary>
    /// Filled in when the reference resolved to nothing; the rest of the descriptor is then empty and must not be
    /// handed to the editors.
    /// </summary>
    /// <example>File not found</example>
    public string Error { get; set; }

    /// <summary>
    /// The title of the document the reference resolved to.
    /// </summary>
    /// <example>Budget 2026.xlsx</example>
    public string Path { get; set; }

    /// <summary>
    /// Where the content is fetched from. It is addressed to the host the document service can reach, which on a
    /// deployment with a private editor network is not the address a browser should follow.
    /// </summary>
    /// <example>https://portal.example.com/filehandler.ashx?action=download&amp;fileid=512</example>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// The format the content is in, without the leading dot.
    /// </summary>
    /// <example>xlsx</example>
    public string FileType { get; set; }

    /// <summary>
    /// Identifies the exact revision to the editors: two clients that receive the same key read the same co-editing
    /// session, and the key changes as soon as the document is saved.
    /// </summary>
    /// <example>1_512_3</example>
    public string Key { get; set; }

    /// <summary>
    /// The address of the document in the portal web editor - the link to put in front of a person, unlike the
    /// download address above.
    /// </summary>
    /// <example>https://portal.example.com/doceditor?fileid=512</example>
    public string Link { get; set; }

    /// <summary>
    /// Signs this descriptor so that the editors can trust it. It stays empty on a portal that has no signature
    /// secret configured for the document service.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; }
}

/// <summary>
/// The pair of values that names a document across portals, as it is written into a spreadsheet formula.
/// </summary>
public class FileReferenceDataDto
{
    /// <summary>
    /// The id of the document inside the portal named below.
    /// </summary>
    /// <example>512</example>
    public string FileKey { get; set; }

    /// <summary>
    /// The portal the document lives in. A reference whose value is not this portal cannot be resolved by the file
    /// key and falls back to the path or the link.
    /// </summary>
    /// <example>1</example>
    public string InstanceId { get; set; }

    /// <summary>
    /// The room the document lies in. It is filled in only for a document opened in a virtual data room, and stays
    /// empty everywhere else.
    /// </summary>
    /// <example>42</example>
    public string RoomId { get; set; }

    /// <summary>
    /// Whether the caller may manage the room named above; it is only meaningful together with it.
    /// </summary>
    /// <example>true</example>
    public bool CanEditRoom { get; set; }
}
