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

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the rooms listing filtered by the room metadata: the combination with the other filters, the response
/// shape, the lifecycle of the values, the error cases and the access rules.
/// </summary>
[Trait("Category", "Metadata")]
public class RoomsMetadataSearchTests(AspireAppFixture fixture) : RoomsMetadataSearchTestsBase(fixture)
{
    #region Combination with the other filters

    [Fact]
    public async Task Rooms_WithoutMetadataFilter_ReturnsAllRooms()
    {
        var data = await ArrangeAsync();

        var rooms = await data.Api.GetRoomsAsync(cancellationToken: TestContext.Current.CancellationToken);

        rooms.RoomIds().Should().Contain([data.MatchingRoomId, data.PartialRoomId, data.BareRoomId]);
    }

    [Fact]
    public async Task Rooms_MetadataFilterAndTitleSearch_NarrowEachOther()
    {
        var data = await ArrangeAsync();

        // the title of another room narrows the metadata filter down to nothing.
        // Only this direction is asserted: a positive title match would depend on the folder title index,
        // which is filled asynchronously through the event bus and is not what the metadata filter changes.
        var empty = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [], filterValue: data.BareRoomTitle);

        empty.Folders.Should().BeEmpty();
    }

    [Fact]
    public async Task Rooms_TextSearch_FindsTheRoomByItsSystemTemplateValue()
    {
        var data = await ArrangeAsync();

        // a custom field lands in the system template, whose string values feed the global text of the
        // metadata document, so the free text search must find the room by that value and not by its title
        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        await data.Api.AddFolderCustomFieldAsync(data.MatchingRoomId, "Reference", marker, TestContext.Current.CancellationToken);

        var rooms = await data.SearchByTextAsync(marker, expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_MetadataFilterAndRoomTypeFilter_AreCombined()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId], roomType: CustomRoomType);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByMetadata_FindsTheArchivedRoom()
    {
        var data = await ArrangeAsync();

        await _roomsApi.ArchiveRoomAsync(
            data.MatchingRoomId,
            new ArchiveRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken);

        // the archive move runs as a long operation and the test classes run in parallel, so it can take a while
        // under load: wait for the unfiltered listing first, so a slow move is not reported as a broken filter
        var moved = await data.ListAsync(
            r => r.RoomIds().Contains(data.MatchingRoomId),
            searchArea: SearchAreaArchive,
            timeout: TimeSpan.FromMinutes(2));

        moved.RoomIds().Should().Contain(data.MatchingRoomId, "the room must reach the archive section");

        // the metadata document keeps the ancestor tree of the active section, so the archived room must still
        // be found: the metadata queries are deliberately not scoped by that tree
        var archived = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId], searchArea: SearchAreaArchive);
        archived.RoomIds().Should().Equal(data.MatchingRoomId);

        // and it must disappear from the active section
        var active = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: []);
        active.Folders.Should().BeEmpty();
    }

    #endregion

    #region Response shape, lifecycle and errors

    [Fact]
    public async Task Rooms_FilteredByMetadata_ReportTheAssignedTemplates()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId]);

        rooms.Folders.Should().ContainSingle()
            .Which.AssignedMetadataTemplates.Should().Contain(data.TemplateId);
    }

    [Fact]
    public async Task Rooms_AfterTheValueIsCleared_AreNotFoundByIt()
    {
        var data = await ArrangeAsync();

        await data.Api.SetFolderValuesAsync(data.MatchingRoomId,
            [new MetadataValuePayload { FieldId = data.FieldId(ClientField), StringValue = null }],
            TestContext.Current.CancellationToken);

        var byClient = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: []);
        byClient.Folders.Should().BeEmpty();

        // the other values of the same room are untouched
        var byAmount = await data.SearchAsync(data.Range(AmountField, from: 150, to: 150), expected: [data.MatchingRoomId]);
        byAmount.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_WithInvalidMetadataFiltersJson_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        using var response = await data.Api.GetRoomsResponseAsync(
            metadataTemplateId: data.TemplateId,
            rawMetadataFilters: "definitely-not-json",
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rooms_WithUnknownFieldId_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        using var response = await data.Api.GetRoomsResponseAsync(
            metadataTemplateId: data.TemplateId,
            metadataFilters: [new { fieldId = int.MaxValue, op = "eq", value = "ACME" }],
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rooms_WithUnsupportedMetadataFilterOperator_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // the operator used to be ignored: "contains" ran as an exact match and returned nothing, without a hint why
        using var response = await data.Api.GetRoomsResponseAsync(
            metadataTemplateId: data.TemplateId,
            metadataFilters: [new { fieldId = data.FieldId(ClientField), op = "contains", value = "ACM" }],
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rooms_WithOperatorOfAnotherFieldType_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // "range" belongs to the number and date fields, a string field takes "eq" only; the value is complete on
        // purpose, so the operator is the only thing wrong with the request
        using var response = await data.Api.GetRoomsResponseAsync(
            metadataTemplateId: data.TemplateId,
            metadataFilters: [new { fieldId = data.FieldId(ClientField), op = "range", value = "ACME" }],
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rooms_WithFieldFromAnotherTemplate_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var otherTemplate = await data.Api.CreateTemplateAsync(
            "Other " + Guid.NewGuid().ToString()[..8],
            [new MetadataFieldPayload { Name = "Owner", Type = 0 }],
            TestContext.Current.CancellationToken);

        using var response = await data.Api.GetRoomsResponseAsync(
            metadataTemplateId: data.TemplateId,
            metadataFilters: [new { fieldId = otherTemplate.Field("Owner").Id, op = "eq", value = "someone" }],
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task Rooms_FilteredByTemplateAlone_ReturnTheRoomsCarryingIt()
    {
        var data = await ArrangeAsync();

        // the template id without conditions used to be read for validation only, so the listing came back unfiltered
        var rooms = await data.Api.GetRoomsAsync(data.TemplateId, cancellationToken: TestContext.Current.CancellationToken);

        rooms.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId }, "both rooms carry the template, the bare one does not");
    }

    [Fact]
    public async Task Rooms_FilteredByUnknownTemplate_ReturnsBadRequest()
    {
        await ArrangeAsync();

        using var response = await new MetadataApiClient(_filesClient).GetRoomsResponseAsync(metadataTemplateId: int.MaxValue, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Access

    [Fact]
    public async Task Rooms_FilteredByMetadata_AreVisibleToTheInvitedUserOnly()
    {
        var data = await ArrangeAsync();

        var member = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomSecurityAsync(data.MatchingRoomId, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Access = FileShare.Read, Id = member.Id }],
            Notify = false,
            Message = "",
            Culture = "en-US"
        }, cancellationToken: TestContext.Current.CancellationToken);

        // the invited user goes through the "rooms for me" branch
        await _filesClient.Authenticate(member);

        var shared = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId]);
        shared.RoomIds().Should().Equal(data.MatchingRoomId);

        // the room that was not shared stays invisible even though it holds a matching value
        var notShared = await data.SearchAsync(data.Range(AmountField, from: 900, to: 900), expected: []);
        notShared.Folders.Should().BeEmpty();

        await _filesClient.Authenticate(Owner);
    }

    #endregion
}
