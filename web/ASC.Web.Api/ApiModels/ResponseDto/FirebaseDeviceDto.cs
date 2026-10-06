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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// One mobile device of the calling user registered for push notifications.
/// </summary>
public class FirebaseDeviceDto
{
    /// <summary>
    /// The id of the registration.
    /// </summary>
    /// <example>1</example>
    public int Id { get; init; }

    /// <summary>
    /// The account the device belongs to; always the caller.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid UserId { get; init; }

    /// <summary>
    /// The portal the registration belongs to; always the current one.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; init; }

    /// <summary>
    /// The Firebase token the device was issued, as it was sent at registration.
    /// </summary>
    /// <example>fcm-token-123</example>
    public string FirebaseDeviceToken { get; init; }

    /// <summary>
    /// The application the registration is for; `doc` for the Documents application.
    /// </summary>
    /// <example>doc</example>
    public string Application { get; init; }

    /// <summary>
    /// Whether the device is currently sent push notifications.
    /// </summary>
    /// <example>true</example>
    public bool? IsSubscribed { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class FirebaseDeviceDtoMapper
{
    public static partial FirebaseDeviceDto Map(this FireBaseUser source);
}
