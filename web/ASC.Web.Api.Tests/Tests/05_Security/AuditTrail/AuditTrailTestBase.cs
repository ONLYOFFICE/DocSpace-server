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
/// Shared polling helpers for the audit trail event endpoints: an audit event is written after the
/// request that triggered it has already returned, so a bare read right after that action races
/// with the write.
/// </summary>
public abstract class AuditTrailTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Generates an audit event that is guaranteed to reach the database in this environment.
    /// Ordinary audit actions (member invited, settings changed, ...) are published to the event
    /// bus and persisted by Web.Studio's consumer — a service this suite does not start — so they
    /// never appear here. A password reminder is on MessagesRepository's force-save list
    /// (UserSentPasswordChangeInstructions, 4015) and is written synchronously by the People
    /// service itself, which is why the TS suite's room-creation trigger (RoomCreated, also
    /// force-saved, but needing the Files service) is replaced with it.
    /// </summary>
    protected async Task TriggerAuditEventAsync(User target)
    {
        var passwordApi = new DocSpace.API.SDK.Api.People.PasswordApi(
            _peopleClient,
            new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') });

        await passwordApi.SendUserPasswordAsync(new EmailMemberRequestDto(target.Email), TestContext.Current.CancellationToken);
    }

    protected async Task<List<AuditEventDto>> PollAuditEventsByFilterAsync(Func<List<AuditEventDto>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var events = new List<AuditEventDto>();

        while (true)
        {
            events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(cancellationToken: TestContext.Current.CancellationToken)).Response ?? [];

            if (until(events) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    protected async Task<List<AuditEventDto>> PollLastAuditEventsAsync(Func<List<AuditEventDto>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var events = new List<AuditEventDto>();

        while (true)
        {
            events = (await _auditTrailDataApi.GetLastAuditEventsAsync(TestContext.Current.CancellationToken)).Response ?? [];

            if (until(events) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }
}
