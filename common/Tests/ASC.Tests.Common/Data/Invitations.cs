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

namespace ASC.Tests.Common.Data;

/// <summary>
/// Creating members in a test's portal — shared by every suite. Both helpers send the pre-computed
/// client password hash instead of the raw password, which spares the server a PBKDF2 pass and the
/// password-policy check on every invite; the hash is also cached on the returned <see cref="User"/>,
/// so a later <c>Authenticate(user)</c> does not hash again.
/// </summary>
public static class Invitations
{
    /// <summary>
    /// Invites and registers a new member of the given type through the typed SDK
    /// (<c>POST /api/2.0/people</c>), acting as <paramref name="inviter"/>.
    /// </summary>
    public static async Task<User> InviteContactAsync(
        ProfilesApi profilesApi,
        HttpClient peopleClient,
        EmployeeType employeeType,
        User inviter,
        CancellationToken cancellationToken)
    {
        await peopleClient.Authenticate(inviter);

        var fakeMember = Initializer.FakerMember.Generate();
        var passwordHash = Initializer.GetClientPassword(fakeMember.Password);

        var memberSw = Stopwatch.StartNew();
        var createMemberResponse = await profilesApi.AddMemberWithHttpInfoAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Email = fakeMember.Email,
            PasswordHash = passwordHash,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,
            Type = employeeType,
        }, cancellationToken);
        Timing.Write($"invite.addMember({employeeType})", memberSw.ElapsedMilliseconds);

        if (createMemberResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Unable to invite user {employeeType}");
        }

        return new User(fakeMember.Email, fakeMember.Password)
        {
            Id = createMemberResponse.Data.Response.Id,
            PasswordHash = passwordHash
        };
    }

    /// <summary>
    /// Creates an activated guest with a known password, acting as <paramref name="inviter"/>.
    /// Goes through raw HTTP because <c>POST /api/2.0/people/active</c> is marked
    /// <c>[ApiExplorerSettings(IgnoreApi = true)]</c> and therefore absent from the SDK.
    /// </summary>
    public static async Task<User> InviteGuestAsync(
        HttpClient peopleClient,
        User inviter,
        CancellationToken cancellationToken)
    {
        await peopleClient.Authenticate(inviter);

        var fakeGuest = Initializer.FakerMember.Generate();
        var passwordHash = Initializer.GetClientPassword(fakeGuest.Password);

        var payload = JsonSerializer.Serialize(new
        {
            firstName = fakeGuest.FirstName,
            lastName = fakeGuest.LastName,
            email = fakeGuest.Email,
            passwordHash,
            type = nameof(EmployeeType.Guest),
            cultureName = "en-US",
            spam = false
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var guestSw = Stopwatch.StartNew();
        using var response = await peopleClient.PostAsync("api/2.0/people/active", content, cancellationToken);
        Timing.Write($"invite.guest({inviter.Email})", guestSw.ElapsedMilliseconds);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to create a guest ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var guestId = json.RootElement.GetProperty("response").GetProperty("id").GetGuid();

        return new User(fakeGuest.Email, fakeGuest.Password) { Id = guestId, PasswordHash = passwordHash };
    }
}
