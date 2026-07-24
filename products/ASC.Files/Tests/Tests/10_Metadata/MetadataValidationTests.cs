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

using MetadataField = ASC.Files.Core.MetadataField;
using MetadataFieldOption = ASC.Files.Core.MetadataFieldOption;
using MetadataFieldType = ASC.Files.Core.MetadataFieldType;
using MetadataService = ASC.Files.Core.MetadataService;
using MetadataValue = ASC.Files.Core.MetadataValue;

namespace ASC.Files.Tests.Tests._10_Metadata;

public class MetadataValidationTests
{
    private static readonly Guid _redOptionId = Guid.NewGuid();
    private static readonly Guid _blueOptionId = Guid.NewGuid();

    [Fact]
    public void ValidateValue_ShouldPass_WhenStringFieldHasStringValue()
    {
        var field = CreateField(MetadataFieldType.String);
        var value = new MetadataValue { FieldId = 1, StringValue = "ACME Corp" };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateValue_ShouldThrow_WhenStringFieldHasNumberValue()
    {
        var field = CreateField(MetadataFieldType.String);
        var value = new MetadataValue { FieldId = 1, StringValue = "x", NumberValue = 42 };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateValue_ShouldPassAndNormalizeToUtc_WhenDateFieldHasLocalDate()
    {
        var field = CreateField(MetadataFieldType.Date);
        var value = new MetadataValue { FieldId = 1, DateValue = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Local) };

        MetadataService.ValidateValue(field, value);

        value.DateValue!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ValidateValue_ShouldThrow_WhenDateFieldHasStringValue()
    {
        var field = CreateField(MetadataFieldType.Date);
        var value = new MetadataValue { FieldId = 1, StringValue = "2026-01-15" };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateValue_ShouldPass_WhenNumberFieldHasNumberValue()
    {
        var field = CreateField(MetadataFieldType.Number);
        var value = new MetadataValue { FieldId = 1, NumberValue = 42 };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateValue_ShouldThrow_WhenNumberFieldHasDateValue()
    {
        var field = CreateField(MetadataFieldType.Number);
        var value = new MetadataValue { FieldId = 1, NumberValue = 1, DateValue = DateTime.UtcNow };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateValue_ShouldPass_WhenSingleChoiceHasOneKnownOption()
    {
        var field = CreateChoiceField(MetadataFieldType.SingleChoice);
        var value = new MetadataValue { FieldId = 1, OptionIds = [_redOptionId] };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateValue_ShouldThrow_WhenSingleChoiceHasTwoOptions()
    {
        var field = CreateChoiceField(MetadataFieldType.SingleChoice);
        var value = new MetadataValue { FieldId = 1, OptionIds = [_redOptionId, _blueOptionId] };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateValue_ShouldThrow_WhenChoiceOptionIsUnknown()
    {
        var field = CreateChoiceField(MetadataFieldType.MultiChoice);
        var value = new MetadataValue { FieldId = 1, OptionIds = [Guid.NewGuid()] };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateValue_ShouldPass_WhenMultiChoiceHasSeveralKnownOptions()
    {
        var field = CreateChoiceField(MetadataFieldType.MultiChoice);
        var value = new MetadataValue { FieldId = 1, OptionIds = [_redOptionId, _blueOptionId] };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateValue_ShouldPass_WhenValueIsEmpty()
    {
        var field = CreateChoiceField(MetadataFieldType.SingleChoice);
        var value = new MetadataValue { FieldId = 1 };

        var act = () => MetadataService.ValidateValue(field, value);

        act.Should().NotThrow();
        value.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData("text", null, null, false)]
    [InlineData(null, 42L, null, false)]
    [InlineData(null, null, "2026-01-15", false)]
    [InlineData(null, null, null, true)]
    [InlineData("", null, null, true)]
    public void IsEmpty_ShouldDetectEmptyValues(string stringValue, long? numberValue, string dateValue, bool expected)
    {
        var value = new MetadataValue
        {
            FieldId = 1,
            StringValue = stringValue,
            NumberValue = numberValue,
            DateValue = dateValue == null ? null : DateTime.Parse(dateValue, System.Globalization.CultureInfo.InvariantCulture)
        };

        value.IsEmpty.Should().Be(expected);
    }

    [Fact]
    public void IsEmpty_ShouldBeFalse_WhenOptionsAreSelected()
    {
        var value = new MetadataValue { FieldId = 1, OptionIds = [_redOptionId] };

        value.IsEmpty.Should().BeFalse();
    }

    private static MetadataField CreateField(MetadataFieldType type)
    {
        return new MetadataField { Id = 1, TemplateId = 1, Name = "Field", Type = type };
    }

    private static MetadataField CreateChoiceField(MetadataFieldType type)
    {
        return new MetadataField
        {
            Id = 1,
            TemplateId = 1,
            Name = "Field",
            Type = type,
            Options =
            [
                new MetadataFieldOption(_redOptionId, "Red"),
                new MetadataFieldOption(_blueOptionId, "Blue")
            ]
        };
    }
}
