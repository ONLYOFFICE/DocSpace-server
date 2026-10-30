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

namespace ASC.Webhooks.Core;

/// <summary>
/// Declares the webhooks an API action raises, for the published documentation: the OpenAPI document lists them on
/// the operation (<c>x-docspace-webhooks</c>) and adds a sentence about them to its description, and the webhooks
/// contract lists the operation under each of the triggers.
/// </summary>
/// <remarks>
/// It documents, it does not publish: the action raises the webhook through its own code, and this attribute has to
/// be kept true when that code changes. Put one attribute per condition: an unconditional trigger needs none, a
/// trigger raised only in some cases names them in <see cref="When"/>, written to follow "raises the webhook", for
/// example "when `autosave` is true" or "for every file of the folder, once the operation processes it".
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class RaisesWebhookAttribute(params WebhookTrigger[] triggers) : Attribute
{
    public WebhookTrigger[] Triggers { get; } = triggers;

    /// <summary>
    /// The condition under which the triggers are raised, or null when the action always raises them.
    /// </summary>
    public string When { get; set; }
}
