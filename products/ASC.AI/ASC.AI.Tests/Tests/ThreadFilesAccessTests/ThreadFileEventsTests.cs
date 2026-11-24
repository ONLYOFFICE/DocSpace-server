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

namespace ASC.AI.Tests.Tests.ThreadFilesAccessTests;

[Trait("Category", "Socket")]
[Trait("Feature", "AI/ThreadFiles")]
public class ThreadFileEventsTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private const string ModifyThreadFiles = "s:modify-thread-files";

    private static readonly TimeSpan _noDeliveryWindow = TimeSpan.FromSeconds(2);

    public enum Watcher
    {
        ProjectOwner,
        PortalAdmin,
        Member
    }

    [Fact]
    public async Task UploadArtifact_ToANewThread_ReachesTheAuthorInTheProjectRoom()
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);
        await using var authorSocket = await WatchAsync(author, projectId);

        await _aiClient.Authenticate(author);
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var artifact = await UploadArtifactAsync(thread.Id, "report.txt");
        await _aiClient.Authenticate(Owner);

        await authorSocket.WaitForAsync(ModifyThreadFiles, Created(artifact.Id, thread.Id, "artifacts"), cancellationToken: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(Watcher.ProjectOwner)]
    [InlineData(Watcher.PortalAdmin)]
    [InlineData(Watcher.Member)]
    public async Task UploadArtifact_DoesNotReachAnotherUser(Watcher watcher)
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);
        var other = watcher switch
        {
            Watcher.ProjectOwner => Owner,
            Watcher.PortalAdmin => await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
            Watcher.Member => await InviteMemberAsync(projectId),
            _ => throw new ArgumentOutOfRangeException(nameof(watcher))
        };
        await using var authorSocket = await WatchAsync(author, projectId);
        await using var otherSocket = await WatchAsync(other, projectId);

        await _aiClient.Authenticate(author);
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var artifact = await UploadArtifactAsync(thread.Id, "report.txt");
        await _aiClient.Authenticate(Owner);

        await authorSocket.WaitForAsync(ModifyThreadFiles, Created(artifact.Id, thread.Id, "artifacts"), cancellationToken: TestContext.Current.CancellationToken);
        (await otherSocket.ArrivesWithinAsync(ModifyThreadFiles, Created(artifact.Id, thread.Id, "artifacts"), _noDeliveryWindow, TestContext.Current.CancellationToken))
            .Should().BeFalse("the files of a thread are visible to its author only");
    }

    [Fact]
    public async Task SendAttachment_ReachesTheAuthorAsAnAttachment()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        await using var authorSocket = await WatchAsync(Owner, projectId);

        var file = await SendAsync(projectId, thread.Id, "notes.txt");

        await authorSocket.WaitForAsync(ModifyThreadFiles, Created(file.Id, thread.Id, "attachments"), cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<PortalSocket> WatchAsync(User user, int folderId)
    {
        var socket = await ConnectSocket(user);

        await socket.SubscribeAsync(true, TestContext.Current.CancellationToken, $"DIR-{folderId}");

        return socket;
    }

    private static Func<JsonElement, bool> Created(int fileId, Guid threadId, string kind)
    {
        return payload => payload.GetProperty("cmd").GetString() == "create"
                          && payload.GetProperty("kind").GetString() == kind
                          && payload.GetProperty("threadId").GetGuid() == threadId
                          && payload.GetProperty("id") is { ValueKind: JsonValueKind.Number } id
                          && id.GetInt32() == fileId;
    }
}
