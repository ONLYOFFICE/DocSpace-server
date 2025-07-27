// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.Helpers;

/// <summary>
/// Represents a file format configuration with supported actions and conversions
/// </summary>
public class FileFormatConfig
{
    /// <summary>
    /// Gets or sets the name of the file format
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the file format
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of supported actions for this format
    /// </summary>
    public List<string> Actions { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of formats this file can be converted to
    /// </summary>
    public List<string> Convert { get; set; } = [];

    /// <summary>
    /// Gets or sets the MIME types associated with this format
    /// </summary>
    public List<string> Mime { get; set; } = [];

    /// <summary>
    /// Checks if the format supports a specific action
    /// </summary>
    /// <param name="action">The action to check</param>
    /// <returns>True if the action is supported, false otherwise</returns>
    public bool SupportsAction(string action)
    {
        return Actions.Contains(action, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the format can be converted to a specific format
    /// </summary>
    /// <param name="targetFormat">The target format to check</param>
    /// <returns>True if conversion is supported, false otherwise</returns>
    public bool CanConvertTo(string targetFormat)
    {
        return Convert.Contains(targetFormat, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the format matches a specific MIME type
    /// </summary>
    /// <param name="mimeType">The MIME type to check</param>
    /// <returns>True if the MIME type matches, false otherwise</returns>
    public bool MatchesMimeType(string mimeType)
    {
        return Mime.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the primary MIME type for this format
    /// </summary>
    /// <returns>The first MIME type or empty string if none available</returns>
    public string GetPrimaryMimeType()
    {
        return Mime.FirstOrDefault() ?? string.Empty;
    }
}
