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


namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// The author, last editor and group manager a subscriber without contacts access (a User or a Guest) receives: the
/// REST API shows such a reader neither the profile link nor the user name, which is the local part of the email
/// address, of anyone but themselves.
/// </summary>
[Trait("Category", "Webhooks")]
public class UserSummaryWebhookRestrictionTests
{
    private static readonly Guid _subscriberId = new("8a2f6e0c-3b1d-4c7e-9f55-1d2c3b4a5e6f");

    private static UserSummaryWebhookDto CreateSummary(Guid id) => new()
    {
        Id = id,
        DisplayName = "Mike Zanyatski",
        UserName = "mike.zanyatski",
        Avatar = "https://example.com/avatar.jpg",
        ProfileUrl = "https://example.onlyoffice.com/accounts/people/filter?search=mike.zanyatski%40example.com"
    };

    [Fact]
    public void SomeoneElse_LosesProfileUrlAndUserName()
    {
        var summary = CreateSummary(Guid.NewGuid());

        var restricted = UserWebhookDtoHelper.RestrictSummaryForSubscriber(summary, _subscriberId);

        restricted.ProfileUrl.Should().BeNull();
        restricted.UserName.Should().BeNull();
        restricted.Id.Should().Be(summary.Id);
        restricted.DisplayName.Should().Be("Mike Zanyatski");
        restricted.Avatar.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public void SomeoneElse_LeavesTheSharedSummaryIntact()
    {
        var summary = CreateSummary(Guid.NewGuid());

        var restricted = UserWebhookDtoHelper.RestrictSummaryForSubscriber(summary, _subscriberId);

        restricted.Should().NotBeSameAs(summary);
        summary.UserName.Should().Be("mike.zanyatski");
        summary.ProfileUrl.Should().NotBeNull();
    }

    [Fact]
    public void SomeoneElse_WithUserNameOnly_LosesIt()
    {
        var summary = CreateSummary(Guid.NewGuid());
        summary.ProfileUrl = null;

        var restricted = UserWebhookDtoHelper.RestrictSummaryForSubscriber(summary, _subscriberId);

        restricted.UserName.Should().BeNull();
    }

    [Fact]
    public void Themselves_KeepEverything()
    {
        var summary = CreateSummary(_subscriberId);

        var restricted = UserWebhookDtoHelper.RestrictSummaryForSubscriber(summary, _subscriberId);

        restricted.Should().BeSameAs(summary);
        restricted.UserName.Should().Be("mike.zanyatski");
        restricted.ProfileUrl.Should().NotBeNull();
    }

    [Fact]
    public void NoSummary_StaysNull()
    {
        UserWebhookDtoHelper.RestrictSummaryForSubscriber(null, _subscriberId).Should().BeNull();
    }

    [Fact]
    public void Mapper_CopiesIdAndUserName_AndLeavesTheEnrichedFieldsToTheHelper()
    {
        var user = new UserInfo { Id = Guid.NewGuid(), UserName = "mike.zanyatski", FirstName = "Mike", LastName = "Zanyatski" };

        var summary = user.MapToSummaryWebhookDto();

        summary.Id.Should().Be(user.Id);
        summary.UserName.Should().Be("mike.zanyatski");
        summary.DisplayName.Should().BeNull();
        summary.Avatar.Should().BeNull();
        summary.ProfileUrl.Should().BeNull();
    }
}
