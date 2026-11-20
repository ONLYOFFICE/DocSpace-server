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

namespace ASC.Site.Core.Tests;

/// <summary>
/// <see cref="SubscriptionRules"/> and the JSON of the subscription DTOs, read and written with the web defaults of
/// ASP.NET Core as the controller does.
/// </summary>
public class SubscriptionRulesTests
{
    [Theory]
    [InlineData(false, false, UnsubscribeAction.UnsubscribeAll)]
    [InlineData(false, true, UnsubscribeAction.UnsubscribeNews)]
    [InlineData(true, true, UnsubscribeAction.Resubscribe)]
    [InlineData(true, false, UnsubscribeAction.Resubscribe)]
    public void UnsubscribeFlags_MapToTheActionOfTheSite(bool subscribe, bool newsOnly, UnsubscribeAction expected)
    {
        SubscriptionRules.GetUnsubscribeAction(subscribe, newsOnly).Should().Be(expected);
    }

    [Theory]
    [InlineData(SubscribeType.AppServer, SubscribeType.AppServer)]
    [InlineData(SubscribeType.Opensource, SubscribeType.Opensource)]
    [InlineData(null, SubscribeType.Common)]
    public void SubscribeType_IsTheListOfTheRequestOrCommon(SubscribeType? fromId, SubscribeType expected)
    {
        var request = new SubscriptionId("a@example.com", "A", fromId, IsSubscriptionRequest: true);

        SubscriptionRules.GetSubscribeType(request).Should().Be(expected);
    }

    [Fact]
    public void SubscribeType_OfAPlainAddress_IsRefused()
    {
        // The id of a login letter or of an unsubscription asked for no subscription.
        SubscriptionRules.GetSubscribeType(new SubscriptionId("a@example.com", null, null)).Should().BeNull();
    }

    [Fact]
    public void SubscribeType_OfAnUnknownList_IsRefused()
    {
        var request = new SubscriptionId("a@example.com", null, null, IsSubscriptionRequest: true, HasUnknownType: true);

        SubscriptionRules.GetSubscribeType(request).Should().BeNull();
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Ann", "Ann")]
    [InlineData("Ann \U0001F338", "Ann ?")]
    public void FirstName_FitsTheUtf8mb3Column(string? firstName, string expected)
    {
        SubscriptionRules.NormalizeFirstName(firstName!).Should().Be(expected);
    }

    [Fact]
    public void FirstName_IsCutToTheColumnWithoutSplittingACharacter()
    {
        var name = new string('a', 63) + "\U0001F338" + "tail";

        var result = SubscriptionRules.NormalizeFirstName(name);

        result.Should().Be(new string('a', 63) + "?");
        result.Length.Should().Be(DbTemplateSubscribe.FirstNameMaxLength);
        SubscriptionRules.NormalizeFirstName(new string('b', 70)).Should().Be(new string('b', 64));
    }

    [Fact]
    public void SubscribeRequest_IgnoresATypeInTheBody()
    {
        // The list comes from the id only; a type the body still carries does not fail the binding.
        var request = JsonSerializer.Deserialize<SubscribeRequestDto>("""{"unsubscribeId":"id","type":"AppServer"}""", JsonSerializerOptions.Web);

        request.Should().Be(new SubscribeRequestDto("id"));
    }

    [Fact]
    public void UnsubscribeRequest_WithoutFlags_LeavesThemNull()
    {
        // The controller answers 400 then, rather than reading a missing flag as false (a full unsubscription).
        var request = JsonSerializer.Deserialize<UnsubscribeRequestDto>("""{"UnsubscribeId":"id"}""", JsonSerializerOptions.Web);

        request.Should().Be(new UnsubscribeRequestDto("id", null, null));
    }

    [Fact]
    public void Responses_AreWrittenAsTheSiteExpects()
    {
        JsonSerializer.Serialize(new UnsubscribeResponseDto("user@example.com"), JsonSerializerOptions.Web)
            .Should().Be("""{"email":"user@example.com"}""");

        JsonSerializer.Serialize(new SubscribeResponseDto("user@example.com", SubscribeType.Common, SubscribeResult.Added), JsonSerializerOptions.Web)
            .Should().Be("""{"email":"user@example.com","type":"Common","result":"added"}""");

        JsonSerializer.Serialize(new SubscribeResponseDto("user@example.com", SubscribeType.AppServer, SubscribeResult.Updated), JsonSerializerOptions.Web)
            .Should().Be("""{"email":"user@example.com","type":"AppServer","result":"updated"}""");
    }
}
