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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// Shared setup for the privacy-room suites. Deliberately does not inherit
/// <c>RoomsPermissionsTestBase</c> (namespace <c>ASC.Files.Tests.Tests._03_Rooms</c>): the two small
/// helpers this feature needs (<see cref="InviteMember"/>, <see cref="InviteToRoom"/>) are lifted here
/// instead, keeping this feature folder self-contained per the "one feature, one folder" rule.
/// </summary>
public abstract class PrivacyRoomTestBase(AspireAppFixture fixture) : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Sets a fresh fake encryption key pair for the currently authenticated user, mirroring the
    /// TypeScript suite's fake <c>pk-...</c> / <c>prv-...</c> helper. Real cryptography for this
    /// feature lives only in <c>PrivacyRoomTest.CRUD_UserPrivateKey</c> and the encrypt-upload flow
    /// next to it; every other test only needs a key to exist and be shaped like one.
    /// </summary>
    protected async Task<EncryptionKeyDto> SetFakeKeys(Guid id = default, string publicKeyPrefix = "pk")
    {
        var publicKey = $"{publicKeyPrefix}-{Guid.NewGuid():N}";
        var privateKey = $"prv-{Guid.NewGuid():N}";

        var sw = Stopwatch.StartNew();
        var keys = (await _privacyRoomApi.SetKeysAsync(
            new EncryptionKeyRequestDto(id, publicKey, privateKey),
            TestContext.Current.CancellationToken)).Response;
        Timing.Write($"setKeys({id})", sw.ElapsedMilliseconds);

        return keys.Single(k => k.Id == id);
    }

    /// <summary>
    /// A target that answers every request with 200, for a subscription whose journal is read from the database: the
    /// subscription is saved only when its address answers the HEAD probe. It listens on the loopback alias the
    /// integration-test blacklist leaves open (<c>Constants.WebhookReceiverHost</c>), on a port of its own, so it
    /// does not collide with the receiver of the Web.Api suite when both suites run at once.
    /// </summary>
    private static readonly Lazy<WireMock.Server.WireMockServer> _webhookTarget = new(() =>
    {
        var server = WireMock.Server.WireMockServer.Start(new WireMock.Settings.WireMockServerSettings
        {
            Urls = [$"http://{ASC.AppHost.Configuration.Constants.WebhookReceiverHost}:18091"]
        });

        server.Given(WireMock.RequestBuilders.Request.Create().UsingAnyMethod())
            .RespondWith(WireMock.ResponseBuilders.Response.Create().WithStatusCode(200).WithBody("ok"));

        return server;
    });

    /// <summary>Subscribes the portal to the triggers as <paramref name="user"/> (the owner by default) and returns the subscription's id.</summary>
    protected async Task<int> SubscribeToWebhook(WebhookTrigger triggers, User? user = null)
    {
        await _webApiClient.Authenticate(user ?? Owner);

        var config = await _webhooksApi.CreateWebhookAsync(
            new CreateWebhooksConfigRequestDto(
                name: $"webhook-{Guid.NewGuid():N}"[..20],
                uri: $"{_webhookTarget.Value.Url}/hooks/{Guid.NewGuid():N}",
                // the secret passes the password rules of the portal: 8 to 30 Latin letters
                secretKey: "Secret" + new string([.. Guid.NewGuid().ToString("N").Select(c => (char)('a' + (c % 26)))])[..20],
                enabled: true,
                ssl: false,
                triggers: triggers),
            TestContext.Current.CancellationToken);

        return config.Response.Id;
    }

    /// <summary>The token a document server wraps its callback in. The secret is the one the stack gives the editors and the services.</summary>
    /// <summary>The audit records of this portal with the action whose description names the id.</summary>
    protected async Task<long> AuditRecords(int action, Guid mentioned)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM audit_events WHERE tenant_id = @tenant AND action = @action AND description LIKE @mentioned;";
        command.Parameters.AddWithValue("@tenant", TenantId);
        command.Parameters.AddWithValue("@action", action);
        command.Parameters.AddWithValue("@mentioned", $"%{mentioned}%");

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
    }

    protected static string SignAsDocumentServer(string payload)
    {
        static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var content = $"{Encode(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""))}.{Encode(Encoding.UTF8.GetBytes(payload))}";

        return $"{content}.{Encode(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes(content)))}";
    }

    /// <summary>
    /// Polls <paramref name="probe"/> on a deadline until <paramref name="until"/> is satisfied, for
    /// state that is written asynchronously (key deletion propagating to room-access checks). Never
    /// throws on timeout — it returns the last observed value so the caller's own assertion message
    /// stays readable.
    /// </summary>
    protected static async Task<T> PollUntil<T>(Func<Task<T>> probe, Func<T, bool> until, TimeSpan? timeout = null)
    {
        var sw = Stopwatch.StartNew();
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        var last = await probe();
        var satisfied = until(last);

        while (!satisfied && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500, TestContext.Current.CancellationToken);
            last = await probe();
            satisfied = until(last);
        }

        Timing.Write($"pollUntil(satisfied: {satisfied})", sw.ElapsedMilliseconds);

        return last;
    }

    protected async Task<User> InviteWithKeys(EmployeeType type)
    {
        var user = await InviteContact(type);
        await _filesClient.Authenticate(user);
        await SetFakeKeys(publicKeyPrefix: "user");

        return user;
    }

    protected async Task<List<Guid>> WriteKeys(int fileId, params Guid[] userIds)
    {
        List<AccessRequestKeyDto> keys = [.. userIds.Select(id => new AccessRequestKeyDto(id, Guid.Empty, "wrapped"))];

        return (await _filesApi.SetEncryptionInfoAsync(fileId, keys, TestContext.Current.CancellationToken)).Response.SkippedUserIds;
    }

    protected async Task<List<RoomMissingFileKeysDto>> GetMissing(int roomId, params Guid[] userIds)
    {
        return (await _privacyRoomApi.GetRoomMissingFileKeysAsync(roomId, [.. userIds], TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>Uploads a file the way a private-room client does: chunked, with the encrypted flag. The bytes do not matter here.</summary>
    protected async Task<int> UploadEncryptedFile(int folderId, string title)
    {
        var content = new byte[1024];

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(Guid.NewGuid() + Path.GetExtension(title), content.Length, encrypted: true),
            TestContext.Current.CancellationToken)).Response;

        await _filesOperationsApi.UploadAsyncSessionAsync(folderId, session.Id, 1, new FileParameter(new MemoryStream(content)), TestContext.Current.CancellationToken);

        var file = (await _filesOperationsApi.FinalizeSessionAsync(folderId, session.Id, TestContext.Current.CancellationToken)).Response;

        return file.Id;
    }

    protected async Task<FileDto> UploadEncryptedFileInfo(int folderId, string title)
    {
        var fileId = await UploadEncryptedFile(folderId, title);

        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>Turns a file into one stored before private rooms refused open content: nothing can create it through the API any more.</summary>
    protected async Task MakeFileOpen(int fileId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "UPDATE files_file SET encrypted = 0 WHERE id = @id;";
        command.Parameters.AddWithValue("@id", fileId);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Writes the user's <c>e2e</c> row exactly as the single-pair release did. Must run before
    /// anything reads that user's keys: the services keep linked profiles in a per-node memory cache
    /// for ten minutes, and a row written behind their back is not seen until it expires.
    /// </summary>
    protected async Task PlantLegacyKeys(Guid userId, string publicKey, string privateKeyEnc)
    {
        // Property order and casing as JsonSerializer.Serialize(EncryptionKeyPairDto) produced them.
        var legacyJson = JsonSerializer.Serialize(new { PrivateKeyEnc = privateKeyEnc, PublicKey = publicKey, UserId = userId });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var encrypted = await new InstanceCrypto(new MachinePseudoKeys(configuration)).EncryptAsync(legacyJson);

        // LoginProfile.ToString(): Id, provider (APr) and name (N), joined by '→' and '·'.
        await PlantAccountLink(userId, "e2e", HashHelper.MD5($"e2e/{userId}"), $"Id→{userId}·APr→e2e·N→{encrypted}");
    }

    /// <summary>Writes one row of <c>account_links</c>; the caching caveat of <see cref="PlantLegacyKeys"/> applies.</summary>
    protected async Task PlantAccountLink(Guid userId, string provider, string uid, string profile)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO account_links (id, uid, provider, profile, linked)
            VALUES (@id, @uid, @provider, @profile, UTC_TIMESTAMP());
            """;

        command.Parameters.AddWithValue("@id", userId.ToString());
        command.Parameters.AddWithValue("@uid", uid);
        command.Parameters.AddWithValue("@provider", provider);
        command.Parameters.AddWithValue("@profile", profile);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Sends a raw request to a privacyroom endpoint with a body or path a typed DTO cannot express
    /// (a malformed, non-GUID key id; an empty id collapsing the route).
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawPrivacyRoomRequest(HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (json != null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
