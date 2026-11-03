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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// The HTTP contract of <c>/api/2.0/ai/attachments/*</c>: a wrong verb is never served and never 405 — the routes are
/// registered per verb on the chat service, so a wrong verb on a live route is indistinguishable from a route that does not
/// exist — and a refused call never touches the draft.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentRouteContractTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Theory]
    [InlineData("GET", "get")]
    [InlineData("GET", "get-many")]
    [InlineData("PUT", "save-file")]
    [InlineData("DELETE", "save-file")]
    [InlineData("PUT", "link-to-message")]
    [InlineData("POST", "delete")]
    [InlineData("POST", "delete-many")]
    public async Task WrongHttpVerb_IsNeverServedAndNeverA405(string verb, string route)
    {
        // The TypeScript suite measured, behind the portal's router, 403 for a GET on the read routes and 404 everywhere
        // else (a GET with no body turned the 403 back into a 404). The chat service itself registers each route for one
        // verb only, so what is pinned here is the part that does not depend on the layer in front of it: the call is
        // refused as a client error other than 405 and the draft survives.
        var id = await SaveDraftAsync(Owner, "Autotest verbs.docx");

        var outcome = await SendRawAsync(new HttpMethod(verb), $"{AttachmentsPath}/{route}", Json(id));

        outcome.Status.Should().BeOneOf([403, 404], $"{verb} instead of the registered verb on /{route}");
        await ExpectStoredAsync(id, "draft after a wrong verb");
    }
}
