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
/// Covers the parts of the one rule for writing the metadata of an entry that the role suites leave open: a member who
/// may only review or comment a file cannot write it, the room manager writes the room, and an agent's knowledge folder
/// refuses every write although its owner may create in it.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataWriteRuleTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Theory]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    public async Task MemberWithoutTheEditRight_CannotWriteTheMetadataOfAFile_AndNothingChanges(FileShare access)
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync();
        var other = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Review {suffix}");
        var file = await CreateFile($"review-{suffix}.docx", room.Id);
        await AssignToFileAsync(file.Id, template.Id);

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoomAsync(room.Id, member.Id, access);
        await _filesClient.Authenticate(member);

        var fieldId = template.Fields.Single().Id;

        (await StatusOfAsync(() => SetFileStringAsync(file.Id, fieldId, "written"))).Should().Be(403, "the values of a file follow the right to edit it");
        (await StatusOfAsync(() => AssignToFileAsync(file.Id, other.Id))).Should().Be(403);
        (await StatusOfAsync(() => _metadataApi.UnassignFileTemplateAsync(file.Id, template.Id, TestContext.Current.CancellationToken))).Should().Be(403);
        (await StatusOfAsync(() => _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("Client", "ACME")]), TestContext.Current.CancellationToken))).Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var metadata = await GetFileMetadataAsync(file.Id);
        metadata.Templates.Select(t => t.Id).Should().Equal(template.Id);
        ValueOf(metadata, template.Id, fieldId).Should().BeNull();
        metadata.CustomFields.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task RoomManager_WritesTheMetadataOfTheRoom()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Managed {suffix}");

        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoomAsync(room.Id, manager.Id, FileShare.RoomManager);
        await _filesClient.Authenticate(manager);

        var fieldId = template.Fields.Single().Id;

        await AssignToFolderAsync(room.Id, template.Id);
        await SetFolderStringAsync(room.Id, fieldId, "managed");
        await _metadataApi.SetFolderCustomFieldsAsync(room.Id, new SetCustomFields([new CustomFieldRequest("Client", "ACME")]), TestContext.Current.CancellationToken);

        var metadata = await GetFolderMetadataAsync(room.Id);
        ValueOf(metadata, template.Id, fieldId)!.StringValue.Should().Be("managed", "the room is written by whoever may edit the room, its manager");
        metadata.CustomFields.Should().ContainSingle(f => f.Name == "Client" && f.Value == "ACME");
    }

    [Fact]
    public async Task KnowledgeFolderOfAnAgent_RefusesEveryMetadataWrite_OfItsOwner()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync();
        var agent = await CreateAiRoom($"Agent {suffix}");
        var knowledgeId = await GetKnowledgeFolderIdAsync(agent.Id);

        // the owner may create in the knowledge folder, yet nobody may edit it, so it carries no metadata
        (await StatusOfAsync(() => AssignToFolderAsync(knowledgeId, template.Id))).Should().Be(403);
        (await StatusOfAsync(() => _metadataApi.AssignFolderTemplatesAsync(knowledgeId, new AssignMetadataTemplates([template.Id], cascade: true), TestContext.Current.CancellationToken))).Should().Be(403);
        (await StatusOfAsync(() => SetFolderStringAsync(knowledgeId, template.Fields.Single().Id, "written"))).Should().Be(403);
        (await StatusOfAsync(() => _metadataApi.SetFolderCustomFieldsAsync(knowledgeId, new SetCustomFields([new CustomFieldRequest("Client", "ACME")]), TestContext.Current.CancellationToken))).Should().Be(403);

        var metadata = await GetFolderMetadataAsync(knowledgeId);
        metadata.Templates.Should().BeNullOrEmpty();
        metadata.CustomFields.Should().BeNullOrEmpty();
    }

    private Task InviteToRoomAsync(int roomId, Guid userId, FileShare access)
    {
        return _roomsApi.SetRoomSecurityAsync(roomId, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Access = access, Id = userId }],
            Notify = false,
            Message = "",
            Culture = "en-US"
        }, cancellationToken: TestContext.Current.CancellationToken);
    }
}
