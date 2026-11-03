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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// The composer's rules, checked from the server's side: archives are refused at attach time (by name and by bytes), a
/// message carries at most five files and five images, a file name cannot traverse anywhere, and a draft is not bound
/// to the thread it was staged for.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentComposerRuleTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    /// <summary>Exactly the 5 files + 5 images the client allows on one message.</summary>
    private const int AtLimitAttachments = 10;

    private static readonly string[] _archiveTitles =
    [
        "autotest.zip", "autotest.rar", "autotest.7z", "autotest.tar", "autotest.gz", "autotest.tgz", "autotest.tar.gz"
    ];

    public static TheoryData<string> ArchiveTitles { get; } = [.. _archiveTitles];

    [Theory]
    [Trait("Bug", "82893")]
    [MemberData(nameof(ArchiveTitles))]
    public async Task SaveFile_OrdinaryBytesUnderAnArchiveName_AreRefused(string title)
    {
        // Filed as BUG 82893 and withdrawn while an inline shape still existed. There is none left and the answer
        // inverted: attaching goes through a stored file, and TWO independent checks refuse an archive — the name (an
        // archive extension is refused even when the bytes are ordinary text) and the bytes (below).
        var fileId = await BackingFileIdAsync(Owner, title, "not really an archive");

        var refused = await AttachByIdAsync(fileId);

        refused.Status.Should().Be(400, $"ordinary bytes named {title}");
    }

    [Fact]
    [Trait("Bug", "82893")]
    public async Task SaveFile_ZipMagicBytesUnderATxtName_AreRefused()
    {
        // Built from char codes rather than written inline: a raw 0x03 0x04 in the source is invisible in a diff.
        var zipMagic = $"PK{(char)3}{(char)4} and then some text";
        const string plain = "not really an archive";

        // Control first, so a refusal cannot be blamed on the store: the same ordinary bytes under a .txt name attach
        // and come back whole.
        var controlId = await SaveDraftAsync(Owner, "autotest.txt", plain);
        (await ExpectStoredAsync(controlId, "the .txt control")).Content.Should().Be(plain);

        var disguised = await BackingFileIdAsync(Owner, "autotest-zip.txt", zipMagic);

        var sniffed = await AttachByIdAsync(disguised);

        sniffed.Status.Should().Be(400, "ZIP magic bytes under a .txt name");
    }

    [Fact]
    public async Task AppendUserMessage_MessageAtTheCap_CarriesAllFiveFilesAndAllFiveImages()
    {
        // The contract half of the cap, and the control the over-limit tests need: ten is stored verbatim and in order,
        // so the list is not truncated, reordered or split by kind on the way in.
        var (_, profile, threadId) = await ArrangeChatAsync();
        var drafts = await StageComposerDraftsAsync(files: 5, images: 5, tag: "at-cap");
        drafts.Should().HaveCount(AtLimitAttachments);
        var attachments = AsMessageAttachments(drafts);

        var status = await AppendWithAttachmentsAsync(threadId, profile.Id, "Autotest attachments at the cap", attachments);

        status.Should().Be(HttpStatusCode.OK);
        ExpectSameJson(await StoredUserAttachmentsAsync(threadId), attachments, "all ten, in order");
    }

    [Fact]
    [Trait("Bug", "82894")]
    public async Task AppendUserMessage_MoreAttachmentsThanTheClientAllows_IsNotStoredWhole()
    {
        // The 5-files-and-5-images cap is a property of the composer. One request puts eleven real drafts on a single
        // message and nothing objects: six files and five images, over the cap on both halves and in total.
        var (_, profile, threadId) = await ArrangeChatAsync();
        var drafts = await StageComposerDraftsAsync(files: 6, images: 5, tag: "over-limit");

        var status = await AppendWithAttachmentsAsync(threadId, profile.Id, "Autotest over-limit attachments", AsMessageAttachments(drafts));

        // A refusal is as good as a trimmed list; what must not happen is an eleven-attachment message in the store.
        var stored = await StoredUserAttachmentsAsync(threadId);
        var storedCount = stored is { ValueKind: JsonValueKind.Array } list ? list.GetArrayLength() : 0;
        storedCount.Should().BeLessThanOrEqualTo(
            AtLimitAttachments,
            $"a message must not carry more than the five files and five images the composer allows (the route answered {(int)status})");
    }

    [Fact]
    [Trait("Bug", "82894")]
    public async Task SendWithStream_MoreAttachmentsThanTheClientAllows_IsNotStoredWhole()
    {
        // append-user-message stores a message without inference, a route the composer never calls. Sending is
        // send-with-stream, and the cap has to be checked there before "the server does not enforce it" can be said
        // about the product: a limit rejected on the real path and missing on the storage-only one would be a different
        // finding altogether.
        var (provider, profile, threadId) = await ArrangeChatAsync();
        var drafts = await StageComposerDraftsAsync(files: 6, images: 5, tag: "over-limit-send");

        var round = await SendWithAttachmentsAsync(threadId, profile.Id, "Reply with OK and nothing else.", AsMessageAttachments(drafts));

        var stored = await StoredUserAttachmentsAsync(threadId);
        var storedCount = stored is { ValueKind: JsonValueKind.Array } list ? list.GetArrayLength() : 0;
        storedCount.Should().BeLessThanOrEqualTo(
            AtLimitAttachments,
            $"a sent message must not carry more than the five files and five images the composer allows (the route answered {(int)round.StatusCode})\n{provider.Describe()}");
    }

    [Theory]
    [InlineData("../../../etc/passwd.txt", ".._.._.._etc_passwd.txt")]
    [InlineData("..\\..\\windows\\win.ini.txt", ".._.._windows_win.ini.txt")]
    [InlineData("/etc/shadow.txt", "_etc_shadow.txt")]
    [InlineData("C:\\Users\\autotest\\secret.txt", "C__Users_autotest_secret.txt")]
    [InlineData("отчёт 2026.txt", "отчёт 2026.txt")]
    public async Task SaveFile_TraversalShapedFileName_CannotReachTheDraft(string name, string sanitised)
    {
        // A traversal-shaped name has nothing to traverse: DocSpace sanitises it on upload — separators become "_" —
        // and the draft takes its title from the stored file rather than from the body, so a `title` a client sends
        // cannot put one back. `path` on the draft is "<fileId>/<sanitised name>", a file id and not a filesystem
        // location. Non-ASCII is not a traversal risk and is left alone. Every name ends in a real extension: a stored
        // file whose name has none cannot be attached at all, which would hide what this test is about.
        var fileId = await BackingFileIdAsync(Owner, name, "autotest");

        // The traversal string is sent as the title too and ignored.
        var saved = await AttachByIdAsync(fileId, name);

        saved.Status.Should().Be(200, $"name {name}: {saved.Error}");
        var stored = await ExpectStoredAsync(saved.Data!.Id, name);
        stored.Title.Should().Be(sanitised, $"title for {name}");
        stored.Path.Should().Be($"{fileId}/{sanitised}", $"path for {name}");
    }

    [Fact]
    public async Task AppendUserMessage_DraftStagedForOneThread_StaysReadableAndCanStillBeSentFromAnother()
    {
        // "Switching to another thread drops the unsent attachments" is the composer forgetting a list of ids: a draft is
        // not bound to a thread, no route lists drafts, and the switch itself has nothing to observe. What IS observable
        // is that nothing on the server participates — the two halves below are what the client's forgetting is hiding.
        var profile = await GetDefaultProfileAsync();
        var abandoned = await CreateThreadAsync("Autotest thread the attachments were staged in", profile.Id);
        var other = await CreateThreadAsync("Autotest thread switched to", profile.Id);
        var drafts = await StageComposerDraftsAsync(files: 1, images: 1, tag: "switch");
        var attachments = AsMessageAttachments(drafts);

        foreach (var draft in drafts)
        {
            await ExpectStoredAsync(draft.Id, "staged draft");
        }

        var status = await AppendWithAttachmentsAsync(other.ThreadId, profile.Id, "Autotest message in a new thread", attachments);
        status.Should().Be(HttpStatusCode.OK);

        // First half: the drafts were accepted by a thread they were not staged for.
        ExpectSameJson(await StoredUserAttachmentsAsync(other.ThreadId), attachments, "the drafts moved to the other thread");
        // The thread they were staged in stayed empty.
        (await ReadStoredMessagesAsync(abandoned.ThreadId)).Should().BeEmpty();

        // Second half: sending them elsewhere consumed nothing either; a client that kept the ids could send them again.
        foreach (var draft in drafts)
        {
            await ExpectStoredAsync(draft.Id, "draft after the switch");
        }
    }
}
