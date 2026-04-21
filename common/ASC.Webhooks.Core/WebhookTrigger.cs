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
    All = 0,


    #region User

    [Description("user.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    UserCreated = 1L << 0,

    [Description("user.invited")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    UserInvited = 1L << 1,

    [Description("user.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    UserUpdated = 1L << 2,

    [Description("user.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    UserDeleted = 1L << 3,

    #endregion User


    #region Group

    [Description("group.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    GroupCreated = 1L << 4,

    [Description("group.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    GroupUpdated = 1L << 5,

    [Description("group.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    GroupDeleted = 1L << 6,

    #endregion


    #region File

    [Description("file.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileCreated = 1L << 7,

    [Description("file.uploaded")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileUploaded = 1L << 8,

    [Description("file.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileUpdated = 1L << 9,

    [Description("file.trashed")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileTrashed = 1L << 10,

    [Description("file.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileDeleted = 1L << 11,

    [Description("file.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileRestored = 1L << 12,

    [Description("file.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileCopied = 1L << 13,

    [Description("file.moved")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FileMoved = 1L << 14,

    #endregion


    #region Folder

    [Description("folder.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderCreated = 1L << 15,

    [Description("folder.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderUpdated = 1L << 16,

    [Description("folder.trashed")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderTrashed = 1L << 17,

    [Description("folder.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderDeleted = 1L << 18,

    [Description("folder.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderRestored = 1L << 19,

    [Description("folder.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderCopied = 1L << 20,

    [Description("folder.moved")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FolderMoved = 1L << 21,

    #endregion


    #region Room

    [Description("room.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    RoomCreated = 1L << 22,

    [Description("room.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    RoomUpdated = 1L << 23,

    [Description("room.archived")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    RoomArchived = 1L << 24,

    [Description("room.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    RoomDeleted = 1L << 25,

    [Description("room.restored")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    RoomRestored = 1L << 26,

    [Description("room.copied")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    RoomCopied = 1L << 27,

    #endregion


    #region Forms

    [Description("form.submit")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FormSubmit = 1L << 28,

    [Description("form.filled.out")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FormFilledOut = 1L << 29,

    [Description("form.stopped")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    FormStopped = 1L << 30,

    #endregion


    #region Agent

    [Description("agent.created")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    AgentCreated = 1L << 31,

    [Description("agent.updated")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    AgentUpdated = 1L << 32,

    [Description("agent.deleted")]
    [AvailableFor(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User)]
    AgentDeleted = 1L << 33,

    #endregion
}


public static partial class WebhookTriggerExtensions
{
    private static readonly Dictionary<WebhookTrigger, string> _customStrings;
    private static readonly Dictionary<WebhookTrigger, EmployeeType[]> _availableFor;

    static WebhookTriggerExtensions()
    {
        _customStrings = [];
        _availableFor = [];

        var type = typeof(WebhookTrigger);

        foreach (var value in Enum.GetValues<WebhookTrigger>())
        {
            var field = type.GetField(value.ToString());

            var description = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            _customStrings.Add(value, description != null ? description.Description : field.Name);

            var availability = (AvailableForAttribute)Attribute.GetCustomAttribute(field, typeof(AvailableForAttribute));
            _availableFor.Add(value, availability != null ? availability.Roles : []);
        }
    }

    extension(WebhookTrigger value)
    {
        public string ToCustomString()
        {
            return _customStrings[value];
        }

        public string GetTargetType()
        {
            return _customStrings[value].Split('.')[0];
        }

        public bool IsAvailableFor(EmployeeType employeeType)
        {
            return _availableFor.TryGetValue(value, out var roles)
                && Array.IndexOf(roles, employeeType) >= 0;
        }
    }
}
