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

namespace ASC.Files.Tests.Tests._03_Rooms;

/// <summary>
/// Access-level matrices shared by the room permission suites, mirroring <c>roomAccesses</c>
/// and the role loops of the TypeScript suite.
/// </summary>
public static class RoomAccessData
{
    /// <summary>The room access levels an invitation can grant, mirroring <c>roomAccesses</c> in the TS suite.</summary>
    private static readonly FileShare[] _allRoomAccesses =
    [
        FileShare.Read, FileShare.Comment, FileShare.Review, FileShare.Editing, FileShare.ContentCreator, FileShare.RoomManager
    ];

    private static readonly FileShare[] _nonManagerRoomAccesses =
    [
        FileShare.Read, FileShare.Comment, FileShare.Review, FileShare.Editing, FileShare.ContentCreator
    ];

    /// <summary>
    /// The access levels a Virtual Data Room accepts. Comment and Review are rejected at invitation
    /// time for a VDR, so any matrix used against a VDR has to leave them out.
    /// </summary>
    private static readonly FileShare[] _vdrRoomAccesses =
    [
        FileShare.Read, FileShare.Editing, FileShare.ContentCreator, FileShare.RoomManager
    ];

    private static readonly FileShare[] _vdrNonManagerRoomAccesses =
    [
        FileShare.Read, FileShare.Editing, FileShare.ContentCreator
    ];

    public static TheoryData<FileShare> AllRoomAccesses => [.. _allRoomAccesses];

    public static TheoryData<FileShare> NonManagerAccesses => [.. _nonManagerRoomAccesses];

    /// <inheritdoc cref="_vdrRoomAccesses"/>
    public static TheoryData<FileShare> VdrRoomAccesses => [.. _vdrRoomAccesses];

    public static TheoryData<FileShare, int> UpdateRoomAccesses => new()
    {
        { FileShare.Read, 403 },
        { FileShare.Editing, 403 },
        { FileShare.ContentCreator, 403 },
        { FileShare.RoomManager, 200 }
    };

    /// <summary>
    /// Access levels the link tests can grant inside a public room. A public room accepts only
    /// RoomManager, ContentCreator and None for a user subject (see
    /// <c>FileSecurity.AvailableRoomAccesses</c>), so Editing and Read are not expressible here.
    /// Reading the primary external link is allowed for both levels.
    /// </summary>
    public static TheoryData<FileShare, int> PrimaryLinkAccesses => new()
    {
        { FileShare.RoomManager, 200 },
        { FileShare.ContentCreator, 200 }
    };

    /// <summary>
    /// Same room constraint as <see cref="PrimaryLinkAccesses"/>, but managing links is stricter:
    /// only RoomManager may do it, ContentCreator gets 403.
    /// </summary>
    public static TheoryData<FileShare, int> SetRoomLinkAccesses => new()
    {
        { FileShare.RoomManager, 200 },
        { FileShare.ContentCreator, 403 }
    };

    public static TheoryData<EmployeeType, FileShare> NonManagerAccessesForUserAndGuest
    {
        get
        {
            var data = new TheoryData<EmployeeType, FileShare>();

            foreach (var employeeType in new[] { EmployeeType.User, EmployeeType.Guest })
            {
                foreach (var access in _nonManagerRoomAccesses)
                {
                    data.Add(employeeType, access);
                }
            }

            return data;
        }
    }

    /// <summary>
    /// Every role/access combination an invitation can actually produce in a regular room. Only a
    /// RoomAdmin may be granted RoomManager access — the API rejects that access level for a User
    /// or a Guest, so those two combinations are left out.
    /// </summary>
    public static TheoryData<EmployeeType, FileShare> InvitedMemberAccesses
    {
        get
        {
            var data = new TheoryData<EmployeeType, FileShare>();

            foreach (var employeeType in new[] { EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest })
            {
                foreach (var access in _allRoomAccesses)
                {
                    if (access == FileShare.RoomManager && employeeType != EmployeeType.RoomAdmin)
                    {
                        continue;
                    }

                    data.Add(employeeType, access);
                }
            }

            return data;
        }
    }

    /// <inheritdoc cref="_vdrRoomAccesses"/>
    public static TheoryData<EmployeeType, FileShare> VdrNonManagerInvitedMemberAccesses =>
        BuildInvitedMemberMatrix(_vdrNonManagerRoomAccesses);

    private static TheoryData<EmployeeType, FileShare> BuildInvitedMemberMatrix(FileShare[] accesses)
    {
        var data = new TheoryData<EmployeeType, FileShare>();

        foreach (var employeeType in new[] { EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest })
        {
            foreach (var access in accesses)
            {
                data.Add(employeeType, access);
            }
        }

        return data;
    }
}
