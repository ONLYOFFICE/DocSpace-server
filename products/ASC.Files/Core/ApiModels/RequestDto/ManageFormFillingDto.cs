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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The action to apply to the filling of a PDF form.
/// </summary>
public class ManageFormFillingDto<T>
{
    /// <summary>
    /// The PDF form the action applies to. This is the value the operation reads, rather than the identifier in its
    /// route, and the two are to be sent the same.
    /// </summary>
    /// <example>1</example>
    public required T FormId { get; set; }

    /// <summary>
    /// The action to apply.
    /// </summary>
    /// <example>1</example>
    public FormFillingManageAction Action { get; set; }
}

/// <summary>
/// The actions that drive the filling of a PDF form through its states.
/// </summary>
public enum FormFillingManageAction
{
    /// <summary>
    /// Closes the form for filling. In a virtual data room the interruption is recorded together with the role it
    /// happened at and the other role holders are notified; in a form-filling room the form stops accepting
    /// submissions.
    /// </summary>
    [Description("Stop")]
    Stop,

    /// <summary>
    /// Lifts a stop, clearing the record of it, so the role whose turn it was may go on filling.
    /// </summary>
    [Description("Resume")]
    Resume,

    /// <summary>
    /// Opens the form for filling and lets in the members whose room rights are limited to filling forms. A form that
    /// was edited since it was last started has the drafts of the previous round dropped.
    /// </summary>
    [Description("Start")]
    Start,

    /// <summary>
    /// Puts the form back into editing, closing it for filling and remembering the version it is edited from.
    /// </summary>
    [Description("Edit")]
    Edit
}