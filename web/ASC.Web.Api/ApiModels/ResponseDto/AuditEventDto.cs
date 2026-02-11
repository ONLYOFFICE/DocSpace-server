// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The audit event parameters.
/// </summary>
public class AuditEventDto
{
    /// <summary>
    /// The audit event ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// The audit event date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; }

    /// <summary>
    /// The name of the user who triggered the audit event.
    /// </summary>
    /// <example>John Doe</example>
    public string User { get; set; }

    /// <summary>
    /// The ID of the user who triggered the audit event.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The audit event action.
    /// </summary>
    /// <example>User logged in</example>
    public string Action { get; set; }

    /// <summary>
    /// The specific action that occurred within the audit event.
    /// </summary>
    /// <example>EnumValue</example>
    public MessageAction ActionId { get; set; }

    /// <summary>
    /// The audit event IP.
    /// </summary>
    /// <example>192.0.2.1</example>
    public string IP { get; set; }

    /// <summary>
    /// The audit event country.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; }

    /// <summary>
    /// The audit event city.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; }

    /// <summary>
    /// The audit event browser.
    /// </summary>
    /// <example>Chrome 120.0</example>
    public string Browser { get; set; }

    /// <summary>
    /// The audit event platform.
    /// </summary>
    /// <example>Windows</example>
    public string Platform { get; set; }

    /// <summary>
    /// The audit event page.
    /// </summary>
    /// <example>/rooms/shared</example>
    public string Page { get; set; }

    /// <summary>
    /// The type of action performed in the audit event (e.g., Create, Update, Delete).
    /// </summary>
    /// <example>Create</example>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// The type of product related to the audit event.
    /// </summary>
    /// <example>Documents</example>
    public ProductType Product { get; set; }

    /// <summary>
    /// The location where the audit event occurred.
    /// </summary>
    /// <example>Files</example>
    public LocationType Location { get; set; }

    /// <summary>
    /// The list of target objects affected by the audit event (e.g., document ID, user account).
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public IEnumerable<string> Target { get; set; }

    /// <summary>
    /// The list of audit entry types (e.g., Folder, User, File).
    /// </summary>
    /// <example>["File", "Folder"]</example>
    public IEnumerable<EntryType> Entries { get; set; }

    /// <summary>
    /// The audit event context.
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