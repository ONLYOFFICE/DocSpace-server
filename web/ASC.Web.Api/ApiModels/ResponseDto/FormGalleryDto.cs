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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// Where the ready-made form templates are served from, for browsing them and for submitting new ones.
/// </summary>
public class FormGalleryDto
{
    /// <summary>
    /// The path under `domain` that the gallery's own listing API is reached at. It is joined to `domain` by the
    /// client; the portal only relays the values from its configuration.
    /// </summary>
    /// <example>/forms/templates</example>
    public required string Path { get; set; }

    /// <summary>
    /// The address of the gallery service, which is a service of the vendor rather than part of the portal. Every
    /// field of this object is empty on an installation that configures no gallery, and a client should then not
    /// offer the gallery at all.
    /// </summary>
    /// <example>https://forms.example.com</example>
    public required string Domain { get; set; }

    /// <summary>
    /// The file extension to ask the gallery for, which decides which rendition of a template is downloaded when
    /// several are published.
    /// </summary>
    /// <example>.docxf</example>
    public required string Ext { get; set; }

    /// <summary>
    /// The path used for submitting a form of one's own to the gallery, the counterpart of `path` for the upload
    /// side. The four `upload` fields are empty when the installation allows browsing but not submitting.
    /// </summary>
    /// <example>/forms/upload</example>
    public required string UploadPath { get; set; }

    /// <summary>
    /// The address the submission is sent to, which may differ from `domain`.
    /// </summary>
    /// <example>https://upload.forms.example.com</example>
    public required string UploadDomain { get; set; }

    /// <summary>
    /// The file extension a submitted form has to carry.
    /// </summary>
    /// <example>.docxf</example>
    public required string UploadExt { get; set; }

    /// <summary>
    /// The page a person is sent to in order to follow up on a submission, joined to `uploadDomain` the same way
    /// as `uploadPath`.
    /// </summary>
    /// <example>/dashboard/forms</example>
    public required string UploadDashboard { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class FormGalleryDtoMapper
{
    public static partial FormGalleryDto Map(this OFormSettings source);
}