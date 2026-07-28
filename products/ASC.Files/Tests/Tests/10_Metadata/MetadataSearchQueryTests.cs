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

using DbFilesMetadataValue = ASC.Files.Core.EF.DbFilesMetadataValue;
using MetadataFieldType = ASC.Files.Core.MetadataFieldType;
using MetadataFilterCondition = ASC.Files.Core.MetadataFilterCondition;
using MetadataSearchQuery = ASC.Web.Files.Core.Search.MetadataSearchQuery;

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the condition predicate shared by the OpenSearch metadata queries and their SQL fallback.
/// </summary>
public class MetadataSearchQueryTests
{
    private const int FieldId = 100;
    private const int OtherFieldId = 200;

    private static bool Matches(MetadataFilterCondition condition, DbFilesMetadataValue value)
    {
        return MetadataSearchQuery.BuildConditionPredicate(condition).Compile()(value);
    }

    private static DbFilesMetadataValue String(string value, int fieldId = FieldId)
    {
        return new DbFilesMetadataValue { FieldId = fieldId, OptionId = "", ValueString = value };
    }

    private static DbFilesMetadataValue Number(long value, int fieldId = FieldId)
    {
        return new DbFilesMetadataValue { FieldId = fieldId, OptionId = "", ValueNumber = value };
    }

    private static DbFilesMetadataValue Date(DateTime value, int fieldId = FieldId)
    {
        return new DbFilesMetadataValue { FieldId = fieldId, OptionId = "", ValueDate = value };
    }

    private static DbFilesMetadataValue Option(Guid optionId, int fieldId = FieldId)
    {
        return new DbFilesMetadataValue { FieldId = fieldId, OptionId = optionId.ToString() };
    }

    #region String

    [Fact]
    public void String_ShouldMatch_OnExactValue()
    {
        // the helper lowers the requested value, the stored value is lowered by the predicate itself
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.String, StringValue = "acme" };

        Matches(condition, String("acme")).Should().BeTrue();
        Matches(condition, String("ACME")).Should().BeTrue();
        Matches(condition, String("AcMe")).Should().BeTrue();
    }

    [Fact]
    public void String_ShouldNotMatch_OnSubstring()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.String, StringValue = "acme" };

        Matches(condition, String("acme corp")).Should().BeFalse();
        Matches(condition, String("the acme")).Should().BeFalse();
    }

    [Fact]
    public void String_ShouldNotMatch_WhenFieldIsDifferent()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.String, StringValue = "acme" };

        Matches(condition, String("acme", OtherFieldId)).Should().BeFalse();
    }

    #endregion

    #region Number

    [Fact]
    public void Number_ShouldMatch_WithinRangeInclusively()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Number, NumberFrom = 10, NumberTo = 20 };

        Matches(condition, Number(10)).Should().BeTrue();
        Matches(condition, Number(15)).Should().BeTrue();
        Matches(condition, Number(20)).Should().BeTrue();
        Matches(condition, Number(9)).Should().BeFalse();
        Matches(condition, Number(21)).Should().BeFalse();
    }

    [Fact]
    public void Number_ShouldMatch_WithOpenUpperBound()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Number, NumberFrom = 10 };

        Matches(condition, Number(10)).Should().BeTrue();
        Matches(condition, Number(1_000_000)).Should().BeTrue();
        Matches(condition, Number(9)).Should().BeFalse();
    }

    [Fact]
    public void Number_ShouldMatch_WithOpenLowerBound()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Number, NumberTo = 20 };

        Matches(condition, Number(20)).Should().BeTrue();
        Matches(condition, Number(-5)).Should().BeTrue();
        Matches(condition, Number(21)).Should().BeFalse();
    }

    [Fact]
    public void Number_ShouldNotMatch_WhenFieldIsDifferent()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Number, NumberFrom = 10, NumberTo = 20 };

        Matches(condition, Number(15, OtherFieldId)).Should().BeFalse();
    }

    #endregion

    #region Date

    [Fact]
    public void Date_ShouldMatch_WithinRangeInclusively()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Date, DateFrom = from, DateTo = to };

        Matches(condition, Date(from)).Should().BeTrue();
        Matches(condition, Date(to)).Should().BeTrue();
        Matches(condition, Date(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc))).Should().BeTrue();
        Matches(condition, Date(from.AddDays(-1))).Should().BeFalse();
        Matches(condition, Date(to.AddDays(1))).Should().BeFalse();
    }

    [Fact]
    public void Date_ShouldMatch_WithOpenUpperBound()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Date, DateFrom = from };

        Matches(condition, Date(from)).Should().BeTrue();
        Matches(condition, Date(from.AddYears(10))).Should().BeTrue();
        Matches(condition, Date(from.AddSeconds(-1))).Should().BeFalse();
    }

    [Fact]
    public void Date_ShouldNotMatch_WhenFieldIsDifferent()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.Date, DateFrom = from };

        Matches(condition, Date(from, OtherFieldId)).Should().BeFalse();
    }

    #endregion

    #region Choice

    [Theory]
    [InlineData(MetadataFieldType.SingleChoice)]
    [InlineData(MetadataFieldType.MultiChoice)]
    public void Choice_ShouldMatch_AnyOfTheRequestedOptions(MetadataFieldType fieldType)
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var foreign = Guid.NewGuid();

        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = fieldType, OptionIds = [first, second] };

        Matches(condition, Option(first)).Should().BeTrue();
        Matches(condition, Option(second)).Should().BeTrue();
        Matches(condition, Option(foreign)).Should().BeFalse();
    }

    [Fact]
    public void Choice_ShouldNotMatch_WhenFieldIsDifferent()
    {
        var optionId = Guid.NewGuid();
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.SingleChoice, OptionIds = [optionId] };

        Matches(condition, Option(optionId, OtherFieldId)).Should().BeFalse();
    }

    [Fact]
    public void Choice_ShouldNotMatch_AValueRowWithoutOption()
    {
        // a string/number/date row is stored with an empty option id, it must never satisfy a choice condition
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = MetadataFieldType.MultiChoice, OptionIds = [Guid.NewGuid()] };

        Matches(condition, String("acme")).Should().BeFalse();
    }

    #endregion

    [Fact]
    public void BuildConditionPredicate_ShouldThrow_OnUnknownFieldType()
    {
        var condition = new MetadataFilterCondition { FieldId = FieldId, FieldType = (MetadataFieldType)42 };

        var act = () => MetadataSearchQuery.BuildConditionPredicate(condition);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
