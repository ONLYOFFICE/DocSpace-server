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
/// The webhook trigger type.
/// </summary>
public enum WebhookTrigger : long
{
    [Description("*")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(0)]
    [WebhookPayload(WebhookPayloadKind.None)]
    All = 0,


    #region User

    [Description("user.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(10)]
    [WebhookPayload(WebhookPayloadKind.User)]
    UserCreated = 1L << 0,

    [Description("user.invited")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(11)]
    [WebhookPayload(WebhookPayloadKind.User)]
    UserInvited = 1L << 1,

    [Description("user.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(12)]
    [WebhookPayload(WebhookPayloadKind.User)]
    UserUpdated = 1L << 2,

    [Description("user.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(13)]
    [WebhookPayload(WebhookPayloadKind.User)]
    UserDeleted = 1L << 3,

    #endregion User


    #region Group

    [Description("group.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(20)]
    [WebhookPayload(WebhookPayloadKind.Group)]
    GroupCreated = 1L << 4,

    [Description("group.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(21)]
    [WebhookPayload(WebhookPayloadKind.Group)]
    GroupUpdated = 1L << 5,

    [Description("group.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(22)]
    [WebhookPayload(WebhookPayloadKind.Group)]
    GroupDeleted = 1L << 6,

    #endregion


    #region File

    [Description("file.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(30)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileCreated = 1L << 7,

    [Description("file.uploaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(31)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileUploaded = 1L << 8,

    [Description("file.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(32)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileUpdated = 1L << 9,

    [Description("file.trashed")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(33)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileTrashed = 1L << 10,

    [Description("file.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(34)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileDeleted = 1L << 11,

    [Description("file.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(35)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileRestored = 1L << 12,

    [Description("file.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(36)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileCopied = 1L << 13,

    [Description("file.moved")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(37)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileMoved = 1L << 14,

    #endregion


    #region Folder

    [Description("folder.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(40)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderCreated = 1L << 15,

    [Description("folder.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(41)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderUpdated = 1L << 16,

    [Description("folder.trashed")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(42)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderTrashed = 1L << 17,

    [Description("folder.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(43)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderDeleted = 1L << 18,

    [Description("folder.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(44)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderRestored = 1L << 19,

    [Description("folder.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(45)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderCopied = 1L << 20,

    [Description("folder.moved")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(46)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderMoved = 1L << 21,

    #endregion


    #region Room

    [Description("room.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(50)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    RoomCreated = 1L << 22,

    [Description("room.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(51)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    RoomUpdated = 1L << 23,

    [Description("room.archived")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(52)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    RoomArchived = 1L << 24,

    [Description("room.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(53)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    RoomDeleted = 1L << 25,

    [Description("room.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(54)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    RoomRestored = 1L << 26,

    [Description("room.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(55)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    RoomCopied = 1L << 27,

    #endregion


    #region Forms

    [Description("form.submit")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(60)]
    [WebhookPayload(WebhookPayloadKind.FormSubmit)]
    FormSubmit = 1L << 28,

    [Description("form.filled.out")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(61)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FormFilledOut = 1L << 29,

    [Description("form.stopped")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(62)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FormStopped = 1L << 30,

    #endregion


    #region Agent

    [Description("agent.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [Order(70)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    AgentCreated = 1L << 31,

    [Description("agent.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(71)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    AgentUpdated = 1L << 32,

    [Description("agent.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(72)]
    [WebhookPayload(WebhookPayloadKind.Room)]
    AgentDeleted = 1L << 33,

    #endregion


    #region Download

    [Description("file.downloaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(38)]
    [WebhookPayload(WebhookPayloadKind.File)]
    FileDownloaded = 1L << 34,

    [Description("folder.downloaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    [Order(47)]
    [WebhookPayload(WebhookPayloadKind.Folder)]
    FolderDownloaded = 1L << 35,

    #endregion
}


public static class WebhookTriggerExtensions
{
    private record WebhookTriggerInfo(string CustomString, EmployeeType[] AvailableFor, int Order, WebhookPayloadKind PayloadKind);

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

            var payloadAttr = (WebhookPayloadAttribute)Attribute.GetCustomAttribute(field, typeof(WebhookPayloadAttribute));
            var payloadKind = payloadAttr?.Kind ?? WebhookPayloadKind.None;

            _triggers.Add(value, new WebhookTriggerInfo(customString, availableFor, order, payloadKind));
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

        public WebhookPayloadKind GetPayloadKind()
        {
            return _triggers.TryGetValue(value, out var info) ? info.PayloadKind : WebhookPayloadKind.None;
        }
    }
}
