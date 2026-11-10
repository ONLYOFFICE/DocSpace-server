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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// The language a failed reply reaches the user in. The profile language is switched with
/// <c>PUT /people/{userId}/culture</c> and the same provider failure is provoked twice — once in the portal default
/// (en-US), once in the new language — in two threads, so the two payloads differ in nothing but the caller's culture.
/// </summary>
/// <remarks>
/// An ordinary portal error (<c>GET /files/file/{missing}</c>) is read on both sides of the switch as the control: it
/// is a message the portal does translate, so two identical AI messages cannot be explained by a culture switch that
/// did not take effect.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatFailureLanguageTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private const string FailureMessage = "model is not a chat model";

    [Fact]
    public async Task SendWithStream_ProfileLanguageChanged_TheFailureCodeStaysTheSame()
    {
        // The half a client can rely on: whatever language the user picked, the failure is classified identically,
        // so a chat can map the code to a string of its own.
        var (provider, profile) = await ArrangeFailingModelAsync();

        var inEnglish = await FailInAsync(provider, profile, "failure en");
        var controlInEnglish = await PortalErrorMessageAsync();

        await SetOwnerCultureAsync("ru");
        (await PortalErrorMessageAsync()).Should().NotBe(controlInEnglish, "the portal's own errors did switch language");

        var inRussian = await FailInAsync(provider, profile, "failure ru");

        inRussian.Code.Should().Be(inEnglish.Code);
        inRussian.Reason.Should().Be(inEnglish.Reason);
        inRussian.Type.Should().Be(inEnglish.Type);
    }

    /// <summary>
    /// The stored failure of a provider error is the provider's English text unchanged
    /// (<c>400 model is not a chat model</c>) whatever culture the user's profile is set to.
    /// </summary>
    [Theory]
    [InlineData("ru")]
    [InlineData("de")]
    [InlineData("fr")]
    [Trait("Bug", "83046")]
    public async Task SendWithStream_ProfileLanguageChanged_TheFailureMessageIsTranslated(string language)
    {
        // What the user is supposed to see: the failure in the language the portal translates everything else into,
        // not the provider's English string unchanged.
        var (provider, profile) = await ArrangeFailingModelAsync();

        var inEnglish = await FailInAsync(provider, profile, "failure en");
        var controlInEnglish = await PortalErrorMessageAsync();

        await SetOwnerCultureAsync(language);
        (await PortalErrorMessageAsync()).Should().NotBe(controlInEnglish, $"a portal error message translated into {language}");

        var localized = await FailInAsync(provider, profile, $"failure {language}");

        localized.Message.Should().NotBe(inEnglish.Message, $"the failure a {language} user is shown is not English");
    }

    private async Task<(StubModelProvider Provider, AiProfile Profile)> ArrangeFailingModelAsync()
    {
        var provider = NewProvider();

        return (provider, await CreateProfileOnAsync(provider));
    }

    /// <summary>Provokes the provider failure in a thread of its own and returns the stored failure.</summary>
    private async Task<(string? Type, string? Reason, string? Code, string? Message)> FailInAsync(StubModelProvider provider, AiProfile profile, string title)
    {
        var thread = await CreateThreadAsync(title, profile.Id);
        provider.Enqueue(StubModelProvider.Error(400, FailureMessage));

        await SendWithStreamAsync(thread.ThreadId, "Reply with OK.", profile.Id);

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        var status = StatusOf(AssistantMessages(messages).Should().ContainSingle(string.Join("\n", messages)).Subject);
        status.Reason.Should().Be("error", $"the send in {title} really failed");

        return status;
    }

    /// <summary>The message of an ordinary portal error, in whatever language the owner's profile is set to.</summary>
    private async Task<string> PortalErrorMessageAsync()
    {
        await FilesHttpClient.Authenticate(Owner);

        using var response = await new RawApiClient(FilesHttpClient).GetAsync("/api/2.0/files/file/99999999", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"GET /files/file/{{missing}} as the control: {body}");

        using var document = JsonDocument.Parse(body);
        var message = document.RootElement.TryGetProperty("error", out var error) ? Str(error, "message") : null;
        message.Should().NotBeNullOrEmpty($"the control error carries a message: {body}");

        return message!;
    }

    /// <summary>Switches the owner's profile language and proves the portal kept it.</summary>
    private async Task SetOwnerCultureAsync(string cultureName)
    {
        await PeopleHttpClient.Authenticate(Owner);

        var updated = await ProfilesApi.UpdateMemberCultureAsync(
            Owner.Id.ToString(),
            new UpdateMemberCultureRequest(cultureName),
            TestContext.Current.CancellationToken);

        updated.Response.CultureName.Should().Be(cultureName, "the culture read back from the profile");
    }
}
