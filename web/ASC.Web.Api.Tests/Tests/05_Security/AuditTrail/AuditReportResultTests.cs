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

namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// The audit trail and login history reports in CSV, built by the Files worker: the task completes
/// with a result file, and the download is recorded as an audit event of its own
/// (<c>AuditTrailReportDownloaded</c>, <c>LoginHistoryReportDownloaded</c>).
/// </summary>
[Trait("Category", "Security")]
public class AuditReportResultTests(
    AspireAppFixture fixture)
    : AuditTrailTestBase(fixture)
{
    [Fact]
    public async Task CreateAuditTrailReport_Csv_CompletesAndRecordsAuditTrailReportDownloaded()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _auditTrailDataApi.CreateAuditTrailReportAsync(format: AuditReportFormat.Csv, cancellationToken: TestContext.Current.CancellationToken);
        var task = await PollReportAsync(() => _auditTrailDataApi.GetAuditTrailReportAsync(TestContext.Current.CancellationToken));

        // Assert
        task.Should().NotBeNull();
        task!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report ended with: {task.Error}");
        task.ResultFileName.Should().EndWith(".csv");
        var events = await PollAuditEventsByFilterAsync(e => e.Any(x => x.ActionId == MessageAction.AuditTrailReportDownloaded));
        events.Should().Contain(e => e.ActionId == MessageAction.AuditTrailReportDownloaded && e.UserId == Owner.Id);
    }

    [Fact]
    public async Task CreateLoginHistoryReport_Csv_CompletesAndRecordsLoginHistoryReportDownloaded()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _loginHistoryApi.CreateLoginHistoryReportAsync(format: AuditReportFormat.Csv, cancellationToken: TestContext.Current.CancellationToken);
        var task = await PollReportAsync(() => _loginHistoryApi.GetLoginHistoryReportAsync(TestContext.Current.CancellationToken));

        // Assert
        task.Should().NotBeNull();
        task!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report ended with: {task.Error}");
        task.ResultFileName.Should().EndWith(".csv");
        var events = await PollAuditEventsByFilterAsync(e => e.Any(x => x.ActionId == MessageAction.LoginHistoryReportDownloaded));
        events.Should().Contain(e => e.ActionId == MessageAction.LoginHistoryReportDownloaded && e.UserId == Owner.Id);
    }

    private static async Task<DocumentBuilderTaskDto?> PollReportAsync(Func<Task<DocumentBuilderTaskWrapper>> read)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var task = (await read()).Response;

            if (task is { IsCompleted: true } || DateTime.UtcNow >= deadline)
            {
                return task;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }
}
