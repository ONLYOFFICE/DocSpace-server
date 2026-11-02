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

namespace ASC.AI.Tests.Tests.WebSearchTests;

/// <summary>
/// Who may use the passthrough routes (DocSpaceAdmin, RoomAdmin and User; a Guest and a portal with AI switched off get
/// 403) and which document a search is attributed to: the entry named by <c>entityId</c> and <c>entityKind</c> is
/// resolved under the caller's own credentials and sent upstream as <c>metadata</c>, and an entry the caller cannot
/// open sends none.
/// </summary>
/// <remarks>
/// The TypeScript attribution test reads the billing rows the gateway writes; with the stub provider the same
/// metadata is read straight from the request the provider received, which is the contract's own wire format.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchPassthroughAccessTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    private const string SearchBody = """{"engine":"exa","query":"ONLYOFFICE Docs","numResults":1}""";

    [Theory]
    [MemberData(nameof(WebSearchPermissionsTests.ReadRoles), MemberType = typeof(WebSearchPermissionsTests))]
    public async Task Routes_AreOpenToEveryRoleButGuest(EmployeeType type, bool allowed)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await ActAsAsync(type);

        var search = await PassthroughAsync("search", SearchBody);
        // No urls: for a role that is allowed this proves the request got past the gate without a second search.
        var contents = await PassthroughAsync("contents", "{}");

        if (!allowed)
        {
            new { search = search.Status, contents = contents.Status }.Should().BeEquivalentTo(new { search = 403, contents = 403 });
            search.Text.Should().Contain("Forbidden");
            contents.Text.Should().Contain("Forbidden");

            return;
        }

        search.Status.Should().Be(200, search.ToString());
        search.Json.GetProperty("results").GetArrayLength().Should().BeGreaterThan(0);
        contents.Status.Should().Be(400, contents.ToString());
        contents.Error.Should().Be("urls is required and must be a non-empty array of strings");
    }

    [Fact]
    public async Task AiDisabled_RefusesBothRoutesWith403_AndTheConfigSurvives()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        var control = await PassthroughAsync("search", "{}");
        await SetPortalAiAccessAsync(false);

        var search = await PassthroughAsync("search", SearchBody);
        var contents = await PassthroughAsync("contents", """{"engine":"exa","urls":["https://example.com"]}""");
        await SetPortalAiAccessAsync(true);
        var after = await PassthroughAsync("search", "{}");

        control.Status.Should().Be(400, "the body is refused by validation, so the 403 below is the switch: " + control);
        new { search = search.Status, contents = contents.Status }.Should().BeEquivalentTo(new { search = 403, contents = 403 });
        stub.CallsTo(WebSearchProviderStub.SearchPath).Should().BeEmpty(stub.Describe());
        after.Status.Should().Be(400, "the switch hides the feature, it does not cancel the config: " + after);
    }

    [Fact]
    public async Task Search_IsAttributedToADocumentItsCallerCanOpen_AndToNothingOtherwise()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        var cancellationToken = TestContext.Current.CancellationToken;
        await FilesHttpClient.Authenticate(Owner);
        var title = $"Autotest Attribution {Guid.NewGuid():N}.docx";
        var file = await FilesApi.CreateFileInMyDocumentsAsync(new CreateFileRequest(title), cancellationToken);
        var fileId = file.Response.Id.ToString();
        var reader = await InviteContact(EmployeeType.User, cancellationToken);
        var stranger = await InviteContact(EmployeeType.User, cancellationToken);
        // Read-only is enough to open it: attribution is about visibility, not edit.
        await SharingApi.SetFileSecurityInfoAsync(
            file.Response.Id,
            new SecurityInfoSimpleRequestDto([new FileShareParams(reader.Id, FileShare.Read)], notify: false),
            cancellationToken);

        async Task<JsonElement?> MetadataOfAsync(User? actor, string entityId, bool withKind)
        {
            await _aiChatClient.Authenticate(actor);
            var before = stub.CallsTo(WebSearchProviderStub.SearchPath).Count;
            var query = $"?entityId={entityId}" + (withKind ? "&entityKind=file" : string.Empty);

            var reply = await PassthroughAsync("search", SearchBody, query);

            reply.Status.Should().Be(200, reply.ToString());
            var calls = stub.CallsTo(WebSearchProviderStub.SearchPath);
            calls.Should().HaveCount(before + 1, stub.Describe());

            return JsonDocument.Parse(calls[^1].Body).RootElement.TryGetProperty("metadata", out var metadata)
                ? metadata.Clone()
                : null;
        }

        var owner = await MetadataOfAsync(Owner, fileId, withKind: true);
        var noKind = await MetadataOfAsync(Owner, fileId, withKind: false);
        var unknown = await MetadataOfAsync(Owner, MissingEntityId, withKind: true);
        var asReader = await MetadataOfAsync(reader, fileId, withKind: true);
        var asStranger = await MetadataOfAsync(stranger, fileId, withKind: true);

        foreach (var attributed in new[] { owner, asReader })
        {
            attributed.Should().NotBeNull("the caller can open the document");
            attributed!.Value.GetProperty("source_id").ToString().Should().Be(fileId);
            attributed.Value.GetProperty("source_title").GetString().Should().Be(title);
        }

        unknown.Should().BeNull("an id that does not exist attributes nothing");
        asStranger.Should().BeNull("a document the caller cannot open is not attributed to them");
        (noKind?.GetProperty("source_title").GetString()).Should().NotBe(title, "without the kind the id is read as a folder id");
        stub.CallsTo(WebSearchProviderStub.SearchPath).Last().Body.Should().NotContain(title, "the closed document's title is not disclosed");
    }
}
