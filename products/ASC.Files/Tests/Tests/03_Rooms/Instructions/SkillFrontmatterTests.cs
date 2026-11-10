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

namespace ASC.Files.Tests.Tests._03_Rooms.Instructions;

/// <summary>
/// The frontmatter of a skill file is read from the first bytes of the file when it is written into an
/// Instructions room: an opening <c>---</c> on the first line, a closing <c>---</c> on a line of its own,
/// a YAML mapping between them with a <c>name</c> and a <c>description</c>. Anything else is rejected.
/// </summary>
public class SkillFrontmatterTests
{
    [Fact]
    public void TryParse_ShouldReturnNameAndDescription_WhenFrontmatterIsValid()
    {
        var error = Parse("---\nname: pdf-review\ndescription: Reviews PDF documents\n---\n# Body\n", out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Name.Should().Be("pdf-review");
        frontmatter.Description.Should().Be("Reviews PDF documents");
    }

    [Fact]
    public void TryParse_ShouldAcceptBomAndCrlf()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat("---\r\nname: pdf-review\r\ndescription: Reviews PDF documents\r\n---\r\nbody"u8.ToArray())
            .ToArray();

        var error = SkillFrontmatter.TryParse(bytes, out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Name.Should().Be("pdf-review");
    }

    [Fact]
    public void TryParse_ShouldAcceptFoldedDescription_AndIgnoreUnknownKeys()
    {
        var error = Parse(
            """
            ---
            name: pdf-review
            description: >
              Reviews PDF documents
              and finds mistakes.
            license: MIT
            allowed-tools: [Read, Grep]
            metadata:
              author: someone
            ---
            """,
            out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Description.Should().Be("Reviews PDF documents and finds mistakes.");
    }

    [Fact]
    public void TryParse_ShouldTrimValues()
    {
        var error = Parse("---\nname: \"  pdf-review  \"\ndescription: \"  Reviews  \"\n---\n", out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Name.Should().Be("pdf-review");
        frontmatter.Description.Should().Be("Reviews");
    }

    [Fact]
    public void TryParse_ShouldAcceptUnicodeLowercaseName()
    {
        var error = Parse("---\nname: обзор-pdf-文档\ndescription: Reviews\n---\n", out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Name.Should().Be("обзор-pdf-文档");
    }

    [Fact]
    public void TryParse_ShouldAcceptNumericName()
    {
        var error = Parse("---\nname: 2024\ndescription: Reviews\n---\n", out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Name.Should().Be("2024");
    }

    [Theory]
    [InlineData("")]
    [InlineData("# Just a heading\n---\nname: a\ndescription: b\n---\n")]
    [InlineData(" ---\nname: a\ndescription: b\n---\n")]
    [InlineData("--- yaml\nname: a\ndescription: b\n---\n")]
    [InlineData("---")]
    [InlineData("---\nname: a\ndescription: b\n")]
    [InlineData("---\nname: a\ndescription: b\n--- \n")]
    [InlineData("---\nname: a\ndescription: b\n----\n")]
    [InlineData("---\nname: a\ndescription: b ---\n")]
    public void TryParse_ShouldReportMissing_WhenDelimitersAreNotFound(string content)
    {
        Parse(content, out var frontmatter).Should().Be(SkillFrontmatterError.Missing);
        frontmatter.Should().BeNull();
    }

    [Theory]
    [InlineData("---\n---\n")]
    [InlineData("---\n\n---\n")]
    [InlineData("---\n- name: a\n- description: b\n---\n")]
    [InlineData("---\njust a scalar\n---\n")]
    [InlineData("---\nname: a\ndescription: [unclosed\n---\n")]
    [InlineData("---\nname: a\nname: b\ndescription: c\n---\n")]
    [InlineData("---\nname: a\n  description: b\n---\n")]
    public void TryParse_ShouldReportMissing_WhenYamlIsNotAMapping(string content)
    {
        Parse(content, out _).Should().Be(SkillFrontmatterError.Missing);
    }

    [Fact]
    public void TryParse_ShouldReportMissing_WhenFrontmatterIsNotUtf8()
    {
        var bytes = "---\nname: a\ndescription: "u8.ToArray()
            .Concat(new byte[] { 0xC3, 0x28 })
            .Concat("\n---\n"u8.ToArray())
            .ToArray();

        SkillFrontmatter.TryParse(bytes, out _).Should().Be(SkillFrontmatterError.Missing);
    }

    [Fact]
    public void TryParse_ShouldReportMissing_WhenFileIsUtf16()
    {
        var bytes = Encoding.Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes("---\nname: a\ndescription: b\n---\n"))
            .ToArray();

        SkillFrontmatter.TryParse(bytes, out _).Should().Be(SkillFrontmatterError.Missing);
    }

    [Theory]
    [InlineData("description: b")]
    [InlineData("name:\ndescription: b")]
    [InlineData("name: null\ndescription: b")]
    [InlineData("name: ~\ndescription: b")]
    [InlineData("name: \"\"\ndescription: b")]
    [InlineData("name: \"   \"\ndescription: b")]
    [InlineData("name: [a, b]\ndescription: b")]
    [InlineData("name:\n  first: a\ndescription: b")]
    [InlineData("name: Pdf-Review\ndescription: b")]
    [InlineData("name: pdf review\ndescription: b")]
    [InlineData("name: pdf_review\ndescription: b")]
    [InlineData("name: -pdf-review\ndescription: b")]
    [InlineData("name: pdf-review-\ndescription: b")]
    [InlineData("name: pdf--review\ndescription: b")]
    [InlineData("name: pdf.review\ndescription: b")]
    [InlineData("name: <b>pdf</b>\ndescription: b")]
    [InlineData("name: ПДФ\ndescription: b")]
    public void TryParse_ShouldReportInvalidName(string yaml)
    {
        Parse($"---\n{yaml}\n---\n", out var frontmatter).Should().Be(SkillFrontmatterError.InvalidName);
        frontmatter.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldReportInvalidName_WhenLongerThanLimit()
    {
        var name = new string('a', SkillFrontmatter.MaxNameLength + 1);

        Parse($"---\nname: {name}\ndescription: b\n---\n", out _).Should().Be(SkillFrontmatterError.InvalidName);
        Parse($"---\nname: {name[..^1]}\ndescription: b\n---\n", out _).Should().Be(SkillFrontmatterError.None);
    }

    [Theory]
    [InlineData("name: a")]
    [InlineData("name: a\ndescription:")]
    [InlineData("name: a\ndescription: null")]
    [InlineData("name: a\ndescription: \"  \"")]
    [InlineData("name: a\ndescription: [a, b]")]
    [InlineData("name: a\ndescription:\n  text: b")]
    [InlineData("name: a\ndescription: Use <tool>Read</tool> first")]
    [InlineData("name: a\ndescription: Ends with a <br/>")]
    public void TryParse_ShouldReportInvalidDescription(string yaml)
    {
        Parse($"---\n{yaml}\n---\n", out var frontmatter).Should().Be(SkillFrontmatterError.InvalidDescription);
        frontmatter.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldReportInvalidDescription_WhenLongerThanLimit()
    {
        var description = new string('b', SkillFrontmatter.MaxDescriptionLength + 1);

        Parse($"---\nname: a\ndescription: {description}\n---\n", out _).Should().Be(SkillFrontmatterError.InvalidDescription);
        Parse($"---\nname: a\ndescription: {description[..^1]}\n---\n", out _).Should().Be(SkillFrontmatterError.None);
    }

    [Fact]
    public void TryParse_ShouldAllowComparisonSigns_InDescription()
    {
        var error = Parse("---\nname: a\ndescription: Use when x < 10 and y > 2\n---\n", out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Description.Should().Be("Use when x < 10 and y > 2");
    }

    [Fact]
    public void TryParse_ShouldCheckNameBeforeDescription()
    {
        Parse("---\nname: Bad Name\n---\n", out _).Should().Be(SkillFrontmatterError.InvalidName);
    }

    [Fact]
    public void TryParse_ShouldIgnoreDelimiterInsideBody()
    {
        var error = Parse("---\nname: a\ndescription: b\n---\n\n---\n\nSecond rule\n", out var frontmatter);

        error.Should().Be(SkillFrontmatterError.None);
        frontmatter!.Name.Should().Be("a");
    }

    private static SkillFrontmatterError Parse(string content, out SkillFrontmatter? frontmatter)
    {
        return SkillFrontmatter.TryParse(Encoding.UTF8.GetBytes(content), out frontmatter);
    }
}
