// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The room template status.
/// </summary>
public class RoomTemplateStatusDto
{
    /// <summary>
    /// The room template ID.
    /// </summary>
    /// <example>123</example>
    public required int TemplateId { get; set; }

    /// <summary>
    /// The progress of the room template creation process.
    /// </summary>
    /// <example>75.5</example>
    public required double Progress { get; set; }

    /// <summary>
    /// The error message that is sent when the room template is not created successfully.
    /// </summary>
    /// <example>Template creation failed</example>
    public string Error { get; set; }

    /// <summary>
    /// Specifies whether the process of creating the room template is completed.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }
}

/// <summary>
/// The progress parameters of creating a room from the template.
/// </summary>
public class RoomFromTemplateStatusDto
{
    /// <summary>
    /// The room ID.
    /// </summary>
    /// <example>456</example>
    public required int RoomId { get; set; }

    /// <summary>
    /// The progress of creating a room from the template.
    /// </summary>
    /// <example>50.0</example>
    public required double Progress { get; set; }

    /// <summary>
    /// The error message that is sent when a room is not created successfully from the template.
    /// </summary>
    /// <example>Room creation failed</example>
    public required string Error { get; set; }

    /// <summary>
    /// Specifies whether the process of creating a room from the template is completed.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }
}