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
    /// <example>Example Plugin</example>
    public required string Name { get; set; }

    /// <summary>
    /// The web plugin version.
    /// </summary>
    /// <example>1.0.0</example>
    public required string Version { get; set; }

    /// <summary>
    /// The minimum version of DocSpace with which the plugin is guaranteed to work.
    /// </summary>
    /// <example>12.0.0</example>
    public string MinDocSpaceVersion { get; set; }

    /// <summary>
    /// The web plugin description.
    /// </summary>
    /// <example>A plugin that provides additional functionality</example>
    public required string Description { get; set; }

    /// <summary>
    /// The web plugin license.
    /// </summary>
    /// <example>MIT</example>
    public required string License { get; set; }

    /// <summary>
    /// The web plugin author.
    /// </summary>
    /// <example>ONLYOFFICE</example>
    public required string Author { get; set; }

    /// <summary>
    /// The web plugin home page URL.
    /// </summary>
    /// <example>https://example.com</example>
    public required string HomePage { get; set; }

    /// <summary>
    /// The name by which the web plugin is registered in the window object.
    /// </summary>
    /// <example>examplePlugin</example>
    public required string PluginName { get; set; }

    /// <summary>
    /// The web plugin scopes.
    /// </summary>
    /// <example>Files,Rooms</example>
    public required string Scopes { get; set; }

    /// <summary>
    /// The web plugin image.
    /// </summary>
    /// <example>https://example.com/image.png</example>
    public required string Image { get; set; }

    /// <summary>
    /// The user who created the web plugin.
    /// </summary>
    /// <example>{"displayName": "John Doe", "email": "john.doe@example.com"}</example>
    public required EmployeeDto CreateBy { get; set; }

    /// <summary>
    /// The date and time when the web plugin was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public required DateTime CreateOn { get; set; }

    /// <summary>
    /// Specifies if the web plugin is enabled or not.
    /// </summary>
    /// <example>true</example>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Specifies if the web plugin is system or not.
    /// </summary>
    /// <example>false</example>
    public required bool System { get; set; }

    /// <summary>
    /// The web plugin URL.
    /// </summary>
    /// <example>https://example.com/plugin.js</example>
    public required string Url { get; set; }

    /// <summary>
    /// The web plugin css URL.
    /// </summary>
    /// <example>https://example.com/plugin.css</example>
    public required string CssUrl { get; set; }

    /// <summary>
    /// The web plugin settings.
    /// </summary>
    /// <example>{}</example>
    public required string Settings { get; set; }

    /// <summary>
    /// The web plugin localized name.
    /// </summary>
    /// <example>{}</example>
    public Dictionary<string, string> NameLocale { get; set; }

    /// <summary>
    /// The web plugin localized description.
    /// </summary>
    /// <example>{}</example>
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