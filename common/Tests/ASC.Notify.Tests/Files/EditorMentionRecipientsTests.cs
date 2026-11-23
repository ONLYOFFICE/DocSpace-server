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


namespace ASC.Notify.Tests.Files;

/// <summary>
/// Who the Files notify client sends an editor mention to, on the letter host's real services and a real entry of
/// the stack's portal. The mention endpoint first checks the author's own rights on the entry and then hands the same
/// instance to the client, which checks every mentioned account on it. A check of one account used to answer from the
/// share record the previous check had left on the instance, so every mentioned account was let through on the
/// author's record.
/// </summary>
[Collection(nameof(NotifyQueueCollection))]
public class EditorMentionRecipientsTests
{
    [Fact]
    public async Task EditorMention_ReachesOnlyTheMentionedAccountsThatMayReadTheEntry()
    {
        using var scope = await OpenScopeAsync();

        // Arrange: an entry in the owner's My documents, shared for reading with the author of the mention and with
        // one more account; a third account has no access to it
        var author = await CreateMemberAsync(scope);
        var reader = await CreateMemberAsync(scope);
        var outsider = await CreateMemberAsync(scope);

        var entryId = await CreateSharedEntryAsync(scope, author.Id, reader.Id);

        await scope.Services.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(scope.Tenant.Id, author.Id);

        // the instance the endpoint works on, after its check of the author's rights
        var entry = await scope.Services.GetRequiredService<IDaoFactory>().GetFolderDao<int>().GetFolderAsync(entryId);
        (await scope.Services.GetRequiredService<FileSecurity>().CanReadAsync(entry)).Should().BeTrue("the author may read the entry");

        // Act
        await Client(scope).SendEditorMentions(entry, $"{scope.PortalUrl}/doc", [reader.Id, outsider.Id], "Please have a look");

        // Assert
        (await DeliveredAsync(scope, entry.UniqID)).Select(r => r.Recipient.ID)
            .Should().BeEquivalentTo([reader.Id.ToString()], "only the reader may read the entry");
    }

    private static async Task<int> CreateSharedEntryAsync(LetterScope scope, params Guid[] readers)
    {
        var services = scope.Services;
        var folderDao = services.GetRequiredService<IDaoFactory>().GetFolderDao<int>();

        var folder = services.GetRequiredService<Folder<int>>();
        folder.Title = $"Mentioned {Guid.NewGuid():N}";
        folder.ParentId = await services.GetRequiredService<GlobalFolderHelper>().FolderMyAsync;

        var folderId = await folderDao.SaveFolderAsync(folder);

        var fileSecurity = services.GetRequiredService<FileSecurity>();

        foreach (var reader in readers)
        {
            await fileSecurity.ShareAsync(folderId, FileEntryType.Folder, reader, FileShare.Read, SubjectType.User);
        }

        return folderId;
    }

    private static async Task<LetterScope> OpenScopeAsync()
    {
        var stack = await LetterStackFixture.CurrentAsync();

        return await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));
    }

    private static FilesNotifyClient Client(LetterScope scope)
    {
        return scope.Services.GetRequiredService<FilesNotifyClient>();
    }

    private static async Task<List<NotifyRequest>> DeliveredAsync(LetterScope scope, string objectId)
    {
        var delivered = new List<NotifyRequest>();

        foreach (var request in await NotifyQueue.TakeAsync(scope.Services, objectId))
        {
            if (await NotifyQueue.PassesEngineSubscriptionCheckAsync(scope.Services, request))
            {
                delivered.Add(request);
            }
        }

        return delivered;
    }

    private static async Task<UserInfo> CreateMemberAsync(LetterScope scope)
    {
        var tag = Guid.NewGuid().ToString("N")[..12];

        var user = new UserInfo
        {
            FirstName = "Mention",
            LastName = $"Member{tag}",
            UserName = $"mention.member.{tag}",
            Email = $"mention.member.{tag}@letters.test",
            Status = EmployeeStatus.Active,
            ActivationStatus = EmployeeActivationStatus.Activated
        };

        return await scope.Services.GetRequiredService<UserManager>().SaveUserInfo(user, EmployeeType.RoomAdmin, paidUserQuotaCheck: false);
    }
}
