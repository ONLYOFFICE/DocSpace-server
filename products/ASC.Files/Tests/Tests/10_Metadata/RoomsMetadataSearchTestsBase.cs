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
/// The arranged template and rooms shared by the rooms metadata search suites: the field type filters live in
/// <see cref="RoomsMetadataFieldFilterTests"/>, everything around them in <see cref="RoomsMetadataSearchTests"/>.
/// </summary>
/// <remarks>
/// Both the OpenSearch path and its SQL fallback must produce the same result, so these tests are valid
/// regardless of whether the metadata index is reachable during the run.
/// </remarks>
public abstract class RoomsMetadataSearchTestsBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const SearchArea SearchAreaArchive = SearchArea.Archive;
    protected const RoomType CustomRoomType = RoomType.CustomRoom;

    protected const string ClientField = "Client";
    protected const string SignedField = "Signed";
    protected const string AmountField = "Amount";
    protected const string StatusField = "Status";
    protected const string TagsField = "Tags";

    protected static readonly DateTime _matchingDate = new(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
    protected static readonly DateTime _otherDate = new(2027, 1, 20, 0, 0, 0, DateTimeKind.Utc);

    #region Arrange

    protected async Task<MetadataSearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await CreateTemplateAsync("Contracts " + suffix,
        [
            new MetadataFieldRequest(ClientField, MetadataFieldType.String),
            new MetadataFieldRequest(SignedField, MetadataFieldType.Date),
            new MetadataFieldRequest(AmountField, MetadataFieldType.Number),
            new MetadataFieldRequest(StatusField, MetadataFieldType.SingleChoice,
                [new MetadataFieldOptionRequest(value: "Draft"), new MetadataFieldOptionRequest(value: "Signed")]),
            new MetadataFieldRequest(TagsField, MetadataFieldType.MultiChoice,
            [
                new MetadataFieldOptionRequest(value: "Legal"),
                new MetadataFieldOptionRequest(value: "Finance"),
                new MetadataFieldOptionRequest(value: "Urgent")
            ])
        ]);

        var matching = await CreateCustomRoom($"Matching {suffix}");
        var partial = await CreateCustomRoom($"Partial {suffix}");
        var bare = await CreateCustomRoom($"Bare {suffix}");

        var data = new MetadataSearchData(_roomsApi, template)
        {
            MatchingRoomId = matching.Id,
            PartialRoomId = partial.Id,
            BareRoomId = bare.Id,
            BareRoomTitle = bare.Title
        };

        foreach (var roomId in new[] { matching.Id, partial.Id })
        {
            await _metadataApi.AssignFolderTemplatesAsync(roomId, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        }

        await _metadataApi.SetFolderValuesAsync(matching.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(data.FieldId(ClientField), stringValue: "ACME"),
            new MetadataValueRequest(data.FieldId(SignedField), dateValue: _matchingDate),
            new MetadataValueRequest(data.FieldId(AmountField), numberValue: 150),
            new MetadataValueRequest(data.FieldId(StatusField), optionIds: [data.OptionId(StatusField, "Signed")]),
            new MetadataValueRequest(data.FieldId(TagsField), optionIds: [data.OptionId(TagsField, "Legal"), data.OptionId(TagsField, "Finance")])
        ]), TestContext.Current.CancellationToken);

        await _metadataApi.SetFolderValuesAsync(partial.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(data.FieldId(ClientField), stringValue: "Globex"),
            new MetadataValueRequest(data.FieldId(SignedField), dateValue: _otherDate),
            new MetadataValueRequest(data.FieldId(AmountField), numberValue: 900),
            new MetadataValueRequest(data.FieldId(StatusField), optionIds: [data.OptionId(StatusField, "Draft")]),
            new MetadataValueRequest(data.FieldId(TagsField), optionIds: [data.OptionId(TagsField, "Urgent")])
        ]), TestContext.Current.CancellationToken);

        // the values reach the index with a small lag; a negative search polled before that lag is over would
        // pass for the wrong reason, so every test starts from a state where the written values are searchable
        var indexed = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId]);
        indexed.RoomIds().Should().Equal(new[] { data.MatchingRoomId }, "the arranged values must be searchable before the test starts");

        return data;
    }

    protected async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// The arranged template and rooms plus the condition builders and the polling request helper.
    /// </summary>
    protected sealed class MetadataSearchData(RoomsApi roomsApi, MetadataTemplateDto template)
    {
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

        public MetadataFilterConditionRequest Eq(string fieldName, string value)
        {
            return new MetadataFilterConditionRequest(fieldId: FieldId(fieldName), op: "eq", value: value);
        }

        public MetadataFilterConditionRequest Range(string fieldName, long? from, long? to)
        {
            return new MetadataFilterConditionRequest(
                fieldId: FieldId(fieldName),
                op: "range",
                from: from?.ToString(CultureInfo.InvariantCulture)!,
                to: to?.ToString(CultureInfo.InvariantCulture)!);
        }

        public MetadataFilterConditionRequest DateRange(string fieldName, DateTime from, DateTime to)
        {
            return new MetadataFilterConditionRequest(
                fieldId: FieldId(fieldName),
                op: "range",
                from: from.ToString("O", CultureInfo.InvariantCulture),
                to: to.ToString("O", CultureInfo.InvariantCulture));
        }

        public MetadataFilterConditionRequest In(string fieldName, params string[] optionValues)
        {
            return new MetadataFilterConditionRequest(
                fieldId: FieldId(fieldName),
                op: "in",
                optionIds: optionValues.Select(v => OptionId(fieldName, v)).ToList());
        }

        public Task<RoomsContentResponse> SearchAsync(MetadataFilterConditionRequest condition, int[] expected, string? filterValue = null, SearchArea? searchArea = null, RoomType? roomType = null, TimeSpan? timeout = null)
        {
            return PollAsync([condition], expected, filterValue, searchArea, roomType, timeout);
        }

        public Task<RoomsContentResponse> SearchAsync(MetadataFilterConditionRequest[] conditions, int[] expected, string? filterValue = null, SearchArea? searchArea = null, RoomType? roomType = null, TimeSpan? timeout = null)
        {
            return PollAsync(conditions, expected, filterValue, searchArea, roomType, timeout);
        }

        /// <summary>
        /// Searches by the free text only, without a structured metadata filter.
        /// </summary>
        public Task<RoomsContentResponse> SearchByTextAsync(string text, int[] expected, TimeSpan? timeout = null, bool? includeHiddenMetadata = null)
        {
            return PollAsync(conditions: null, expected, filterValue: text, searchArea: null, roomType: null, timeout, includeHiddenMetadata);
        }

        /// <summary>
        /// Requests the unfiltered rooms listing, retrying until the condition holds.
        /// </summary>
        public Task<RoomsContentResponse> ListAsync(Func<RoomsContentResponse, bool> until, SearchArea? searchArea = null, TimeSpan? timeout = null)
        {
            return PollAsync(until, conditions: null, filterValue: null, searchArea, roomType: null, timeout);
        }

        private Task<RoomsContentResponse> PollAsync(MetadataFilterConditionRequest[]? conditions, int[] expected, string? filterValue, SearchArea? searchArea, RoomType? roomType, TimeSpan? timeout, bool? includeHiddenMetadata = null)
        {
            return PollAsync(r => r.RoomIds().Order().SequenceEqual(expected.Order()), conditions, filterValue, searchArea, roomType, timeout, includeHiddenMetadata);
        }

        /// <summary>
        /// Requests the rooms listing, retrying until the condition holds.
        /// The metadata values are indexed right after they are written, the retry only absorbs the indexing lag.
        /// </summary>
        private async Task<RoomsContentResponse> PollAsync(Func<RoomsContentResponse, bool> until, MetadataFilterConditionRequest[]? conditions, string? filterValue, SearchArea? searchArea, RoomType? roomType, TimeSpan? timeout, bool? includeHiddenMetadata = null)
        {
            using var deadline = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));

            while (true)
            {
                var rooms = await roomsApi.GetRoomsWithMetadataAsync(
                    metadataTemplateId: conditions == null ? null : TemplateId,
                    metadataFilters: conditions,
                    filterValue: filterValue,
                    searchArea: searchArea,
                    type: roomType.HasValue ? new List<RoomType> { roomType.Value } : null,
                    includeHiddenMetadata: includeHiddenMetadata,
                    cancellationToken: TestContext.Current.CancellationToken);

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
