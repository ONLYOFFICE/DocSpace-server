// (c) Copyright Ascensio System SIA 2009-2025
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

using Constants = ASC.Core.Configuration.Constants;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.AuditTrail.Models.Mappings;

[Scope]
internal class EventTypeConverter(
    UserFormatter userFormatter,
    AuditActionMapper actionMapper,
    TenantUtil tenantUtil)
{
    public void Convert(LoginEventQuery source, LoginEvent dest)
    {
        if (source?.Event == null)
        {
            return;
        }        
        
        if (source.Event.DescriptionRaw != null)
        {
            dest.Description = JsonSerializer.Deserialize<IList<string>>(source.Event.DescriptionRaw);
        }

        if (!(string.IsNullOrEmpty(source.FirstName) || string.IsNullOrEmpty(source.LastName)))
        {
            dest.UserName = userFormatter.GetUserName(source.FirstName, source.LastName);
        }
        else if (!string.IsNullOrEmpty(source.FirstName))
        {
            dest.UserName = source.FirstName;
        }
        else if (!string.IsNullOrEmpty(source.LastName))
        {
            dest.UserName = source.LastName;
        }
        else if (!string.IsNullOrWhiteSpace(dest.Login))
        {
            dest.UserName = dest.Login;
        }
        else if (dest.UserId == Constants.Guest.ID)
        {
            dest.UserName = AuditReportResource.GuestAccount;
        }
        else
        {
            dest.UserName = AuditReportResource.UnknownAccount;
        }

        dest.ActionText = actionMapper.GetActionText(actionMapper.GetMessageMaps(dest.Action), dest);

        dest.Date = tenantUtil.DateTimeFromUtc(dest.Date);
        dest.IP = dest.IP.Split(':').Length > 1 ? dest.IP.Split(':')[0] : dest.IP;
    }

    public void Convert(AuditEventQuery source, AuditEvent dest)
    {
        if (source?.Event == null)
        {
            return;
        }

        var target = source.Event.Target;
        source.Event.Target = null;

        dest.Target = MessageTarget.Parse(target);

        if (source.Event.DescriptionRaw != null)
        {
            dest.Description = JsonSerializer.Deserialize<IList<string>>(source.Event.DescriptionRaw);
        }

        if (dest.UserId == Constants.CoreSystem.ID)
        {
            dest.UserName = AuditReportResource.SystemAccount;
        }
        else if (dest.UserId == Constants.Guest.ID)
        {
            dest.UserName = AuditReportResource.GuestAccount;
        }
        else if (!(string.IsNullOrEmpty(source.UserData?.FirstName) || string.IsNullOrEmpty(source.UserData?.LastName)))
        {
            dest.UserName = userFormatter.GetUserName(source.UserData?.FirstName, source.UserData?.LastName);
        }
        else if (!string.IsNullOrEmpty(source.UserData?.FirstName))
        {
            dest.UserName = source.UserData.FirstName;
        }
        else if (!string.IsNullOrEmpty(source.UserData?.LastName))
        {
            dest.UserName = source.UserData.LastName;
        }
        else
        {
            dest.UserName = dest.Initiator ?? AuditReportResource.UnknownAccount;
        }

        var map = actionMapper.GetMessageMaps(dest.Action);
        if (map != null)
        {
            if (dest.Action is 
                (int)MessageAction.QuotaPerPortalChanged or 
                (int)MessageAction.QuotaPerRoomChanged or 
                (int)MessageAction.QuotaPerUserChanged
                && long.TryParse(dest.Description.FirstOrDefault(), out var size))
            { 
                dest.ActionText = string.Format(map.GetActionText(), CommonFileSizeComment.FilesSizeToString(AuditReportResource.FileSizePostfix, size));
            }
            else if (dest.Action is (int)MessageAction.CustomQuotaPerRoomDefault or
                (int)MessageAction.CustomQuotaPerRoomChanged or
                (int)MessageAction.CustomQuotaPerUserDefault or
                (int)MessageAction.CustomQuotaPerUserChanged
                && long.TryParse(dest.Description.FirstOrDefault(), out var customSize))
            {
                dest.ActionText = string.Format(map.GetActionText(), dest.Description.LastOrDefault(), CommonFileSizeComment.FilesSizeToString(AuditReportResource.FileSizePostfix, customSize));
            }
            else
            {
                dest.ActionText = actionMapper.GetActionText(map, dest);
            }

            dest.ActionTypeText = actionMapper.GetActionTypeText(map);
            dest.Product = actionMapper.GetProductText(map);
            dest.Module = actionMapper.GetModuleText(map);
        }
        
        dest.Date = tenantUtil.DateTimeFromUtc(dest.Date);
        if (!string.IsNullOrEmpty(dest.IP))
        {
            var splitIp = dest.IP.Split(':');
            if (splitIp.Length > 1)
            {
                dest.IP = splitIp[0];
            }
        }

        if (map?.ProductType == ProductType.Documents)
        {
            var rawNotificationInfo = dest.Description?.LastOrDefault();

            if (!string.IsNullOrEmpty(rawNotificationInfo) && rawNotificationInfo.StartsWith('{') && rawNotificationInfo.EndsWith('}'))
            {
                var notificationInfo = JsonSerializer.Deserialize<EventDescription<JsonElement>>(rawNotificationInfo);

                dest.Context = dest.Action == (int)MessageAction.RoomRenamed ? notificationInfo.RoomOldTitle :
                    !string.IsNullOrEmpty(notificationInfo.RoomTitle) ? notificationInfo.RoomTitle : notificationInfo.RootFolderTitle;
            }
        }

        if (string.IsNullOrEmpty(dest.Context))
        {
            dest.Context = dest.Module;
        }
    }
}
