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

using EnumMappingStrategy = Riok.Mapperly.Abstractions.EnumMappingStrategy;

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The web plugin information.
/// </summary>
public class WebPluginDto
{
    /// <summary>
    /// The web plugin name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The web plugin version.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// The minimum version of DocSpace with which the plugin is guaranteed to work.
    /// </summary>
    public string MinDocSpaceVersion { get; set; }

    /// <summary>
    /// The web plugin description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The web plugin license.
    /// </summary>
    public required string License { get; set; }

    /// <summary>
    /// The web plugin author.
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// The web plugin home page URL.
    /// </summary>
    public required string HomePage { get; set; }

    /// <summary>
    /// The name by which the web plugin is registered in the window object. 
    /// </summary>
    public required string PluginName { get; set; }

    /// <summary>
    /// The web plugin scopes.
    /// </summary>
    public required string Scopes { get; set; }

    /// <summary>
    /// The web plugin image.
    /// </summary>
    public required string Image { get; set; }

    /// <summary>
    /// The user who created the web plugin.
    /// </summary>
    public required EmployeeDto CreateBy { get; set; }

    /// <summary>
    /// The date and time when the web plugin was created.
    /// </summary>
    public required DateTime CreateOn { get; set; }

    /// <summary>
    /// Specifies if the web plugin is enabled or not.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Specifies if the web plugin is system or not.
    /// </summary>
    public required bool System { get; set; }

    /// <summary>
    /// The web plugin URL.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// The web plugin settings.
    /// </summary>
    public required string Settings { get; set; }

    /// <summary>
    /// The web plugin localized name.
    /// </summary>
    public Dictionary<string, string> NameLocale { get; set; }

    /// <summary>
    /// The web plugin localized description.
    /// </summary>
    public Dictionary<string, string> DescriptionLocale { get; set; }
}

[Scope]
[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName, EnumMappingIgnoreCase = true)]
public partial class WebPluginMapper(EmployeeDtoHelper employeeDtoHelper)
{
    [MapperIgnoreSource(nameof(WebPlugin.CspDomains))]
    [MapProperty(nameof(WebPluginDto.CreateBy), nameof(WebPlugin.CreateBy), Use = nameof(MapCreateBy))]
    private partial WebPluginDto ToDto(WebPlugin webPlugin);

    [UserMapping(Default = false)]
    private static EmployeeDto MapCreateBy(Guid _) => new();

    public async Task<WebPluginDto> ToDtoManual(WebPlugin source)
    {
        var dto = ToDto(source);
        dto.CreateBy = await employeeDtoHelper.GetAsync(source.CreateBy);

        return dto;
    }
}