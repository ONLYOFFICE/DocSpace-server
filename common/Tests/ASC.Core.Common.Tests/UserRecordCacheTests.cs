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
/// A request keeps the share record each account has under a folder, so the next entries of that folder are not
/// resolved again. Before, a record found through a link was kept under the link's key, which names no account, and
/// any account the request's link applied to read it before its own records: a member checked after a link visitor in
/// the editor callback got the visitor's link access. The key now always names the account, and only a record the
/// entry inherits is kept for its siblings.
/// </summary>
public class UserRecordCacheTests
{
    private const int TenantId = 1;
    private const int ParentId = 10;
    private const int FileId = 11;

    private static readonly Guid _member = Guid.NewGuid();
    private static readonly Guid _linkVisitor = Guid.NewGuid();

    [Fact]
    public void GetUserRecordCacheKey_TwoAccountsTheRequestsLinkAppliesTo_GetDifferentKeys()
    {
        var member = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, ParentId, _member, requestLinkApplies: true);
        var visitor = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, ParentId, _linkVisitor, requestLinkApplies: true);

        member.Should().NotBe(visitor);
    }

    [Fact]
    public void GetUserRecordCacheKey_OneAccountWithAndWithoutTheRequestsLink_GetsDifferentKeys()
    {
        var withLink = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, ParentId, _member, requestLinkApplies: true);
        var withoutLink = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, ParentId, _member, requestLinkApplies: false);

        withLink.Should().NotBe(withoutLink);
    }

    [Fact]
    public void GetUserRecordCacheKey_AnotherTenantOrFolder_GetsAnotherKey()
    {
        var key = ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, ParentId, _member, requestLinkApplies: false);

        ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId + 1, ParentId, _member, requestLinkApplies: false).Should().NotBe(key);
        ASC.Files.Core.Security.FileSecurity.GetUserRecordCacheKey(TenantId, ParentId + 1, _member, requestLinkApplies: false).Should().NotBe(key);
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.User)]
    [InlineData(ASC.Files.Core.Security.SubjectType.ExternalLink)]
    [InlineData(ASC.Files.Core.Security.SubjectType.PrimaryExternalLink)]
    public void RecordHoldsForSiblings_ARecordOfTheFolder_Holds(ASC.Files.Core.Security.SubjectType subjectType)
    {
        var record = Record(ParentId, FileEntryType.Folder, subjectType);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File).Should().BeTrue();
    }

    [Theory]
    [InlineData(ASC.Files.Core.Security.SubjectType.ExternalLink)]
    [InlineData(ASC.Files.Core.Security.SubjectType.PrimaryExternalLink)]
    public void RecordHoldsForSiblings_ALinkOfTheFileItself_DoesNotHold(ASC.Files.Core.Security.SubjectType subjectType)
    {
        var record = Record(FileId, FileEntryType.File, subjectType);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File).Should().BeFalse();
    }

    [Fact]
    public void RecordHoldsForSiblings_AFolderRecordWithTheFilesId_Holds()
    {
        // a folder and a file may share an id; the kind of the entry tells them apart
        var record = Record(FileId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.User);

        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(record, FileId, FileEntryType.File).Should().BeTrue();
    }

    [Fact]
    public void RecordHoldsForSiblings_NoRecordOrAGroupRecord_DoesNotHold()
    {
        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings<int>(null, FileId, FileEntryType.File).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.RecordHoldsForSiblings(Record(ParentId, FileEntryType.Folder, ASC.Files.Core.Security.SubjectType.Group), FileId, FileEntryType.File).Should().BeFalse();
    }

    private static ASC.Files.Core.Security.FileShareRecord<int> Record(int entryId, FileEntryType entryType, ASC.Files.Core.Security.SubjectType subjectType)
    {
        return new ASC.Files.Core.Security.FileShareRecord<int>
        {
            EntryId = entryId,
            EntryType = entryType,
            SubjectType = subjectType,
            Subject = Guid.NewGuid(),
            Share = ASC.Files.Core.Security.FileShare.Editing
        };
    }
}
