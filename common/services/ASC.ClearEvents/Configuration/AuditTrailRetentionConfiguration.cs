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

namespace ASC.ClearEvents.Configuration;

public class AuditTrailRetentionConfiguration
{
    public const string SectionName = "clearEvents:auditTrail";

    /// <summary>
    /// The shortest life time a pass accepts: the longest window the audit trail can be set to show
    /// (<see cref="TenantAuditSettings.MaxLifeTime"/>), so that no setting deletes events a portal still reports.
    /// </summary>
    public const int MinLifeTimeDays = TenantAuditSettings.MaxLifeTime;

    /// <summary>
    /// The longest life time a pass accepts, a century. Far beyond any retention policy, and far enough from the
    /// smallest <see cref="DateTime"/> that subtracting it from today cannot fail.
    /// </summary>
    public const int MaxLifeTimeDays = 100 * 365;

    /// <summary>The largest batch a pass accepts: its ids travel to the database as one IN list.</summary>
    public const int MaxBatchSize = 10_000;

    /// <summary>The shortest period between passes: a pass walks every portal with old events.</summary>
    public static readonly TimeSpan MinPeriod = TimeSpan.FromHours(1);

    /// <summary>The longest period between passes: the longest delay <see cref="Task.Delay(TimeSpan)"/> accepts.</summary>
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    public bool Enabled { get; init; }

    public int PaidLifeTimeDays { get; init; } = 3 * 365;

    public int FreeLifeTimeDays { get; init; } = 365;

    public TimeSpan Period { get; init; } = TimeSpan.FromDays(1);

    public int BatchSize { get; init; } = 1000;

    /// <summary>
    /// Why the section could not be read, such as a period written as <c>1d</c>; null when it was. The other values
    /// are then the defaults, and no pass runs.
    /// </summary>
    public string BindingError { get; private init; }

    public bool IsPeriodUsable => Period >= MinPeriod && Period <= MaxPeriod;

    /// <summary>
    /// Whether a pass may run with these settings. A life time of a few days would delete almost the whole audit
    /// trail of every portal, a free life time above the paid one would keep the events of free portals longer, and
    /// values past the upper bounds would fail every pass or run them back to back.
    /// </summary>
    public bool IsUsable =>
        BindingError is null &&
        FreeLifeTimeDays >= MinLifeTimeDays &&
        PaidLifeTimeDays >= FreeLifeTimeDays &&
        PaidLifeTimeDays <= MaxLifeTimeDays &&
        BatchSize is > 0 and <= MaxBatchSize &&
        IsPeriodUsable;

    /// <summary>
    /// Reads the <see cref="SectionName"/> section. A value that does not convert does not throw: it would stop the
    /// service, and the whole monolith with it, even with the cleanup disabled. The settings come back unusable
    /// instead, with <see cref="BindingError"/> set and <see cref="Enabled"/> read on its own, so that an enabled
    /// cleanup reports the error on every pass rather than going quiet. An <c>enabled</c> that is set to anything but
    /// <c>false</c> counts as enabled, the unreadable ones too (<c>1</c>, <c>yes</c>, <c>on</c>): read as disabled,
    /// such a value would leave the cleanup unregistered without a word in the log, and the trail growing unbounded.
    /// No pass removes anything while the error is there.
    /// </summary>
    public static AuditTrailRetentionConfiguration Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        try
        {
            return section.Get<AuditTrailRetentionConfiguration>() ?? new();
        }
        catch (InvalidOperationException ex)
        {
            return new AuditTrailRetentionConfiguration
            {
                Enabled = !string.IsNullOrWhiteSpace(section[nameof(Enabled)]) && !(bool.TryParse(section[nameof(Enabled)], out var enabled) && !enabled),
                BindingError = ex.InnerException?.Message is { } inner ? $"{ex.Message} {inner}" : ex.Message
            };
        }
    }
}
