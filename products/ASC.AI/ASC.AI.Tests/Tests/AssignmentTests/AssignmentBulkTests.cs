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

namespace ASC.AI.Tests.Tests.AssignmentTests;

/// <summary>Bulk assignment: a flat action-to-profile map, applied atomically (assignments.spec.ts, "bulk assignment").</summary>
[Trait("Category", "Write")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentBulkTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task BulkAssign_FlatMap_IsApplied()
    {
        var vision = await CreateProfileWithAsync(TextVisionCapabilities);
        var text = await CreateProfileWithAsync(TextCapabilities);

        var result = await BulkAssignAsync(new Dictionary<string, string>
        {
            [nameof(AiActionType.Summarization)] = vision.Id,
            [nameof(AiActionType.Translation)] = text.Id,
            [nameof(AiActionType.Vision)] = vision.Id
        });

        result.Success.Should().BeTrue(JsonSerializer.Serialize(result.Errors));
        var all = await GetAllAssignmentsAsync();
        all[nameof(AiActionType.Summarization)].Should().Be(vision.Id);
        all[nameof(AiActionType.Translation)].Should().Be(text.Id);
        all[nameof(AiActionType.Vision)].Should().Be(vision.Id);
    }

    [Fact]
    public async Task BulkAssign_OneInvalidEntry_RejectsTheWholeBatch()
    {
        var vision = await CreateProfileWithAsync(TextVisionCapabilities);
        var text = await CreateProfileWithAsync(TextCapabilities);

        // A known-good starting point, so "unchanged" below means something.
        (await BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Translation)] = text.Id })).Success.Should().BeTrue();

        // ImageGeneration cannot take a text profile; Translation could.
        var result = await BulkAssignAsync(new Dictionary<string, string>
        {
            [nameof(AiActionType.ImageGeneration)] = vision.Id,
            [nameof(AiActionType.Translation)] = vision.Id
        });

        result.Success.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.ActionType.Should().Be(AiActionType.ImageGeneration);
        error.Error.Message.Should().Be("Profile lacks capabilities required for ActionType.ImageGeneration");

        // The valid half was not applied either: the batch is atomic, so there is no partially saved state.
        (await GetAssignmentAsync(AiActionType.Translation)).Should().Be(text.Id, "the valid entry was not applied");
    }

    [Fact]
    public async Task BulkAssign_EmptyMap_IsAcceptedAndChangesNothing()
    {
        await CreateProfileWithAsync(TextCapabilities);
        var before = await GetAllAssignmentsAsync();

        var result = await BulkAssignAsync(new Dictionary<string, string>());

        result.Success.Should().BeTrue();
        (await GetAllAssignmentsAsync()).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task BulkAssign_MalformedProfileIdIs400_EmptyOneIsAPerItemError()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        (await BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Translation)] = text.Id })).Success.Should().BeTrue();

        // The value is parsed as a GUID, so a malformed one never reaches the per-item validation...
        var malformed = await StatusOfAsync(() => BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Translation)] = "not-a-guid" }));
        malformed.Should().Be(400);

        // ...while an empty string parses and then fails the lookup, as a per-item error inside a 200.
        var empty = await BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Translation)] = string.Empty });
        empty.Success.Should().BeFalse();
        empty.Errors.Should().NotBeEmpty().And.OnlyContain(e => e.ActionType == AiActionType.Translation);

        (await GetAssignmentAsync(AiActionType.Translation)).Should().Be(text.Id, "neither attempt changed the binding");
    }

    /// <summary>
    /// An unknown action key used to be dropped silently under a <c>success: true</c> that stored nothing. The batch
    /// route now answers it the way the single-assignment route does: a 400, and nothing is applied.
    /// </summary>
    [Fact]
    [Trait("Bug", "82831")]
    public async Task BulkAssign_UnknownActionKey_IsRejectedAndNothingIsStored()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        var other = await CreateProfileWithAsync(TextVisionCapabilities);

        (await StatusOfAsync(() => BulkAssignAsync(new Dictionary<string, string> { ["NotAnAction"] = text.Id })))
            .Should().Be(400, "an unknown key in a batch");

        // The enum is case-sensitive: a wrong-cased action name is an unknown key like any other.
        (await StatusOfAsync(() => BulkAssignAsync(new Dictionary<string, string> { ["chat"] = other.Id })))
            .Should().Be(400, "a wrong-cased action name");

        var afterUnknown = await GetAllAssignmentsAsync();
        afterUnknown.Keys.Should().NotContain("NotAnAction").And.NotContain(nameof(AiActionType.Chat)).And.NotContain("chat");

        // A mixed batch is refused whole: the valid half must not be applied on its way past the unknown key.
        (await StatusOfAsync(() => BulkAssignAsync(new Dictionary<string, string>
        {
            [nameof(AiActionType.Chat)] = text.Id,
            ["NotAnAction"] = other.Id
        }))).Should().Be(400, "a batch carrying one unknown key");
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull("the valid half of the refused batch was not applied");
    }

    /// <summary>A null value is refused (it used to report success and leave the binding in place) and the binding stays.</summary>
    [Fact]
    [Trait("Bug", "82831")]
    public async Task BulkAssign_NullValue_IsRejectedAndLeavesTheBindingAlone()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);
        (await BulkAssignAsync(new Dictionary<string, string> { [nameof(AiActionType.Translation)] = text.Id })).Success.Should().BeTrue();

        // Raw: a typed Dictionary<string, string> cannot be relied on to put a JSON null on the wire.
        var (status, body) = await PutRawAsync("bulk-assign", """{"Translation":null}""");

        status.Should().Be(400, $"a null value must be rejected: {body}");
        (await GetAssignmentAsync(AiActionType.Translation)).Should().Be(text.Id, "the binding the refused write did not touch");
    }
}
