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
/// The audit trail actions of one product, grouped by module.
/// </summary>
public class AuditTrailProductMapperDto
{
    /// <summary>
    /// The product this branch of the tree belongs to, as the `productType` filter of this operation spells it and
    /// as `GET api/2.0/security/audit/types` lists it under `productTypes`.
    /// </summary>
    /// <example>Documents</example>
    public string ProductType { get; set; }

    /// <summary>
    /// The locations inside the product. It is empty when `moduleType` was passed and this product has no module
    /// of that name, which is why a product can come back with nothing under it.
    /// </summary>
    public IEnumerable<AuditTrailModuleMapperDto> Modules { get; set; }
}

/// <summary>
/// The audit trail actions of one module.
/// </summary>
public class AuditTrailModuleMapperDto
{
    /// <summary>
    /// The location inside the product, as the `moduleType` filter of `GET api/2.0/security/audit/events/filter`
    /// spells it.
    /// </summary>
    /// <example>Files</example>
    public string ModuleType { get; set; }

    /// <summary>
    /// Every action this module can record. Each action appears under exactly one module, so this tree is where a
    /// caller learns which module a given action belongs to.
    /// </summary>
    public IEnumerable<AuditTrailActionMapperDto> Actions { get; set; }
}

/// <summary>
/// One audit trail action, with the kind of change it stands for and the kind of object it applies to.
/// </summary>
public class AuditTrailActionMapperDto
{
    /// <summary>
    /// The action name to send as the `action` filter of `GET api/2.0/security/audit/events/filter`, and the value
    /// that comes back as `actionId` on an event.
    /// </summary>
    /// <example>FileCreated</example>
    public string MessageAction { get; set; }

    /// <summary>
    /// The kind of change the action makes, accepted by the `actionType` filter of the same operation.
    /// </summary>
    /// <example>Create</example>
    public string ActionType { get; set; }

    /// <summary>
    /// The kind of object the action applies to, accepted by the `entryType` filter. It is `None` for an action
    /// that targets no object, such as a settings change, and an action with a second object type reports only the
    /// first one here.
    /// </summary>
    /// <example>File</example>
    public string Entity { get; set; }
}
