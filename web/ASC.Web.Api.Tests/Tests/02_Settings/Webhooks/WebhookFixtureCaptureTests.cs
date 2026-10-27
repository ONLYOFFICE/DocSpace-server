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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// Writes the signed delivery fixtures of the webhook SDKs (<c>Fixtures/</c> next to this file) from what this server
/// really sends: one delivery of every event family the suite can drive, and one automatic retry. The SDK
/// verification of the <c>generate-webhook-sdk</c> skill replays them through every language's sample, so the
/// SDKs are checked against the current wire format instead of a hand-made snapshot.
/// </summary>
/// <remarks>
/// Explicit: it rewrites files in the repository, so it runs only when asked for, for instance
/// <c>dotnet test web/ASC.Web.Api.Tests/ASC.Web.Api.Tests.csproj -- --filter-trait "Category=WebhookFixtures" --explicit only</c>.
/// The subscriptions are signed with the secret of <c>samples/SECRET</c>, set with a MySQL statement because the
/// password rules may refuse it through the API. Bodies and signature headers are written byte for byte as the
/// receiver got them. <c>unknown.trigger.json</c> is not a real delivery and is left alone; form.submit cannot be
/// captured, since the integration-test profile runs no Document Server.
/// </remarks>
[Trait("Category", "WebhookFixtures")]
public class WebhookFixtureCaptureTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    private static readonly string[] _capturedTriggers =
    [
        "user.created", "user.invited", "user.updated", "user.deleted",
        "group.created", "group.updated", "group.deleted",
        "room.created", "folder.created", "file.created", "agent.created"
    ];

    [Fact(Explicit = true)]
    public async Task CaptureSdkFixtures()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var samples = FindSamplesDirectory();
        var secret = (await File.ReadAllTextAsync(Path.Combine(samples, "SECRET"), cancellationToken)).Trim();

        // a catch-all subscription for one delivery of every family, and a file.created one whose receiver accepts the
        // first file, fails the second once and accepts its retry, so the retry carries lastSuccessOn as well
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        var retryReceiver = await WebhookReceiver.CreateSequenceAsync(_webhookReceiverClient, 200, 500, 200);
        var retrySubscription = await CreateSubscriptionAsync(retryReceiver, WebhookTrigger.FileCreated);

        foreach (var id in new[] { subscription.Id, retrySubscription.Id })
        {
            await SetSecretAsync(id, secret);
        }

        var targetReceiver = await FireEveryFamilyAsync(secret);

        var captured = await WaitForTriggersAsync(receiver);

        _capturedTriggers.Where(t => !captured.ContainsKey(t)).Should().BeEmpty("every family the suite drives should have been delivered");

        // two files are created and their deliveries may arrive in either order; the fixture is always the first file
        captured["file.created"] = (await receiver.GetDeliveriesAsync())
            .Should().Contain(d => TriggerOf(d.Body) == "file.created" && d.Body.Contains("\"title\":\"Report.docx\""), "the first file was created").Which;

        // user.updated comes from the subscription scoped to that member, so the fixture carries webhook.target; of
        // the rename and the termination it takes the rename, which shows an active member
        captured["user.updated"] = (await targetReceiver.WaitForDeliveriesAsync(2))
            .Should().Contain(d => StatusOf(d.Body) == (int)EmployeeStatus.Active, "the member renamed themselves while active").Which;

        var retries = await retryReceiver.WaitForDeliveriesAsync(3);
        var retry = retries.Should().Contain(d => RetryCountOf(d.Body) == 1, "the receiver failed one attempt").Which;

        using (var retryBody = JsonDocument.Parse(retry.Body))
        {
            retryBody.RootElement.GetProperty("webhook").TryGetProperty("lastSuccessOn", out _)
                .Should().BeTrue("the first file was delivered before the retry");
        }

        foreach (var (trigger, delivery) in captured)
        {
            await WriteFixtureAsync(samples, trigger, delivery, secret);
        }

        await WriteFixtureAsync(samples, "file.created.retry", retry, secret);
    }

    private async Task SetSecretAsync(int subscriptionId, string secret)
    {
        await ExecuteSqlAsync("UPDATE webhooks_config SET secret_key = @secret WHERE id = @id", ("@secret", secret), ("@id", subscriptionId));
    }

    /// <summary>
    /// One event of every family, as the owner, in the order a portal would see them. Returns the receiver of a
    /// user.updated subscription scoped to the member the events are about.
    /// </summary>
    private async Task<WebhookReceiver> FireEveryFamilyAsync(string secret)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // user.created
        var member = await InviteContact(EmployeeType.User);

        await _webApiClient.Authenticate(Owner);
        var targetReceiver = await CreateReceiverAsync();
        var targeted = WebhooksTestData.CreateWebhookDto(enabled: true, triggers: WebhookTrigger.UserUpdated);
        targeted.Uri = targetReceiver.TargetUri;
        targeted.TargetId = member.Id.ToString();
        await SetSecretAsync((await _webhooksApi.CreateWebhookAsync(targeted, cancellationToken)).Response.Id, secret);

        // user.updated: only the member can rename themselves
        await _peopleClient.Authenticate(member);
        await _profilesApi.UpdateMemberAsync(
            member.Id.ToString(), new UpdateMemberRequestDto { FirstName = "Jane", LastName = "Doe" }, cancellationToken);

        // user.invited
        await _peopleClient.Authenticate(Owner);
        await _profilesApi.InviteUsersAsync(
            new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = Initializer.FakerMember.Generate().Email }]),
            cancellationToken);

        // group.created, group.updated
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: "Marketing", groupManager: Owner.Id, members: [member.Id]), cancellationToken)).Response;
        await _groupApi.UpdateGroupAsync(group.Id, new UpdateGroupRequest(groupManager: Owner.Id, groupName: "Marketing Team"), cancellationToken);

        // room.created, folder.created, file.created, agent.created
        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: "Projects", roomType: RoomType.CustomRoom), cancellationToken)).Response;
        await _foldersApi.CreateFolderAsync(room.Id, new CreateFolderRequest("Drafts"), cancellationToken);
        await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest("Report.docx"), cancellationToken);
        await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest("Notes.docx"), cancellationToken);
        await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(title: "Assistant", roomType: RoomType.AiRoom), cancellationToken);

        // group.deleted, user.deleted
        await _peopleClient.Authenticate(Owner);
        await _groupApi.DeleteGroupAsync(group.Id, cancellationToken);
        await TerminateAsync(member);
        await _profilesApi.DeleteMemberAsync(member.Id.ToString(), cancellationToken);

        return targetReceiver;
    }

    /// <summary>
    /// Polls the receiver until it holds a delivery of every captured trigger, and returns the first delivery of each
    /// trigger it got, by <c>event.trigger</c>.
    /// </summary>
    private static async Task<Dictionary<string, ReceivedDelivery>> WaitForTriggersAsync(WebhookReceiver receiver)
    {
        var deadline = DateTime.UtcNow.AddSeconds(120);

        while (true)
        {
            var byTrigger = new Dictionary<string, ReceivedDelivery>();

            foreach (var delivery in await receiver.GetDeliveriesAsync())
            {
                byTrigger.TryAdd(TriggerOf(delivery.Body), delivery);
            }

            if (_capturedTriggers.All(byTrigger.ContainsKey) || DateTime.UtcNow >= deadline)
            {
                return byTrigger.Where(p => _capturedTriggers.Contains(p.Key)).ToDictionary();
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    private static async Task WriteFixtureAsync(string samples, string name, ReceivedDelivery delivery, string secret)
    {
        var signature = delivery.Header("x-docspace-signature-256");

        signature.Should().BeEquivalentTo($"sha256={Sign(secret, delivery.Body)}", "the fixture of {0} must verify with samples/SECRET", name);

        var utf8 = new UTF8Encoding(false);

        await File.WriteAllTextAsync(Path.Combine(samples, $"{name}.json"), delivery.Body, utf8, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(samples, $"{name}.sig"), signature, utf8, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The <c>Fixtures</c> directory next to this file, in the repository the suite was built from: the suite that
    /// captures the deliveries keeps them, and the SDKs read them from here.
    /// </summary>
    private static string FindSamplesDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var samples = Path.Combine(directory.FullName, "web", "ASC.Web.Api.Tests", "Tests", "02_Settings", "Webhooks", "Fixtures");

            if (File.Exists(Path.Combine(samples, "SECRET")))
            {
                return samples;
            }
        }

        throw new DirectoryNotFoundException($"No web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures/SECRET above {AppContext.BaseDirectory}.");
    }

    private static string TriggerOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("event").GetProperty("trigger").GetString()!;
    }

    private static int StatusOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("payload").GetProperty("status").GetInt32();
    }

    private static int RetryCountOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("webhook").TryGetProperty("retryCount", out var retryCount) ? retryCount.GetInt32() : 0;
    }
}
