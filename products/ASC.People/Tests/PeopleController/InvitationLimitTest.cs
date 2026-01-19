// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.People.Tests.Data;
using ASC.People.Tests.Factory;

namespace ASC.People.Tests.PeopleController;

[Collection("Test Collection")]
public class InvitationLimitTest(
    PeopleFactory peopleFactory,
    WepApiFactory apiFactory)
    : BaseTest(peopleFactory, apiFactory)
{
    readonly WepApiFactory _apiFactory = apiFactory;

    [Fact]
    public async Task InviteUsers_ShouldChangeInvitationLimit()
    {
        await _apiClient.Authenticate(Initializer.Owner);
        await _peopleClient.Authenticate(Initializer.Owner);

        var settings = (await _apiFactory.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        settings.Should().NotBeNull();

        var limit = settings.InvitationLimit;

        if (limit == 0)
        {
            return;
        }

        var inDto = new InviteUsersRequestDto(
            invitations: [
                new UserInvitationRequestDto(EmployeeType.User) {Email = Initializer.FakerMember.Generate().Email}
            ]
        );

        var wrapper = (await _peopleProfilesApi.InviteUsersAsync(inDto, TestContext.Current.CancellationToken)).Response;

        wrapper.Should().NotBeNull();
        wrapper.Count.Should().Be(1);

        settings = (await _apiFactory.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        settings.Should().NotBeNull();
        settings.InvitationLimit.Should().Be(limit == int.MaxValue ? int.MaxValue : limit - 1);

        if (limit == int.MaxValue)
        {
            return;
        }

        var invitations = new List<UserInvitationRequestDto>();
        while(invitations.Count < limit)
        {
            invitations.Add(new UserInvitationRequestDto(EmployeeType.User) {Email = Initializer.FakerMember.Generate().Email});
        }

        inDto = new InviteUsersRequestDto(invitations);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => 
            await _peopleProfilesApi.InviteUsersAsync(inDto, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.Message.Should().Contain(Web.Core.PublicResources.Resource.ErrorInvitationLimitExceeded);
    }
}