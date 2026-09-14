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
/// Which mobile device receives the Documents push notifications, and whether it is subscribed.
/// </summary>
/// <example>
/// {
///   "firebaseDeviceToken": "dGhpc2lzYXRva2Vu...",
///   "isSubscribed": true
/// }
/// </example>
public class FirebaseRequestsDto
{
    /// <summary>
    /// The registration token Firebase issued to the mobile client for this device, obtained on the device itself.
    /// It is kept as an opaque string of up to 255 characters and is never verified here; it identifies the device
    /// and is matched but never changed, and a token belonging to another member or another portal matches nothing.
    /// </summary>
    /// <example>dGhpc2lzYXRva2Vu...</example>
    public string FirebaseDeviceToken { get; set; }

    /// <summary>
    /// Whether the device is to receive the room activity messages - an invitation, a role change, an archived room,
    /// a new document. On a first registration it is stored as given; on a registration that already exists it is
    /// ignored, because registering does not update, and the subscription is changed with
    /// `PUT api/2.0/settings/push/docsubscribe` instead.
    /// </summary>
    /// <example>true</example>
    public bool IsSubscribed { get; set; }
}
