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

namespace ASC.AI.Tests.Tests.EditorToolTests;

/// <summary>
/// The DocSpace REST tools of the editor catalogue (<c>get_file_info</c>, <c>delete_file</c>, <c>create_folder</c>,
/// <c>archive_room</c>, ...): each runs as the caller against the portal's own REST API through the DocSpace MCP
/// server, so the proof of an effect is read back through the Files API.
/// </summary>
/// <remarks>
/// These tools are published by the <c>onlyoffice-docspace-mcp</c> container, which the AppHost starts for the
/// development and preview profiles but not for <c>integration-test</c>. Every test starts by checking that the
/// catalogue carries <c>get_room_types</c> and is skipped with that reason when it does not (an environment gap, not
/// a product defect). With the container added to the integration-test profile they run as written.
/// </remarks>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolRestToolTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    private static object Fields(params string[] names) => new { fields = names };

    private static string FileTitle() => $"Autotest {Guid.NewGuid().ToString("N")[..6]}.docx";

    /// <summary>Signs in as the owner and asserts the REST tools are published; returns the owner's My documents folder.</summary>
    private async Task<int> StartAsync()
    {
        await ActAsOwnerAsync();
        await RequireRestToolsAsync();

        return await MyFolderIdAsync();
    }

    [Fact]
    public async Task ReadTool_WithNoArguments_AnswersWithItsContent()
    {
        await StartAsync();

        var outcome = await RunAsync("get_room_types");

        outcome.IsError.Should().BeFalse(outcome.Raw);
        outcome.Text.Should().Contain("Form Filling Room");
    }

    [Fact]
    public async Task GetFileInfo_ReturnsTheFileTheFilesApiHolds()
    {
        await StartAsync();
        var title = FileTitle();
        var fileId = await CreateFileAsync(title);

        var outcome = await RunAsync("get_file_info", new { fileId, filters = Fields("id", "title") });

        outcome.IsError.Should().BeFalse(outcome.Text);
        var response = outcome.Json.GetProperty("response");
        response.GetProperty("id").GetInt32().Should().Be(fileId);
        response.GetProperty("title").GetString().Should().Be(title);
    }

    /// <summary>A failing tool still answers 200 and says so inside <c>result</c>, with the REST status it met.</summary>
    [Fact]
    public async Task FailingTool_IsReportedInsideTheResult_NotByTheStatus()
    {
        await StartAsync();

        var reply = await CallToolAsync("delete_file", new { fileId = 999999999 });

        var outcome = OutcomeOf(reply);
        reply.Status.Should().Be(200, reply.ToString());
        outcome.IsError.Should().BeTrue(outcome.Raw);
        outcome.Text.Should().Contain("404");
    }

    [Fact]
    public async Task CreateFolder_ReallyCreatesTheFolder()
    {
        var parentId = await StartAsync();
        var title = RandomName();

        var outcome = await RunAsync("create_folder", new { parentId, title, filters = Fields("id", "title") });

        outcome.IsError.Should().BeFalse(outcome.Text);
        var content = await FoldersApi.GetFolderByFolderIdAsync(parentId, cancellationToken: TestContext.Current.CancellationToken);
        content.Response.Folders.Select(f => f.Title).Should().Contain(title, "the folder the tool reported is in My documents");
    }

    [Fact]
    public async Task UpdateFile_ReallyRenamesTheFile()
    {
        var folderId = await StartAsync();
        var fileId = await CreateFileAsync(FileTitle());
        var renamed = $"Autotest renamed {Guid.NewGuid().ToString("N")[..6]}.docx";

        var outcome = await RunAsync("update_file", new { fileId, title = renamed });

        outcome.IsError.Should().BeFalse(outcome.Text);
        (await TitlesInAsync(folderId)).Should().Contain(renamed);
    }

    [Fact]
    public async Task DeleteFile_ReallyDeletesTheFileAndOnlyThatFile()
    {
        var folderId = await StartAsync();
        var doomed = $"Autotest doomed {Guid.NewGuid().ToString("N")[..6]}.docx";
        var bystander = $"Autotest bystander {Guid.NewGuid().ToString("N")[..6]}.docx";
        var doomedId = await CreateFileAsync(doomed);
        await CreateFileAsync(bystander);

        var outcome = await RunAsync("delete_file", new { fileId = doomedId });

        outcome.IsError.Should().BeFalse(outcome.Text);
        var titles = await TitlesInAsync(folderId);
        titles.Should().Contain(bystander);
        titles.Should().NotContain(doomed);
    }

    [Theory]
    [MemberData(nameof(NonGuestRoles))]
    public async Task ReadTool_IsCallableByEveryNonGuestRole(EmployeeType type)
    {
        await StartAsync();
        await ActAsAsync(type);

        var reply = await CallToolAsync("get_room_types");

        var outcome = OutcomeOf(reply);
        reply.Status.Should().Be(200, reply.ToString());
        outcome.IsError.Should().BeFalse(outcome.Text);
        outcome.Text.Should().Contain("Form Filling Room");
    }

    /// <summary>
    /// A member's tools never exceed the member's rights: a User pointing the tools at the owner's file and room gets
    /// the portal's own 403 inside <c>result</c>, and the owner's data is exactly what it was.
    /// </summary>
    [Fact]
    public async Task User_CannotReadRenameDeleteOrArchiveTheOwnersData()
    {
        var myId = await StartAsync();
        const string title = "Autotest owner private.docx";
        var fileId = await CreateFileAsync(title);
        var roomId = await CreateCustomRoomAsync("Autotest owner room");
        // The premise: both tools work for the owner on exactly these targets, so the refusals below are about who asks.
        (await RunAsync("get_file_info", new { fileId, filters = Fields("id") })).IsError.Should().BeFalse();
        (await RunAsync("get_room_info", new { roomId, filters = Fields("id") })).IsError.Should().BeFalse();
        await ActAsAsync(EmployeeType.User);

        var attempts = new (string Label, object Arguments)[]
        {
            ("get_file_info", new { fileId, filters = Fields("id") }),
            ("download_file_as_text", new { fileId }),
            ("update_file", new { fileId, title = "hijacked.docx" }),
            ("delete_file", new { fileId }),
            ("get_room_info", new { roomId, filters = Fields("id") }),
            ("archive_room", new { roomId }),
            ("upload_file", new { parentId = roomId, filename = "x.txt", content = "hi" }),
            ("create_folder", new { parentId = myId, title = "intruder", filters = Fields("id") })
        };
        var offenders = new List<string>();
        foreach (var (label, arguments) in attempts)
        {
            var reply = await CallToolAsync(label, arguments);
            var outcome = OutcomeOf(reply);
            if (reply.Status != 200 || !outcome.IsError || !outcome.Text.Contains(": 403 ", StringComparison.Ordinal))
            {
                offenders.Add($"{label}: {reply.Status} isError={outcome.IsError} \"{outcome.Text[..Math.Min(120, outcome.Text.Length)]}\"");
            }
        }

        // State first: a refusal that still did the work is the same defect.
        await ActAsOwnerAsync();
        (await TitlesInAsync(myId)).Should().Contain(title);
        (await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.RootFolderType
            .Should().NotBe(DocSpace.API.SDK.Model.FolderType.Archive);
        (await FoldersApi.GetFolderByFolderIdAsync(myId, cancellationToken: TestContext.Current.CancellationToken))
            .Response.Folders.Select(f => f.Title).Should().NotContain("intruder", "no folder was created in the owner's My documents");
        (await TitlesInAsync(roomId)).Should().BeEmpty();
        offenders.Should().BeEmpty();
    }

    [Fact]
    public async Task RestTool_AnswersTheSameWithAndWithoutAMadeUpEntityId()
    {
        await StartAsync();
        var fileId = await CreateFileAsync(FileTitle());
        var arguments = new { fileId, filters = Fields("id", "title") };

        var without = await RunAsync("get_file_info", arguments);
        var withBogus = await RunAsync("get_file_info", arguments, MissingEntityId);

        without.IsError.Should().BeFalse(without.Text);
        withBogus.Text.Should().Be(without.Text);
    }

    /// <summary>
    /// <c>create_room</c> listed <c>roomType</c> as required while giving it a default, and ran without it: the
    /// property is not required, and the tool still runs without it.
    /// </summary>
    [Fact]
    [Trait("Bug", "84395")]
    public async Task CreateRoom_RunsWithoutRoomType_AndTheSchemaDoesNotRequireIt()
    {
        await StartAsync();
        var createRoom = (await ToolsAsync()).Single(t => t.Name == "create_room");
        createRoom.Required.Should().NotContain("roomType");
        var arguments = SampleArguments(createRoom.InputSchema);
        arguments.Remove("roomType");

        var outcome = await RunAsync("create_room", arguments);

        outcome.IsError.Should().BeFalse("the tool does not need roomType: " + outcome.Text);
    }

    /// <summary>
    /// <c>filters.fields</c> items are restricted by the tool (an unknown field is refused, a known one accepted), so
    /// the published schema must list the values it accepts instead of a bare string.
    /// </summary>
    [Fact]
    [Trait("Bug", "84398")]
    public async Task GetFileInfo_FiltersFieldsItems_AreAnEnumInTheSchemaAsWellAsInPractice()
    {
        await StartAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == "get_file_info");
        var items = tool.Property("filters").GetProperty("properties").GetProperty("fields").GetProperty("items");
        items.GetProperty("type").GetString().Should().Be("string");
        var fileId = await CreateFileAsync(FileTitle());

        var refused = await RunAsync("get_file_info", new { fileId, filters = Fields("zzNotAField") });
        var accepted = await RunAsync("get_file_info", new { fileId, filters = Fields("id") });

        refused.IsError.Should().BeTrue(refused.Text);
        refused.Text.Should().Contain("Invalid option");
        accepted.IsError.Should().BeFalse(accepted.Text);
        (items.TryGetProperty("enum", out var values) && values.EnumerateArray().Any(v => v.GetString() == "id"))
            .Should().BeTrue("the schema lists the values the tool accepts");
    }

    /// <summary>
    /// Arguments built from the published schema (required only, then every property) must never be refused at
    /// parsing. Read-only tools only: the point is the schema, not the effect.
    /// </summary>
    [Fact]
    public async Task ArgumentsThePublishedSchemaAccepts_AreNeverRefusedAtParsing()
    {
        await StartAsync();
        var tools = (await ToolsAsync()).Where(t => !t.RequireApproval).ToList();
        tools.Should().NotBeEmpty();

        var offenders = new List<string>();
        foreach (var tool in tools)
        {
            foreach (var all in new[] { false, true })
            {
                var outcome = await RunAsync(tool.Name, SampleArguments(tool.InputSchema, all));
                if (outcome.Text.StartsWith("Parsing input.", StringComparison.Ordinal))
                {
                    offenders.Add($"{tool.Name} ({(all ? "all properties" : "required only")}): {outcome.Text[..Math.Min(160, outcome.Text.Length)]}");
                }
            }
        }

        offenders.Should().BeEmpty();
    }

    /// <summary>
    /// The portal switch is bypassed by naming a REST tool when only the generator and the list are gated: with the
    /// switch off the folder must not be created, and the call is refused with 403.
    /// </summary>
    [Fact]
    [Trait("Bug", "84390")]
    public async Task CreateFolder_IsRefusedWhenTheSwitchIsOff()
    {
        var folderId = await StartAsync();
        var onTitle = RandomName("Autotest on");
        var on = await RunAsync("create_folder", new { parentId = folderId, title = onTitle, filters = Fields("id", "title") });
        on.IsError.Should().BeFalse(on.Text);
        await SwitchAiOffAsync();
        var offTitle = RandomName("Autotest off");

        var reply = await CallToolAsync("create_folder", new { parentId = folderId, title = offTitle, filters = Fields("id", "title") });

        var folders = (await FoldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken))
            .Response.Folders.Select(f => f.Title).ToList();
        folders.Should().Contain(onTitle, "with the switch on the tool works");
        folders.Should().NotContain(offTitle, "no folder is created while AI is off");
        reply.Status.Should().Be(403, reply.ToString());
    }

    [Fact]
    [Trait("Bug", "84391")]
    public async Task DeleteFile_IsRefusedWhenTheSwitchIsOff()
    {
        var folderId = await StartAsync();
        var victim = $"Autotest AI off victim {Guid.NewGuid().ToString("N")[..6]}.docx";
        var control = $"Autotest AI off control {Guid.NewGuid().ToString("N")[..6]}.docx";
        var victimId = await CreateFileAsync(victim);
        await CreateFileAsync(control);
        await SwitchAiOffAsync();

        var reply = await CallToolAsync("delete_file", new { fileId = victimId });

        // The control file is the listing's positive control: it proves the folder could be read.
        var titles = await TitlesInAsync(folderId);
        titles.Should().Contain(control);
        titles.Should().Contain(victim, "the file survives while AI is off");
        reply.Status.Should().Be(403, reply.ToString());
    }
}
