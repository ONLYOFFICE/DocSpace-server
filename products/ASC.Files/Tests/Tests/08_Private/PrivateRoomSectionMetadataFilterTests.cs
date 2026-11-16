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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// The metadata filter on the private rooms section. Unlike the Templates sections, which refuse it, the section is a
/// view of rooms and filters them the way the rooms listing does: by its identifier, by the typed search and through
/// the rooms listing with <c>SearchArea.PrivateRooms</c>. Only the legacy Privacy folder (<c>FolderType.Privacy</c>,
/// the documents of the desktop encryption) refuses a non-empty filter.
/// </summary>
[Trait("Feature", "PrivacyRoom")]
[Trait("Category", "Metadata")]
public class PrivateRoomSectionMetadataFilterTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task PrivateRoomsSection_AppliesTheMetadataFilter_ByIdByTypedSearchAndInTheRoomsListing()
    {
        var (template, sectionId, assignedTitle) = await ArrangeAsync();
        var expected = "200: " + assignedTitle;

        var byId = await AnswerOfAsync(async () => (await _foldersApi.GetFolderContentWithMetadataAsync(sectionId, metadataTemplateId: template.Id, cancellationToken: TestContext.Current.CancellationToken)).Folders);
        var typed = await AnswerOfAsync(async () => (await _foldersApi.SearchFolderWithMetadataAsync(sectionId, new FolderMetadataSearch(template.Id, count: 25), TestContext.Current.CancellationToken)).Folders);
        var listed = await AnswerOfAsync(async () => (await _roomsApi.GetRoomsWithMetadataAsync(metadataTemplateId: template.Id, searchArea: SearchArea.PrivateRooms, cancellationToken: TestContext.Current.CancellationToken)).Folders);

        // the private room without the template is left out: the filter is applied, not ignored
        byId.Should().Be(expected);
        typed.Should().Be(expected);
        listed.Should().Be(expected);
    }

    /// <summary>
    /// The titles of the listed rooms for an answer, the status for a refusal, so a failure shows what came back.
    /// </summary>
    private static async Task<string> AnswerOfAsync(Func<Task<List<RoomEntryResponse>>> request)
    {
        try
        {
            return "200: " + string.Join(" | ", (await request()).Select(f => f.Title));
        }
        catch (ApiException exception)
        {
            return exception.ErrorCode.ToString(CultureInfo.InvariantCulture);
        }
    }

    private async Task<(MetadataTemplateDto Template, int SectionId, string AssignedTitle)> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var template = (await _metadataApi.CreateTemplateAsync(
            new CreateMetadataTemplateRequestDto("Private " + suffix, true, [new MetadataFieldRequest("Client", MetadataFieldType.String)]),
            TestContext.Current.CancellationToken)).Response;

        var assigned = await CreatePrivateRoom($"Assigned {suffix}", RoomType.CustomRoom);
        await CreatePrivateRoom($"Bare {suffix}", RoomType.CustomRoom);
        await _metadataApi.AssignFolderTemplatesAsync(assigned.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        var sectionId = (await _foldersApi.GetPrivateRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        return (template, sectionId, assigned.Title);
    }
}
