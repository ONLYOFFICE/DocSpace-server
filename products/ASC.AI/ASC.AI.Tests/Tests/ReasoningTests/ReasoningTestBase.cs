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

namespace ASC.AI.Tests.Tests.ReasoningTests;

/// <summary>
/// Shared plumbing of the CSV attachment suites ported from ai-reasoning.spec.ts.
/// </summary>
/// <remarks>
/// The TypeScript suite is a model-quality battery: it runs 16 to 27 questions about payroll arithmetic against a real
/// model and checks the answers, once per way a user can put a file in front of an agent. A stub model cannot reason, so
/// that part has no portable counterpart. What the server owns, and what the battery silently depends on, is that the
/// whole file reaches the model intact; that is what these tests check, with a condensed copy of each CSV fixture
/// (the originals live in the TypeScript suite's assets) whose rows keep the traits the questions probe: a plain
/// payroll, an all-zero row, a negative net, a very large amount and a cent-level amount, and two pay periods per
/// employee. The "Knowledge upload" method needs vectorization and an embedding service and is not portable.
/// </remarks>
public abstract class ReasoningTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    // The File type discriminator of an attachment input (FileType.Document).
    private const int DocumentFileType = 7;

    /// <summary>A condensed employees_standard.csv.</summary>
    protected const string StandardCsv =
        "Employee_ID,First_Name,Last_Name,Department,Pay_Period,Gross_Pay,Total_Deductions,Net_Pay\n" +
        "E001,Olivia,Carter,Finance,2026-07,5830.00,1557.80,4272.20\n" +
        "E002,Liam,Brooks,Engineering,2026-07,7920.00,2254.20,5665.80\n" +
        "E003,Emma,Reed,Human Resources,2026-07,4850.00,1271.90,3578.10\n";

    /// <summary>A condensed employees_edge_cases.csv: zero-net, negative net pay, a very large amount, a cent-level amount.</summary>
    protected const string EdgeCaseCsv =
        "Employee_ID,First_Name,Last_Name,Department,Pay_Period,Gross_Pay,Total_Deductions,Net_Pay\n" +
        "X001,Zero,Row,Finance,2026-07,0.00,0.00,0.00\n" +
        "X002,Neg,Net,Sales,2026-07,1000.00,1200.00,-200.00\n" +
        "X003,Big,Amount,Engineering,2026-07,9999999.99,100.00,9999899.99\n" +
        "X004,Cent,Level,Support,2026-07,0.01,0.00,0.01\n";

    /// <summary>A condensed employees_monthly_payroll.csv: two pay periods per employee.</summary>
    protected const string MonthlyCsv =
        "Employee_ID,First_Name,Last_Name,Pay_Period,Gross_Pay,Net_Pay\n" +
        "E001,Olivia,Carter,2026-06,5700.00,4180.00\n" +
        "E001,Olivia,Carter,2026-07,5830.00,4272.20\n" +
        "E002,Liam,Brooks,2026-06,7800.00,5590.00\n" +
        "E002,Liam,Brooks,2026-07,7920.00,5665.80\n";

    /// <summary>
    /// Puts <paramref name="csv"/> in front of a chat the way the composer does and asks one question; returns every
    /// row of the file and what the attachment, the model's request and the round made of it.
    /// </summary>
    /// <param name="csv">The file content.</param>
    /// <param name="existingFile">
    /// False: "attach from device", the file is uploaded into My documents after the chat exists. True: "select an
    /// existing DocSpace file", the file already sits in an unrelated custom room.
    /// </param>
    protected async Task<CsvAttachResult> AttachAndAskAsync(string csv, bool existingFile)
    {
        await FilesHttpClient.Authenticate(Owner);

        var content = System.Text.Encoding.UTF8.GetBytes(csv);
        var file = new FileParameter($"autotest-payroll-{Guid.NewGuid():N}.csv", "text/csv", new MemoryStream(content));

        FileDto stored;
        if (existingFile)
        {
            var room = await RoomsApi.CreateRoomAsync(
                new CreateRoomRequestDto($"Autotest Payroll Storage {Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken);
            stored = (await FoldersApi.UploadFileAsync(room.Response.Id, file: file, cancellationToken: TestContext.Current.CancellationToken)).Response.Single();
        }
        else
        {
            stored = (await FoldersApi.UploadFileToMyAsync(file: file, cancellationToken: TestContext.Current.CancellationToken)).Response.Single();
        }

        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var roomId = (await CreateRoomAsync()).ToString();
        var thread = await CreateThreadAsync(profileId: profile.Id, entityId: roomId);

        // An attachment is a reference to a DocSpace file: the chat service reads it with the caller's credentials.
        var attachment = await _aiAttachments.AiAttachmentsSaveFileAsync(
            new AiAttachmentsSaveFileRequest(
                input: new AiAttachmentsSaveFileRequestInput(path: stored.Id.ToString(), content: string.Empty, type: DocumentFileType, title: stored.Title),
                entityId: roomId),
            TestContext.Current.CancellationToken);

        // The first message carries the attachment as a file block that references the draft.
        var draft = System.Text.Json.Nodes.JsonNode.Parse(attachment.ToJson())!.AsObject();
        var reference = new System.Text.Json.Nodes.JsonObject
        {
            ["ref"] = draft["id"]?.DeepClone(),
            ["title"] = draft["title"]?.DeepClone(),
            ["kind"] = draft["kind"]?.DeepClone(),
            ["path"] = draft["path"]?.DeepClone(),
            ["type"] = draft["type"]?.DeepClone()
        };

        var userMessage = new AiThreadMessageLike(
            role: AiThreadMessageLike.RoleEnum.User,
            content: new AiMessageContent(
            [
                new AiMessageContentBlock(type: "file", mimeType: reference.ToJsonString(), data: string.Empty),
                new AiMessageContentBlock(type: "text", text: "How many employees are in the attached CSV file?")
            ]));
        var body = new AiSendStreamBody(threadId: thread.ThreadId, userMessage: userMessage, entityId: roomId, profileId: profile.Id);

        provider.Enqueue(StubModelProvider.Text("Three."));
        // The response is raw for the same reason as BaseTest.SendWithStreamAsync: an ndjson stream the SDK declares as one frame.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/send-with-stream",
            body.ToJson(),
            TestContext.Current.CancellationToken);
        var round = ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var requests = provider.CompletionRequestBodies
            .Where(StubModelProvider.IsStreamed)
            .Where(b => !b.Contains("probe_echo", StringComparison.Ordinal))
            .ToList();

        return new CsvAttachResult(attachment.Content ?? string.Empty, round, requests, provider.Describe());
    }

    /// <summary>The data rows and the header of a CSV, one entry per line.</summary>
    protected static string[] RowsOf(string csv) => csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    protected sealed record CsvAttachResult(string AttachmentContent, ChatRound Round, List<string> Requests, string ProviderLog);
}
