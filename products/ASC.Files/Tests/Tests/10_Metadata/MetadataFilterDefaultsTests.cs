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
/// Covers a condition that names no operator: the operator is read from the field type and the arguments ("eq" for a
/// string and for a number with a value, "range" for a number with bounds and for a date, "in" for a choice), and a
/// condition naming neither a field nor a custom field name is refused.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFilterDefaultsTests(AspireAppFixture fixture) : RoomsMetadataSearchTestsBase(fixture)
{
    [Fact]
    public async Task StringCondition_WithoutOp_IsAnExactMatch()
    {
        var data = await ArrangeAsync();

        var whole = await data.SearchAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(ClientField), value: "acme"), expected: [data.MatchingRoomId]);
        whole.RoomIds().Should().Equal(data.MatchingRoomId);

        // "contains" would find "ACME" by its prefix, "eq" does not
        var prefix = await data.SearchAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(ClientField), value: "ACM"), expected: []);
        prefix.Folders.Should().BeEmpty();
    }

    [Fact]
    public async Task NumberCondition_WithoutOp_AndAValue_IsAnExactMatch()
    {
        var data = await ArrangeAsync();

        var exact = await data.SearchAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), value: "150"), expected: [data.MatchingRoomId]);
        exact.RoomIds().Should().Equal(data.MatchingRoomId);

        var near = await data.SearchAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), value: "151"), expected: []);
        near.Folders.Should().BeEmpty();
    }

    [Fact]
    public async Task NumberCondition_WithoutOp_AndBounds_IsARange()
    {
        var data = await ArrangeAsync();

        // 150 and 900: the bounds take both, an exact match on either bound would take none
        var rooms = await data.SearchAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(AmountField), from: "100", to: "1000"), expected: [data.MatchingRoomId, data.PartialRoomId]);

        rooms.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId });
    }

    [Fact]
    public async Task DateCondition_WithoutOp_IsARange()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(new MetadataFilterConditionRequest(fieldId: data.FieldId(SignedField), from: "2026-03-01", to: "2026-03-31"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task ChoiceCondition_WithoutOp_IsAnyOfTheOptions()
    {
        var data = await ArrangeAsync();

        // Legal is held by the matching room, Urgent by the partial one: "in" joins the options with OR
        var multi = await data.SearchAsync(
            new MetadataFilterConditionRequest(fieldId: data.FieldId(TagsField), optionIds: [data.OptionId(TagsField, "Legal"), data.OptionId(TagsField, "Urgent")]),
            expected: [data.MatchingRoomId, data.PartialRoomId]);
        multi.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId });

        var single = await data.SearchAsync(
            new MetadataFilterConditionRequest(fieldId: data.FieldId(StatusField), optionIds: [data.OptionId(StatusField, "Signed")]),
            expected: [data.MatchingRoomId]);
        single.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Condition_WithNeitherAFieldIdNorAName_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsWithMetadataAsync(
                metadataTemplateId: data.TemplateId,
                metadataFilters: [new MetadataFilterConditionRequest(op: "eq", value: "ACME")],
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("fieldId or a name");
    }
}
