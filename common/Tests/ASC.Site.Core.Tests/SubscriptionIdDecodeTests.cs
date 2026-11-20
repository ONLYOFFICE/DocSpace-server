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
/// <see cref="ValidationKeyProvider.DecodeSubscriptionIdAsync"/>: the three kinds of id a subscription link carries,
/// and the ids it must refuse.
/// </summary>
public class SubscriptionIdDecodeTests
{
    private readonly InstanceCrypto _crypto = CreateCrypto("site-core-tests");
    private readonly ValidationKeyProvider _provider;

    public SubscriptionIdDecodeTests()
    {
        // The decoding uses only the crypto; the cache and the email key provider serve the /key endpoints.
        _provider = new ValidationKeyProvider(null!, _crypto, null!);
    }

    [Fact]
    public async Task EmailKeyOfKeyGenerate_GivesTheLowercasedAddress()
    {
        // /key/generate encrypts the address as the caller sent it.
        var id = await _provider.EncryptAndEncode("John.Smith@Example.com");

        var result = await _provider.DecodeSubscriptionIdAsync(id);

        result.Should().Be(new SubscriptionId("john.smith@example.com", null, null));
    }

    [Fact]
    public async Task UnsubscribeIdWithJson_GivesTheAddressNameAndList()
    {
        var id = await _provider.EncryptAndEncode("""{"email":" Jane@Example.com ","firstname":" Jane ","type":1}""");

        var result = await _provider.DecodeSubscriptionIdAsync(id);

        result.Should().Be(new SubscriptionId("jane@example.com", "Jane", SubscribeType.AppServer));
    }

    [Fact]
    public async Task FooterIdOfDocSpaceLetters_IsReadAsBase64Url()
    {
        // As TextileStyler.GetSiteUnsubscribeLinkAsync builds it: Base64Url without the padding digit of UrlToken.
        var id = WebEncoders.Base64UrlEncode(await _crypto.EncryptAsync(Encoding.UTF8.GetBytes("user@example.com")));

        var result = await _provider.DecodeSubscriptionIdAsync(id);

        result.Should().Be(new SubscriptionId("user@example.com", null, null));
    }

    [Fact]
    public async Task IdOfAnotherInstallation_IsRefused()
    {
        var foreign = new ValidationKeyProvider(null!, CreateCrypto("another-installation"), null!);
        var id = await foreign.EncryptAndEncode("user@example.com");

        var result = await _provider.DecodeSubscriptionIdAsync(id);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-id")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA2")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA9")]
    [InlineData("@@@@")]
    public async Task IdThatIsNotOurs_IsRefused(string? id)
    {
        var result = await _provider.DecodeSubscriptionIdAsync(id!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EncryptedTextThatIsNoAddress_IsRefused()
    {
        var id = await _provider.EncryptAndEncode("hello world");

        var result = await _provider.DecodeSubscriptionIdAsync(id);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("  USER@Example.COM  ", "user@example.com")]
    public void PlainAddress_IsTrimmedAndLowercased(string text, string expected)
    {
        ValidationKeyProvider.ParseSubscriptionId(text).Should().Be(new SubscriptionId(expected, null, null));
    }

    [Fact]
    public void AddressLongerThanTheColumn_IsRefused()
    {
        // template_unsubscribe.email is VARCHAR(100): such an address cannot be stored, so no id of ours holds it.
        var tooLong = new string('a', 89) + "@example.com";
        tooLong.Length.Should().Be(101);

        ValidationKeyProvider.ParseSubscriptionId(tooLong).Should().BeNull();
        ValidationKeyProvider.ParseSubscriptionId(tooLong[1..]).Should().Be(new SubscriptionId(tooLong[1..], null, null));
    }

    [Theory]
    [InlineData("""{"email":"a@example.com","firstname":"A","type":0}""", SubscribeType.Common)]
    [InlineData("""{"email":"a@example.com","firstname":"A","type":2}""", SubscribeType.Opensource)]
    [InlineData("""{"email":"a@example.com","firstname":"A","type":"AppServer"}""", SubscribeType.AppServer)]
    [InlineData("""{"email":"a@example.com","firstname":"A","type":"opensource"}""", SubscribeType.Opensource)]
    public void JsonType_IsReadByNumberOrName(string text, SubscribeType expected)
    {
        ValidationKeyProvider.ParseSubscriptionId(text)!.Type.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{"email":"a@example.com","firstname":"A"}""")]
    [InlineData("""{"email":"a@example.com","firstname":"A","type":null}""")]
    public void JsonWithoutType_GivesNoList(string text)
    {
        ValidationKeyProvider.ParseSubscriptionId(text).Should().Be(new SubscriptionId("a@example.com", "A", null));
    }

    [Theory]
    [InlineData("""{"email":"a@example.com","type":7}""")]
    [InlineData("""{"email":"a@example.com","type":"Unknown"}""")]
    [InlineData("""{"email":"a@example.com","type":"7"}""")]
    [InlineData("""{"email":"a@example.com","type":true}""")]
    [InlineData("""{"email":"not an address","type":0}""")]
    [InlineData("""{"firstname":"A","type":0}""")]
    [InlineData("""{"email":""")]
    [InlineData("{}")]
    public void JsonWithoutAValidAddressOrList_IsRefused(string text)
    {
        ValidationKeyProvider.ParseSubscriptionId(text).Should().BeNull();
    }

    private static InstanceCrypto CreateCrypto(string machineKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = machineKey })
            .Build();

        return new InstanceCrypto(new MachinePseudoKeys(configuration));
    }
}
