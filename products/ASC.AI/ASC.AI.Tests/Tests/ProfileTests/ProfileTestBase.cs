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

namespace ASC.AI.Tests.Tests.ProfileTests;

/// <summary>
/// Shared helpers of the profile suites: outcomes of failing calls, raw access for what the typed client cannot
/// express, role switching and the provider stubs the model-listing routes are pointed at.
/// </summary>
public abstract class ProfileTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string ProfilesRoute = "/api/2.0/ai/profiles";
    protected const string UnknownProfileId = "019fcc1d-2c4d-7557-b8d2-6b4f1be1b212";
    protected const string ZeroGuid = "00000000-0000-0000-0000-000000000000";
    protected const string LeakProbeKey = "sk-autotest-leakprobe-7f3a9c1e5b2d4a68";
    protected const string InvalidKeyMessage = "Invalid API key for the AI provider";
    protected const string ProfileNotFoundMessage = "Profile not found";
    protected const string UnreachableMessage = "The AI provider is unreachable";

    // The bits of `capabilities` the chat service publishes: text, image generation, vision, tools.
    protected const int CapabilityText = 1;
    protected const int CapabilityImageGeneration = 2;
    protected const int CapabilityVision = 128;
    protected const int CapabilityTools = 256;
    protected const int KnownCapabilityBits = CapabilityText | CapabilityImageGeneration | CapabilityVision | CapabilityTools;

    /// <summary>Ids the read routes bind as a GUID and refuse.</summary>
    public static TheoryData<string> MalformedProfileIds => new()
    {
        "not-a-guid",
        "019fcc1d-2c4d-7557",
        "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz",
        new string('a', 2000),
        "' OR 1=1 --"
    };

    protected readonly record struct Outcome(int Status, string Body);

    private readonly List<System.Net.Sockets.Socket> _closedPorts = [];

    /// <summary>
    /// <see cref="BaseTest.OutcomeOfAsync"/> as an <see cref="Outcome"/>, so a typed call compares with a raw one
    /// (<see cref="RawGetAsync"/>); a success is 200 with no body.
    /// </summary>
    protected static async Task<Outcome> OutcomeOfAsync<T>(Func<Task<T>> call)
    {
        var (status, error) = await OutcomeOfAsync((Func<Task>)call);

        return new Outcome(status, error);
    }

    protected async Task<Outcome> RawGetAsync(string path)
    {
        using var response = await new RawApiClient(_aiChatClient).GetAsync(path, TestContext.Current.CancellationToken);

        return new Outcome((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    protected async Task<Outcome> RawPostAsync(string path, string json)
    {
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(path, json, TestContext.Current.CancellationToken);

        return new Outcome((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    protected async Task<Outcome> RawPutAsync(string path, string json)
    {
        using var response = await new RawApiClient(_aiChatClient).PutRawAsync(path, json, TestContext.Current.CancellationToken);

        return new Outcome((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    protected static AiProfilesListProviderModelsRequest ModelsRequest(string providerType, string baseUrl, string? apiKey = null) =>
        new(new AiProviderType(providerType), baseUrl, apiKey!);

    protected Task<List<AiModel>> ListProviderModelsAsync(string providerType, string baseUrl, string? apiKey = null) =>
        _aiProfiles.AiProfilesListProviderModelsAsync(ModelsRequest(providerType, baseUrl, apiKey), TestContext.Current.CancellationToken);

    protected Task<Outcome> ProviderModelsOutcomeAsync(string providerType, string baseUrl, string? apiKey = null) =>
        OutcomeOfAsync(() => ListProviderModelsAsync(providerType, baseUrl, apiKey));

    /// <summary>Creates a profile on the run-wide stub after letting the test adjust the input.</summary>
    protected async Task<AiProfile> CreateConfiguredProfileAsync(Action<AiCreateProfileInput> configure)
    {
        var input = BuildProfileInput();
        configure(input);

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);

        // Described only on a failure: the run-wide provider's log holds every request of the run.
        if (!result.Success)
        {
            result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{ModelProvider.Describe()}");
        }

        return result.Profile;
    }

    /// <summary>The catalogue as the owner reads it, serialised in a stable order so two reads compare as text.</summary>
    protected async Task<string> CatalogueSnapshotAsync()
    {
        var profiles = await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken);

        return string.Join("\n", profiles.OrderBy(p => p.Id, StringComparer.Ordinal).Select(p => p.ToJson()));
    }

    /// <summary>Signs the chat client in as a new member of the given type (a guest is invited separately).</summary>
    protected async Task<User> ActAsAsync(EmployeeType type)
    {
        var user = type == EmployeeType.Guest
            ? await InviteGuest(TestContext.Current.CancellationToken)
            : await InviteContact(type, TestContext.Current.CancellationToken);

        await _aiChatClient.Authenticate(user);

        return user;
    }

    protected async Task ActAsOwnerAsync() => await _aiChatClient.Authenticate(Owner);

    protected static HashSet<string> PropertyNamesOf(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        IEnumerable<JsonElement> objects = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : [root];

        return [.. objects.SelectMany(o => o.EnumerateObject().Select(p => p.Name))];
    }

    /// <summary>Makes every request to the provider fail with the given status (an OpenAI-shaped error body).</summary>
    protected static void RejectEverything(StubModelProvider provider, int status)
    {
        var answer = StubModelProvider.Error(status);

        provider.Server
            .Given(Request.Create().WithPath("/*"))
            .AtPriority(-10)
            .RespondWith(Response.Create().WithStatusCode(answer.StatusCode).WithHeader("Content-Type", "application/json").WithBody(answer.Body));
    }

    /// <summary>A provider that lists its models only to the key <see cref="StubModelProvider.Key"/> and answers 401 to anything else.</summary>
    protected static StubModelProvider CreateKeyCheckingProvider()
    {
        var provider = new StubModelProvider();

        RejectEverything(provider, 401);
        provider.Server
            .Given(Request.Create().WithPath("/v1/models").UsingGet().WithHeader("Authorization", $"Bearer {StubModelProvider.Key}"))
            .AtPriority(-20)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
            {
                @object = "list",
                data = new[] { new { id = StubModelProvider.ModelId, @object = "model", created = 0, owned_by = "test" } }
            }));

        return provider;
    }

    /// <summary>A provider whose model list carries both an OpenAI-style and an Anthropic-style entry, so any transport can read it.</summary>
    protected static StubModelProvider CreateDualShapeProvider()
    {
        var provider = new StubModelProvider();

        provider.Server
            // The Anthropic transport builds its own path from the base URL, so the list is served on the path variants too.
            .Given(Request.Create().WithPath("/v1/models", "/v1/v1/models", "/models").UsingGet())
            .AtPriority(-10)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
            {
                @object = "list",
                data = new[]
                {
                    new
                    {
                        id = StubModelProvider.ModelId,
                        @object = "model",
                        created = 0,
                        owned_by = "test",
                        type = "model",
                        display_name = "Stub Model",
                        created_at = "2025-01-01T00:00:00Z"
                    }
                },
                has_more = false,
                first_id = StubModelProvider.ModelId,
                last_id = StubModelProvider.ModelId
            }));

        return provider;
    }

    /// <summary>A loopback URL nothing listens on: the connection is refused at once.</summary>
    /// <remarks>
    /// The port stays bound, but never listened on, until the test ends. A port that was only found free and released
    /// could be taken by the WireMock a parallel test starts, and the "unreachable" provider would then answer.
    /// </remarks>
    protected string CreateClosedPortUrl()
    {
        var socket = new System.Net.Sockets.Socket(
            System.Net.Sockets.AddressFamily.InterNetwork,
            System.Net.Sockets.SocketType.Stream,
            System.Net.Sockets.ProtocolType.Tcp);
        _closedPorts.Add(socket);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        return $"http://127.0.0.1:{((IPEndPoint)socket.LocalEndPoint!).Port}/v1";
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var socket in _closedPorts)
            {
                socket.Dispose();
            }
        }
        finally
        {
            // The portal clients go even when a socket fails to close.
            await base.DisposeAsync();
        }
    }
}
