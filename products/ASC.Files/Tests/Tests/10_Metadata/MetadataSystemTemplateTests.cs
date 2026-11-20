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
/// Covers the hidden template that holds the custom fields: the template endpoints answer as if it did not exist, the
/// values endpoint refuses its fields, and the listing of the templates leaves it out for every member.
/// </summary>
/// <remarks>
/// The API never names the identifiers of that template and of its fields. They are found by their place in the
/// identifier sequence: the hidden template and its first field are created by the first custom field write of the
/// portal, so they lie between the identifiers of a template (and of a field) created right before that write and of
/// one created right after it. Templates of other portals created meanwhile can lie there too; every identifier of
/// the gap is probed, which holds for them as well.
/// </remarks>
[Trait("Category", "Metadata")]
public class MetadataSystemTemplateTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    private const int MaxGap = 500;

    [Fact]
    public async Task GetTemplates_ByAGuest_ListsTheUserTemplatesWithTheirFieldsInDisplayOrder_AndNotTheCustomFieldTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Listing {suffix}");
        await _metadataApi.SetFolderCustomFieldsAsync(room.Id, new SetCustomFields([new CustomFieldRequest($"Custom {suffix}", "A-42")]), TestContext.Current.CancellationToken);

        // the fields are sent out of order: the listing puts them by their position, not by the request
        var template = await CreateTemplateAsync(
        [
            new MetadataFieldRequest("Third", MetadataFieldType.String, order: 3),
            new MetadataFieldRequest("First", MetadataFieldType.Number, order: 1),
            new MetadataFieldRequest("Second", MetadataFieldType.Date, order: 2)
        ]);

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // no right is demanded: a guest reads the templates like any member
        var templates = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        templates.Select(t => t.Id).Should().Equal(new[] { template.Id }, "the hidden template of the custom fields is never listed");
        templates.Single().Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public async Task TheCustomFieldTemplate_IsNotFound_ByGetTemplate_NorByTheFileAssignment()
    {
        await _filesClient.Authenticate(Owner);
        var probe = await ArrangeProbeAsync();

        probe.TemplateCandidates.Should().NotBeEmpty("the hidden template was created between the two templates");

        foreach (var id in probe.TemplateCandidates)
        {
            (await StatusOfAsync(() => _metadataApi.GetTemplateAsync(id, TestContext.Current.CancellationToken)))
                .Should().Be(404, "template {0} is the hidden one or another portal's, and neither exists for the template endpoints", id);

            (await StatusOfAsync(() => AssignToFileAsync(probe.FileId, id)))
                .Should().Be(404, "the hidden template {0} cannot be assigned through the template endpoints", id);
        }

        var metadata = await GetFileMetadataAsync(probe.FileId);
        metadata.Templates.Select(t => t.Id).Should().Equal(new[] { probe.TemplateId }, "nothing was linked by the refused assignments");
    }

    [Fact]
    public async Task SetFileValues_OnACustomField_ReturnsBadRequest_AndWritesNothing()
    {
        await _filesClient.Authenticate(Owner);
        var probe = await ArrangeProbeAsync();

        var answers = new Dictionary<int, (int Status, string Body)>();

        foreach (var id in probe.FieldCandidates)
        {
            try
            {
                await _metadataApi.SetFileValuesAsync(probe.FileId, new SetMetadataValues([new MetadataValueRequest(id, stringValue: "written")]), TestContext.Current.CancellationToken);
                answers[id] = (200, "");
            }
            catch (ApiException exception)
            {
                answers[id] = (exception.ErrorCode, exception.ErrorContent!.ToString()!);
            }
        }

        // the first custom field of the portal lies in the gap; the fields of other portals are unknown here (404)
        answers.Values.Should().ContainSingle(a => a.Status == 400, "exactly one field of the gap is the custom field: {0}", string.Join(", ", answers.Select(a => $"{a.Key}={a.Value.Status}")))
            .Which.Body.Should().Contain("customFields", "the custom fields are written through their own endpoint");
        answers.Values.Should().OnlyContain(a => a.Status == 400 || a.Status == 404);

        var metadata = await GetFileMetadataAsync(probe.FileId);
        metadata.CustomFields.Should().BeNullOrEmpty("the file holds no custom field and the refused write gave it none");
        ValueOf(metadata, probe.TemplateId, probe.FieldId).Should().BeNull();
    }

    /// <summary>
    /// A file carrying a template, and the identifiers the hidden template and its first field can have. The first
    /// custom field write of the portal goes to a room, so the file stays without custom fields.
    /// </summary>
    private async Task<Probe> ArrangeProbeAsync()
    {
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Probe {suffix}");
        var file = await CreateFile($"probe-{suffix}.docx", room.Id);

        var before = await CreateTemplateAsync();
        await _metadataApi.SetFolderCustomFieldsAsync(room.Id, new SetCustomFields([new CustomFieldRequest($"Custom {suffix}", "A-42")]), TestContext.Current.CancellationToken);
        var after = await CreateTemplateAsync();

        await AssignToFileAsync(file.Id, before.Id);

        var templateGap = after.Id - before.Id - 1;
        var fieldGap = after.Fields.Single().Id - before.Fields.Single().Id - 1;

        if (templateGap > MaxGap || fieldGap > MaxGap)
        {
            Assert.Skip($"Too many templates ({templateGap}) or fields ({fieldGap}) of other portals were created meanwhile to probe them all.");
        }

        return new Probe(
            file.Id,
            before.Id,
            before.Fields.Single().Id,
            Enumerable.Range(before.Id + 1, templateGap).ToList(),
            Enumerable.Range(before.Fields.Single().Id + 1, fieldGap).ToList());
    }

    private sealed record Probe(int FileId, int TemplateId, int FieldId, List<int> TemplateCandidates, List<int> FieldCandidates);
}
