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

namespace ASC.Files.Core.Core.Entries;

/// <summary>
/// The settings for opening the form document.
/// </summary>
public record FormOpenSetup<T>
{
    /// <summary>
    /// Specifies if the form can be edited or not.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Specifies if the form can be filled out or not.
    /// </summary>
    public bool CanFill { get; set; }

    /// <summary>
    /// Specifies if the "Start filling" button is displayed in the form or not.
    /// </summary>
    public bool CanStartFilling { get; set; } = true;

    /// <summary>
    /// Specifies if the completed form can be submitted only or not.
    /// </summary>
    public bool IsSubmitOnly { get; set; }

    /// <summary>
    /// The form filling session ID.
    /// </summary>
    public string FillingSessionId { get; set; }

    /// <summary>
    /// The editor type.
    /// </summary>
    public EditorType EditorType { get; set; }

    /// <summary>
    /// The form draft parameters.
    /// </summary>
    public File<T> Draft { get; set; }

    /// <summary>
    /// The role name of the user who fills out the form.
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// The root folder where the current form is located.
    /// </summary>
    public Folder<T> RootFolder { get; set; }

    /// <summary>
    /// Disable embedded config
    /// </summary>
    public bool DisableEmbeddedConfig { get; set; }

    /// <summary>
    /// Specifies if the room can be edited out or not.
    /// </summary>
    public bool CanEditRoom { get; set; }

    public bool HasRole { get; set; }

}