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
/// One entry of the portal audit trail: who changed what, from where, and where it belongs in the product.
/// </summary>
public class AuditEventDto
{
    /// <summary>
    /// The ID of the recorded entry. Nothing accepts it as an argument - no operation fetches a single audit event
    /// - so it serves only to tell two otherwise identical entries apart.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// When the action happened, in the portal time zone. The `from` and `to` filters are read as UTC instants, so
    /// the two do not line up on a portal that is not on UTC.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; }

    /// <summary>
    /// The display name of the user who acted, taken from the account as it stands now rather than as it stood
    /// when the entry was written. A localised placeholder stands in when there is no account to read: a portal
    /// background job, an anonymous guest, or a user who has since been deleted.
    /// </summary>
    /// <example>John Doe</example>
    public string User { get; set; }

    /// <summary>
    /// The ID of the user who acted, which is what the `userId` filter of this operation matches on. It stays
    /// readable after the account is deleted, which is when `user` falls back to a placeholder.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The whole event as a readable sentence in the portal language, with the names of the objects involved
    /// substituted into it. On the two `audit/.../last` operations each substituted value is cut to 50 characters;
    /// the filtered operations substitute them in full. It is empty when the build has no wording for the action.
    /// </summary>
    /// <example>User logged in</example>
    public string Action { get; set; }

    /// <summary>
    /// The action itself, as the `action` filter of this operation spells it and as
    /// `GET api/2.0/security/audit/mappers` lists it under `messageAction`. Use this rather than parsing `action`,
    /// which is prose and changes with the portal language.
    /// </summary>
    /// <example>None</example>
    public MessageAction ActionId { get; set; }

    /// <summary>
    /// The IP address the request came from, with the port stripped off. It is empty for an action a portal
    /// background job performed, which has no request behind it.
    /// </summary>
    /// <example>192.0.2.1</example>
    public string IP { get; set; }

    /// <summary>
    /// The English name of the country the IP address is located in, empty when the address cannot be located -
    /// the normal outcome for private and loopback addresses.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; }

    /// <summary>
    /// The city the IP address is located in, empty under the same conditions as `country`.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; }

    /// <summary>
    /// The browser and its version as parsed from the user agent of the request, empty when the client sent none
    /// that could be parsed or when no request was involved.
    /// </summary>
    /// <example>Chrome 120.0</example>
    public string Browser { get; set; }

    /// <summary>
    /// The operating system as parsed from the same user agent, empty under the same conditions as `browser`.
    /// </summary>
    /// <example>Windows</example>
    public string Platform { get; set; }

    /// <summary>
    /// Where in the portal the action was made from: the referrer of the request, or that request's own path when
    /// it carried no referrer. Long values are cut off at 512 characters.
    /// </summary>
    /// <example>/rooms/shared</example>
    public string Page { get; set; }

    /// <summary>
    /// The kind of change the action stands for, as the `actionType` filter of this operation spells it. It is
    /// derived from `actionId`, not stored per entry, so it is the same on every entry of one action.
    /// </summary>
    /// <example>Create</example>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// The product the action belongs to. It cannot be filtered on here; the tree that groups actions by product
    /// is `GET api/2.0/security/audit/mappers`.
    /// </summary>
    /// <example>Documents</example>
    public ProductType Product { get; set; }

    /// <summary>
    /// The location inside that product, as the `moduleType` filter of this operation spells it. It is also
    /// derived from `actionId` rather than stored per entry.
    /// </summary>
    /// <example>Files</example>
    public LocationType Location { get; set; }

    /// <summary>
    /// The objects the action was applied to, as the trail recorded them - a title, an account, an ID - one string
    /// each. It is empty for an action that targets nothing, such as a settings change, and the `target` filter of
    /// this operation matches one of these values in full.
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public IEnumerable<string> Target { get; set; }

    /// <summary>
    /// The kinds of object the action applies to, holding at most two entries and none at all for an action that
    /// targets nothing. Only the first of them can be filtered on, through `entryType`.
    /// </summary>
    /// <example>["File", "Folder"]</example>
    public IEnumerable<EntryType> Entries { get; set; }

    /// <summary>
    /// Where the action took place, spelled out in the portal language rather than as a code: for a Documents
    /// event the room or the root folder it happened in, and for anything else the name of the module. Nothing
    /// filters on it.
    /// </summary>
    /// <example>Security settings updated</example>
    public string Context { get; set; }

    public AuditEventDto(AuditEvent auditEvent, AuditActionMapper auditActionMapper, ApiDateTimeHelper apiDateTimeHelper)
    {
        Id = auditEvent.Id;
        Date = apiDateTimeHelper.Get(auditEvent.Date);
        User = auditEvent.UserName;
        UserId = auditEvent.UserId;
        Action = auditEvent.ActionText;
        ActionId = (MessageAction)auditEvent.Action;
        IP = auditEvent.IP;
        Country = auditEvent.Country;
        City = auditEvent.City;
        Browser = auditEvent.Browser;
        Platform = auditEvent.Platform;
        Page = auditEvent.Page;

        var maps = auditActionMapper.GetMessageMaps(auditEvent.Action);

        ActionType = maps.ActionType;
        Product = maps.ProductType;
        Location = maps.LocationType;

        var list = new List<EntryType>(2);

        if (maps.EntryType1 != EntryType.None)
        {
            list.Add(maps.EntryType1);
        }

        if (maps.EntryType2 != EntryType.None)
        {
            list.Add(maps.EntryType2);
        }

        Entries = list;

        if (auditEvent.Target != null)
        {
            Target = auditEvent.Target.GetItems();
        }

        Context = auditEvent.Context;
    }
}