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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// An event an integration module sends to the browsers of the portal, together with the key that identifies the
/// module.
/// </summary>
public class SendModuleEventRequestDto
{
    /// <summary>
    /// The secret key the installation administrator issued to the module. It is not a user or portal credential:
    /// the module the event belongs to is the one this key is configured for, so the key is the module's identity.
    /// </summary>
    /// <example>mk_Q2hhdE1vZHVsZUtleTIwMjYtMTAtMjM</example>
    [FromHeader(Name = "X-Module-Key")]
    public string ModuleKey { get; set; }

    /// <summary>
    /// The event to deliver.
    /// </summary>
    [FromBody]
    public SendModuleEventRequest Event { get; set; }
}

/// <summary>
/// A socket event from an integration module: who receives it in the portal, its name and its payload.
/// </summary>
/// <example>{"user": "e.novikova@example.com", "event": "new-message", "message": "{\"unread\":3}"}</example>
public class SendModuleEventRequest
{
    /// <summary>
    /// A room of the module in this portal: everyone subscribed to `plugin-{module}-{room}` receives the event. Latin
    /// letters, digits and the characters `_ . : -`.
    /// </summary>
    /// <example>shared-inbox</example>
    public string Room { get; set; }

    /// <summary>
    /// The e-mail of the portal user who receives the event; with single sign-on it is the same e-mail the module's own
    /// application knows the person by. Only that user's open sessions receive it.
    /// </summary>
    /// <example>e.novikova@example.com</example>
    public string User { get; set; }

    /// <summary>
    /// The event name. Browsers receive it as `plugin:{module}:{event}`. Latin letters, digits and the characters
    /// `_ . : -`.
    /// </summary>
    /// <example>new-message</example>
    public string Event { get; set; }

    /// <summary>
    /// Passed to the browser as it is, as `{ message }`. Meant as a signal to re-read, such as an unread count; use
    /// JSON text for structured data and keep personal content out of it.
    /// </summary>
    /// <example>{"unread":3}</example>
    public string Message { get; set; }
}
