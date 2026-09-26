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
/// The progress of the job that builds a room template out of an existing room.
/// </summary>
public class RoomTemplateStatusDto
{
    /// <summary>
    /// The template the job is building. It is meaningful once the job has created the template folder, and the
    /// template can be opened with the room operations only after `isCompleted` turns true.
    /// </summary>
    /// <example>123</example>
    public required int TemplateId { get; set; }

    /// <summary>
    /// How far the job has got. The value climbs while the contents of the room are being copied and reaches its
    /// maximum at the very end, so it is an indication of life rather than a reliable estimate of the time left.
    /// </summary>
    /// <example>75.5</example>
    public required double Progress { get; set; }

    /// <summary>
    /// Why the job stopped. It is empty while the job runs and after a successful one; when it is filled the
    /// half-built template has already been removed, so nothing has to be cleaned up by the caller.
    /// </summary>
    /// <example>Template creation failed</example>
    public string Error { get; set; }

    /// <summary>
    /// Whether the job has ended. It is set both after a successful build and after a failure, so `error` is what
    /// tells the two apart, and the record keeps answering with the same values until another job is started.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }
}

/// <summary>
/// The progress of the job that creates a room out of a room template.
/// </summary>
public class RoomFromTemplateStatusDto
{
    /// <summary>
    /// The room the job is creating. It is meaningful once the room exists, which is guaranteed only after
    /// `isCompleted` turns true and `error` stays empty; until then it carries no usable id.
    /// </summary>
    /// <example>456</example>
    public required int RoomId { get; set; }

    /// <summary>
    /// How far the job has got. The value climbs while the contents of the template are being copied into the new
    /// room and reaches its maximum at the very end.
    /// </summary>
    /// <example>50.0</example>
    public required double Progress { get; set; }

    /// <summary>
    /// Why the job stopped. It is empty while the job runs and after a successful one, and a filled value means that
    /// no room was created, so the request has to be repeated rather than waited out.
    /// </summary>
    /// <example>Room creation failed</example>
    public required string Error { get; set; }

    /// <summary>
    /// Whether the job has ended. It is set both after a successful creation and after a failure, so it is the flag
    /// to poll for, while `error` is what separates the two outcomes.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }
}