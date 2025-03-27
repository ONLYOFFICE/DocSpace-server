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

namespace ASC.Webhooks.Core;

public enum WebhookTrigger
{
    [SwaggerEnum("*")]
    All = 0,


    #region User

    [SwaggerEnum("user.created")]
    UserCreated = 1,

    [SwaggerEnum("user.invited")]
    UserInvited = 2,

    [SwaggerEnum("user.updated")]
    UserUpdated = 4,

    [SwaggerEnum("user.deleted")]
    UserDeleted = 8,

    #endregion User


    #region Group

    [SwaggerEnum("group.created")]
    GroupCreated = 16,

    [SwaggerEnum("group.updated")]
    GroupUpdated = 32,

    [SwaggerEnum("group.deleted")]
    GroupDeleted = 64,

    #endregion


    #region File

    [SwaggerEnum("file.created")]
    FileCreated = 128,

    [SwaggerEnum("file.uploaded")]
    FileUploaded = 256,

    [SwaggerEnum("file.updated")]
    FileUpdated = 512,

    [SwaggerEnum("file.trashed")]
    FileTrashed = 1024,

    [SwaggerEnum("file.deleted")]
    FileDeleted = 2048,

    [SwaggerEnum("file.restored")]
    FileRestored = 4096,

    [SwaggerEnum("file.copied")]
    FileCopied = 8192,

    [SwaggerEnum("file.moved")]
    FileMoved = 16384,

    #endregion


    #region Folder

    [SwaggerEnum("folder.created")]
    FolderCreated = 32768,

    [SwaggerEnum("folder.updated")]
    FolderUpdated = 65536,

    [SwaggerEnum("folder.trashed")]
    FolderTrashed = 131072,

    [SwaggerEnum("folder.deleted")]
    FolderDeleted = 262144,

    [SwaggerEnum("folder.restored")]
    FolderRestored = 524288,

    [SwaggerEnum("folder.copied")]
    FolderCopied = 1048576,

    [SwaggerEnum("folder.moved")]
    FolderMoved = 2097152,

    #endregion


    #region Room

    [SwaggerEnum("room.created")]
    RoomCreated = 4194304,

    [SwaggerEnum("room.updated")]
    RoomUpdated = 8388608,

    [SwaggerEnum("room.archived")]
    RoomArchived = 16777216,

    [SwaggerEnum("room.deleted")]
    RoomDeleted = 33554432,

    [SwaggerEnum("room.restored")]
    RoomRestored = 67108864,

    [SwaggerEnum("room.copied")]
    RoomCopied = 134217728

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

            var attribute = (SwaggerEnumAttribute)Attribute.GetCustomAttribute(field, typeof(SwaggerEnumAttribute));

            _customStrings.Add(value, attribute != null ? attribute.Description : field.Name);
        }
    }

    public static string ToCustomString(this WebhookTrigger value)
    {
        return _customStrings[value];
    }

    public static string GetTargetType(this WebhookTrigger value)
    {
        return _customStrings[value].Split('.')[0];
    }
}