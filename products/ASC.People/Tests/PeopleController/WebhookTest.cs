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


using ASC.Webhooks.Core;

using InternalWebhookTrigger = ASC.Webhooks.Core.WebhookTrigger;
using InternalEmployeeType = ASC.Core.Users.EmployeeType;
using WebhookTrigger = DocSpace.API.SDK.Model.WebhookTrigger;

namespace ASC.People.Tests.PeopleController;

[Collection("Test Collection")]
public class WebhookTest(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static TheoryData<InternalWebhookTrigger> AdminOnlyTriggers =>
    [
        InternalWebhookTrigger.UserCreated,
        InternalWebhookTrigger.UserInvited,
        InternalWebhookTrigger.GroupCreated,
        InternalWebhookTrigger.GroupUpdated,
        InternalWebhookTrigger.GroupDeleted,
        InternalWebhookTrigger.RoomCreated,
        InternalWebhookTrigger.RoomCopied,
        InternalWebhookTrigger.AgentCreated,
    ];

    public static TheoryData<InternalWebhookTrigger> AllRolesTriggers =>
    [
        InternalWebhookTrigger.All,
        InternalWebhookTrigger.UserUpdated,
        InternalWebhookTrigger.UserDeleted,
        InternalWebhookTrigger.FileCreated,
        InternalWebhookTrigger.FileUploaded,
        InternalWebhookTrigger.FileUpdated,
        InternalWebhookTrigger.FileTrashed,
        InternalWebhookTrigger.FileDeleted,
        InternalWebhookTrigger.FileRestored,
        InternalWebhookTrigger.FileCopied,
        InternalWebhookTrigger.FileMoved,
        InternalWebhookTrigger.FolderCreated,
        InternalWebhookTrigger.FolderUpdated,
        InternalWebhookTrigger.FolderTrashed,
        InternalWebhookTrigger.FolderDeleted,
        InternalWebhookTrigger.FolderRestored,
        InternalWebhookTrigger.FolderCopied,
        InternalWebhookTrigger.FolderMoved,
        InternalWebhookTrigger.RoomUpdated,
        InternalWebhookTrigger.RoomArchived,
        InternalWebhookTrigger.RoomDeleted,
        InternalWebhookTrigger.RoomRestored,
        InternalWebhookTrigger.FormSubmit,
        InternalWebhookTrigger.FormFilledOut,
        InternalWebhookTrigger.FormStopped,
        InternalWebhookTrigger.AgentUpdated,
        InternalWebhookTrigger.AgentDeleted,
    ];

    [Theory]
    [MemberData(nameof(AdminOnlyTriggers))]
    public void IsAvailableFor_AdminOnlyTrigger_DocSpaceAdmin_ReturnsTrue(InternalWebhookTrigger trigger)
        => trigger.IsAvailableFor(InternalEmployeeType.DocSpaceAdmin).Should().BeTrue();

    [Theory]
    [MemberData(nameof(AdminOnlyTriggers))]
    public void IsAvailableFor_AdminOnlyTrigger_RoomAdmin_ReturnsTrue(InternalWebhookTrigger trigger)
        => trigger.IsAvailableFor(InternalEmployeeType.RoomAdmin).Should().BeTrue();

    [Theory]
    [MemberData(nameof(AdminOnlyTriggers))]
    public void IsAvailableFor_AdminOnlyTrigger_User_ReturnsFalse(InternalWebhookTrigger trigger)
        => trigger.IsAvailableFor(InternalEmployeeType.User).Should().BeFalse();

    [Theory]
    [MemberData(nameof(AllRolesTriggers))]
    public void IsAvailableFor_AllRolesTrigger_DocSpaceAdmin_ReturnsTrue(InternalWebhookTrigger trigger)
        => trigger.IsAvailableFor(InternalEmployeeType.DocSpaceAdmin).Should().BeTrue();

    [Theory]
    [MemberData(nameof(AllRolesTriggers))]
    public void IsAvailableFor_AllRolesTrigger_RoomAdmin_ReturnsTrue(InternalWebhookTrigger trigger)
        => trigger.IsAvailableFor(InternalEmployeeType.RoomAdmin).Should().BeTrue();

    [Theory]
    [MemberData(nameof(AllRolesTriggers))]
    public void IsAvailableFor_AllRolesTrigger_User_ReturnsTrue(InternalWebhookTrigger trigger)
        => trigger.IsAvailableFor(InternalEmployeeType.User).Should().BeTrue();

    [Fact]
    public void AllTriggers_HaveAvailableForAttribute()
    {
        var type = typeof(InternalWebhookTrigger);

        foreach (var value in Enum.GetValues<InternalWebhookTrigger>())
        {
            var field = type.GetField(value.ToString());
            var attr = Attribute.GetCustomAttribute(field!, typeof(AvailableForAttribute));
            attr.Should().NotBeNull($"WebhookTrigger.{value} is missing [AvailableFor] attribute");
        }
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "73741")]
    public async Task GetTriggers_CheckUserRestrictions()
    {
        await _peopleClient.Authenticate(Initializer.Owner);

        var user = await Initializer.InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);

        var triggers = await _webhooksApi.GetWebhookTriggersAsync(TestContext.Current.CancellationToken);

        triggers.Should().NotBeNull();
        triggers.Response.Should().NotBeNull();

        //var trigger = triggers.Response.FirstOrDefault(x => x.Id == (long)WebhookTrigger.UserCreated);
        //trigger.Should().NotBeNull();
        //trigger!.Available.Should().Be(false);

        var createWebhooksConfigRequestsDto = new CreateWebhooksConfigRequestsDto(
            "test",
            "https://onlyoffice.com",
            "test123!@#%ABC",
            true,
            true,
            WebhookTrigger.UserCreated
            );

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _webhooksApi.CreateWebhookAsync(
                createWebhooksConfigRequestsDto,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80980")]
    public async Task WebhookCreation_CheckBlacklist()
    {
        await _peopleClient.Authenticate(Initializer.Owner);

        var createWebhooksConfigRequestsDto = new CreateWebhooksConfigRequestsDto(
            "test",
            "http://localhost",
            "test123!@#%ABC",
            true);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _webhooksApi.CreateWebhookAsync(
                createWebhooksConfigRequestsDto,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }
}
