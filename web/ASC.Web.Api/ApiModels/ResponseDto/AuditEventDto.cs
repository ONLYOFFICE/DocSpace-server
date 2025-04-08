// (c) Copyright Ascensio System SIA 2009-2024
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
    public int Id { get; set; }

    /// <summary>
    /// The audit event date.
    /// </summary>
    public ApiDateTime Date { get; set; }

    /// <summary>
    /// The name of the user who triggered the audit event.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// The ID of the user who triggered the audit event.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The audit event action.
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// The specific action that occurred within the audit event.
    /// </summary>
    public MessageAction ActionId { get; set; }

    /// <summary>
    /// The audit event IP.
    /// </summary>
    public string IP { get; set; }

    /// <summary>
    /// The audit event country.
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// The audit event city.
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// The audit event browser.
    /// </summary>
    public string Browser { get; set; }

    /// <summary>
    /// The audit event platform.
    /// </summary>
    public string Platform { get; set; }

    /// <summary>
    /// The audit event page.
    /// </summary>
    public string Page { get; set; }

    /// <summary>
    /// The type of action performed in the audit event (e.g., Create, Update, Delete).
    /// </summary>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// The type of product related to the audit event.
    /// </summary>
    public ProductType Product { get; set; }

    /// <summary>
    /// The module within the product where the audit event occurred.
    /// </summary>
    public ModuleType Module { get; set; }

    /// <summary>
    /// The list of target objects affected by the audit event (e.g., document ID, user account).
    /// </summary>
    public IEnumerable<string> Target { get; set; }

    /// <summary>
    /// The list of audit entry types (e.g., Folder, User, File).
    /// </summary>
    public IEnumerable<EntryType> Entries { get; set; }

    /// <summary>
    /// The audit event context.
    /// </summary>
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
        Module = maps.ModuleType;

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