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

[EnumExtensions]
public enum WebhookTrigger
{
    [SwaggerEnum("None")]
    None = 0,


    #region User

    [SwaggerEnum("User created")]
    UserCreated = 100,

    [SwaggerEnum("User invited")]
    UserInvited = 101,

    [SwaggerEnum("User updated")]
    UserUpdated = 102,

    [SwaggerEnum("User deleted")]
    UserDeleted = 103,

    #endregion User


    #region Group

    [SwaggerEnum("Group created")]
    GroupCreated = 200,

    [SwaggerEnum("Group updated")]
    GroupUpdated = 201,

    [SwaggerEnum("Group deleted")]
    GroupDeleted = 203,

    #endregion


    #region File

    [SwaggerEnum("File created")]
    FileCreated = 300,

    [SwaggerEnum("File uploaded")]
    FileUploaded = 301,

    [SwaggerEnum("File updated")]
    FileUpdated = 302,

    [SwaggerEnum("File moved to trash")]
    FileTrashed = 303,

    [SwaggerEnum("File permanently deleted")]
    FileDeleted = 304,

    [SwaggerEnum("File restored from trash")]
    FileRestored = 305,

    [SwaggerEnum("File copied")]
    FileCopied = 306,

    [SwaggerEnum("File moved from one folder to another")]
    FileMoved = 307,

    #endregion


    #region Folder

    [SwaggerEnum("Folder created")]
    FolderCreated = 400,

    [SwaggerEnum("Folder updated")]
    FolderUpdated = 401,

    [SwaggerEnum("Folder moved to trash")]
    FolderTrashed = 402,

    [SwaggerEnum("Folder permanently deleted")]
    FolderDeleted = 403,

    [SwaggerEnum("Folder restored from trash")]
    FolderRestored = 404,

    [SwaggerEnum("Folder copied")]
    FolderCopied = 405,

    [SwaggerEnum("Folder moved from one folder to another")]
    FolderMoved = 406,

    #endregion


    #region Room

    [SwaggerEnum("Room created")]
    RoomCreated = 500,

    [SwaggerEnum("Room updated")]
    RoomUpdated = 501,

    [SwaggerEnum("Room moved to archive")]
    RoomArchived = 502,

    [SwaggerEnum("Room permanently deleted")]
    RoomDeleted = 503,

    [SwaggerEnum("Room restored from archive")]
    RoomRestored = 504

    #endregion
}