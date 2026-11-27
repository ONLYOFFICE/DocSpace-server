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

namespace ASC.Core.Common.Tests;

/// <summary>
/// A request keeps the share record each account has under a folder, and the record each account has of a room, so
/// the next entries are not resolved again. Before, a record found through a link was kept under the link's key, which
/// names no account, and any account the request's link applied to read it before its own records; and the record of a
/// room was computed from the shares of the entry checked and kept under the key of the folder records, so a link of
/// one file could answer for its siblings. The keys now always name the account, a room's record has keys of its own,
/// a room's record is kept only when it is the room's, and a folder's record only when it is the account's own or the
/// record of the link the request was opened with.
/// </summary>
public class UserRecordCacheTests
{
    private const int TenantId = 1;
    private const int RoomId = 10;
    private const int FolderId = 12;
    private const int FileId = 11;

    private static readonly Guid _member = Guid.NewGuid();
    private static readonly Guid _linkVisitor = Guid.NewGuid();
    private static readonly Guid _requestLink = Guid.NewGuid();

    [Fact]
    public void GetUserRecordCacheKey_TwoAccountsTheRequestsLinkAppliesTo_GetDifferentKeys()
    {
        var member = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: true);
        var visitor = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId, _linkVisitor, requestLinkApplies: true);

        member.Should().NotBe(visitor);
    }

    [Fact]
    public void GetUserRecordCacheKey_OneAccountWithAndWithoutTheRequestsLink_GetsDifferentKeys()
    {
        var withLink = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: true);
        var withoutLink = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: false);

        withLink.Should().NotBe(withoutLink);
    }

    [Fact]
    public void GetUserRecordCacheKey_AnotherTenantOrFolder_GetsAnotherKey()
    {
        var key = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: false);

        ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId + 1, RoomId, _member, requestLinkApplies: false).Should().NotBe(key);
        ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId + 1, _member, requestLinkApplies: false).Should().NotBe(key);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetRoomRecordCacheKey_TheSameRoomAndAccount_NeverMatchesTheFolderRecordKey(bool requestLinkApplies)
    {
        // a room is the parent of the entries in it, so both keys are built from the same id
        var room = ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies);
        var folder = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies);

        room.Should().NotBe(folder);
    }

    [Fact]
    public void GetRoomRecordCacheKey_AnotherTenantRoomOrLinkScope_GetsAnotherKey()
    {
        var key = ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: false);

        ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId + 1, RoomId, _member, requestLinkApplies: false).Should().NotBe(key);
        ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId, RoomId + 1, _member, requestLinkApplies: false).Should().NotBe(key);
        ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: true).Should().NotBe(key);
    }

    [Fact]
    public void GetRoomRecordCacheKey_TwoAccounts_GetDifferentKeys()
    {
        var member = ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId, RoomId, _member, requestLinkApplies: true);
        var visitor = ASC.Files.Core.Security.FileSecurity.GetRoomRecordCacheKey(TenantId, RoomId, _linkVisitor, requestLinkApplies: true);

        member.Should().NotBe(visitor);
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.User)]
    [InlineData(ASC.Files.Core.Security.SubjectType.ExternalLink)]
    public void IsRecordOfTheRoom_ARecordOfTheRoom_IsTheRooms(ASC.Files.Core.Security.SubjectType subjectType)
    {
        var record = Record(RoomId, FileEntryType.Folder, subjectType);

        ASC.Files.Core.Security.FileSecurity.IsRecordOfTheRoom(record, RoomId).Should().BeTrue();
    }

    [Fact]
    public void IsRecordOfTheRoom_ALinkOfAFileOrAFolderInTheRoom_IsNotTheRooms()
    {
        var fileLink = Record(FileId, FileEntryType.File, ASC.Files.Core.Security.SubjectType.ExternalLink);
        var folderLink = Record(FolderId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.PrimaryExternalLink);
        // a file may share its id with the room; the kind of the entry tells them apart
        var fileWithTheRoomsId = Record(RoomId, FileEntryType.File, ASC.Files.Core.Security.SubjectType.ExternalLink);

        ASC.Files.Core.Security.FileSecurity.IsRecordOfTheRoom(fileLink, RoomId).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.IsRecordOfTheRoom(folderLink, RoomId).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.IsRecordOfTheRoom(fileWithTheRoomsId, RoomId).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.IsRecordOfTheRoom<int>(null, RoomId).Should().BeFalse();
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.User)]
    [InlineData(ASC.Files.Core.Security.SubjectType.AiProject)]
    public void RecordHoldsForSiblings_AnInheritedRecordOfTheAccount_Holds(ASC.Files.Core.Security.SubjectType subjectType)
    {
        var record = Record(FolderId, FileEntryType.Folder, subjectType);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File, Guid.Empty).Should().BeTrue();
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.ExternalLink)]
    [InlineData(ASC.Files.Core.Security.SubjectType.PrimaryExternalLink)]
    [InlineData(ASC.Files.Core.Security.SubjectType.User)]
    public void RecordHoldsForSiblings_ARecordOfTheFileItself_DoesNotHold(ASC.Files.Core.Security.SubjectType subjectType)
    {
        var record = Record(FileId, FileEntryType.File, subjectType);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File, Guid.Empty).Should().BeFalse();
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.ExternalLink)]
    [InlineData(ASC.Files.Core.Security.SubjectType.PrimaryExternalLink)]
    public void RecordHoldsForSiblings_AnInheritedLink_DoesNotHold(ASC.Files.Core.Security.SubjectType subjectType)
    {
        // a sibling's own link, or another link the account remembers, can outrank the folder's link
        var record = Record(FolderId, FileEntryType.Folder, subjectType);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File, Guid.Empty).Should().BeFalse();
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.ExternalLink)]
    [InlineData(ASC.Files.Core.Security.SubjectType.PrimaryExternalLink)]
    public void RecordHoldsForSiblings_TheInheritedRecordOfTheRequestsLink_Holds(ASC.Files.Core.Security.SubjectType subjectType)
    {
        // no sibling has a record of its own for that link, and no remembered link outranks it
        var record = Record(FolderId, FileEntryType.Folder, subjectType, _requestLink);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File, _requestLink).Should().BeTrue();
    }

    [Fact]
    public void RecordHoldsForSiblings_TheRequestsLinkOnTheFileItselfOrAnotherLink_DoesNotHold()
    {
        var onTheFile = Record(FileId, FileEntryType.File, ASC.Files.Core.Security.SubjectType.ExternalLink, _requestLink);
        var rememberedLink = Record(FolderId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.ExternalLink);
        var noRequestLink = Record(FolderId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.ExternalLink, _requestLink);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(onTheFile, FileId, FileEntryType.File, _requestLink).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(rememberedLink, FileId, FileEntryType.File, _requestLink).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(noRequestLink, FileId, FileEntryType.File, Guid.Empty).Should().BeFalse();
    }

    [Fact]
    public void RecordHoldsForSiblings_AFolderRecordWithTheFilesId_Holds()
    {
        // a folder and a file may share an id; the kind of the entry tells them apart
        var record = Record(FileId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.User);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File, Guid.Empty).Should().BeTrue();
    }

    [Fact]
    public void RecordHoldsForSiblings_NoRecordOrAGroupRecord_DoesNotHold()
    {
        var group = Record(FolderId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.Group);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings<int>(null, FileId, FileEntryType.File, Guid.Empty).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(group, FileId, FileEntryType.File, Guid.Empty).Should().BeFalse();
    }

    private static ASC.Files.Core.Security.FileShareRecord<int> Record(int entryId, FileEntryType entryType, ASC.Files.Core.Security.SubjectType subjectType, Guid? subject = null)
    {
        return new ASC.Files.Core.Security.FileShareRecord<int>
        {
            EntryId = entryId,
            EntryType = entryType,
            SubjectType = subjectType,
            Subject = subject ?? Guid.NewGuid(),
            Share = ASC.Files.Core.Security.FileShare.Editing
        };
    }
}
