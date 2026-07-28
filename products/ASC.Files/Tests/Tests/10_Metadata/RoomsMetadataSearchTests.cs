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
/// Covers the rooms listing filtered by the room metadata.
/// </summary>
/// <remarks>
/// Both the OpenSearch path and its SQL fallback must produce the same result, so these tests are valid
/// regardless of whether the metadata index is reachable during the run.
/// </remarks>
[Trait("Category", "Metadata")]
public class RoomsMetadataSearchTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int SearchAreaArchive = 1;
    private const int CustomRoomType = 5;

    private const string ClientField = "Client";
    private const string SignedField = "Signed";
    private const string AmountField = "Amount";
    private const string StatusField = "Status";
    private const string TagsField = "Tags";

    private static readonly DateTime _matchingDate = new(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _otherDate = new(2027, 1, 20, 0, 0, 0, DateTimeKind.Utc);

    #region String

    [Fact]
    public async Task Rooms_FilteredByStringField_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
        rooms.Total.Should().Be(1, "the total must reflect the filtered listing");
    }

    [Fact]
    public async Task Rooms_FilteredByStringField_IsCaseInsensitive()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Eq(ClientField, "aCmE"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByStringField_DoesNotMatchSubstring()
    {
        var data = await ArrangeAsync();

        // the matching room holds exactly "ACME", the condition is an exact match and must not match a prefix
        var rooms = await data.SearchAsync(data.Eq(ClientField, "ACM"), expected: []);

        rooms.Folders.Should().BeEmpty();
    }

    #endregion

    #region Number

    [Fact]
    public async Task Rooms_FilteredByNumberRange_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Range(AmountField, from: 100, to: 200), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByNumberRange_IncludesTheBoundaries()
    {
        var data = await ArrangeAsync();

        // the matching room holds exactly 150
        var rooms = await data.SearchAsync(data.Range(AmountField, from: 150, to: 150), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByNumberRange_SupportsTheOpenUpperBound()
    {
        var data = await ArrangeAsync();

        // 150 and 900: both rooms match
        var rooms = await data.SearchAsync(data.Range(AmountField, from: 100, to: null), expected: [data.MatchingRoomId, data.PartialRoomId]);

        rooms.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId });
    }

    #endregion

    #region Date

    [Fact]
    public async Task Rooms_FilteredByDateRange_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(
            data.DateRange(SignedField, from: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), to: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)),
            expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByDateRange_ExcludesTheRoomOutsideTheRange()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(
            data.DateRange(SignedField, from: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), to: new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            expected: []);

        rooms.Folders.Should().BeEmpty();
    }

    #endregion

    #region Choice

    [Fact]
    public async Task Rooms_FilteredBySingleChoice_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.In(StatusField, "Signed"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredBySingleChoice_CombinesTheOptionsWithOr()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.In(StatusField, "Signed", "Draft"), expected: [data.MatchingRoomId, data.PartialRoomId]);

        rooms.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId });
    }

    [Fact]
    public async Task Rooms_FilteredByMultiChoice_MatchesAnySelectedOptionWithoutDuplicates()
    {
        var data = await ArrangeAsync();

        // the matching room holds both Legal and Finance: it must be returned exactly once
        var rooms = await data.SearchAsync(data.In(TagsField, "Legal", "Finance"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    #endregion

    #region Several conditions

    [Fact]
    public async Task Rooms_FilteredBySeveralConditions_CombinesThemWithAnd()
    {
        var data = await ArrangeAsync();

        // the partial room shares the client but neither the amount nor the status
        var rooms = await data.SearchAsync(
            [data.Eq(ClientField, "ACME"), data.Range(AmountField, from: 100, to: 200), data.In(StatusField, "Signed")],
            expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredBySeveralConditions_ExcludesThePartiallyMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(
            [data.Eq(ClientField, "ACME"), data.In(StatusField, "Draft")],
            expected: []);

        rooms.Folders.Should().BeEmpty("the partially matching room holds the Draft status but not the ACME client");
    }

    #endregion

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

    #region Arrange

    private async Task<MetadataSearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Contracts " + suffix,
        [
            new MetadataFieldPayload { Name = ClientField, Type = 0 },
            new MetadataFieldPayload { Name = SignedField, Type = 1 },
            new MetadataFieldPayload { Name = AmountField, Type = 2 },
            new MetadataFieldPayload
            {
                Name = StatusField,
                Type = 3,
                Options = [new MetadataFieldOptionPayload { Value = "Draft" }, new MetadataFieldOptionPayload { Value = "Signed" }]
            },
            new MetadataFieldPayload
            {
                Name = TagsField,
                Type = 4,
                Options =
                [
                    new MetadataFieldOptionPayload { Value = "Legal" },
                    new MetadataFieldOptionPayload { Value = "Finance" },
                    new MetadataFieldOptionPayload { Value = "Urgent" }
                ]
            }
        ], TestContext.Current.CancellationToken);

        var matching = await CreateCustomRoom($"Matching {suffix}");
        var partial = await CreateCustomRoom($"Partial {suffix}");
        var bare = await CreateCustomRoom($"Bare {suffix}");

        var data = new MetadataSearchData(api, template)
        {
            MatchingRoomId = matching.Id,
            PartialRoomId = partial.Id,
            BareRoomId = bare.Id,
            BareRoomTitle = bare.Title
        };

        foreach (var roomId in new[] { matching.Id, partial.Id })
        {
            await api.AssignFolderTemplatesAsync(roomId, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        }

        await api.SetFolderValuesAsync(matching.Id,
        [
            new MetadataValuePayload { FieldId = data.FieldId(ClientField), StringValue = "ACME" },
            new MetadataValuePayload { FieldId = data.FieldId(SignedField), DateValue = _matchingDate },
            new MetadataValuePayload { FieldId = data.FieldId(AmountField), NumberValue = 150 },
            new MetadataValuePayload { FieldId = data.FieldId(StatusField), OptionIds = [data.OptionId(StatusField, "Signed")] },
            new MetadataValuePayload
            {
                FieldId = data.FieldId(TagsField),
                OptionIds = [data.OptionId(TagsField, "Legal"), data.OptionId(TagsField, "Finance")]
            }
        ], TestContext.Current.CancellationToken);

        await api.SetFolderValuesAsync(partial.Id,
        [
            new MetadataValuePayload { FieldId = data.FieldId(ClientField), StringValue = "Globex" },
            new MetadataValuePayload { FieldId = data.FieldId(SignedField), DateValue = _otherDate },
            new MetadataValuePayload { FieldId = data.FieldId(AmountField), NumberValue = 900 },
            new MetadataValuePayload { FieldId = data.FieldId(StatusField), OptionIds = [data.OptionId(StatusField, "Draft")] },
            new MetadataValuePayload { FieldId = data.FieldId(TagsField), OptionIds = [data.OptionId(TagsField, "Urgent")] }
        ], TestContext.Current.CancellationToken);

        return data;
    }

    /// <summary>
    /// The arranged template and rooms plus the condition builders and the polling request helper.
    /// </summary>
    private sealed class MetadataSearchData(MetadataApiClient api, MetadataTemplateResponse template)
    {
        public MetadataApiClient Api { get; } = api;
        public int TemplateId { get; } = template.Id;

        public int MatchingRoomId { get; init; }
        public int PartialRoomId { get; init; }
        public int BareRoomId { get; init; }
        public string BareRoomTitle { get; init; } = "";

        public int FieldId(string name)
        {
            return template.Field(name).Id;
        }

        public Guid OptionId(string fieldName, string optionValue)
        {
            return template.Field(fieldName).Option(optionValue);
        }

        public object Eq(string fieldName, string value)
        {
            return new { fieldId = FieldId(fieldName), op = "eq", value };
        }

        public object Range(string fieldName, long? from, long? to)
        {
            return new
            {
                fieldId = FieldId(fieldName),
                op = "range",
                from = from?.ToString(CultureInfo.InvariantCulture),
                to = to?.ToString(CultureInfo.InvariantCulture)
            };
        }

        public object DateRange(string fieldName, DateTime from, DateTime to)
        {
            return new
            {
                fieldId = FieldId(fieldName),
                op = "range",
                from = from.ToString("O", CultureInfo.InvariantCulture),
                to = to.ToString("O", CultureInfo.InvariantCulture)
            };
        }

        public object In(string fieldName, params string[] optionValues)
        {
            return new
            {
                fieldId = FieldId(fieldName),
                op = "in",
                optionIds = optionValues.Select(v => OptionId(fieldName, v)).ToList()
            };
        }

        public Task<RoomsContentResponse> SearchAsync(object condition, int[] expected, string? filterValue = null, int? searchArea = null, int? roomType = null, TimeSpan? timeout = null)
        {
            return PollAsync([condition], expected, filterValue, searchArea, roomType, timeout);
        }

        public Task<RoomsContentResponse> SearchAsync(object[] conditions, int[] expected, string? filterValue = null, int? searchArea = null, int? roomType = null, TimeSpan? timeout = null)
        {
            return PollAsync(conditions, expected, filterValue, searchArea, roomType, timeout);
        }

        /// <summary>
        /// Searches by the free text only, without a structured metadata filter.
        /// </summary>
        public Task<RoomsContentResponse> SearchByTextAsync(string text, int[] expected, TimeSpan? timeout = null)
        {
            return PollAsync(conditions: null, expected, filterValue: text, searchArea: null, roomType: null, timeout);
        }

        /// <summary>
        /// Requests the unfiltered rooms listing, retrying until the condition holds.
        /// </summary>
        public Task<RoomsContentResponse> ListAsync(Func<RoomsContentResponse, bool> until, int? searchArea = null, TimeSpan? timeout = null)
        {
            return PollAsync(until, conditions: null, filterValue: null, searchArea, roomType: null, timeout);
        }

        private Task<RoomsContentResponse> PollAsync(object[]? conditions, int[] expected, string? filterValue, int? searchArea, int? roomType, TimeSpan? timeout)
        {
            return PollAsync(r => r.RoomIds().Order().SequenceEqual(expected.Order()), conditions, filterValue, searchArea, roomType, timeout);
        }

        /// <summary>
        /// Requests the rooms listing, retrying until the condition holds.
        /// The metadata values are indexed right after they are written, the retry only absorbs the indexing lag.
        /// </summary>
        private async Task<RoomsContentResponse> PollAsync(Func<RoomsContentResponse, bool> until, object[]? conditions, string? filterValue, int? searchArea, int? roomType, TimeSpan? timeout)
        {
            using var deadline = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));

            while (true)
            {
                var rooms = await Api.GetRoomsAsync(
                    conditions == null ? null : TemplateId,
                    conditions,
                    filterValue,
                    searchArea,
                    roomType,
                    TestContext.Current.CancellationToken);

                if (until(rooms) || deadline.IsCancellationRequested)
                {
                    return rooms;
                }

                await Task.Delay(200, TestContext.Current.CancellationToken);
            }
        }
    }

    #endregion
}
