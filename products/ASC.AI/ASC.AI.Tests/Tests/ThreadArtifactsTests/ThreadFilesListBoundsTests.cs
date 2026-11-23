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

namespace ASC.AI.Tests.Tests.ThreadArtifactsTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ThreadArtifacts")]
public class ThreadFilesListBoundsTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    [Theory]
    [InlineData(true, 0, null)]
    [InlineData(true, 101, null)]
    [InlineData(true, null, -1)]
    [InlineData(false, 0, null)]
    [InlineData(false, 101, null)]
    [InlineData(false, null, -1)]
    public async Task List_WithAValueOutsideTheBounds_Returns400(bool artifacts, int? count, int? startIndex)
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var status = await ListStatusAsync(artifacts, projectId, thread.Id, count, startIndex);

        status.Should().Be(400);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task List_WithTheLargestCountAndTheFirstIndex_Returns200(bool artifacts)
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var status = await ListStatusAsync(artifacts, projectId, thread.Id, 100, 0);

        status.Should().Be(200);
    }

    private Task<int> ListStatusAsync(bool artifacts, int projectId, Guid threadId, int? count, int? startIndex)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        return artifacts
            ? StatusOfAsync(() => RoomsAiApi.GetThreadArtifactsAsync(projectId, threadId, count, startIndex, cancellationToken: cancellationToken))
            : StatusOfAsync(() => RoomsAiApi.GetThreadAttachmentsAsync(projectId, threadId, count, startIndex, cancellationToken: cancellationToken));
    }
}
