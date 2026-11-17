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

namespace ASC.AI.Tests.Tests.ProfileStorageTests;

/// <summary>
/// The length limits of a profile's free-form fields: a base URL of up to 2048 characters and a key of up to 4096 are
/// stored, one character more is a 400.
/// </summary>
/// <remarks>
/// Internal contract: the limits are the AI service's request validation. The public create first checks the key
/// against the provider at that URL, so an over-long URL or key is decided by the chat service's live check before it
/// reaches the limit.
/// </remarks>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Profiles")]
public class ProfileFieldLimitTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int MaxBaseUrlLength = 2048;
    private const int MaxKeyLength = 4096;
    private const string BaseUrlPrefix = "https://api.openai.com/v1/";
    private const string KeyPrefix = "sk-autotest-fake-";

    [Theory]
    [InlineData(MaxBaseUrlLength, HttpStatusCode.OK)]
    [InlineData(MaxBaseUrlLength + 1, HttpStatusCode.BadRequest)]
    public async Task Create_BaseUrlOfLength_IsStoredUpToTheLimit(int length, HttpStatusCode expected)
    {
        var template = BuildCreateDto();
        var dto = new CreateProfileRequestDto
        {
            Name = template.Name,
            ProviderType = template.ProviderType,
            BaseUrl = BaseUrlPrefix + new string('a', length - BaseUrlPrefix.Length),
            Key = template.Key,
            ModelId = template.ModelId,
            Capabilities = template.Capabilities
        };

        using var response = await _ai.PostAsync(ProfilesPath, dto, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(MaxKeyLength, HttpStatusCode.OK)]
    [InlineData(MaxKeyLength + 1, HttpStatusCode.BadRequest)]
    public async Task Create_KeyOfLength_IsStoredUpToTheLimit(int length, HttpStatusCode expected)
    {
        var template = BuildCreateDto();
        var dto = new CreateProfileRequestDto
        {
            Name = template.Name,
            ProviderType = template.ProviderType,
            BaseUrl = template.BaseUrl,
            Key = KeyPrefix + new string('k', length - KeyPrefix.Length),
            ModelId = template.ModelId,
            Capabilities = template.Capabilities
        };

        using var response = await _ai.PostAsync(ProfilesPath, dto, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
