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

namespace ASC.Webhooks.Core;

/// <summary>
/// The webhook trigger type.
/// </summary>
public enum WebhookTrigger : long
{
    [Description("*")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(0)]
    All = 0,


    #region User

    [Description("user.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(10)]
    UserCreated = 1L << 0,

    [Description("user.invited")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(11)]
    UserInvited = 1L << 1,

    [Description("user.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(12)]
    UserUpdated = 1L << 2,

    [Description("user.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(13)]
    UserDeleted = 1L << 3,

    #endregion User


    #region Group

    [Description("group.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(20)]
    GroupCreated = 1L << 4,

    [Description("group.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(21)]
    GroupUpdated = 1L << 5,

    [Description("group.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(22)]
    GroupDeleted = 1L << 6,

    #endregion


    #region File

    [Description("file.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(30)]
    FileCreated = 1L << 7,

    [Description("file.uploaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(31)]
    FileUploaded = 1L << 8,

    [Description("file.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(32)]
    FileUpdated = 1L << 9,

    [Description("file.trashed")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(33)]
    FileTrashed = 1L << 10,

    [Description("file.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(34)]
    FileDeleted = 1L << 11,

    [Description("file.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(35)]
    FileRestored = 1L << 12,

    [Description("file.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(36)]
    FileCopied = 1L << 13,

    [Description("file.moved")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(37)]
    FileMoved = 1L << 14,

    #endregion


    #region Folder

    [Description("folder.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(40)]
    FolderCreated = 1L << 15,

    [Description("folder.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(41)]
    FolderUpdated = 1L << 16,

    [Description("folder.trashed")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(42)]
    FolderTrashed = 1L << 17,

    [Description("folder.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(43)]
    FolderDeleted = 1L << 18,

    [Description("folder.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(44)]
    FolderRestored = 1L << 19,

    [Description("folder.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(45)]
    FolderCopied = 1L << 20,

    [Description("folder.moved")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(46)]
    FolderMoved = 1L << 21,

    #endregion


    #region Room

    [Description("room.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(50)]
    RoomCreated = 1L << 22,

    [Description("room.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(51)]
    RoomUpdated = 1L << 23,

    [Description("room.archived")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(52)]
    RoomArchived = 1L << 24,

    [Description("room.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(53)]
    RoomDeleted = 1L << 25,

    [Description("room.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(54)]
    RoomRestored = 1L << 26,

    [Description("room.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(55)]
    RoomCopied = 1L << 27,

    #endregion


    #region Forms

    [Description("form.submit")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(60)]
    FormSubmit = 1L << 28,

    [Description("form.filled.out")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(61)]
    FormFilledOut = 1L << 29,

    [Description("form.stopped")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(62)]
    FormStopped = 1L << 30,

    #endregion


    #region Agent

    [Description("agent.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(70)]
    AgentCreated = 1L << 31,

    [Description("agent.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(71)]
    AgentUpdated = 1L << 32,

    [Description("agent.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(72)]
    AgentDeleted = 1L << 33,

    #endregion


    #region Download

    [Description("file.downloaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(38)]
    FileDownloaded = 1L << 34,

    [Description("folder.downloaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(47)]
    FolderDownloaded = 1L << 35,

    #endregion
}


public static class WebhookTriggerExtensions
{
    private record WebhookTriggerInfo(string CustomString, EmployeeType[] AvailableFor, int Order);

    private static readonly Dictionary<WebhookTrigger, WebhookTriggerInfo> _triggers;

    static WebhookTriggerExtensions()
    {
        _triggers = [];

        var type = typeof(WebhookTrigger);

        foreach (var value in Enum.GetValues<WebhookTrigger>())
        {
            var field = type.GetField(value.ToString());

            if (field == null)
            {
                continue;
            }

            var description = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            var customString = description?.Description ?? field.Name;

            var availability = (AvailableForAttribute)Attribute.GetCustomAttribute(field, typeof(AvailableForAttribute));
            var availableFor = availability?.Roles ?? [];

            var orderAttr = (OrderAttribute)Attribute.GetCustomAttribute(field, typeof(OrderAttribute));
            var order = orderAttr?.Order ?? int.MaxValue;

            _triggers.Add(value, new WebhookTriggerInfo(customString, availableFor, order));
        }
    }

    extension(WebhookTrigger value)
    {
        public string ToCustomString()
        {
            return _triggers[value].CustomString;
        }

        public string GetTargetType()
        {
            return _triggers[value].CustomString.Split('.')[0];
        }

        public bool IsAvailableFor(EmployeeType employeeType)
        {
            return _triggers.TryGetValue(value, out var info)
                && Array.IndexOf(info.AvailableFor, employeeType) >= 0;
        }

        public int GetOrder()
        {
            return _triggers.TryGetValue(value, out var info) ? info.Order : int.MaxValue;
        }
    }
}
