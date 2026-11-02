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

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers who may manage the template dictionary: only a DocSpace admin writes it, every member reads it, and an
/// anonymous caller is turned away from every route. A refused request must change nothing.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTemplatePermissionsTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    public static TheoryData<EmployeeType, int> Writers => new()
    {
        { EmployeeType.DocSpaceAdmin, 200 },
        { EmployeeType.Guest, 403 }
    };

    public static TheoryData<EmployeeType> Readers => new()
    {
        EmployeeType.RoomAdmin,
        EmployeeType.User
    };

    [Theory]
    [MemberData(nameof(Writers))]
    public async Task CreateTemplate_IsAllowedToADocSpaceAdminOnly(EmployeeType employeeType, int expected)
    {
        var member = await InviteMember(employeeType);
        var name = "Perm " + Suffix();

        await _filesClient.Authenticate(member);

        var status = await StatusOfAsync(async () => await CreateTemplateAsync([], name));

        status.Should().Be(expected);

        await _filesClient.Authenticate(Owner);

        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Any(t => t.Name == name).Should().Be(expected == 200);
    }

    [Theory]
    [MemberData(nameof(Writers))]
    public async Task UpdateAndDeleteTemplateAndFields_AreAllowedToADocSpaceAdminOnly_AndARefusalChangesNothing(EmployeeType employeeType, int expected)
    {
        var template = await CreateTemplateAsync([new MetadataFieldRequest("keep", MetadataFieldType.String)]);
        var fieldId = template.Field("keep").Id;
        var member = await InviteMember(employeeType);

        await _filesClient.Authenticate(member);

        var results = new[]
        {
            await StatusOfAsync(async () => await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(visible: false), TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("added", MetadataFieldType.String), TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.UpdateFieldAsync(template.Id, fieldId, new UpdateMetadataFieldRequest(name: "renamed"), TestContext.Current.CancellationToken))
        };

        results.Should().OnlyContain(status => status == expected);

        if (expected == 403)
        {
            var deleteField = await StatusOfAsync(async () => await _metadataApi.DeleteFieldAsync(template.Id, fieldId, TestContext.Current.CancellationToken));
            var deleteTemplate = await StatusOfAsync(async () => await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken));

            deleteField.Should().Be(403);
            deleteTemplate.Should().Be(403);

            await _filesClient.Authenticate(Owner);

            var reloaded = await GetTemplateAsync(template.Id);

            reloaded.Name.Should().Be(template.Name);
            reloaded.Visible.Should().BeTrue();
            reloaded.Fields.Select(f => f.Name).Should().Equal("keep");
        }
        else
        {
            await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

            await _filesClient.Authenticate(Owner);

            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(404);
        }
    }

    [Theory]
    [MemberData(nameof(Readers))]
    public async Task GetTemplates_AreReadableByEveryMember(EmployeeType employeeType)
    {
        var template = await CreateTemplateAsync();
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Select(t => t.Id).Should().Contain(template.Id);
        (await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken)).Response.Id.Should().Be(template.Id);
    }

    [Fact]
    public async Task EveryTemplateRoute_ForAnAnonymousCaller_ReturnsUnauthorized()
    {
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;

        await _filesClient.Authenticate(null);

        var statuses = new[]
        {
            await StatusOfAsync(async () => await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await CreateTemplateAsync([], "anon")),
            await StatusOfAsync(async () => await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate("anon"), TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("x", MetadataFieldType.String), TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.UpdateFieldAsync(template.Id, fieldId, new UpdateMetadataFieldRequest(name: "x"), TestContext.Current.CancellationToken)),
            await StatusOfAsync(async () => await _metadataApi.DeleteFieldAsync(template.Id, fieldId, TestContext.Current.CancellationToken))
        };

        statuses.Should().OnlyContain(status => status == 401);

        await _filesClient.Authenticate(Owner);

        (await GetTemplateAsync(template.Id)).Id.Should().Be(template.Id, "the refused requests changed nothing");
    }
}
