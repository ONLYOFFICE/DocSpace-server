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

namespace ASC.AI.Core.MCP.Data;

/// <summary>
/// Set of server icon URLs in multiple resolutions, used for rendering in different UI contexts (lists, cards, tooltips, etc.).
/// </summary>
public class Icon
{
    /// <summary>
    /// Icon URL at 48x48 pixels resolution. Used for large previews and detail views.
    /// </summary>
    /// <example>https://example.com/icon-48.png</example>
    public required string Icon48 { get; init; }

    /// <summary>
    /// Icon URL at 32x32 pixels resolution. Used for standard list items and cards.
    /// </summary>
    /// <example>https://example.com/icon-32.png</example>
    public required string Icon32 { get; init; }

    /// <summary>
    /// Icon URL at 24x24 pixels resolution. Used for compact list items and inline displays.
    /// </summary>
    /// <example>https://example.com/icon-24.png</example>
    public required string Icon24 { get; init; }

    /// <summary>
    /// Icon URL at 16x16 pixels resolution. Used for small indicators, tooltips, and favicons.
    /// </summary>
    /// <example>https://example.com/icon-16.png</example>
    public required string Icon16 { get; init; }
}