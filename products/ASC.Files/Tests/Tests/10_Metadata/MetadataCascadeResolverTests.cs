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

using MetadataCascadeResolver = ASC.Files.Core.MetadataCascadeResolver;

namespace ASC.Files.Tests.Tests._10_Metadata;

public class MetadataCascadeResolverTests
{
    // tree: room 10 (level 2) -> folder 20 (level 1) -> folder 30 (level 0, the parent of the entry)
    private static readonly Dictionary<int, int> _levels = new() { { 10, 2 }, { 20, 1 }, { 30, 0 } };

    [Fact]
    public void ResolveNearestSources_ShouldPickNearestAncestor_WhenSameTemplateCascadesAtSeveralLevels()
    {
        var links = new List<(int TemplateId, int FolderId)> { (1, 10), (1, 20) };

        var sources = MetadataCascadeResolver.ResolveNearestSources(links, _levels);

        sources.Should().ContainKey(1).WhoseValue.Should().Be(20);
    }

    [Fact]
    public void ResolveNearestSources_ShouldBeDeterministic_RegardlessOfInputOrder()
    {
        var forward = new List<(int, int)> { (1, 10), (1, 20), (1, 30) };
        var backward = new List<(int, int)> { (1, 30), (1, 20), (1, 10) };

        var fromForward = MetadataCascadeResolver.ResolveNearestSources(forward, _levels);
        var fromBackward = MetadataCascadeResolver.ResolveNearestSources(backward, _levels);

        fromForward.Should().BeEquivalentTo(fromBackward);
        fromForward[1].Should().Be(30);
    }

    [Fact]
    public void ResolveNearestSources_ShouldResolveTemplatesIndependently()
    {
        var links = new List<(int, int)> { (1, 10), (1, 20), (2, 10) };

        var sources = MetadataCascadeResolver.ResolveNearestSources(links, _levels);

        sources[1].Should().Be(20);
        sources[2].Should().Be(10);
    }

    [Fact]
    public void ResolveFieldSources_ShouldPickNearestAncestorHoldingTheValue()
    {
        var links = new List<(int, int)> { (1, 10), (1, 20) };
        var fieldTemplates = new Dictionary<int, int> { { 100, 1 } };
        var values = new List<(int FieldId, int FolderId)> { (100, 10), (100, 20) };

        var sources = MetadataCascadeResolver.ResolveFieldSources(values, links, fieldTemplates, _levels);

        sources[100].Should().Be(20);
    }

    [Fact]
    public void ResolveFieldSources_ShouldFallBackToFartherAncestor_WhenNearerOneLeftTheFieldEmpty()
    {
        // both folders cascade template 1; folder 20 holds only field 100, the room holds field 101 too
        var links = new List<(int, int)> { (1, 10), (1, 20) };
        var fieldTemplates = new Dictionary<int, int> { { 100, 1 }, { 101, 1 } };
        var values = new List<(int, int)> { (100, 10), (100, 20), (101, 10) };

        var sources = MetadataCascadeResolver.ResolveFieldSources(values, links, fieldTemplates, _levels);

        sources[100].Should().Be(20);
        sources[101].Should().Be(10);
    }

    [Fact]
    public void ResolveFieldSources_ShouldIgnoreValues_WhenTheFolderDoesNotCascadeTheFieldsTemplate()
    {
        // folder 20 cascades only template 2, so its value for the template-1 field must not leak
        var links = new List<(int, int)> { (1, 10), (2, 20) };
        var fieldTemplates = new Dictionary<int, int> { { 100, 1 } };
        var values = new List<(int, int)> { (100, 10), (100, 20) };

        var sources = MetadataCascadeResolver.ResolveFieldSources(values, links, fieldTemplates, _levels);

        sources[100].Should().Be(10);
    }

    [Fact]
    public void ResolveFieldSources_ShouldIgnoreValues_WhenTheFieldTemplateIsUnknown()
    {
        var links = new List<(int, int)> { (1, 10) };
        var fieldTemplates = new Dictionary<int, int> { { 100, 1 } };
        var values = new List<(int, int)> { (100, 10), (999, 10) };

        var sources = MetadataCascadeResolver.ResolveFieldSources(values, links, fieldTemplates, _levels);

        sources.Should().ContainKey(100).And.NotContainKey(999);
    }

    [Fact]
    public void ResolveFieldSources_ShouldPickSingleSourcePerField_ForMultiChoiceRows()
    {
        // a multi-choice field is a row per option: two rows on each folder must resolve to ONE folder
        var links = new List<(int, int)> { (1, 10), (1, 20) };
        var fieldTemplates = new Dictionary<int, int> { { 100, 1 } };
        var values = new List<(int, int)> { (100, 10), (100, 10), (100, 20), (100, 20) };

        var sources = MetadataCascadeResolver.ResolveFieldSources(values, links, fieldTemplates, _levels);

        sources.Should().HaveCount(1);
        sources[100].Should().Be(20);
    }

    [Fact]
    public void ResolveFieldSources_ShouldBeDeterministic_RegardlessOfInputOrder()
    {
        var links = new List<(int, int)> { (1, 10), (1, 20) };
        var fieldTemplates = new Dictionary<int, int> { { 100, 1 } };
        var forward = new List<(int, int)> { (100, 10), (100, 20) };
        var backward = new List<(int, int)> { (100, 20), (100, 10) };

        var fromForward = MetadataCascadeResolver.ResolveFieldSources(forward, links, fieldTemplates, _levels);
        var fromBackward = MetadataCascadeResolver.ResolveFieldSources(backward, links, fieldTemplates, _levels);

        fromForward.Should().BeEquivalentTo(fromBackward);
    }

    [Fact]
    public void ResolveNearestSources_ShouldNotThrow_WhenAncestorLevelIsUnknown()
    {
        // a link from a folder missing in the level map loses to any known ancestor
        var links = new List<(int, int)> { (1, 99), (1, 10) };

        var sources = MetadataCascadeResolver.ResolveNearestSources(links, _levels);

        sources[1].Should().Be(10);
    }
}
