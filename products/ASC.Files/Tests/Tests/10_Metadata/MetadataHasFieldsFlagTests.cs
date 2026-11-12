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
// design elements, icons, logos, and text, are the property of
// Ascensio System SIA and are protected by copyright and trademark laws.
// These elements may not be used in derivative works or modified in any way
// without prior written permission from Ascensio System SIA.
//
// Pursuant to Section 7 § 3(b) of the License you must retain the original
// Product logo when distributing the program. Pursuant to Section 7 § 3(e) we
// decline to grant you any rights under trademark law for use of our trademarks.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers <c>hasMetadataFields</c>: the flag a client hides "fill the metadata" behind. It is answered on every entry of
/// a folder listing and by the file, folder and entry-metadata info endpoints, from one rule: a visible assigned
/// template with a field, or a custom field. It is a fact about the data, not a right, and it is not derivable from
/// <c>assignedMetadataTemplates</c>, which lists the hidden templates and never the custom fields.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataHasFieldsFlagTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task Flag_OnAPortalWithoutTemplates_IsFalseNotAbsent()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("No templates " + Suffix());
        var file = await CreateFile("Plain.docx", room.Id);
        var folder = await CreateFolder("Plain", room.Id);

        // a portal without a single template skips the metadata queries of the listing, and the flag must still be there:
        // an absent flag means "not computed in this response", which a client cannot read as "nothing to fill"
        (await ListedFlagAsync(room.Id, file.Id, FileEntryType.File)).Should().BeFalse();
        (await ListedFlagAsync(room.Id, folder.Id, FileEntryType.Folder)).Should().BeFalse();
        (await InfoFlagAsync(file.Id, FileEntryType.File)).Should().BeFalse();
        (await InfoFlagAsync(folder.Id, FileEntryType.Folder)).Should().BeFalse();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeFalse();
        (await MetadataFlagAsync(folder.Id, FileEntryType.Folder)).Should().BeFalse();
    }

    [Fact]
    public async Task Flag_WithAVisibleTemplate_IsTrueOnTheFileAndTheFolderEverywhere()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom("Visible " + Suffix());
        var file = await CreateFile("Contract.docx", room.Id);
        var folder = await CreateFolder("Contracts", room.Id);
        var untouched = await CreateFile("Untouched.docx", room.Id);

        await AssignToFileAsync(file.Id, template.Id);
        await AssignToFolderAsync(folder.Id, template.Id);

        (await ListedFlagAsync(room.Id, file.Id, FileEntryType.File)).Should().BeTrue();
        (await ListedFlagAsync(room.Id, folder.Id, FileEntryType.Folder)).Should().BeTrue();
        (await ListedFlagAsync(room.Id, untouched.Id, FileEntryType.File)).Should().BeFalse("the flag is per entry, not per page");
        (await InfoFlagAsync(file.Id, FileEntryType.File)).Should().BeTrue();
        (await InfoFlagAsync(folder.Id, FileEntryType.Folder)).Should().BeTrue();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeTrue();
        (await MetadataFlagAsync(folder.Id, FileEntryType.Folder)).Should().BeTrue();
    }

    [Fact]
    public async Task Flag_WithAHiddenTemplateOnly_IsFalseWhileTheTemplateIsStillListedAsAssigned()
    {
        await _filesClient.Authenticate(Owner);
        var hidden = await CreateTemplateAsync(visible: false);
        var room = await CreateCustomRoom("Hidden " + Suffix());
        var file = await CreateFile("Secret.docx", room.Id);

        await AssignToFileAsync(file.Id, hidden.Id);

        // this is why the flag exists: the assignment is reported, and a client that counted the assignments would
        // offer a filling that has nothing to fill
        var listed = await ListedEntryAsync(room.Id, file.Id, FileEntryType.File);
        FlagOf(listed).Should().BeFalse();
        listed.GetProperty("assignedMetadataTemplates").EnumerateArray().Select(t => t.GetInt32()).Should().Equal(hidden.Id);

        (await InfoFlagAsync(file.Id, FileEntryType.File)).Should().BeFalse();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeFalse();
    }

    [Fact]
    public async Task Flag_WithACustomFieldOnly_IsTrueWithoutAnyAssignedTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Custom " + Suffix());
        var file = await CreateFile("Tagged.docx", room.Id);
        var folder = await CreateFolder("Tagged", room.Id);

        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("Project code", "A-42")]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderCustomFieldsAsync(folder.Id, new SetCustomFields([new CustomFieldRequest("Project code", "B-7")]), TestContext.Current.CancellationToken);

        // a custom field is a value in the hidden system template, never an assignment: the only way to know it is there
        // from a listing is this flag
        var listedFile = await ListedEntryAsync(room.Id, file.Id, FileEntryType.File);
        FlagOf(listedFile).Should().BeTrue();
        listedFile.TryGetProperty("assignedMetadataTemplates", out _).Should().BeFalse("the system template never shows as assigned");

        (await ListedFlagAsync(room.Id, folder.Id, FileEntryType.Folder)).Should().BeTrue();
        (await InfoFlagAsync(file.Id, FileEntryType.File)).Should().BeTrue();
        (await InfoFlagAsync(folder.Id, FileEntryType.Folder)).Should().BeTrue();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeTrue();
        (await MetadataFlagAsync(folder.Id, FileEntryType.Folder)).Should().BeTrue();
    }

    [Fact]
    public async Task Flag_FollowsTheVisibilityOfTheTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom("Toggle " + Suffix());
        var file = await CreateFile("Toggle.docx", room.Id);

        await AssignToFileAsync(file.Id, template.Id);
        (await ListedFlagAsync(room.Id, file.Id, FileEntryType.File)).Should().BeTrue();

        // hiding the template keeps the assignment and the values, but there is nothing a user can fill any more
        await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(visible: false), TestContext.Current.CancellationToken);
        (await ListedFlagAsync(room.Id, file.Id, FileEntryType.File)).Should().BeFalse();
        (await InfoFlagAsync(file.Id, FileEntryType.File)).Should().BeFalse();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeFalse();

        await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(visible: true), TestContext.Current.CancellationToken);
        (await ListedFlagAsync(room.Id, file.Id, FileEntryType.File)).Should().BeTrue();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeTrue();
    }

    [Fact]
    public async Task Flag_StaysTrueOnceEveryFieldIsFilled()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom("Filled " + Suffix());
        var file = await CreateFile("Filled.docx", room.Id);

        await AssignToFileAsync(file.Id, template.Id);
        await SetFileStringAsync(file.Id, template.Fields.Single().Id, "done");

        // "has fields", not "has empty fields": the filling with overwrite still has something to do, and a client
        // that wants to hide the action after the values are in reads the values, not this flag
        (await ListedFlagAsync(room.Id, file.Id, FileEntryType.File)).Should().BeTrue();
        (await MetadataFlagAsync(file.Id, FileEntryType.File)).Should().BeTrue();
    }

    [Fact]
    public async Task Flag_IsAFactAboutTheData_NotARightOfTheCaller()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom("Reader " + Suffix());
        var file = await CreateFile("Shared.docx", room.Id);
        await AssignToFileAsync(file.Id, template.Id);

        var reader = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Access = FileShare.Read, Id = reader.Id }],
            Notify = false,
            Message = "",
            Culture = "en-US"
        }, cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(reader);

        // the reader sees the same flag as the owner and cannot write a value: a client combines the flag with
        // security.Edit, the flag alone is not permission to fill
        var listed = await ListedEntryAsync(room.Id, file.Id, FileEntryType.File);
        FlagOf(listed).Should().BeTrue();
        listed.GetProperty("security").GetProperty("Edit").GetBoolean().Should().BeFalse();
    }

    #region Raw reads

    // raw: the generated SDK (4.0.0) predates hasMetadataFields, so FileDto, FolderDto and EntryMetadataDto carry no
    // property for it and the flag has to be read from the JSON

    private async Task<bool?> ListedFlagAsync(int parentId, int entryId, FileEntryType entryType)
    {
        return FlagOf(await ListedEntryAsync(parentId, entryId, entryType));
    }

    private async Task<JsonElement> ListedEntryAsync(int parentId, int entryId, FileEntryType entryType)
    {
        var response = await ReadResponseAsync($"api/2.0/files/{parentId}");
        var list = response.GetProperty(entryType == FileEntryType.File ? "files" : "folders");

        return list.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == entryId);
    }

    private async Task<bool?> InfoFlagAsync(int entryId, FileEntryType entryType)
    {
        return FlagOf(await ReadResponseAsync(entryType == FileEntryType.File ? $"api/2.0/files/file/{entryId}" : $"api/2.0/files/folder/{entryId}"));
    }

    private async Task<bool?> MetadataFlagAsync(int entryId, FileEntryType entryType)
    {
        return FlagOf(await ReadResponseAsync($"api/2.0/files/metadata/{(entryType == FileEntryType.File ? "file" : "folder")}/{entryId}"));
    }

    /// <summary>
    /// Null when the flag is absent from the entry, which a test distinguishes from false on purpose.
    /// </summary>
    private static bool? FlagOf(JsonElement entry)
    {
        return entry.TryGetProperty("hasMetadataFields", out var flag) ? flag.GetBoolean() : null;
    }

    private async Task<JsonElement> ReadResponseAsync(string path)
    {
        using var response = await new RawApiClient(_filesClient).GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        return json.RootElement.GetProperty("response").Clone();
    }

    #endregion
}
