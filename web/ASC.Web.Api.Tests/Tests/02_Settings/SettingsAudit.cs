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

namespace ASC.Web.Api.Tests.Tests._02_Settings;

/// <summary>
/// Settings changes are ordinary audit actions: they are published to the event bus and written by Web.Studio's
/// consumer, which the stack starts, so they appear after the request has returned and have to be polled. Shared
/// by the settings and security folders, which is why it sits one level up.
/// </summary>
internal static class SettingsAudit
{
    /// <summary>The audit events carrying <paramref name="action"/>, polled until one shows up or 30 s pass.</summary>
    public static async Task<List<AuditEventDto>> PollAsync(AuditTrailDataApi auditApi, MessageAction action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = ((await auditApi.GetAuditEventsByFilterAsync(cancellationToken: TestContext.Current.CancellationToken)).Response ?? [])
                .Where(e => e.ActionId == action)
                .ToList();

            if (events.Count > 0 || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// One settings row of the calling portal as stored (<c>webstudio_settings</c>): the <c>Data</c> per user id,
    /// <see cref="Guid.Empty"/> for the portal-wide row. MySQL only, like the other raw reads of this suite.
    /// </summary>
    public static async Task<Dictionary<Guid, string>> ReadSettingsRowsAsync(MySqlConnector.MySqlConnection connection, int tenantId, Guid settingsId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserID, Data FROM webstudio_settings WHERE TenantID = @tenant AND LOWER(ID) = @id";
        command.Parameters.AddWithValue("@tenant", tenantId);
        command.Parameters.AddWithValue("@id", settingsId.ToString().ToLowerInvariant());

        var rows = new Dictionary<Guid, string>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            rows[Guid.Parse(reader.GetString(0))] = reader.GetString(1);
        }

        return rows;
    }
}
