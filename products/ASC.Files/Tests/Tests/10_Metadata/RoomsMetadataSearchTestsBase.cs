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
    protected const int SearchAreaArchive = 1;
    protected const int CustomRoomType = 5;

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

        // the values reach the index with a small lag; a negative search polled before that lag is over would
        // pass for the wrong reason, so every test starts from a state where the written values are searchable
        var indexed = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId]);
        indexed.RoomIds().Should().Equal(new[] { data.MatchingRoomId }, "the arranged values must be searchable before the test starts");

        return data;
    }

    /// <summary>
    /// The arranged template and rooms plus the condition builders and the polling request helper.
    /// </summary>
    protected sealed class MetadataSearchData(MetadataApiClient api, MetadataTemplateResponse template)
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
