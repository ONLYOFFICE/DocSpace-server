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

namespace ASC.Core.Common.Tests;

/// <summary>
/// Data annotation coverage for the DocsCloud tenant configuration a portal owner can write through
/// PUT api/2.0/settings/docscloud/tenant/config. The validation runs the same way MVC runs it on the bound
/// model, so a rule that throws here answers 500 there instead of rejecting the request with 400.
/// </summary>
public class DocsCloudConfigValidationTests
{
    private const long MaxFileSizeLimit = 209715200;

    /// <summary>
    /// A file size limit above <see cref="int.MaxValue"/> used to be declared with the int overload of
    /// <see cref="RangeAttribute"/>, whose conversion overflowed on a long value — the attribute threw instead of
    /// reporting a validation error, so the request failed with 500 Internal Server Error. The range now matches
    /// the property type and the value is simply rejected.
    /// </summary>
    [Fact]
    [Trait("Bug", "83326")]
    public void FileSizeLimit_AboveInt32Range_IsRejectedInsteadOfThrowing()
    {
        Validate(9999999999).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(DocsCloudServerConfig.FileSizeLimit));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(104857600)]
    [InlineData(MaxFileSizeLimit)]
    public void FileSizeLimit_WithinTheAllowedRange_IsAccepted(long fileSizeLimit)
    {
        Validate(fileSizeLimit).Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(MaxFileSizeLimit + 1)]
    [InlineData(long.MaxValue)]
    public void FileSizeLimit_OutsideTheAllowedRange_IsRejected(long fileSizeLimit)
    {
        Validate(fileSizeLimit).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(DocsCloudServerConfig.FileSizeLimit));
    }

    /// <summary>
    /// Validates the server configuration the way MVC validates a bound model: every property, attribute by attribute.
    /// </summary>
    private static List<ValidationResult> Validate(long fileSizeLimit)
    {
        var config = new DocsCloudServerConfig { FileSizeLimit = fileSizeLimit };

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true);

        return results;
    }
}
