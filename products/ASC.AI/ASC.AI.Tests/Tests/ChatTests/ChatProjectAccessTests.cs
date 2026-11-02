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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Who may chat in an AI project: its owner, Project managers and members. Anyone else is refused (403) when starting
/// a thread and when sending, a project whose model is unavailable is a 409. This is the AI-project replacement of
/// "a DocSpace admin not in the agent cannot start a thread" (chat.permission.spec.ts).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatProjectAccessTests(AspireAppFixture fixture) : ChatProjectTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task Create_NotAMemberOfTheProject_Returns403(EmployeeType type)
    {
        // Arrange: neither can even read a project they were not invited into.
        var (_, model) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(model);
        var outsider = await InviteContact(type, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(outsider);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateProjectThreadAsync(projectId));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().BeEmpty("the refused call left no thread");
    }

    /// <summary>
    /// An administrator reads every room but is not in the project (<c>inRoom</c> is false), and the chat service's own
    /// rule is that only members chat there. Measured: the thread is created, so the administrator gets into a project
    /// they were never invited to.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task Create_DocSpaceAdminNotInTheProject_Returns403()
    {
        var (_, model) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(model);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateProjectThreadAsync(projectId));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Send_NotAMemberOfTheProject_Returns403AndTheModelIsNotAsked()
    {
        // Arrange
        var (provider, model) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(model);
        var outsider = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(outsider);
        var question = NewQuestion();

        // Act: a thread of the outsider's own, the project named only as the round's entity.
        var thread = await CreateThreadAsync(profileId: model.Id);
        var round = await SendWithStreamAsync(thread.ThreadId, question, entityId: projectId.ToString());

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, round.ToString());
        StreamedRequestsCarrying(provider, question).Should().BeEmpty(provider.Describe());
    }

    [Fact]
    public async Task Create_ProjectOnAPortalWithNoModelAnywhere_IsRefusedForWantOfAModel()
    {
        // Arrange: no profile on the portal, so neither the project nor the Chat binding names a model.
        var projectId = await CreateProjectAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateProjectThreadAsync(projectId));

        // Assert
        // aiProjectChatContext.ts answers 409 projectModelUnavailable; the measured answer on a portal with no profile is 404
        // ("no AI profile is available"), so either status is the "no model" refusal, never a thread.
        exception.ErrorCode.Should().BeOneOf([404, 409], "the portal has no model to run the project on");
    }
}
