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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// The field rules of <c>POST api/2.0/settings/webhook</c>: name and secret up to 50 characters, a target up to 255,
/// a secret that is required on creation and has to pass the portal's password rules, no second subscription with
/// the same name and address, and a secret that no answer ever carries.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookFieldValidationTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Theory]
    [InlineData("name")]
    [InlineData("secretKey")]
    [InlineData("targetId")]
    public async Task CreateWebhook_FieldOverItsLimit_ThrowsBadRequest(string field)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateAcceptedWebhookDto(triggers: WebhookTrigger.FileCreated);

        switch (field)
        {
            case "name":
                dto.Name = new string('n', 51);
                break;
            case "secretKey":
                dto.SecretKey = "Aa1" + new string('s', 48);
                break;
            default:
                dto.TargetId = new string('1', 256);
                break;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await ListNamesAsync()).Should().NotContain(dto.Name);
    }

    [Fact]
    public async Task CreateWebhook_FieldsAtTheirLimit_AreAccepted()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateAcceptedWebhookDto(triggers: WebhookTrigger.FileCreated);
        dto.Name = Initializer.Faker.Random.AlphaNumeric(50);
        dto.SecretKey = Initializer.Faker.Random.AlphaNumeric(30); // the default password rules stop at 30, below the field limit of 50
        dto.TargetId = new string('1', 255);

        // Act
        var result = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Name.Should().Be(dto.Name);
        result.Response.TargetId.Should().Be(dto.TargetId);
    }

    [Fact]
    public async Task CreateWebhook_WithoutSecret_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateAcceptedWebhookDto();
        dto.SecretKey = null!;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await ListNamesAsync()).Should().NotContain(dto.Name);
    }

    [Fact]
    public async Task CreateWebhook_SecretShorterThanThePasswordRules_ThrowsBadRequest()
    {
        // Arrange: the default password rules of a portal ask for at least 8 characters
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateAcceptedWebhookDto();
        dto.SecretKey = "abc";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await ListNamesAsync()).Should().NotContain(dto.Name);
    }

    [Fact]
    public async Task CreateWebhook_SameNameAndUriTwice_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateAcceptedWebhookDto();
        await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await ListNamesAsync()).Count(name => name == dto.Name).Should().Be(1);
    }

    /// <summary>
    /// The secret is in no answer: neither the creation's echo nor the list. Read raw, because the generated models
    /// have no secret property at all, so a secret on the wire would be dropped silently by the parse.
    /// </summary>
    [Fact]
    public async Task CreateAndListWebhook_NeverReturnTheSecret()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateAcceptedWebhookDto();

        // Act
        using var created = await _webApi.PostAsync("api/2.0/settings/webhook", dto, TestContext.Current.CancellationToken);
        using var listed = await _webApi.GetAsync("api/2.0/settings/webhook", TestContext.Current.CancellationToken);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdBody = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var listedBody = await listed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listedBody.Should().Contain(dto.Name, "the list should hold the new subscription");
        createdBody.Should().NotContain(dto.SecretKey);
        listedBody.Should().NotContain(dto.SecretKey);
        listedBody.Should().NotContainEquivalentOf("\"secretKey\"");
    }

    private async Task<List<string>> ListNamesAsync()
    {
        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);

        return [.. list.Response.Select(w => w.Configs.Name)];
    }
}
