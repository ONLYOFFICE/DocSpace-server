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

public class AuditEventDto
{
    [SwaggerSchemaCustom("ID")]
    public int Id { get; set; }

    [SwaggerSchemaCustom("Date")]
    public ApiDateTime Date { get; set; }

    [SwaggerSchemaCustom("User")]
    public string User { get; set; }

    [SwaggerSchemaCustom("User ID")]
    public Guid UserId { get; set; }

    [SwaggerSchemaCustom("Action")]
    public string Action { get; set; }

    [SwaggerSchemaCustom("Action ID")]
    public MessageAction ActionId { get; set; }

    [SwaggerSchemaCustom("IP")]
    public string IP { get; set; }

    [SwaggerSchemaCustom("Country")]
    public string Country { get; set; }

    [SwaggerSchemaCustom("City")]
    public string City { get; set; }

    [SwaggerSchemaCustom("Browser")]
    public string Browser { get; set; }

    [SwaggerSchemaCustom("Platform")]
    public string Platform { get; set; }

    [SwaggerSchemaCustom("Page")]
    public string Page { get; set; }

    [SwaggerSchemaCustom("Action type")]
    public ActionType ActionType { get; set; }

    [SwaggerSchemaCustom("Product type")]
    public ProductType Product { get; set; }

    [SwaggerSchemaCustom("Module type")]
    public ModuleType Module { get; set; }

    [SwaggerSchemaCustom("List of targets")]
    public IEnumerable<string> Target { get; set; }

    [SwaggerSchemaCustom("List of entry types")]
    public IEnumerable<EntryType> Entries { get; set; }

    [SwaggerSchemaCustom("Context")]
    public string Context { get; set; }

    public AuditEventDto(AuditEvent auditEvent, AuditActionMapper auditActionMapper)
    {
        Id = auditEvent.Id;
        Date = new ApiDateTime(auditEvent.Date, TimeSpan.Zero);
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