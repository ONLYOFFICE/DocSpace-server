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
/// <see cref="ASC.Files.Core.Security.FileSecurity.RequestLinkApplies"/> decides whether a security check of an account
/// may use the link the request was opened with. Outside the document server's edit callback the link is the current
/// account's alone, also in a request with no signed-in account (an anonymous upload through the link checks the
/// webhook subscribers). Inside the callback's scope every checked editor uses it, an anonymous one as
/// <see cref="Guid.Empty"/>; a check that leaves the link out there drops those editors from the editor.
/// </summary>
public class RequestLinkAppliesTests
{
    private static readonly Guid _current = Guid.NewGuid();
    private static readonly Guid _other = Guid.NewGuid();

    [Fact]
    public void RequestLinkApplies_TheCurrentAccount_UsesTheLink()
    {
        ASC.Files.Core.Security.FileSecurity.RequestLinkApplies(_current, _current, insideRequestLinkScope: false).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(OtherAccounts))]
    public void RequestLinkApplies_AnotherAccountOutsideTheScope_DoesNotUseTheLink(Guid checkedAccount)
    {
        ASC.Files.Core.Security.FileSecurity.RequestLinkApplies(checkedAccount, ASC.Core.Configuration.Constants.Guest.ID, insideRequestLinkScope: false).Should().BeFalse();
        ASC.Files.Core.Security.FileSecurity.RequestLinkApplies(checkedAccount, _current, insideRequestLinkScope: false).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(OtherAccounts))]
    public void RequestLinkApplies_AnyAccountInsideTheScope_UsesTheLink(Guid checkedAccount)
    {
        ASC.Files.Core.Security.FileSecurity.RequestLinkApplies(checkedAccount, ASC.Core.Configuration.Constants.Guest.ID, insideRequestLinkScope: true).Should().BeTrue();
    }

    public static TheoryData<Guid> OtherAccounts => new()
    {
        Guid.Empty,
        Guid.NewGuid()
    };
}
