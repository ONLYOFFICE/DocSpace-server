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

namespace ASC.AI.Tests.Tests.SkillTests;

/// <summary>
/// POST /api/2.0/ai/skills/generate — the checks of a request and the failures of the model
/// (<c>skillsController.generate</c>, <c>generateSkill</c>). The access gate runs before the body is read, the body
/// limits before the room is looked up, and the room before any model; a failure of the model answers its own status
/// and code, and passes the provider's text on only for a rate limit, a lack of credit or the spending limit. The
/// generation profile is the portal's Chat assignment, so a test that scripts the model assigns a profile on a
/// provider of its own.
/// </summary>
[Trait("Category", "Skills")]
[Trait("Feature", "AI/Skills")]
public class SkillGenerateRulesTests(AspireAppFixture fixture) : SkillTestBase(fixture)
{
    private DocSpace.API.SDK.Api.AI.SkillsApi SkillsApi => new(
        _aiChatClient,
        new Configuration { BasePath = _aiChatClient.BaseAddress!.ToString().TrimEnd('/') });

    private static AiSkillsGenerateRequest GenerateRequest(string destId, string title = "Release notes", string description = "Write release notes from a list of merged changes.") =>
        new(new AiSkillsGenerateRequestDestId(destId), title, description);

    private async Task UseProviderForGenerationAsync(StubModelProvider provider)
    {
        await _aiChatClient.Authenticate(Owner);
        var profile = await CreateProfileOnAsync(provider);

        // The public assign refuses Chat for a profile without the chat capabilities the stub declares; the slot is
        // what generation reads, so it is written through the AI service's own storage route.
        await _aiClient.Authenticate(Owner);
        await CreateAssignmentInternalAsync("Chat", Guid.Parse(profile.Id));
    }

    [Fact]
    public async Task GenerateSkill_GuestWithInvalidBody_ThrowsForbiddenBeforeTheBodyIsRead()
    {
        // Arrange
        var guest = await InviteGuest(TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SkillsApi.AiSkillsGenerateAsync(GenerateRequest("1", title: ""), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData("abc", "Release notes", "Write release notes.")]
    [InlineData("0", "Release notes", "Write release notes.")]
    [InlineData("1", "", "Write release notes.")]
    [InlineData("1", "65", "Write release notes.")]
    [InlineData("1", "Release notes", "")]
    [InlineData("1", "Release notes", "2001")]
    public async Task GenerateSkill_FieldOutOfLimits_ThrowsBadRequest(string destId, string title, string description)
    {
        // Arrange: "65" and "2001" stand for a title and a description one character over the limit
        await _aiChatClient.Authenticate(Owner);
        title = title == "65" ? new string('a', 65) : title;
        description = description == "2001" ? new string('d', 2001) : description;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SkillsApi.AiSkillsGenerateAsync(GenerateRequest(destId, title, description), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GenerateSkill_FolderThatDoesNotExist_ThrowsNotFound()
    {
        // Arrange
        await _aiChatClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SkillsApi.AiSkillsGenerateAsync(GenerateRequest("999999999"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GenerateSkill_MemberWhoCannotCreateInTheRoom_ThrowsForbidden()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareAsync(roomId, user.Id, FileShare.Read);
        await _aiChatClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SkillsApi.AiSkillsGenerateAsync(GenerateRequest(roomId.ToString(System.Globalization.CultureInfo.InvariantCulture)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Answers the generation request itself. It is a non-streamed completion, which the provider's queue does not
    /// serve (that one is for streamed chats), so the answer is a mapping of its own matched by the skill-writer
    /// system prompt; the tool probe of the profile create keeps its own mapping.
    /// </summary>
    private static void AnswerGeneration(StubModelProvider provider, int status, object body)
    {
        provider.Server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost()
                .WithBody((string? requestBody) => requestBody is not null && requestBody.Contains("You write skills", StringComparison.Ordinal)))
            .AtPriority(0)
            .RespondWith(Response.Create().WithStatusCode(status).WithHeader("Content-Type", "application/json").WithBodyAsJson(body));
    }

    private static object ProviderError(string message) =>
        new { error = new { message, type = "invalid_request_error", code = (string?)null } };

    /// <summary>The failure of a generation in the owner's room; a success fails the test with what the model saw.</summary>
    private async Task<ApiException> GenerateExpectingFailureAsync(StubModelProvider provider, int roomId)
    {
        try
        {
            var result = await SkillsApi.AiSkillsGenerateAsync(
                GenerateRequest(roomId.ToString(System.Globalization.CultureInfo.InvariantCulture)), TestContext.Current.CancellationToken);
            Assert.Fail($"generation succeeded with \"{result.Content}\"\n{provider.Describe()}");

            return null!;
        }
        catch (ApiException exception)
        {
            return exception;
        }
    }

    [Fact]
    public async Task GenerateSkill_ModelAnswersNothing_ThrowsBadGatewayWithEmptyAnswerCode()
    {
        // Arrange
        using var provider = new StubModelProvider();
        await UseProviderForGenerationAsync(provider);
        var roomId = await CreateInstructionsRoomAsync();
        AnswerGeneration(provider, 200, new { id = "chatcmpl-stub", @object = "chat.completion", created = 0, model = StubModelProvider.ModelId, choices = new[] { new { index = 0, message = new { role = "assistant", content = "  " }, finish_reason = "stop" } } });

        // Act
        var exception = await GenerateExpectingFailureAsync(provider, roomId);

        // Assert
        exception.ErrorCode.Should().Be(502, provider.Describe());
        exception.ErrorContent!.ToString().Should().Contain("empty_answer");
    }

    [Fact]
    public async Task GenerateSkill_ProviderRefusesTheKey_DoesNotRelayTheProviderText()
    {
        // Arrange
        using var provider = new StubModelProvider();
        await UseProviderForGenerationAsync(provider);
        var roomId = await CreateInstructionsRoomAsync();
        AnswerGeneration(provider, 401, ProviderError("Incorrect API key provided: sk-sec***cret"));

        // Act
        var exception = await GenerateExpectingFailureAsync(provider, roomId);

        // Assert
        exception.ErrorCode.Should().BeGreaterThanOrEqualTo(400, provider.Describe());
        var body = exception.ErrorContent!.ToString();
        body.Should().NotContain("providerMessage");
        body.Should().NotContain("sk-sec");
    }

    [Fact]
    public async Task GenerateSkill_ProviderRateLimit_RelaysTheProviderText()
    {
        // Arrange: every attempt, retries included, gets the 429
        using var provider = new StubModelProvider();
        await UseProviderForGenerationAsync(provider);
        var roomId = await CreateInstructionsRoomAsync();
        const string limit = "Rate limit reached for requests per minute";
        AnswerGeneration(provider, 429, ProviderError(limit));

        // Act
        var exception = await GenerateExpectingFailureAsync(provider, roomId);

        // Assert
        var body = exception.ErrorContent!.ToString();
        body.Should().Contain("rate_limit", provider.Describe());
        body.Should().Contain("providerMessage");
        body.Should().Contain(limit);
    }
}
