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
    [InlineData(SubscribeType.Opensource, SubscribeType.AppServer, SubscribeType.Opensource)]
    [InlineData(null, SubscribeType.AppServer, SubscribeType.AppServer)]
    [InlineData(SubscribeType.Common, SubscribeType.AppServer, SubscribeType.Common)]
    [InlineData(null, null, SubscribeType.Common)]
    public void SubscribeType_TakesTheRequestThenTheIdThenCommon(SubscribeType? requested, SubscribeType? fromId, SubscribeType expected)
    {
        SubscriptionRules.ResolveSubscribeType(requested, fromId).Should().Be(expected);
    }

    [Fact]
    public void SubscribeType_OutsideTheEnum_IsRefused()
    {
        SubscriptionRules.ResolveSubscribeType((SubscribeType)7, SubscribeType.Common).Should().BeNull();
    }

    [Theory]
    [InlineData("""{"UnsubscribeId":"id","Type":"AppServer"}""", SubscribeType.AppServer)]
    [InlineData("""{"unsubscribeId":"id","type":"opensource"}""", SubscribeType.Opensource)]
    [InlineData("""{"unsubscribeId":"id","type":0}""", SubscribeType.Common)]
    [InlineData("""{"unsubscribeId":"id"}""", null)]
    public void SubscribeRequest_ReadsTheListByNameOrNumber(string json, SubscribeType? expected)
    {
        var request = JsonSerializer.Deserialize<SubscribeRequestDto>(json, JsonSerializerOptions.Web);

        request.Should().Be(new SubscribeRequestDto("id", expected));
    }

    [Fact]
    public void SubscribeRequest_WithAnUnknownName_DoesNotBind()
    {
        // ASP.NET Core answers 400 for a body that does not bind.
        var read = () => JsonSerializer.Deserialize<SubscribeRequestDto>("""{"unsubscribeId":"id","type":"Weekly"}""", JsonSerializerOptions.Web);

        read.Should().Throw<JsonException>();
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
