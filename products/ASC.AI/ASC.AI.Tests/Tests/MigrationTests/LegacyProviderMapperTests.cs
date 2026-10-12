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

namespace ASC.AI.Tests.Tests.MigrationTests;

public class LegacyProviderMapperTests
{
    [Theory]
    [InlineData(ProviderType.OpenAi, "openai")]
    [InlineData(ProviderType.TogetherAi, "together")]
    [InlineData(ProviderType.OpenAiCompatible, "openaicompatible")]
    [InlineData(ProviderType.Anthropic, "anthropic")]
    [InlineData(ProviderType.OpenRouter, "openrouter")]
    [InlineData(ProviderType.DeepSeek, "deepseek")]
    [InlineData(ProviderType.XAi, "xai")]
    [InlineData(ProviderType.GoogleAi, "genai")]
    public void ToProviderType_MapsBuiltInProviders(ProviderType type, string expected)
    {
        LegacyProviderMapper.ToProviderType(type).Should().Be(expected);
    }

    [Theory]
    [InlineData(ProviderType.PortalAi)]
    [InlineData((ProviderType)100)]
    public void ToProviderType_ReturnsNullForPortalAndUnknown(ProviderType type)
    {
        LegacyProviderMapper.ToProviderType(type).Should().BeNull();
    }

    [Fact]
    public void ToProfileData_WithoutModelSettings_LeavesCapabilitiesUnknown()
    {
        var provider = CreateProvider();

        var profile = LegacyProviderMapper.ToProfileData(provider, "openai", "gpt-4o", null, "plain-key");

        profile.Name.Should().Be("My OpenAI · gpt-4o");
        profile.ProviderType.Should().Be("openai");
        profile.BaseUrl.Should().Be(provider.Url);
        profile.Key.Should().Be("plain-key");
        profile.ModelId.Should().Be("gpt-4o");
        profile.Capabilities.Should().BeNull();
        profile.Reasoning.Should().BeNull();
        profile.CanUseTool.Should().BeNull();
        profile.UseResponsesApi.Should().BeNull();
    }

    [Fact]
    public void ToProfileData_WithModelSettings_MapsCapabilitiesAliasAndReasoning()
    {
        var provider = CreateProvider();
        var settings = new DbAiModelSettings
        {
            TenantId = 1,
            ProviderId = provider.Id,
            ModelId = "gpt-4o",
            Alias = "GPT-4o",
            IsEnabled = true,
            Capabilities = new AiModelCapabilities { Vision = true, ToolCalling = true, Thinking = true }
        };

        var profile = LegacyProviderMapper.ToProfileData(provider, "openai", "gpt-4o", settings, "plain-key");

        profile.Name.Should().Be("My OpenAI · GPT-4o");
        profile.Capabilities.Should().Be(Capabilities.Chat | Capabilities.Vision | Capabilities.Tools);
        profile.CanUseTool.Should().BeTrue();
        profile.Reasoning.Should().NotBeNull();
        profile.Reasoning!.Thinks.Should().BeTrue();
        profile.Reasoning.CanDisable.Should().BeTrue();
    }

    [Fact]
    public void ToProfileData_WithoutThinking_HasNoReasoning()
    {
        var provider = CreateProvider();
        var settings = new DbAiModelSettings
        {
            TenantId = 1,
            ProviderId = provider.Id,
            ModelId = "gpt-4o-mini",
            Capabilities = new AiModelCapabilities { Vision = false, ToolCalling = false, Thinking = false }
        };

        var profile = LegacyProviderMapper.ToProfileData(provider, "openai", "gpt-4o-mini", settings, "plain-key");

        profile.Capabilities.Should().Be(Capabilities.Chat);
        profile.CanUseTool.Should().BeFalse();
        profile.Reasoning.Should().BeNull();
    }

    [Fact]
    public void BuildName_TruncatesTo255Characters()
    {
        var name = LegacyProviderMapper.BuildName(new string('a', 250), null, new string('b', 50));

        name.Should().HaveLength(255);
        name.Should().StartWith(new string('a', 250));
    }

    [Fact]
    public void BuildName_FallsBackToModelIdWhenAliasIsBlank()
    {
        LegacyProviderMapper.BuildName("Title", "   ", "model").Should().Be("Title · model");
    }

    private static DbAiProvider CreateProvider()
    {
        return new DbAiProvider
        {
            Id = 7,
            TenantId = 1,
            Type = ProviderType.OpenAi,
            Title = "My OpenAI",
            Url = "https://api.openai.com/v1",
            Key = "encrypted"
        };
    }
}
