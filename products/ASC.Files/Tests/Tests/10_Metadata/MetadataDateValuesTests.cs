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
/// Covers the date values: a date written without an offset must be found by a date-only filter,
/// i.e. the write and the filter must agree on the time zone of an offset-less value.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataDateValuesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string SignedField = "Signed";

    [Fact]
    public async Task DateValue_WrittenWithoutOffset_IsFoundByTheSameDayFilter()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Dates " + suffix,
            [new MetadataFieldPayload { Name = SignedField, Type = 1 }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Dated {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        // an offset-less date is serialized as "2026-01-15T00:00:00": the filter treats such strings as UTC,
        // so the write must do the same instead of applying the server time zone
        var midnight = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);

        await api.SetFolderValuesAsync(room.Id,
            [new MetadataValuePayload { FieldId = template.Field(SignedField).Id, DateValue = midnight }],
            TestContext.Current.CancellationToken);

        var stored = await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken);
        stored.Single(m => m.Id == template.Id).Field(SignedField).Value!.DateValue!.Value.UtcDateTime
            .Should().Be(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        var condition = new { fieldId = template.Field(SignedField).Id, from = "2026-01-15", to = "2026-01-15" };

        var rooms = await PollRoomsAsync(api, template.Id, condition, room.Id);

        rooms.RoomIds().Should().Contain(room.Id, "the day the value was written on must match the day filter");
    }

    [Fact]
    public async Task DateValue_WithTime_IsFoundByTheDateOnlyRangeEndingOnItsDay()
    {
        var (api, template, room) = await ArrangeAsync(new DateTime(2026, 6, 30, 14, 0, 0, DateTimeKind.Utc));

        // the UI sends the range as two dates: "to" without a time must cover the whole day, not stop at its midnight
        var condition = new { fieldId = template.Field(SignedField).Id, from = "2026-06-01", to = "2026-06-30" };

        var rooms = await PollRoomsAsync(api, template.Id, condition, room.Id);

        rooms.RoomIds().Should().Contain(room.Id, "a date-only upper bound is inclusive of the whole day");
    }

    [Fact]
    public async Task DateValue_WithTime_IsNotFoundByAnUpperBoundInstantBeforeIt()
    {
        var (api, template, room) = await ArrangeAsync(new DateTime(2026, 6, 30, 14, 0, 0, DateTimeKind.Utc));

        // arrange check: the value is indexed and found by the day, so the miss below is the bound, not the index lagging
        var byDay = await PollRoomsAsync(api, template.Id, new { fieldId = template.Field(SignedField).Id, to = "2026-06-30" }, room.Id);
        byDay.RoomIds().Should().Contain(room.Id);

        var condition = new { fieldId = template.Field(SignedField).Id, to = "2026-06-30T12:00:00Z" };

        var rooms = await api.GetRoomsAsync(template.Id, [condition], cancellationToken: TestContext.Current.CancellationToken);

        rooms.RoomIds().Should().NotContain(room.Id, "a bound carrying a time is an instant and is not stretched to the end of the day");
    }

    private async Task<(MetadataApiClient Api, MetadataTemplateResponse Template, FolderDtoInteger Room)> ArrangeAsync(DateTime value)
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Dates " + suffix,
            [new MetadataFieldPayload { Name = SignedField, Type = 1 }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Dated {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id,
            [new MetadataValuePayload { FieldId = template.Field(SignedField).Id, DateValue = value }],
            TestContext.Current.CancellationToken);

        return (api, template, room);
    }

    private static async Task<RoomsContentResponse> PollRoomsAsync(MetadataApiClient api, int templateId, object condition, int expectedRoomId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var rooms = await api.GetRoomsAsync(templateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (rooms.RoomIds().Contains(expectedRoomId) || DateTime.UtcNow > deadline)
            {
                return rooms;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }
}
