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

namespace ASC.People.Tests.PeopleController;

[Collection("Test Collection")]
public class GuestTest(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateGuest_ShouldCreateGuestSuccessfully()
    {
        await _apiClient.Authenticate(Initializer.Owner);
        var fakeMember = Initializer.FakerMember.Generate();
        
        // Act
        var shortLink = (await _portalUsersApi.GetInvitationLinkAsync(EmployeeType.Guest, TestContext.Current.CancellationToken)).Response;
        var fullLink = await _apiClient.GetAsync(shortLink, TestContext.Current.CancellationToken);
        var confirmHeader = fullLink.RequestMessage?.RequestUri?.Query.Substring(1);
        
        await _peopleClient.Authenticate(Initializer.Owner);
        _peopleClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", confirmHeader);
        
        var parsedQuery = HttpUtility.ParseQueryString(confirmHeader);
        if(!Enum.TryParse(parsedQuery["emplType"], out EmployeeType parsedEmployeeType))
        {
            parsedEmployeeType = EmployeeType.Guest;
        }
        
        var response = await _profilesApi.AddMemberWithHttpInfoAsync(new MemberRequestDto
        {
            FromInviteLink = true,
            CultureName = "en-US",
            Spam = false,
            
            Email = fakeMember.Email,
            Password = fakeMember.Password,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,
            
            Type = parsedEmployeeType,
            Key = parsedQuery["key"],
        }, TestContext.Current.CancellationToken);
        
        _peopleClient.DefaultRequestHeaders.Remove("confirm");
        
        // Arrange
        await _peopleClient.Authenticate(Initializer.Owner);
        
        
        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var createdGuest = response.Data.Response;
        createdGuest.Should().NotBeNull();
        createdGuest.Email.Should().Be(fakeMember.Email);
        createdGuest.FirstName.Should().Be(fakeMember.FirstName);
        createdGuest.LastName.Should().Be(fakeMember.LastName);
        createdGuest.IsVisitor.Should().BeTrue();
    }
    
    // [Fact]
    // [Trait("Category", "Bug")]
    // [Trait("Bug", "79419")]
    // public async Task CreateUser_AsRoomAdmin_ShouldCreateUserSuccessfully()
    // {
    //     await _apiClient.Authenticate(Initializer.Owner);
    //     var roomAdmin = await Initializer.InviteContact(EmployeeType.RoomAdmin);
    //     var guestFromOwner = await Initializer.InviteContact(EmployeeType.Guest, roomAdmin);
    //     
    //     await _peopleClient.Authenticate(roomAdmin);
    //     var guestFromRoomAdmin = await Initializer.InviteContact(EmployeeType.Guest, roomAdmin);
    //     
    //     var updateUserTypeResponse = (await _userTypeApi.UpdateUserTypeAsync(EmployeeType.User, new UpdateMembersRequestDto([guestFromOwner.Id]), TestContext.Current.CancellationToken)).Response;
    //     updateUserTypeResponse.Should().BeEmpty();
    //     
    //     updateUserTypeResponse = (await _userTypeApi.UpdateUserTypeAsync(EmployeeType.User, new UpdateMembersRequestDto([guestFromRoomAdmin.Id]), TestContext.Current.CancellationToken)).Response;
    //     updateUserTypeResponse.Should().Contain(r=> r.Id == guestFromRoomAdmin.Id);
    // }
}