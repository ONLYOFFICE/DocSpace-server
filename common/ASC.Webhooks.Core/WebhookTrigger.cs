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
public enum WebhookTrigger
{
    [Description("*")]
    All = 0,


    #region User

    [Description("user.created")]
    UserCreated = 1,

    [Description("user.invited")]
    UserInvited = 2,

    [Description("user.updated")]
    UserUpdated = 4,

    [Description("user.deleted")]
    UserDeleted = 8,

    #endregion User


    #region Group

    [Description("group.created")]
    GroupCreated = 16,

    [Description("group.updated")]
    GroupUpdated = 32,

    [Description("group.deleted")]
    GroupDeleted = 64,

    #endregion


    #region File

    [Description("file.created")]
    FileCreated = 128,

    [Description("file.uploaded")]
    FileUploaded = 256,

    [Description("file.updated")]
    FileUpdated = 512,

    [Description("file.trashed")]
    FileTrashed = 1024,

    [Description("file.deleted")]
    FileDeleted = 2048,

    [Description("file.restored")]
    FileRestored = 4096,

    [Description("file.copied")]
    FileCopied = 8192,

    [Description("file.moved")]
    FileMoved = 16384,

    #endregion


    #region Folder

    [Description("folder.created")]
    FolderCreated = 32768,

    [Description("folder.updated")]
    FolderUpdated = 65536,

    [Description("folder.trashed")]
    FolderTrashed = 131072,

    [Description("folder.deleted")]
    FolderDeleted = 262144,

    [Description("folder.restored")]
    FolderRestored = 524288,

    [Description("folder.copied")]
    FolderCopied = 1048576,

    [Description("folder.moved")]
    FolderMoved = 2097152,

    #endregion


    #region Room

    [Description("room.created")]
    RoomCreated = 4194304,

    [Description("room.updated")]
    RoomUpdated = 8388608,

    [Description("room.archived")]
    RoomArchived = 16777216,

    [Description("room.deleted")]
    RoomDeleted = 33554432,

    [Description("room.restored")]
    RoomRestored = 67108864,

    [Description("room.copied")]
    RoomCopied = 134217728,

    #endregion

    //remaining possible values: 268435456, 536870912, 1073741824
}


public static partial class WebhookTriggerExtensions
{
    private static readonly Dictionary<WebhookTrigger, string> _customStrings;

    static WebhookTriggerExtensions()
    {
        _customStrings = [];

        var type = typeof(WebhookTrigger);

        foreach (var value in Enum.GetValues<WebhookTrigger>())
        {
            var field = type.GetField(value.ToString());

            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

            _customStrings.Add(value, attribute != null ? attribute.Description : field.Name);
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
    }
}