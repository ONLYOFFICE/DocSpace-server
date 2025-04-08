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

using NetEscapades.EnumGenerators;

namespace ASC.Webhooks.Core;

/// <summary>
/// The webhook trigger type.
/// </summary>
[EnumExtensions]
public enum WebhookTrigger
{
    [SwaggerEnum("All")]
    All = 0,


    #region User

    [SwaggerEnum("User created")]
    UserCreated = 1,

    [SwaggerEnum("User invited")]
    UserInvited = 2,

    [SwaggerEnum("User updated")]
    UserUpdated = 4,

    [SwaggerEnum("User deleted")]
    UserDeleted = 8,

    #endregion User


    #region Group

    [SwaggerEnum("Group created")]
    GroupCreated = 16,

    [SwaggerEnum("Group updated")]
    GroupUpdated = 32,

    [SwaggerEnum("Group deleted")]
    GroupDeleted = 64,

    #endregion


    #region File

    [SwaggerEnum("File created")]
    FileCreated = 128,

    [SwaggerEnum("File uploaded")]
    FileUploaded = 256,

    [SwaggerEnum("File updated")]
    FileUpdated = 512,

    [SwaggerEnum("File moved to trash")]
    FileTrashed = 1024,

    [SwaggerEnum("File permanently deleted")]
    FileDeleted = 2048,

    [SwaggerEnum("File restored from trash")]
    FileRestored = 4096,

    [SwaggerEnum("File copied")]
    FileCopied = 8192,

    [SwaggerEnum("File moved from one folder to another")]
    FileMoved = 16384,

    #endregion


    #region Folder

    [SwaggerEnum("Folder created")]
    FolderCreated = 32768,

    [SwaggerEnum("Folder updated")]
    FolderUpdated = 65536,

    [SwaggerEnum("Folder moved to trash")]
    FolderTrashed = 131072,

    [SwaggerEnum("Folder permanently deleted")]
    FolderDeleted = 262144,

    [SwaggerEnum("Folder restored from trash")]
    FolderRestored = 524288,

    [SwaggerEnum("Folder copied")]
    FolderCopied = 1048576,

    [SwaggerEnum("Folder moved from one folder to another")]
    FolderMoved = 2097152,

    #endregion


    #region Room

    [SwaggerEnum("Room created")]
    RoomCreated = 4194304,

    [SwaggerEnum("Room updated")]
    RoomUpdated = 8388608,

    [SwaggerEnum("Room moved to archive")]
    RoomArchived = 16777216,

    [SwaggerEnum("Room permanently deleted")]
    RoomDeleted = 33554432,

    [SwaggerEnum("Room restored from archive")]
    RoomRestored = 67108864,

    [SwaggerEnum("Room copied")]
    RoomCopied = 134217728

    #endregion

    //remaining possible values: 268435456, 536870912, 1073741824
}