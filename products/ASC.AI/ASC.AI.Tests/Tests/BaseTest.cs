// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Tests.Tests;

[Collection("Test Collection")]
public class BaseTest(AspireAppFixture fixture) : IAsyncLifetime
{
    protected const string ProfilesPath = "/api/2.0/ai/integration/profiles";
    protected const string ProfilesBatchPath = "/api/2.0/ai/integration/profiles/batch";

    protected readonly AspireAppFixture Fixture = fixture;
    protected readonly HttpClient AiClient = fixture.AiHttpClient;
    protected readonly AiApiClient Ai = fixture.AiApi;

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public async ValueTask InitializeAsync()
    {
        await Initializer.InitializeAsync(Fixture);
        await AiClient.Authenticate(Initializer.Owner);
    }

    public async ValueTask DisposeAsync()
    {
        await _resetDatabase();
    }

    protected static CreateProfileRequestDto BuildCreateDto(string? name = null) =>
        new()
        {
            Name = name ?? $"profile-{Guid.NewGuid():N}",
            ProviderType = "openai",
            BaseUrl = "https://api.openai.com/v1",
            Key = "sk-test-key-" + Guid.NewGuid().ToString("N"),
            ModelId = "gpt-4o-mini",
            Reasoning = false,
            Capabilities = Capabilities.Chat
        };

    protected static UpdateProfileBody BuildUpdateBody(string? name = null) =>
        new()
        {
            Name = name ?? $"updated-{Guid.NewGuid():N}",
            ProviderType = "anthropic",
            BaseUrl = "https://api.anthropic.com/v1",
            Key = "sk-ant-" + Guid.NewGuid().ToString("N"),
            ModelId = "claude-sonnet-4-6",
            Reasoning = true,
            Capabilities = Capabilities.Chat | Capabilities.Vision
        };

    protected async Task<ProfileDto> CreateProfileAsync(CreateProfileRequestDto? dto = null)
    {
        dto ??= BuildCreateDto();
        using var response = await Ai.PostAsync(ProfilesPath, dto, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<ProfileDto>(response, TestContext.Current.CancellationToken);
    }
}
