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
    [InlineData("artifacts", "?count=0")]
    [InlineData("artifacts", "?count=101")]
    [InlineData("artifacts", "?startIndex=-1")]
    [InlineData("attachments", "?count=0")]
    [InlineData("attachments", "?count=101")]
    [InlineData("attachments", "?startIndex=-1")]
    public async Task List_WithAValueOutsideTheBounds_Returns400(string list, string query)
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        using var response = await GetAsync(projectId, thread.Id, list, query);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("artifacts")]
    [InlineData("attachments")]
    public async Task List_WithTheLargestCountAndTheFirstIndex_Returns200(string list)
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        using var response = await GetAsync(projectId, thread.Id, list, "?count=100&startIndex=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> GetAsync(int projectId, Guid threadId, string list, string query)
    {
        return FilesHttpClient.GetAsync($"/api/2.0/files/rooms/{projectId}/threads/{threadId}/{list}{query}", TestContext.Current.CancellationToken);
    }
}
