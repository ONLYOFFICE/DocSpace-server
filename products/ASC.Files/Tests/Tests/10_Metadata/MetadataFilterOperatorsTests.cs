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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the filter operators beyond the exact match and the inclusive range: "contains" on a string field, and the
/// one-sided "gt", "gte", "lt" and "lte" on number and date fields, with their date-only bound stepping over the day
/// as a whole. The listing takes the conditions as a JSON string, the typed search as a body; both roads are checked.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFilterOperatorsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";
    private const string AmountField = "Amount";
    private const string SignedField = "Signed";

    #region Contains

    [Fact]
    public async Task Contains_MatchesTheSubstringWithoutRegardToCase()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(ClientField, "contains", "acme"), [data.LowId, data.HighId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.LowId, data.HighId }, "\"ACME Corp\" and \"acme\" both hold the substring, \"Globex\" does not");
    }

    [Fact]
    public async Task Equal_MatchesTheWholeValueOnly()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(ClientField, "eq", "acme"), [data.HighId]);

        content.FolderIds().Should().Equal(new[] { data.HighId }, "the exact match leaves \"ACME Corp\" out, unlike \"contains\"");
    }

    [Fact]
    public async Task Contains_AgreesBetweenTheListingAndTheTypedSearch()
    {
        var data = await ArrangeAsync();
        var condition = data.Condition(ClientField, "contains", "acme");

        var listed = await data.SearchAsync(condition, [data.LowId, data.HighId]);
        var typed = await data.TypedSearchAsync(condition, [data.LowId, data.HighId]);

        typed.FolderIds().Should().BeEquivalentTo(listed.FolderIds());
        typed.Total.Should().Be(listed.Total);
    }

    [Fact]
    public async Task Contains_OnANumberField_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(data.Condition(AmountField, "contains", "20")));

        exception.ErrorCode.Should().Be(400, "a substring has no meaning for a number");
    }

    #endregion

    #region Number bounds

    [Fact]
    public async Task NumberGreaterThan_ExcludesTheBound()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(AmountField, "gt", "200"), [data.HighId]);

        content.FolderIds().Should().Equal(data.HighId);
    }

    [Fact]
    public async Task NumberGreaterThanOrEqual_IncludesTheBound()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(AmountField, "gte", "200"), [data.MidId, data.HighId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.MidId, data.HighId });
    }

    [Fact]
    public async Task NumberLessThan_ExcludesTheBound()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(AmountField, "lt", "200"), [data.LowId]);

        content.FolderIds().Should().Equal(data.LowId);
    }

    [Fact]
    public async Task NumberLessThanOrEqual_IncludesTheBound()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(AmountField, "lte", "200"), [data.LowId, data.MidId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.LowId, data.MidId });
    }

    [Fact]
    public async Task NumberBound_AgreesBetweenTheListingAndTheTypedSearch()
    {
        var data = await ArrangeAsync();
        var condition = data.Condition(AmountField, "lt", "300");

        var listed = await data.SearchAsync(condition, [data.LowId, data.MidId]);
        var typed = await data.TypedSearchAsync(condition, [data.LowId, data.MidId]);

        typed.FolderIds().Should().BeEquivalentTo(listed.FolderIds());
    }

    [Fact]
    public async Task NumberBound_GivenInARange_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // the bound of a one-sided operator comes in "value"; "from" and "to" belong to "range"
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), op: "gt", from: "200")));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task NumberBound_BeyondTheSafeInteger_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // 2^53: a 64-bit integer, but past what a JSON client reads exactly and past what a stored value may hold
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(data.Condition(AmountField, "lt", "9007199254740992")));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task NumberBound_WithoutAValue_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), op: "gt")));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task NumberEqual_WithABound_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // "eq" with bounds used to be read as a range; the named operator fixes the form instead
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), op: "eq", from: "200")));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task NumberRange_WithAStrayValue_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), op: "range", value: "200")));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GreaterThan_OnAStringField_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(data.Condition(ClientField, "gt", "acme")));

        exception.ErrorCode.Should().Be(400, "a string has no order to compare by");
    }

    #endregion

    #region Date bounds

    [Fact]
    public async Task DateGreaterThan_DateOnly_StartsAfterTheDay()
    {
        var data = await ArrangeAsync();

        // "low" was signed at 14:00 on 30 June: the strict bound given as a date steps over the whole day
        var content = await data.SearchAsync(data.Condition(SignedField, "gt", "2026-06-30"), [data.MidId, data.HighId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.MidId, data.HighId });
    }

    [Fact]
    public async Task DateGreaterThan_WithTime_IsAnInstant()
    {
        var data = await ArrangeAsync();

        // the same day, but a bound carrying a time is an instant: 14:00 is after noon
        var content = await data.SearchAsync(data.Condition(SignedField, "gt", "2026-06-30T12:00:00Z"), [data.LowId, data.MidId, data.HighId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.LowId, data.MidId, data.HighId });
    }

    [Fact]
    public async Task DateGreaterThanOrEqual_DateOnly_IncludesTheDay()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(SignedField, "gte", "2026-06-30"), [data.LowId, data.MidId, data.HighId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.LowId, data.MidId, data.HighId });
    }

    [Fact]
    public async Task DateLessThan_DateOnly_EndsBeforeTheDay()
    {
        var data = await ArrangeAsync();

        // "mid" was signed at 09:00 on 1 July: the strict bound given as a date ends before the day begins
        var content = await data.SearchAsync(data.Condition(SignedField, "lt", "2026-07-01"), [data.LowId]);

        content.FolderIds().Should().Equal(data.LowId);
    }

    [Fact]
    public async Task DateLessThanOrEqual_DateOnly_IncludesTheDay()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Condition(SignedField, "lte", "2026-07-01"), [data.LowId, data.MidId]);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.LowId, data.MidId });
    }

    [Fact]
    public async Task DateRange_WithAStrayValue_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // a "value" next to the bounds of a range used to be ignored; it is refused the way it is for a number
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(SignedField), op: "range", value: "2026-06-30", from: "2026-06-01")));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task DateBound_WithAMalformedValue_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await data.ListAsync(data.Condition(SignedField, "lt", "yesterday")));

        exception.ErrorCode.Should().Be(400);
    }

    #endregion

    #region Arrange

    /// <summary>
    /// Three folders of one room, ordered by every field: "low" holds "ACME Corp", 100 and 30 June 14:00, "mid" holds
    /// "Globex", 200 and 1 July 09:00, "high" holds "acme", 300 and 15 July 00:00 (all UTC).
    /// </summary>
    private async Task<OperatorsData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var suffix = Guid.NewGuid().ToString()[..8];

        var template = (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto("Operators " + suffix, true,
        [
            new MetadataFieldRequest(ClientField, MetadataFieldType.String),
            new MetadataFieldRequest(AmountField, MetadataFieldType.Number),
            new MetadataFieldRequest(SignedField, MetadataFieldType.Date)
        ]), TestContext.Current.CancellationToken)).Response;

        var room = await CreateCustomRoom($"Operators {suffix}");
        var low = await CreateFolder($"Low {suffix}", room.Id);
        var mid = await CreateFolder($"Mid {suffix}", room.Id);
        var high = await CreateFolder($"High {suffix}", room.Id);

        var data = new OperatorsData(_foldersApi, template, room.Id) { LowId = low.Id, MidId = mid.Id, HighId = high.Id };

        foreach (var folderId in new[] { low.Id, mid.Id, high.Id })
        {
            await _metadataApi.AssignFolderTemplatesAsync(folderId, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        }

        await _metadataApi.SetFolderValuesAsync(low.Id, data.Values("ACME Corp", 100, new DateTime(2026, 6, 30, 14, 0, 0, DateTimeKind.Utc)), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(mid.Id, data.Values("Globex", 200, new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(high.Id, data.Values("acme", 300, new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc)), TestContext.Current.CancellationToken);

        // the values reach the index with a small lag; a negative search polled before that lag is over would
        // pass for the wrong reason, so every test starts from a state where the written values are searchable
        var indexed = await data.SearchAsync(data.Condition(ClientField, "eq", "Globex"), [mid.Id]);
        indexed.FolderIds().Should().Equal(new[] { mid.Id }, "the arranged values must be searchable before the test starts");

        return data;
    }

    private sealed class OperatorsData(FoldersApi foldersApi, MetadataTemplateDto template, int roomId)
    {
        public int LowId { get; init; }
        public int MidId { get; init; }
        public int HighId { get; init; }

        public int FieldId(string name)
        {
            return template.Field(name).Id;
        }

        public MetadataFilterConditionRequest Condition(string fieldName, string op, string value)
        {
            return new MetadataFilterConditionRequest(fieldId: FieldId(fieldName), op: op, value: value);
        }

        public SetMetadataValues Values(string client, long amount, DateTime signed)
        {
            return new SetMetadataValues(
            [
                new MetadataValueRequest(FieldId(ClientField), stringValue: client),
                new MetadataValueRequest(FieldId(AmountField), numberValue: amount),
                new MetadataValueRequest(FieldId(SignedField), dateValue: signed)
            ]);
        }

        /// <summary>
        /// Lists the room with the condition in the query once, without waiting for any result.
        /// </summary>
        public Task<FolderContentResponse> ListAsync(MetadataFilterConditionRequest condition)
        {
            return foldersApi.GetFolderContentWithMetadataAsync(roomId, template.Id, [condition], cancellationToken: TestContext.Current.CancellationToken);
        }

        /// <summary>
        /// Lists the room with the condition in the query, retrying until the expected folders are returned; the
        /// retry only absorbs the indexing lag, the last observed state is returned for the assertion.
        /// </summary>
        public Task<FolderContentResponse> SearchAsync(MetadataFilterConditionRequest condition, int[] expected)
        {
            return PollAsync(() => ListAsync(condition), expected);
        }

        /// <summary>
        /// The same condition through the typed search body.
        /// </summary>
        public Task<FolderContentResponse> TypedSearchAsync(MetadataFilterConditionRequest condition, int[] expected)
        {
            var body = new FolderMetadataSearch(metadataTemplateId: template.Id, metadataFilters: [condition]);

            return PollAsync(() => foldersApi.SearchFolderWithMetadataAsync(roomId, body, TestContext.Current.CancellationToken), expected);
        }

        private static async Task<FolderContentResponse> PollAsync(Func<Task<FolderContentResponse>> read, int[] expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);

            while (true)
            {
                var content = await read();

                if (content.FolderIds().Order().SequenceEqual(expected.Order()) || DateTime.UtcNow >= deadline)
                {
                    return content;
                }

                await Task.Delay(200, TestContext.Current.CancellationToken);
            }
        }
    }

    #endregion
}
