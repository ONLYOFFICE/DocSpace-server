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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// <see cref="DbWorker.IsPreviousVersionBody"/> tells a body the publisher of the previous version stored (whole domain
/// objects) from one of this version, by property names only the old shapes had. A manual retry of an old body is
/// refused as outdated, so a name of that list appearing in a current payload type would make every manual retry of
/// its trigger fail: the payload types are held to the list here.
/// </summary>
[Trait("Category", "Webhooks")]
public class WebhookPreviousVersionBodyTests
{
    [Theory]
    [InlineData("""{"event":{"id":1},"payload":{"id":5,"title":"a.docx","createOn":"2025-01-01T00:00:00Z"}}""")]
    [InlineData("""{"event":{"id":1},"payload":{"id":5,"title":"Docs","modifiedOn":"2025-01-01T00:00:00Z"}}""")]
    [InlineData("""{"event":{"id":1},"payload":{"id":"9924256a-739c-462b-be1a-e1d3a1d6f2d4","lastModified":"2025-01-01T00:00:00Z"}}""")]
    [InlineData("""{"event":{"id":1},"payload":{"id":"9924256a-739c-462b-be1a-e1d3a1d6f2d4","authenticationType":0}}""")]
    [InlineData("""{"event":{"id":1},"payload":{"originalForm":{"id":5,"createOn":"2025-01-01T00:00:00Z"},"submittedForm":{"id":6}}}""")]
    public void IsPreviousVersionBody_OldShapes_AreRecognised(string body)
    {
        DbWorker.IsPreviousVersionBody(body).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"event":{"id":1}}""")]
    [InlineData("""{"event":{"id":1},"payload":{"id":5,"title":"a.docx","created":"2025-01-01T00:00:00Z","updated":"2025-01-01T00:00:00Z"}}""")]
    [InlineData("""{"event":{"id":1,"createOn":"2025-01-01T00:00:00Z"},"payload":{"id":5}}""")]
    [InlineData("""{"event":{"id":1},"payload":{"id":5,"createdBy":{"id":"9924256a-739c-462b-be1a-e1d3a1d6f2d4","info":{"createOn":"x"}}}}""")]
    public void IsPreviousVersionBody_CurrentShapesAndNonBodies_AreNotOld(string? body)
    {
        DbWorker.IsPreviousVersionBody(body!).Should().BeFalse();
    }

    public static TheoryData<Type> PayloadTypes =>
    [
        typeof(UserWebhookDto),
        typeof(GroupWebhookDto),
        typeof(FileWebhookDto<int>),
        typeof(FolderWebhookDto<int>),
        typeof(RoomWebhookDto<int>),
        typeof(FormSubmitWebhookDto<int>)
    ];

    [Theory]
    [MemberData(nameof(PayloadTypes))]
    public void PayloadType_HasNoPropertyThatMarksAnOldBody(Type payloadType)
    {
        // Arrange — the names as the payload goes on the wire, at the top level and one level down, the two levels the
        // check reads
        var names = new List<string>();

        foreach (var property in WireProperties(payloadType))
        {
            names.Add($"{JsonNamingPolicy.CamelCase.ConvertName(property.Name)}");

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (propertyType.IsClass && propertyType != typeof(string))
            {
                names.AddRange(WireProperties(propertyType).Select(child => JsonNamingPolicy.CamelCase.ConvertName(child.Name)));
            }
        }

        // Assert
        names.Should().NotIntersectWith(DbWorker.PreviousVersionProperties,
            "a manual retry of every {0} event would be refused as a body of the previous version", payloadType.Name);
    }

    private static IEnumerable<PropertyInfo> WireProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetMethod != null && p.GetIndexParameters().Length == 0);
    }
}
