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
/// One web plugin available to the portal: its manifest, where to load it from, and the state the portal keeps.
/// </summary>
public class WebPluginDto
{
    /// <summary>
    /// The plugin's manifest name, which is what every other operation of this group addresses it by and what
    /// makes it unique within the portal - an installation-wide plugin wins the name over a portal one.
    /// </summary>
    /// <example>Example Plugin</example>
    public required string Name { get; set; }

    /// <summary>
    /// The plugin's own version from its manifest. The portal does not compare it against anything; it is there
    /// for a person to read.
    /// </summary>
    /// <example>1.0.0</example>
    public required string Version { get; set; }

    /// <summary>
    /// The oldest portal version the plugin declares it works with. It is a claim from the manifest and is not
    /// enforced, so a plugin can be loaded on an older portal and simply misbehave; compare it with the `version`
    /// of `GET api/2.0/settings`.
    /// </summary>
    /// <example>12.0.0</example>
    public string MinDocSpaceVersion { get; set; }

    /// <summary>
    /// The plugin's description from its manifest, in the language the manifest was written in. The translations
    /// of it are in `descriptionLocale`.
    /// </summary>
    /// <example>A plugin that provides additional functionality</example>
    public required string Description { get; set; }

    /// <summary>
    /// The licence the plugin is published under, as its manifest states it. Nothing checks it.
    /// </summary>
    /// <example>MIT</example>
    public required string License { get; set; }

    /// <summary>
    /// Who wrote the plugin, as its manifest states it - not the portal member who uploaded it, who is
    /// `createBy`.
    /// </summary>
    /// <example>ONLYOFFICE</example>
    public required string Author { get; set; }

    /// <summary>
    /// The plugin's own page, for a person to read more about it. It is empty when the manifest names none.
    /// </summary>
    /// <example>https://example.com</example>
    public required string HomePage { get; set; }

    /// <summary>
    /// The global the plugin registers itself under in the browser once its script has run, which is how a
    /// client reaches it. It is distinct from `name`, the identifier the portal uses.
    /// </summary>
    /// <example>examplePlugin</example>
    public required string PluginName { get; set; }

    /// <summary>
    /// Which parts of the interface the plugin hooks into, as one comma-separated string rather than a list.
    /// </summary>
    /// <example>Files,Rooms</example>
    public required string Scopes { get; set; }

    /// <summary>
    /// The plugin's icon exactly as its manifest declares it, which is normally a file name inside the plugin's
    /// own package rather than an absolute address - resolve it against the directory `url` points into.
    /// </summary>
    /// <example>icon.svg</example>
    public required string Image { get; set; }

    /// <summary>
    /// The portal member who uploaded the plugin. For a plugin that ships with the installation it is an empty
    /// profile, since no member put it there.
    /// </summary>
    /// <example>{"displayName": "John Doe", "email": "john.doe@example.com"}</example>
    public required EmployeeDto CreateBy { get; set; }

    /// <summary>
    /// When the plugin was uploaded. It stays at its zero value for a plugin that ships with the installation.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public required DateTime CreateOn { get; set; }

    /// <summary>
    /// Whether the portal loads the plugin. It is the state this portal stored, so an installation-wide plugin
    /// can be on for one portal and off for another.
    /// </summary>
    /// <example>true</example>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Whether the plugin ships with the installation rather than having been uploaded here. A system plugin
    /// cannot be deleted through `DELETE api/2.0/settings/webplugins/{name}`, only switched off.
    /// </summary>
    /// <example>false</example>
    public required bool System { get; set; }

    /// <summary>
    /// The address of the plugin's script, which a client loads to run it. It ends in a `hash` query taken from
    /// `version`, so the address changes whenever the plugin is updated and an old one may be cached.
    /// </summary>
    /// <example>https://example.com/plugin.js</example>
    public required string Url { get; set; }

    /// <summary>
    /// The absolute address of the plugin's stylesheet, empty for a plugin that ships none.
    /// </summary>
    /// <example>https://example.com/plugin.css</example>
    public required string CssUrl { get; set; }

    /// <summary>
    /// The settings string the portal keeps for the plugin, stored and returned verbatim - only the plugin knows
    /// its shape. It is empty until `PUT api/2.0/settings/webplugins/{name}` saves one.
    /// </summary>
    /// <example>{"theme":"dark"}</example>
    public required string Settings { get; set; }

    /// <summary>
    /// The plugin's name translated, keyed by culture name. A culture that is missing falls back to `name`, and
    /// the whole map is empty for a plugin that ships no translations.
    /// </summary>
    /// <example>{"en-US": "Example plugin", "de-DE": "Beispiel-Plugin"}</example>
    public Dictionary<string, string> NameLocale { get; set; }

    /// <summary>
    /// The plugin's description translated, keyed the same way as `nameLocale` and falling back to
    /// `description`.
    /// </summary>
    /// <example>{"en-US": "Adds extra actions", "de-DE": "Fugt Aktionen hinzu"}</example>
    public Dictionary<string, string> DescriptionLocale { get; set; }

    /// <summary>
    /// How the script at `url` is to be loaded - as an ES module or as a classic script. It is empty for a
    /// plugin whose manifest does not say, which a client treats as a classic script.
    /// </summary>
    /// <example>module</example>
    public string Runtime { get; set; }
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
