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
/// The kind of payload a webhook trigger carries.
/// </summary>
/// <remarks>
/// This is the join key between a <see cref="WebhookTrigger"/> and the DTO that models its payload.
/// Both sides carry a <see cref="WebhookPayloadAttribute"/> with the same kind: the trigger, so that
/// <c>WebhookTrigger.cs</c> stays the single readable index of which trigger sends which model, and the
/// DTO, which cannot be named from here because it lives in a product assembly above this one.
/// The pairing is resolved by reflection and is the source of truth for the webhook SDK contract.
/// </remarks>
public enum WebhookPayloadKind
{
    /// <summary>
    /// No payload model. Only the <see cref="WebhookTrigger.All"/> catch-all uses this.
    /// </summary>
    None = 0,

    /// <summary>
    /// A portal user.
    /// </summary>
    User = 1,

    /// <summary>
    /// A group of portal users.
    /// </summary>
    Group = 2,

    /// <summary>
    /// A file.
    /// </summary>
    File = 3,

    /// <summary>
    /// A folder that is not a room.
    /// </summary>
    Folder = 4,

    /// <summary>
    /// A room, including an AI agent room.
    /// </summary>
    Room = 5,

    /// <summary>
    /// A submitted form together with the original form it was filled from.
    /// </summary>
    FormSubmit = 6
}
