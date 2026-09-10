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

namespace ASC.People.Tests.Tests._06_Photos;

/// <summary>
/// Shared setup for the member-photo suites (<c>POST/GET/DELETE /people/{userid}/photo</c>):
/// a portal-role abstraction that also covers the fixed <see cref="BaseTest.Owner"/> (which is
/// not an <see cref="EmployeeType"/> value), a minimal test image and a raw multipart helper for
/// the one request shape the typed SDK cannot express (an upload with no file part at all).
/// </summary>
public abstract class PhotosTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>The minimal valid 1x1 PNG used throughout the suite.</summary>
    private const string TestImageBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";

    protected static byte[] CreateTestImageBytes() => Convert.FromBase64String(TestImageBase64);

    /// <summary>
    /// The full set of portal roles a photo test cares about. <see cref="Role.Owner"/> is the fixed
    /// portal owner (never invited, see <see cref="BaseTest.Owner"/>); the rest are invited members
    /// of the matching <see cref="EmployeeType"/>.
    /// </summary>
    public enum Role
    {
        Owner,
        DocSpaceAdmin,
        RoomAdmin,
        User,
        Guest
    }

    protected static readonly Role[] _allRoles = [Role.Owner, Role.DocSpaceAdmin, Role.RoomAdmin, Role.User, Role.Guest];

    /// <summary>Every role other than <paramref name="actor"/> — the standard "other users" target list.</summary>
    protected static Role[] OthersOf(Role actor) => [.. _allRoles.Where(role => role != actor)];

    public static TheoryData<Role> AllRolesData()
    {
        var data = new TheoryData<Role>();

        foreach (var role in _allRoles)
        {
            data.Add(role);
        }

        return data;
    }

    /// <summary>Every (actor, target) pair where target ranges over the other four roles.</summary>
    public static TheoryData<Role, Role> AllActorOtherTargetPairs()
    {
        var data = new TheoryData<Role, Role>();

        foreach (var actor in _allRoles)
        {
            foreach (var target in OthersOf(actor))
            {
                data.Add(actor, target);
            }
        }

        return data;
    }

    /// <summary>
    /// Resolves a role to a concrete portal user, inviting it (via the Owner) the first time it is
    /// needed. Not cached — each call site that needs the same role more than once should keep the
    /// returned <see cref="User"/> itself, matching how the TS suite creates its actors once per
    /// test and reuses them across steps.
    /// </summary>
    protected Task<User> ResolveUserAsync(Role role)
    {
        return role switch
        {
            Role.Owner => Task.FromResult(Owner),
            Role.DocSpaceAdmin => InviteContact(EmployeeType.DocSpaceAdmin),
            Role.RoomAdmin => InviteContact(EmployeeType.RoomAdmin),
            Role.User => InviteContact(EmployeeType.User),
            Role.Guest => InviteGuest(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    /// <summary>Uploads a test image as <paramref name="actor"/> onto <paramref name="targetUserId"/>.</summary>
    protected async Task<FileUploadResultDto> UploadPhotoAsync(User? actor, Guid targetUserId, byte[]? bytes = null, string fileName = "avatar.png", string contentType = "image/png")
    {
        await _peopleClient.Authenticate(actor);

        await using var stream = new MemoryStream(bytes ?? CreateTestImageBytes());

        return (await _photosApi.UploadMemberPhotoAsync(
            targetUserId.ToString(),
            new FileParameter(fileName, contentType, stream),
            autosave: true,
            TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Raw multipart POST to <c>api/2.0/people/{userid}/photo</c> with no file part at all — the one
    /// shape <see cref="FileParameter"/> cannot express, since it always carries a stream.
    /// </summary>
    protected async Task<HttpResponseMessage> UploadPhotoWithoutFileRaw(User actor, Guid targetUserId)
    {
        await _peopleClient.Authenticate(actor);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/people/{targetUserId}/photo")
        {
            Content = new MultipartFormDataContent()
        };

        return await _peopleClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
