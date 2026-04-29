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

namespace ASC.Files.Tests.Tests._08_AI;

[Collection("Test Collection")]
[Trait("Feature", "AI")]
public class AIProvidersTest(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateProvider_ReturnsOk()
    {
        if (_ollamaHttpClient?.BaseAddress == null || string.IsNullOrEmpty(_ollamaModel))
        {
            Assert.Skip("Ollama is not running.");
        }

        await _aiHttpClient.Authenticate(Initializer.Owner);

        var createProviderRequestDto = new CreateProviderRequestDto(
            ProviderType.OpenAiCompatible,
            "ollama",
            _ollamaHttpClient.BaseAddress.AbsoluteUri,
            "random",
            [new ModelSettingsItemDto(_ollamaModel, true)]);
        var result = await _providersApi.AddProviderAsync(createProviderRequestDto, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
        result.Response.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAgent_ReturnsOk()
    {
        if (_ollamaHttpClient?.BaseAddress == null || string.IsNullOrEmpty(_ollamaModel))
        {
            Assert.Skip("Ollama is not running.");
        }

        await _aiHttpClient.Authenticate(Initializer.Owner);

        var createProviderRequestDto = new CreateProviderRequestDto(
            ProviderType.OpenAiCompatible,
            "ollama",
            _ollamaHttpClient.BaseAddress.AbsoluteUri,
            "random",
            [new ModelSettingsItemDto(_ollamaModel, true)]);

        var provider = (await _providersApi.AddProviderAsync(createProviderRequestDto, TestContext.Current.CancellationToken)).Response;
        var agent = await _agentsApi.CreateAgentAsync(new CreateAgentRequestDto("agent", chatSettings: new ChatSettings(provider.Id, _ollamaModel)), TestContext.Current.CancellationToken);

        agent.Response.Should().NotBeNull();
        agent.Response.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAgent_AskAIAboutForm_ReturnsOk()
    {
        if (_ollamaHttpClient?.BaseAddress == null || string.IsNullOrEmpty(_ollamaModel))
        {
            Assert.Skip("Ollama is not running.");
        }

        await _aiHttpClient.Authenticate(Initializer.Owner);
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);

        var createProviderRequestDto = new CreateProviderRequestDto(
            ProviderType.OpenAiCompatible,
            "ollama",
            _ollamaHttpClient.BaseAddress.AbsoluteUri,
            "random",
            [new ModelSettingsItemDto(_ollamaModel, true)]);

        var provider = (await _providersApi.AddProviderAsync(createProviderRequestDto, TestContext.Current.CancellationToken)).Response;
        var agent = await _agentsApi.CreateAgentAsync(new CreateAgentRequestDto("agent", chatSettings: new ChatSettings(provider.Id, _ollamaModel)), TestContext.Current.CancellationToken);

        const string testFormDb = "test-form";

        var csb = new MySqlConnectionStringBuilder(_dbConnectionString);

        var serverCsb = new MySqlConnectionStringBuilder
        {
            Server = csb.Server,
            Port = csb.Port,
            UserID = csb.UserID,
            Password = csb.Password
        };

        await using (var serverConnection = new MySqlConnection(serverCsb.ConnectionString))
        {
            await serverConnection.OpenAsync(TestContext.Current.CancellationToken);
            await using var cmd = serverConnection.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{testFormDb}`;";
            await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await _authorizationApi.SaveAuthKeysAsync(new AuthServiceRequestsDto("externaldb", props:
        [
            new AuthKey("databaseType", "mysql"),
            new AuthKey("dbHost", csb.Server),
            new AuthKey("dbPort", csb.Port.ToString()),
            new AuthKey("dbName", testFormDb),
            new AuthKey("dbUser", csb.UserID),
            new AuthKey("dbPassword", csb.Password),
            new AuthKey("dbSsl", "false")
        ]), TestContext.Current.CancellationToken);

        var formRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("form room", roomType: RoomType.FillingFormsRoom), TestContext.Current.CancellationToken)).Response;
        var updated = (await _roomsApi.UpdateRoomAsync(formRoom.Id, new UpdateRoomRequest(sendFormToExternalDB: true, saveFormAsXLSX: true), TestContext.Current.CancellationToken)).Response;

        updated.Should().NotBeNull();
        updated.Id.Should().BeGreaterThan(0);
        updated.SendFormToExternalDB.Should().BeTrue();
        updated.SaveFormAsXLSX.Should().BeTrue();

        // Generate the leave-application form PDF via OnlyOffice DocumentBuilder
        const string formScript = "leave_application_form.docbuilder";
        const string formFileName = "leave_application_form.pdf";

        var formPdfBytes = await _runDocBuilderAsync(formScript, formFileName, TestContext.Current.CancellationToken);
        formPdfBytes.Should().NotBeNullOrEmpty();

        // Upload the generated PDF form to the form room
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var uploadSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            updated.Id,
            new SessionRequest(formFileName, formPdfBytes.LongLength),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var chunkNumber = 1;
        var offset = 0;

        while (offset < formPdfBytes.Length)
        {
            var size = Math.Min(chunkSize, formPdfBytes.Length - offset);
            using var chunkStream = new MemoryStream(formPdfBytes, offset, size);

            await _filesOperationsApi.UploadAsyncSessionAsync(
                updated.Id,
                uploadSession.Id,
                chunkNumber,
                new FileParameter(chunkStream),
                TestContext.Current.CancellationToken);

            offset += size;
            chunkNumber++;
        }

        var uploadedFile = (await _filesOperationsApi.FinalizeSessionAsync(
            updated.Id,
            uploadSession.Id,
            TestContext.Current.CancellationToken)).Response;

        uploadedFile.Should().NotBeNull();
        uploadedFile.Uploaded.Should().BeTrue();
        uploadedFile.File.Should().NotBeNull();
        uploadedFile.File.Title.Should().Be(formFileName);

        var formFileId = uploadedFile.File.Id;

        // Invite several test users and grant them FillForms access to the form room.
        const int fillerCount = 3;
        var fillers = new List<User>(fillerCount);
        for (var i = 0; i < fillerCount; i++)
        {
            fillers.Add(await Initializer.InviteContact(EmployeeType.User));
        }

        await _filesClient.Authenticate(Initializer.Owner);

        await _roomsApi.SetRoomSecurityAsync(updated.Id, new RoomInvitationRequest
        {
            Invitations = fillers
                .Select(u => new RoomInvitation { Id = u.Id, Access = FileShare.FillForms })
                .ToList()
        }, TestContext.Current.CancellationToken);

        // Map every invited user to a form role so they are allowed to fill the form.
        var formRoles = fillers
            .Select((u, idx) => new FormRole
            {
                UserId = u.Id,
                RoomId = updated.Id,
                RoleName = $"Filler {idx + 1}",
                RoleColor = "ffefbf",
                Sequence = idx + 1
            })
            .ToList();

        await _filesApi.SaveFormRoleMappingAsync(
            formFileId.ToString(),
            new SaveFormRoleMappingDtoInteger(formFileId, formRoles),
            TestContext.Current.CancellationToken);

        // Owner enables filling on the form.
        await _filesApi.StartFillingFileAsync(formFileId, TestContext.Current.CancellationToken);

        const string filledFormScript = "filled_leave_application_form.docbuilder";
        const string filledFormResultName = "filled_leave_application_form.pdf";

        var submissions = new (User User, string FirstName, string LastName, string StartDate, string EndDate, string Destination)[]
        {
            (fillers[0], "John",  "Doe",    "01/05/2026", "10/05/2026", "Riga, Latvia"),
            (fillers[1], "Anna",  "Smith",  "15/06/2026", "29/06/2026", "Lisbon, Portugal"),
            (fillers[2], "Peter", "Ivanov", "03/08/2026", "17/08/2026", "Tbilisi, Georgia")
        };

        foreach (var s in submissions)
        {
            // Authenticate as filler and let DocSpace create the user's draft (mimics opening the form in the editor).
            await _filesClient.Authenticate(s.User);


            // var draftEditorUrl = (await _filesApi.CheckFillFormDraftAsync(
            //     formFileId,
            //     new CheckFillFormDraft(version: 1, action: "edit"),
            //     TestContext.Current.CancellationToken)).Response;
            //
            // draftEditorUrl.Should().NotBeNullOrEmpty();
            //
            // var draftFileId = ExtractFileIdFromEditorUrl(draftEditorUrl);

            var draftFileId = (await _filesApi.OpenEditFileAsync(formFileId, cancellationToken: TestContext.Current.CancellationToken)).Response.File.Id;

            // Generate a per-user filled PDF.
            var argumentJson =
                $"{{\"first_name\":\"{s.FirstName}\"," +
                $"\"last_name\":\"{s.LastName}\"," +
                $"\"start_date\":\"{s.StartDate}\"," +
                $"\"end_date\":\"{s.EndDate}\"," +
                $"\"vacation_destination\":\"{s.Destination}\"}}";

            var filledPdfBytes = await _runDocBuilderWithArgAsync(
                filledFormScript,
                filledFormResultName,
                argumentJson,
                TestContext.Current.CancellationToken);

            filledPdfBytes.Should().NotBeNullOrEmpty();

            // Simulate the docservice "force-save" callback: serve the filled PDF on a temporary URL the
            // DocSpace process can reach, and ask DocSpace to replace the draft content with it.
            // DocSpace runs as a .NET project on the host, so reach it via localhost.
            var savedFile = await _fixture.ServeBytesOverHttpAsync(
                filledPdfBytes,
                $"submission_{s.LastName.ToLowerInvariant()}.pdf",
                "application/pdf",
                "localhost",
                async downloadUri => (await _filesApi.SaveEditingFileFromFormAsync(
                    draftFileId,
                    downloadUri: downloadUri,
                    fileExtension: "pdf",
                    forcesave: true,
                    cancellationToken: TestContext.Current.CancellationToken)).Response,
                TestContext.Current.CancellationToken);

            savedFile.Should().NotBeNull();
            savedFile.Id.Should().Be(draftFileId);
        }

        await _filesClient.Authenticate(Initializer.Owner);
    }

    private static int ExtractFileIdFromEditorUrl(string editorUrl)
    {
        // Editor URL ends with something like "/products/files/doceditor?fileId=42#message/..."
        // We just need the fileId query parameter.
        var hashIdx = editorUrl.IndexOf('#');
        var trimmed = hashIdx >= 0 ? editorUrl[..hashIdx] : editorUrl;

        var queryIdx = trimmed.IndexOf('?');
        if (queryIdx < 0)
        {
            throw new InvalidOperationException($"Editor URL has no query string: {editorUrl}");
        }

        var query = HttpUtility.ParseQueryString(trimmed[(queryIdx + 1)..]);
        var fileId = query["fileId"];
        if (string.IsNullOrEmpty(fileId) || !int.TryParse(fileId, out var id))
        {
            throw new InvalidOperationException($"Could not extract fileId from editor URL: {editorUrl}");
        }

        return id;
    }
}
