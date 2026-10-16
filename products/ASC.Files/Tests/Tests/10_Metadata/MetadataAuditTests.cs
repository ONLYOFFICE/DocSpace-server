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
/// Covers the audit trail of the metadata actions: every change of a template, of a field, of an assignment and of
/// the values is written as an event whose text names the template, the field or the entry it happened to.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataAuditTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int StringType = 0;
    private const string ClientField = "Client";
    private const string DepartmentField = "Department";

    [Fact]
    public async Task MetadataActions_AreWrittenToTheAuditTrail_WithTheNamesOfTheirObjects()
    {
        await _filesClient.Authenticate(Owner);
        // the audit trail is read through the Web.Api client, which carries its own credentials
        await _webApiClient.Authenticate(Owner);
        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];
        var templateName = "Audited " + suffix;
        var renamed = templateName + " renamed";
        var roomTitle = $"Audited {suffix}";

        var template = await api.CreateTemplateAsync(templateName, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var department = await api.CreateFieldAsync(template.Id, new MetadataFieldPayload { Name = DepartmentField, Type = StringType }, TestContext.Current.CancellationToken);
        await api.UpdateTemplateAsync(template.Id, new { name = renamed }, TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom(roomTitle);

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = template.Field(ClientField).Id, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.UnassignFolderTemplateAsync(room.Id, template.Id, TestContext.Current.CancellationToken);
        await api.DeleteFieldAsync(template.Id, department.Id, TestContext.Current.CancellationToken);
        await api.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

        // the events are written by the audit service behind the message bus, after the requests return
        // the events are asserted by their text alone: the generated MessageAction of the SDK predates the metadata actions
        var events = await PollLastAuditEventsAsync(e => e.Any(x => x.Action == "Metadata template deleted: " + renamed));
        var actions = events.Select(e => e.Action).ToList();

        // the texts used to carry no placeholder: the names were passed and dropped, every event read the same
        actions.Should().Contain("Metadata template created: " + templateName);
        actions.Should().Contain("Metadata field created: " + DepartmentField);
        actions.Should().Contain("Metadata template updated: " + renamed);
        actions.Should().Contain("Metadata template assigned: " + roomTitle);
        actions.Should().Contain("Metadata values updated: " + roomTitle);
        actions.Should().Contain("Metadata template unassigned: " + roomTitle);
        actions.Should().Contain("Metadata field deleted: " + DepartmentField);
        actions.Should().Contain("Metadata template deleted: " + renamed);
    }

    private async Task<List<AuditEventDto>> PollLastAuditEventsAsync(Func<List<AuditEventDto>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = (await _auditTrailDataApi.GetLastAuditEventsAsync(TestContext.Current.CancellationToken)).Response ?? [];

            if (until(events) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }
}
