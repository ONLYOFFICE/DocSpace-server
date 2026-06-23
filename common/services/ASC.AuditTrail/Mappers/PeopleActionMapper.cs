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

namespace ASC.AuditTrail.Mappers;

internal class PeopleActionMapper : IProductActionMapper
{
    public List<ILocationActionMapper> Mappers { get; } =
    [
        new UsersActionMapper(),
        new GroupsActionMapper()
    ];

    public ProductType Product => ProductType.Contacts;
}

internal class UsersActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public UsersActionMapper()
    {
        Location = LocationType.Contacts;

        Actions = new MessageMapsDictionary(ProductType.Contacts, Location)
        {
            {
                EntryType.User,
                new Dictionary<ActionType, MessageAction[]>
                {
                    {
                        ActionType.Create, [
                            MessageAction.UserCreated, MessageAction.GuestCreated,
                            MessageAction.UserCreatedViaInvite, MessageAction.GuestCreatedViaInvite,
                            MessageAction.SendJoinInvite
                        ]
                    },
                    {
                        ActionType.Update, [
                            MessageAction.UserActivated, MessageAction.GuestActivated, MessageAction.UserUpdated,
                            MessageAction.UserUpdatedMobileNumber, MessageAction.UserUpdatedLanguage, MessageAction.UserAddedAvatar,
                            MessageAction.UserUpdatedAvatarThumbnails, MessageAction.UserUpdatedEmail, MessageAction.UsersUpdatedType,
                            MessageAction.UsersUpdatedStatus, MessageAction.UsersSentActivationInstructions
                        ]
                    },
                    {
                        ActionType.Delete, [MessageAction.UserDeletedAvatar, MessageAction.UserDeleted, MessageAction.UsersDeleted, MessageAction.UserDataRemoving]
                    },
                    { ActionType.Import, [MessageAction.UserImported, MessageAction.GuestImported] },
                    { ActionType.Logout, [MessageAction.UserLogoutActiveConnections, MessageAction.UserLogoutActiveConnection, MessageAction.UserLogoutActiveConnectionsForUser] }
                },
                new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Reassigns, MessageAction.UserDataReassigns }
                }
            },
            { MessageAction.UserLinkedSocialAccount, ActionType.Link },
            { MessageAction.UserUnlinkedSocialAccount, ActionType.Unlink },
            {
                ActionType.Send, [MessageAction.UserSentActivationInstructions, MessageAction.UserSentDeleteInstructions, MessageAction.SentInviteInstructions]
            },
            { MessageAction.UserUpdatedPassword, ActionType.Update },
            { MessageAction.UserSentEmailChangeInstructions, new MessageMaps(nameof(AuditReportResource.UserSentEmailInstructions), ActionType.Send, ProductType.Contacts, Location, EntryType.User) },
            { MessageAction.UserSentPasswordChangeInstructions, new MessageMaps(nameof(AuditReportResource.UserSentPasswordInstructions), ActionType.Send, ProductType.Contacts, Location, EntryType.User) },
            { MessageAction.UserConnectedTfaApp, new MessageMaps(nameof(AuditReportResource.UserTfaGenerateCodes), ActionType.Link, ProductType.Contacts, Location, EntryType.User) },
            { MessageAction.UserDisconnectedTfaApp, new MessageMaps(nameof(AuditReportResource.UserTfaDisconnected), ActionType.Delete, ProductType.Contacts, Location, EntryType.User) }
        };
    }
}

internal class GroupsActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public GroupsActionMapper()
    {
        Location = LocationType.Contacts;

        Actions = new MessageMapsDictionary(ProductType.Contacts, Location)
        {
            {
                EntryType.Group, new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Create, MessageAction.GroupCreated },
                    { ActionType.Update, MessageAction.GroupUpdated },
                    { ActionType.Delete, MessageAction.GroupDeleted }
                }
            }
        };
    }
}