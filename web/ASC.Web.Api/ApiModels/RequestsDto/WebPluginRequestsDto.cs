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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The configuration settings for the web plugin instance.
/// </summary>
public class WebPluginRequests
{
    /// <summary>
    /// Controls whether the web plugin is active and operational.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The JSON-formatted configuration settings for the web plugin.
    /// </summary>
    [StringLength(255)]
    public string Settings { get; set; }
}

/// <summary>
/// The request parameters for creating or updating a web plugin.
/// </summary>
public class WebPluginRequestsDto
{
    /// <summary>
    /// The web plugin name.
    /// </summary>
    [FromRoute(Name = "name")]
    public required string Name { get; set; }

    /// <summary>
    /// The configuration settings for the web plugin instance.
    /// </summary>
    [FromBody]
    public WebPluginRequests WebPlugin { get; set; }
}

/// <summary>
/// The request parameters for operations that require only a plugin name.
/// </summary>
public class WebPluginNameRequestDto
{
    /// <summary>
    /// The web plugin name.
    /// </summary>
    [FromRoute(Name = "name")]
    public required string Name { get; set; }
}

/// <summary>
/// The request parameters for loading plugins from file system.
/// </summary>
public class WebPluginFromFileRequestDto
{
    /// <summary>
    /// Specifies whether to load the system plugins or not.
    /// </summary>
    [FromQuery(Name = "system")]
    public bool System { get; set; }
}


/// <summary>
/// The request parameters for querying the installed plugins.
/// </summary>
public class GetWebPluginsRequestDto
{
    /// <summary>
    /// The optional filter for the plugin enabled state.
    /// </summary>
    [FromQuery(Name = "enabled")]
    public bool? Enabled { get; set; }
}