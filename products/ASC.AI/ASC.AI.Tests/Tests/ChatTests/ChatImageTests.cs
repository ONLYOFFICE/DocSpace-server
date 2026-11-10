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
/// Generating an image from a chat. There is no image route: the feature is the engine's own <c>generate_image</c>
/// tool, offered to the chat model while a profile that can draw is bound to <c>ImageGeneration</c>, and run by the
/// engine against that profile's <c>images/generations</c>.
/// </summary>
/// <remarks>
/// The model's choice to draw is scripted (a <c>generate_image</c> tool call, then the follow-up text); what is
/// asserted is the server's part — the tool offered or not, the image model called, the call resolved and stored.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatImageTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    [Fact]
    [Trait("Bug", "82861")]
    public async Task SendWithStream_ModelCallsGenerateImage_TheCallResolvesWithAReference()
    {
        // Was BUG 82861: the call never resolved and the stream stayed open past two minutes.
        var (chat, chatProfile, image, _) = await ArrangeDrawingAsync();
        var thread = await CreateThreadAsync(profileId: chatProfile.Id);
        chat.Enqueue(StubModelProvider.ToolCall(GenerateImageTool, DrawingArguments), StubModelProvider.Text("Here is your picture."));

        var round = await SendWithStreamAsync(thread.ThreadId, PictureRequest, chatProfile.Id);
        var context = $"{round}\n{chat.Describe()}\n{image.Describe()}";

        round.StatusCode.Should().Be(HttpStatusCode.OK, context);
        round.StreamError.Should().BeNull(context);

        OfferedTools(StreamedRequestsCarrying(chat, PictureRequest).First()).Should().Contain(GenerateImageTool, context);
        image.Server.LogEntries.Should().Contain(e => e.RequestMessage!.Path == ImageGenerationsPath, $"the image model drew it\n{context}");

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        var drawing = AssistantMessages(messages).SelectMany(ToolCallsOf).Where(c => c.Name == GenerateImageTool).ToList();

        drawing.Should().ContainSingle($"the drawing call is stored on the reply: {string.Join("\n", messages)}");
        var result = drawing[0].Result;
        result.Should().NotBeNull("the drawing call resolved");
        result!.Value.ToString().Should().NotContain("\"base64\":\"\"", "no empty picture placeholder");
        ReferenceOf(result.Value).Should().NotBeNullOrEmpty($"a real ref came back: {result}");
        AssertHealthyReply(AssistantMessages(messages)[^1], "the round finished with the model's text");

        // The tool is the engine's own: the tools API does not advertise it.
        var system = await _aiTools.AiToolsListSystemToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        (system.Groups?.Values.SelectMany(tools => tools ?? []).Select(t => t.Name) ?? [])
            .Should().NotContain(GenerateImageTool, "generate_image is server-side, not an advertised DocSpace tool");
    }

    [Fact]
    public async Task SendWithStream_ImageOnlyProfileAsChatModel_FailsTheReplyAndKeepsTheQuestion()
    {
        // Driving the image profile directly. The send is accepted and the refusal arrives as a stored
        // message-incomplete: the HTTP status is 200 throughout, which is the shape a client has to look for.
        var chat = NewProvider();
        await CreateProfileOnAsync(chat);
        var image = NewProvider();
        var imageProfile = await CreateCapableProfileAsync(image, ImageOnlyCapabilities);
        var thread = await CreateThreadAsync(profileId: imageProfile.Id);
        image.Enqueue(StubModelProvider.Error(400, "model is not a chat model"));

        var round = await SendWithStreamAsync(thread.ThreadId, "Draw a red circle.", imageProfile.Id);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.OfType("message-incomplete").Should().NotBeEmpty($"{round}\n{image.Describe()}");

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        var reply = AssistantMessages(messages).Should().ContainSingle(string.Join("\n", messages)).Subject;
        var status = StatusOf(reply);

        status.Type.Should().Be("incomplete", reply.ToString());
        status.Reason.Should().Be("error", reply.ToString());
        MessageText(reply).Should().BeEmpty();
        UserMessages(messages).Should().HaveCount(1, "the failure costs the reply, not the question");
    }

    [Fact]
    [Trait("Bug", "83137")]
    public async Task Unassign_ImageGeneration_TheDrawingToolIsNoLongerOffered()
    {
        // "Available when Model Assignment has an image model; with no such model the generation is off and the tool
        // is not offered to the model." Both states in one test: the bound one is the positive control. Was BUG 83137:
        // unassign answered success and the binding stayed.
        var (chat, chatProfile, _, imageProfile) = await ArrangeDrawingAsync();
        (await GetAssignmentAsync(AiActionType.ImageGeneration)).Should().Be(imageProfile.Id, "an image model is bound");

        var withModel = $"with-model-{Guid.NewGuid():N}";
        chat.Enqueue(StubModelProvider.Text("with a model"));
        await SendWithStreamAsync((await CreateThreadAsync(profileId: chatProfile.Id)).ThreadId, withModel, chatProfile.Id);

        OfferedTools(StreamedRequestsCarrying(chat, withModel).Should().ContainSingle(chat.Describe()).Subject)
            .Should().Contain(GenerateImageTool, "with an image model bound the drawing tool is offered");

        var removed = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.ImageGeneration), TestContext.Current.CancellationToken);

        removed.Success.Should().BeTrue();
        (await GetAllAssignmentsAsync()).Should().NotContainKey(nameof(AiActionType.ImageGeneration));
        (await GetAssignmentAsync(AiActionType.ImageGeneration)).Should().BeNull();

        var withoutModel = $"without-model-{Guid.NewGuid():N}";
        chat.Enqueue(StubModelProvider.Text("without a model"));
        var round = await SendWithStreamAsync((await CreateThreadAsync(profileId: chatProfile.Id)).ThreadId, withoutModel, chatProfile.Id);

        OfferedTools(StreamedRequestsCarrying(chat, withoutModel).Should().ContainSingle(chat.Describe()).Subject)
            .Should().NotContain(GenerateImageTool, "no image model is bound, so nothing may call the drawing tool");
        round.StreamError.Should().BeNull(round.ToString());
        round.AssistantText.Should().Be("without a model", "the user is answered rather than left on a stream that never ends");
    }

    /// <summary><c>data.ref</c> of a drawing result, which is stored either as an object or as its JSON text.</summary>
    private static string? ReferenceOf(JsonElement result)
    {
        if (result.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var parsed = JsonDocument.Parse(result.GetString()!);

                return ReferenceOf(parsed.RootElement.Clone());
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("data", out var data) || !data.TryGetProperty("ref", out var reference))
        {
            return null;
        }

        return reference.ValueKind == JsonValueKind.String ? reference.GetString() : reference.ToString();
    }
}
