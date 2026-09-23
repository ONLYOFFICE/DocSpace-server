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
/// The parameters of an editing session heartbeat.
/// </summary>
public class TrackEditFileRequestDto<T>
{
    /// <summary>
    /// The file whose editing session is being tracked.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The client tab that holds the session, a value the client makes up once and repeats on every call about that
    /// tab. Two tabs sending different values are tracked as two sessions on the same file, while the all-zero value
    /// belongs to a session claimed for a single editor.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "tabId")]
    public Guid TabId { get; set; }

    /// <summary>
    /// The document key of the revision being edited, as `POST api/2.0/files/file/{fileId}/startedit` returned it. It
    /// is checked against the file's current key on every call, so a key left over from an older revision is refused.
    /// </summary>
    /// <example>abc123</example>
    [FromQuery(Name = "docKeyForTrack")]
    public string DocKeyForTrack { get; set; }

    /// <summary>
    /// Ends the session for this tab and tells the other clients that editing has stopped. Left off, the session is
    /// refreshed and the file stays marked as being edited.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "isFinish")]
    public bool IsFinish { get; set; }
}
